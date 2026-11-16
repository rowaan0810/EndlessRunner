using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using ActiveMotion3D.EndlessRunner.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ActiveMotion3D.EndlessRunner.UI
{
    internal sealed class StandVoiceMenu : MonoBehaviour
    {
        private Text voiceName;
        private Text previewLabel;
        private Button preview;
        private AudioSource player;
        private Coroutine pending;
        private CancellationTokenSource cancellation;
        private bool killerQueen;

        public void Initialize(Button characterButton)
        {
            var anchor = characterButton.transform;
            var voiceChoice = CoachUi.Button(anchor, "Voice", new Vector2(-54, -52), new Vector2(105, 30), out voiceName);
            voiceChoice.onClick.AddListener(Cycle);
            preview = CoachUi.Button(anchor, "Preview voice", new Vector2(54, -52), new Vector2(105, 30), out previewLabel);
            preview.onClick.AddListener(Preview);
            player = gameObject.AddComponent<AudioSource>();
            player.playOnAwake = false;
            Refresh();
        }

        public void Refresh()
        {
            StopPreview();
            killerQueen = StandCoachRuntime.IsKillerQueen;
            voiceName.text = StandCoachRuntime.VoiceName(killerQueen) + "  ↻";
        }

        private void Cycle()
        {
            StandCoachRuntime.CycleVoice(killerQueen);
            Refresh();
        }

        private void Preview()
        {
            StopPreview();
            cancellation = new CancellationTokenSource();
            pending = StartCoroutine(PlayPreview(cancellation.Token));
        }

        private IEnumerator PlayPreview(CancellationToken token)
        {
            previewLabel.text = "Preparing…";
            preview.interactable = false;
            yield return null;
            var runtime = StandCoachRuntime.Instance;
            if (runtime == null || !runtime.Available)
            {
                previewLabel.text = "Unavailable";
                preview.interactable = true;
                yield break;
            }
            string example = killerQueen ? "An illiterate reading of the course. Kindly demonstrate some precision!"
                : "Dumb move. You saw the opening, and you wasted it!";
            var result = runtime.Speak(example, killerQueen, 0, token);
            yield return new WaitUntil(() => result.IsCompleted);
            if (result.Status == TaskStatus.RanToCompletion)
            {
                player.clip = CoachUi.Clip(result.Result);
                player.pitch = StandCoachRuntime.SpeakingPitch(killerQueen, 0);
                player.Play();
                previewLabel.text = "Preview voice";
            }
            else
            {
                if (result.Exception != null) Debug.LogWarning(result.Exception.GetBaseException().Message);
                previewLabel.text = "Unavailable";
            }
            preview.interactable = true;
            pending = null;
        }

        public void StopPreview()
        {
            if (pending != null) StopCoroutine(pending);
            pending = null;
            cancellation?.Cancel();
            cancellation?.Dispose();
            cancellation = null;
            if (player != null)
            {
                player.Stop();
                if (player.clip != null) Destroy(player.clip);
                player.clip = null;
            }
            if (previewLabel != null) previewLabel.text = "Preview voice";
            if (preview != null) preview.interactable = true;
        }

        private void OnDisable() => StopPreview();
    }
}
