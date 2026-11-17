using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ActiveMotion3D.EndlessRunner.Core
{
    public sealed class StandCoachRuntime : MonoBehaviour
    {
        private const string DirectoryName = "StandCoachRuntime";
        internal const string MissingRuntime = "Stand Coach files are unavailable.";
        private static readonly string[] D4CVerdicts =
        {
            "What a stupid way to go.", "You folded under pressure.", "That was sloppy as hell.", "You made a wreck of it.",
            "Dumb move.", "You threw the run away.", "I've seen better warm-ups.", "What a lousy finish."
        };
        private static readonly string[] KillerQueenVerdicts =
        {
            "An illiterate reading of the course.", "How painfully pedestrian.", "Do try to retain some dignity.",
            "I've seen finer work by accident.", "Only an idiot would applaud that.",
            "An affront to finesse.", "Thoroughly unimpressive.", "Such a dreadful lack of composure."
        };
        private static readonly string[] D4CAdvice = { "Listen up.", "Get it together.", "Here's the fix.", "Focus.", "Try this." };
        private static readonly string[] KillerQueenAdvice = { "Observe.", "Do pay attention.", "A lesson, then.", "Kindly take note.", "Some precision, please." };

        private const int OnyxVoice = 17;
        private const int GeorgeVoice = 26;
        public static StandCoachRuntime Instance { get; private set; }
        public bool Available => speech != null && !speech.IsFaulted;

        private StandCoachServer server;
        private Task<StandCoachSpeech> speech;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private readonly HttpClient http = new HttpClient(new HttpClientHandler { UseProxy = false });
        private string url;
        private string apiKey;
        private readonly int[] verdictCursor = new int[2];
        private static readonly string[] VerdictPreferences = { "StandCoachD4CVerdict", "StandCoachKillerQueenVerdict" };
        private readonly Opening[] warmedOpenings = new Opening[2];

        private sealed class Opening
        {
            public string Text;
            public Task<StandCoachSpeech.Wave> Audio;
        }

        [Serializable] private sealed class Message
        {
            public string role;
            public string content;
        }

        [Serializable] private sealed class TipType
        {
            public string type = "string";
            public string[] @enum;
        }
        [Serializable] private sealed class Fields
        {
            public TipType tip = new TipType();
        }
        [Serializable] private sealed class Schema
        {
            public string type = "object";
            public Fields properties = new Fields();
            public string[] required = { "tip" };
            public bool additionalProperties = false;
        }
        [Serializable] private sealed class NamedSchema
        {
            public string name = "stand_coach";
            public bool strict = true;
            public Schema schema = new Schema();
        }
        [Serializable] private sealed class ResponseFormat
        {
            public string type = "json_schema";
            public NamedSchema json_schema = new NamedSchema();
        }
        [Serializable] private sealed class Request
        {
            public Message[] messages;
            public float temperature = 0f;
            public int max_tokens = 16;
            public ResponseFormat response_format = new ResponseFormat();
        }
        [Serializable] private sealed class Choice { public Message message; }
        [Serializable] private sealed class Completion { public Choice[] choices; }
        [Serializable] private sealed class Reply { public string tip; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Preload()
        {
            if (Instance != null) return;
            new GameObject("Stand Coach Runtime").AddComponent<StandCoachRuntime>();
        }

        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            for (int i = 0; i < verdictCursor.Length; i++)
                verdictCursor[i] = Mathf.Clamp(PlayerPrefs.GetInt(VerdictPreferences[i], 0), 0, 39);
            if (Application.platform != RuntimePlatform.WindowsPlayer && Application.platform != RuntimePlatform.WindowsEditor)
                return;

            string directory = Application.isEditor
                ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", DirectoryName))
                : Path.GetFullPath(Path.Combine(Application.streamingAssetsPath, DirectoryName));
            if (!HasRuntimeFiles(directory)) return;

            speech = Task.Run(() => new StandCoachSpeech(Path.Combine(directory, "speech")));
            try
            {
                var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                int port = ((IPEndPoint)listener.LocalEndpoint).Port;
                listener.Stop();
                url = $"http://127.0.0.1:{port}";
                apiKey = Guid.NewGuid().ToString("N");
                int threads = Math.Min(6, Math.Max(1, System.Environment.ProcessorCount / 2));
                string model = Path.Combine(directory, "models", "coach.gguf");
                string arguments = $"-m \"{model}\" --host 127.0.0.1 --port {port} --api-key {apiKey} " +
                    $"-c 1024 -np 1 -t {threads} -ngl 0 --no-webui --log-disable";
                server = new StandCoachServer(Path.Combine(directory, "llama", "llama-server.exe"), arguments);
            }
            catch (Exception error)
            {
                Debug.LogWarning($"Stand Coach could not start: {error.Message}");
            }
        }

        public static bool HasRuntimeFiles(string directory)
        {
            return File.Exists(Path.Combine(directory, "llama", "llama-server.exe")) &&
                File.Exists(Path.Combine(directory, "models", "coach.gguf")) &&
                File.Exists(Path.Combine(directory, "speech", "sherpa-onnx-c-api.dll")) &&
                File.Exists(Path.Combine(directory, "speech", "model.int8.onnx")) &&
                File.Exists(Path.Combine(directory, "speech", "voices.bin")) &&
                File.Exists(Path.Combine(directory, "speech", "tokens.txt")) &&
                Directory.Exists(Path.Combine(directory, "speech", "espeak-ng-data"));
        }

        public static bool IsKillerQueen => PlayerPrefs.GetInt("SelectedCharacter", 0) == 1;

        internal Task<StandCoachSpeech.Wave> Speak(string sentence, bool killerQueen, int lineIndex, CancellationToken cancellation)
        {
            int voice = killerQueen ? GeorgeVoice : OnyxVoice;
            float speed = lineIndex switch
            {
                0 => killerQueen ? 0.88f : 1.02f,
                1 => killerQueen ? 1.03f : 1.08f,
                _ => killerQueen ? 1.00f : 1.12f
            };
            return Synthesize(sentence, voice, speed, killerQueen, lineIndex, cancellation);
        }

        internal static float SpeakingPitch(bool killerQueen, int lineIndex) => lineIndex switch
        {
            0 => killerQueen ? 1.05f : 0.96f,
            1 => killerQueen ? 0.99f : 1.00f,
            _ => killerQueen ? 1.03f : 0.95f
        };

        private async Task<StandCoachSpeech.Wave> Synthesize(string sentence, int voice, float speed,
            bool killerQueen, int lineIndex, CancellationToken cancellation)
        {
            if (speech == null) throw new InvalidOperationException(MissingRuntime);
            StandCoachSpeech engine = await speech.ConfigureAwait(false);
            return await Task.Run(() =>
            {
                cancellation.ThrowIfCancellationRequested();
                lifetime.Token.ThrowIfCancellationRequested();
                var wave = engine.Synthesize(sentence, voice, speed);
                StandCoachSpeech.PreparePlayback(wave, killerQueen);
                return StandCoachSpeech.ShapeDelivery(wave, killerQueen, lineIndex);
            }, cancellation).ConfigureAwait(false);
        }

        internal void WarmOpening(bool killerQueen)
        {
            if (Available) _ = GetOpening(killerQueen);
        }

        private Opening GetOpening(bool killerQueen)
        {
            int stand = killerQueen ? 1 : 0;
            string[] verdicts = killerQueen ? KillerQueenVerdicts : D4CVerdicts;
            string text = verdicts[verdictCursor[stand] % verdicts.Length];
            Opening cached = warmedOpenings[stand];
            if (cached != null && cached.Text == text && !cached.Audio.IsFaulted && !cached.Audio.IsCanceled)
                return cached;

            var opening = new Opening { Text = text, Audio = Speak(text, killerQueen, 0, lifetime.Token) };
            warmedOpenings[stand] = opening;
            return opening;
        }

        internal Task<StandCoachSpeech.Wave> BeginReport(RunFeedback.Report run, bool killerQueen, out string[] lines, out string cue)
        {
            int stand = killerQueen ? 1 : 0;
            Opening opening = Available ? GetOpening(killerQueen) : null;
            string[] verdicts = killerQueen ? KillerQueenVerdicts : D4CVerdicts;
            string[] cues = killerQueen ? KillerQueenAdvice : D4CAdvice;
            int turn = verdictCursor[stand];
            verdictCursor[stand] = (turn + 1) % (verdicts.Length * cues.Length);
            PlayerPrefs.SetInt(VerdictPreferences[stand], verdictCursor[stand]);
            cue = cues[turn % cues.Length];
            lines = new[]
            {
                verdicts[turn % verdicts.Length],
                run.Overview,
                run.Diagnosis,
                cue + " " + run.Tips[0]
            };
            return opening?.Audio;
        }

        internal async Task<string> ChooseTip(RunFeedback.Report run, CancellationToken cancellation)
        {
            string fallback = run.Tips[0];
            if (server == null || !server.IsRunning) return fallback;

            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation, lifetime.Token);
            deadline.CancelAfter(TimeSpan.FromSeconds(4));
            var options = new string[run.Tips.Length];
            var prompt = new StringBuilder($"Controls: {run.Mode}.\nResult: {run.Overview}\nFinal encounter: {run.Diagnosis}");
            for (int i = 0; i < options.Length; i++)
            {
                options[i] = (i + 1).ToString();
                prompt.Append('\n').Append(options[i]).Append(". ").Append(run.Tips[i]);
            }

            var request = new Request
            {
                messages = new[]
                {
                    new Message
                    {
                        role = "system",
                        content = "Choose the single most useful practice tip for the next run based on the recorded result. " +
                            "Prefer advice that addresses the cause of the crash. Return only the tip number in the requested JSON."
                    },
                    new Message { role = "user", content = prompt.ToString() }
                }
            };
            request.response_format.json_schema.schema.properties.tip.@enum = options;

            try
            {
                using var message = new HttpRequestMessage(HttpMethod.Post, url + "/v1/chat/completions");
                message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                message.Content = new StringContent(JsonUtility.ToJson(request), Encoding.UTF8, "application/json");
                using HttpResponseMessage response = await http.SendAsync(message, deadline.Token).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return fallback;
                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var completion = JsonUtility.FromJson<Completion>(body);
                if (completion?.choices == null || completion.choices.Length == 0) return fallback;
                string content = completion.choices[0]?.message?.content;
                if (string.IsNullOrEmpty(content)) return fallback;
                int selected = Array.IndexOf(options, JsonUtility.FromJson<Reply>(content)?.tip);
                return selected >= 0 ? run.Tips[selected] : fallback;
            }
            catch (Exception error) when (error is OperationCanceledException || error is HttpRequestException ||
                error is ArgumentException || error is ObjectDisposedException)
            {
                return fallback;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            lifetime.Cancel();
            http.Dispose();
            server?.Dispose();
            if (speech != null)
                _ = speech.ContinueWith(task =>
                {
                    if (task.Status == TaskStatus.RanToCompletion) task.Result.Dispose();
                    else _ = task.Exception;
                }, TaskScheduler.Default);
        }
    }
}
