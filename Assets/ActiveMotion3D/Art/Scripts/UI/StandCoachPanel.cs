using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ActiveMotion3D.EndlessRunner.Core;
using ActiveMotion3D.EndlessRunner.Player;
using UnityEngine;
using UnityEngine.UI;

namespace ActiveMotion3D.EndlessRunner.UI
{
    internal sealed class StandCoachPanel : MonoBehaviour
    {
        private const float FirstLineBudget = 5.5f;
        private RunResultsView view;
        private Text title, status, caption, captionToggle;
        private Button replay;
        private StandCoachPortrait portrait;
        private AudioSource voice;
        private AudioSource music;
        private float musicVolume;
        private bool musicDucked;
        private Coroutine work;
        private CancellationTokenSource cancellation;
        private readonly List<AudioClip> clips = new List<AudioClip>();
        private string[] lines;
        private string currentLine;
        private bool shown;
        private bool busy;
        private bool captions;
        private bool captionOnly;
        private bool killerQueen;

        public void Initialize(RunResultsView results)
        {
            view = results;
            title = view.standName;
            status = view.voiceStatus;
            caption = view.caption;
            captionToggle = view.captionButtonLabel;
            replay = view.replay;
            voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.ignoreListenerPause = true;
            music = GameObject.Find("Music")?.GetComponent<AudioSource>();

            replay.onClick.AddListener(Replay);
            view.captionButton.onClick.AddListener(ToggleCaptions);
            captions = PlayerPrefs.GetInt("StandCoachCaptions", 0) == 1;
            UpdateCaption();
        }

        public void Begin(RunFeedback.Report report, bool killerQueen)
        {
            ResetReport();
            this.killerQueen = killerQueen;
            title.text = killerQueen ? "Killer Queen" : "D4C";
            status.text = "Preparing feedback…";
            replay.interactable = false;
            cancellation = new CancellationTokenSource();
            busy = true;
            work = StartCoroutine(Generate(report, killerQueen, cancellation.Token));
        }

        public void Present(CharacterSelector selector)
        {
            shown = true;
            portrait?.Dispose();
            portrait = StandCoachPortrait.Create(view, selector, killerQueen);
        }

        private IEnumerator Generate(RunFeedback.Report report, bool killerQueen, CancellationToken token)
        {
            float firstLineDeadline = Time.realtimeSinceStartup + FirstLineBudget;
            var runtime = StandCoachRuntime.Instance;
            if (runtime == null)
            {
                status.text = StandCoachRuntime.MissingRuntime;
                busy = false;
                yield break;
            }

            Task<StandCoachSpeech.Wave> synthesis = runtime.BeginReport(report, killerQueen, out lines, out string adviceCue);
            Task<string> tip = runtime.ChooseTip(report, token);
            bool useVoice = synthesis != null;
            for (int i = 0; i < lines.Length; i++)
            {
                float deadline = i == 0 ? firstLineDeadline : Time.realtimeSinceStartup + 12f;
                while (useVoice && !synthesis.IsCompleted && Time.realtimeSinceStartup < deadline)
                    yield return null;
                AudioClip clip = null;
                if (useVoice && synthesis.Status == TaskStatus.RanToCompletion)
                    clip = CoachUi.Clip(synthesis.Result);
                else if (useVoice)
                {
                    if (synthesis.Exception != null) Debug.LogWarning(synthesis.Exception.GetBaseException().Message);
                    useVoice = !synthesis.IsFaulted && !synthesis.IsCanceled;
                }

                clips.Add(clip);
                int next = i + 1;
                if (next == lines.Length - 1 && tip.Status == TaskStatus.RanToCompletion)
                    lines[next] = adviceCue + " " + tip.Result;
                if (useVoice && next < lines.Length)
                    synthesis = runtime.Speak(lines[next], killerQueen, next, token);
                yield return new WaitUntil(() => shown);
                if (clip != null)
                {
                    var sfx = ActiveMotion3D.EndlessRunner.Core.SfxPlayer.Instance;
                    while (sfx != null && sfx.IsHitPlaying)
                        yield return null;
                }
                yield return PlayLine(clip, lines[i], i);
            }
            FinishPlayback();
        }

        private IEnumerator PlayLine(AudioClip clip, string line, int sentence)
        {
            currentLine = line;
            captionOnly = clip == null;
            UpdateCaption();
            if (captionOnly)
            {
                RestoreMusic();
                status.text = "Stand Coach";
                yield return new WaitForSecondsRealtime(Mathf.Clamp(line.Length / 19f, 1.6f, 5f));
                yield break;
            }

            status.text = "Speaking";
            voice.clip = clip;
            voice.pitch = StandCoachRuntime.SpeakingPitch(killerQueen, sentence);
            if (music != null && !musicDucked)
            {
                musicVolume = music.volume;
                music.volume = musicVolume * 0.13f;
                musicDucked = true;
            }
            voice.Play();
            yield return new WaitWhile(() => voice.isPlaying);
        }

        private void Replay()
        {
            if (busy || clips.Count == 0) return;
            busy = true;
            replay.interactable = false;
            work = StartCoroutine(ReplayLines());
        }

        private IEnumerator ReplayLines()
        {
            for (int i = 0; i < clips.Count; i++) yield return PlayLine(clips[i], lines[i], i);
            FinishPlayback();
        }

        private void FinishPlayback()
        {
            RestoreMusic();
            status.text = "";
            busy = false;
            replay.interactable = true;
            work = null;
        }

        private void ToggleCaptions()
        {
            captions = !captions;
            PlayerPrefs.SetInt("StandCoachCaptions", captions ? 1 : 0);
            PlayerPrefs.Save();
            UpdateCaption();
        }

        private void UpdateCaption()
        {
            captionToggle.text = captions ? "Captions on" : "Captions off";
            caption.text = captions || captionOnly ? currentLine : "";
        }

        private void LateUpdate()
        {
            if (shown && portrait != null)
            {
                bool speaking = voice.isPlaying;
                float progress = speaking && voice.clip != null ? voice.timeSamples / (float)voice.clip.samples : 0f;
                portrait.Animate(speaking, progress);
            }
        }

        public void ResetReport()
        {
            if (work != null) StopCoroutine(work);
            work = null;
            cancellation?.Cancel();
            cancellation?.Dispose();
            cancellation = null;
            RestoreMusic();
            if (voice != null)
            {
                voice.Stop();
                voice.clip = null;
            }
            foreach (AudioClip clip in clips)
                if (clip != null) Destroy(clip);
            clips.Clear();
            lines = null;
            currentLine = null;
            captionOnly = false;
            if (caption != null) caption.text = "";
            shown = false;
            busy = false;
            portrait?.Dispose();
            portrait = null;
        }

        private void RestoreMusic()
        {
            if (musicDucked && music != null) music.volume = musicVolume;
            musicDucked = false;
        }

        private void OnDisable() => ResetReport();
    }

    internal static class CoachUi
    {
        public static AudioClip Clip(StandCoachSpeech.Wave wave)
        {
            AudioClip clip = AudioClip.Create("Stand Coach", wave.Samples.Length, 1, wave.SampleRate, false);
            clip.SetData(wave.Samples, 0);
            return clip;
        }
    }
}
