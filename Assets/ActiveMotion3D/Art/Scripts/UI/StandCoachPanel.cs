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

        public void Begin(string report, bool killerQueen)
        {
            ResetReport();
            this.killerQueen = killerQueen;
            title.text = killerQueen ? "Killer Queen" : "D4C";
            status.text = "Preparing AI feedback…";
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

        private IEnumerator Generate(string report, bool killerQueen, CancellationToken token)
        {
            yield return null;
            var runtime = StandCoachRuntime.Instance;
            string error = StandCoachRuntime.MissingRuntime;
            Task<StandCoachSpeech.Wave> synthesis = null;
            if (runtime != null)
                yield return runtime.Generate(report, killerQueen, token,
                    sentence => synthesis = runtime.Speak(sentence, killerQueen, 0, token),
                    result => lines = result, message => error = message);
            if (lines == null)
            {
                status.text = error;
                busy = false;
                yield break;
            }

            synthesis ??= runtime.Speak(lines[0], killerQueen, 0, token);
            for (int i = 0; i < lines.Length; i++)
            {
                yield return new WaitUntil(() => synthesis.IsCompleted);
                if (synthesis.Status != TaskStatus.RanToCompletion)
                {
                    if (synthesis.Exception != null) Debug.LogWarning(synthesis.Exception.GetBaseException().Message);
                    status.text = "Voice playback is unavailable.";
                    if (captions) caption.text = lines[i];
                    RestoreMusic();
                    busy = false;
                    yield break;
                }

                AudioClip clip = CoachUi.Clip(synthesis.Result);
                clips.Add(clip);
                if (i + 1 < lines.Length) synthesis = runtime.Speak(lines[i + 1], killerQueen, i + 1, token);
                yield return new WaitUntil(() => shown);
                yield return PlayLine(clip, lines[i], i);
            }
            FinishPlayback();
        }

        private IEnumerator PlayLine(AudioClip clip, string line, int sentence)
        {
            currentLine = line;
            UpdateCaption();
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
            status.text = "Preparing AI feedback…";
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
            caption.text = captions ? currentLine : "";
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
            foreach (AudioClip clip in clips) Destroy(clip);
            clips.Clear();
            lines = null;
            currentLine = null;
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
        public static void Place(RectTransform rect, Transform parent, Vector2 position, Vector2 size)
        {
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        public static Text Label(Transform parent, string label, int fontSize, Vector2 position, Vector2 size)
        {
            var element = new GameObject(label.Length > 0 ? label : "Coach text", typeof(RectTransform), typeof(Text));
            Place((RectTransform)element.transform, parent, position, size);
            var text = element.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = label;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.86f, 0.90f, 1f);
            text.raycastTarget = false;
            text.supportRichText = false;
            return text;
        }

        public static Button Button(Transform parent, string label, Vector2 position, Vector2 size, out Text text)
        {
            var element = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            Place((RectTransform)element.transform, parent, position, size);
            var image = element.GetComponent<Image>();
            image.color = new Color(0.20f, 0.25f, 0.38f);
            var button = element.GetComponent<Button>();
            button.targetGraphic = image;
            text = Label(element.transform, label, 14, Vector2.zero, size);
            text.gameObject.name = "Label";
            return button;
        }

        public static AudioClip Clip(StandCoachSpeech.Wave wave)
        {
            AudioClip clip = AudioClip.Create("Stand Coach", wave.Samples.Length, 1, wave.SampleRate, false);
            clip.SetData(wave.Samples, 0);
            return clip;
        }
    }
}
