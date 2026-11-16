using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ActiveMotion3D.EndlessRunner.Core
{
    public sealed class StandCoachRuntime : MonoBehaviour
    {
        private const string DirectoryName = "StandCoachRuntime";
        internal const string MissingRuntime = "Stand Coach files are unavailable.";
        private const string StyleD4C = "You are D4C. Speak like a vicious rival. Use short jabs, contractions and plain words like botched, blew it, or ate the barrier. " +
            "For diagnosis say what the runner botched. For the tip use a clipped order with no courtesy.";
        private const string StyleKillerQueen = "You are Killer Queen. Speak like a disdainful aristocrat. Use polished diction such as squandered, composure, and precision. " +
            "For diagnosis pronounce a formal judgment. Phrase the tip as an exacting demand, using words like kindly, exercise, or demonstrate.";
        private static readonly string[] D4CVerdicts =
        {
            "What a stupid way to go", "Dumb move", "You blew that spectacularly", "That was sloppy as hell",
            "What a stupid finish", "That was dumb, even for you", "Clean up that mess", "You made a wreck of it"
        };
        private static readonly string[] KillerQueenVerdicts =
        {
            "A rather illiterate reading of the course", "Even an idiot could aim higher", "How painfully pedestrian",
            "Do try to retain some dignity", "An illiterate approach to timing", "An idiot would call that precision",
            "I have seen finer work by accident", "A disgrace to the very idea of finesse"
        };

        private static readonly int[] D4CVoices = { 14, 17 };
        private static readonly int[] KillerQueenVoices = { 26, 25 };
        private static readonly string[] D4CVoiceNames = { "Fenrir", "Onyx" };
        private static readonly string[] KillerQueenVoiceNames = { "George", "Fable" };
        private const string JsonString = "(\"(?:\\\\.|[^\"\\\\])*\")";
        private static readonly Regex OpeningLines = new Regex(
            "\"verdict\"\\s*:\\s*" + JsonString + "\\s*,\\s*\"overview\"\\s*:\\s*" + JsonString,
            RegexOptions.CultureInvariant);

        public static StandCoachRuntime Instance { get; private set; }
        public bool Available => server != null && server.IsRunning && speech != null;

        private StandCoachServer server;
        private Task<StandCoachSpeech> speech;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private readonly HttpClient http = new HttpClient(new HttpClientHandler { UseProxy = false });
        private string url;
        private string apiKey;
        private readonly int[] verdictCursor = new int[2];
        private readonly string[][] verdictOrder = new string[2][];

        [Serializable] private sealed class Message
        {
            public string role;
            public string content;
        }

        [Serializable] private sealed class StringType { public string type = "string"; }
        [Serializable] private sealed class VerdictType
        {
            public string type = "string";
            public string[] @enum;
        }
        [Serializable] private sealed class Fields
        {
            public VerdictType verdict = new VerdictType();
            public StringType overview = new StringType();
            public StringType diagnosis = new StringType();
            public StringType tip = new StringType();
        }
        [Serializable] private sealed class Schema
        {
            public string type = "object";
            public Fields properties = new Fields();
            public string[] required = { "verdict", "overview", "diagnosis", "tip" };
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
            public float temperature = 0.4f;
            public int max_tokens = 112;
            public bool stream = true;
            public ResponseFormat response_format = new ResponseFormat();
        }
        [Serializable] private sealed class Choice { public Message delta; }
        [Serializable] private sealed class Chunk { public Choice[] choices; }
        [Serializable] private sealed class Reply { public string verdict, overview, diagnosis, tip; }

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
            if (Application.platform != RuntimePlatform.WindowsPlayer && Application.platform != RuntimePlatform.WindowsEditor)
                return;

            string directory = Application.isEditor
                ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", DirectoryName))
                : Path.GetFullPath(Path.Combine(Application.streamingAssetsPath, DirectoryName));
            if (!HasRuntimeFiles(directory)) return;

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
                speech = Task.Run(() => new StandCoachSpeech(Path.Combine(directory, "speech")));
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

        public static string VoiceName(bool killerQueen) => (killerQueen ? KillerQueenVoiceNames : D4CVoiceNames)[VoiceChoice(killerQueen)];

        public static void CycleVoice(bool killerQueen)
        {
            PlayerPrefs.SetInt(VoicePreference(killerQueen), 1 - VoiceChoice(killerQueen));
            PlayerPrefs.Save();
        }

        private static int VoiceChoice(bool killerQueen) =>
            Mathf.Clamp(PlayerPrefs.GetInt(VoicePreference(killerQueen), killerQueen ? 0 : 1), 0, 1);

        private static string VoicePreference(bool killerQueen) =>
            killerQueen ? "StandCoachKillerQueenVoice" : "StandCoachD4CVoice";

        internal Task<StandCoachSpeech.Wave> Speak(string sentence, bool killerQueen, int lineIndex, CancellationToken cancellation)
        {
            int voice = (killerQueen ? KillerQueenVoices : D4CVoices)[VoiceChoice(killerQueen)];
            float speed = lineIndex switch
            {
                0 => killerQueen ? 0.88f : 1.02f,
                1 => killerQueen ? 0.94f : 0.99f,
                _ => killerQueen ? 0.90f : 1.07f
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

        internal IEnumerator Generate(string run, bool killerQueen, CancellationToken cancellation,
            Action<string> firstSentence, Action<string[]> completed, Action<string> failed)
        {
            if (!Available)
            {
                failed(MissingRuntime);
                yield break;
            }

            string style = killerQueen ? StyleKillerQueen : StyleD4C;
            string[] verdicts = { NextVerdict(killerQueen) };
            string[] cues = killerQueen
                ? new[] { "Kindly pay attention", "Do exercise some precision", "Permit me to be exact", "A word of instruction" }
                : new[] { "Get this through your skull", "Fix it", "Listen up", "Get your act together" };
            string adviceCue = cues[(verdictCursor[killerQueen ? 1 : 0] - 1) % cues.Length];
            var request = new Request
            {
                messages = new[]
                {
                    new Message
                    {
                        role = "system",
                        content = style + " Address the runner as you. " +
                            "Overview: give one short sentence about the measured result. " +
                            "Diagnosis: say what ended the run using the recorded facts. " +
                            "Tip: state the next action as a direct command. " +
                            "Keep it under 40 words. Use punctuation for emphasis and preserve tracking uncertainty."
                    },
                    new Message { role = "user", content = run }
                }
            };
            request.response_format.json_schema.schema.properties.verdict.@enum = verdicts;
            var opening = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            Task<string> response = RequestReport(JsonUtility.ToJson(request), verdicts, opening, cancellation);
            bool sentOpening = false;
            while (!response.IsCompleted)
            {
                if (!sentOpening && opening.Task.Status == TaskStatus.RanToCompletion)
                {
                    sentOpening = true;
                    firstSentence(opening.Task.Result);
                }
                yield return null;
            }
            if (response.Status == TaskStatus.RanToCompletion && TryReadReply(response.Result, verdicts, out string[] sentences))
            {
                sentences[2] = adviceCue + ". " + sentences[2];
                completed(sentences);
                yield break;
            }
            if (response.Exception != null) Debug.LogWarning(response.Exception.GetBaseException().Message);
            failed("Stand Coach couldn't finish this report.");
        }

        private string NextVerdict(bool killerQueen)
        {
            int index = killerQueen ? 1 : 0;
            string[] order = verdictOrder[index];
            if (order == null)
            {
                order = (string[])(killerQueen ? KillerQueenVerdicts : D4CVerdicts).Clone();
                for (int i = order.Length - 1; i > 0; i--)
                {
                    int other = UnityEngine.Random.Range(0, i + 1);
                    (order[i], order[other]) = (order[other], order[i]);
                }
                verdictOrder[index] = order;
            }

            return order[verdictCursor[index]++ % order.Length];
        }

        private async Task<string> RequestReport(string json, string[] verdicts, TaskCompletionSource<string> opening,
            CancellationToken cancellation)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation, lifetime.Token);
            deadline.CancelAfter(TimeSpan.FromSeconds(24));
            while (true)
            {
                try
                {
                    using HttpResponseMessage health = await http.GetAsync(url + "/health", deadline.Token).ConfigureAwait(false);
                    if (health.IsSuccessStatusCode) break;
                }
                catch (HttpRequestException)
                {
                    // The loopback listener may not have opened yet on an unusually short first run.
                }
                await Task.Delay(200, deadline.Token).ConfigureAwait(false);
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, url + "/v1/chat/completions");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
                deadline.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            using var abort = deadline.Token.Register(response.Dispose);
            using var reader = new StreamReader(await response.Content.ReadAsStreamAsync().ConfigureAwait(false));
            var reply = new StringBuilder();
            string line;
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                if (line == "data: [DONE]") break;
                if (!line.StartsWith("data: ", StringComparison.Ordinal)) continue;
                var chunk = JsonUtility.FromJson<Chunk>(line.Substring(6));
                if (chunk?.choices == null || chunk.choices.Length == 0) continue;
                string token = chunk.choices[0]?.delta?.content;
                if (string.IsNullOrEmpty(token)) continue;
                reply.Append(token);
                if (opening.Task.IsCompleted) continue;
                Match match = OpeningLines.Match(reply.ToString());
                if (!match.Success) continue;
                var partial = JsonUtility.FromJson<Reply>(
                    "{\"verdict\":" + match.Groups[1].Value + ",\"overview\":" + match.Groups[2].Value + "}");
                if (TryOpening(partial, verdicts, out string sentence)) opening.TrySetResult(sentence);
            }
            return reply.ToString();
        }

        private static bool TryReadReply(string json, string[] verdicts, out string[] sentences)
        {
            sentences = null;
            try
            {
                var reply = JsonUtility.FromJson<Reply>(json);
                if (reply == null || !TryOpening(reply, verdicts, out string opening) ||
                    !TryNormalize(reply.diagnosis, out string diagnosis) || !TryNormalize(reply.tip, out string tip))
                    return false;
                sentences = new[] { opening, diagnosis, tip };
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static bool TryOpening(Reply reply, string[] verdicts, out string sentence)
        {
            sentence = null;
            if (Array.IndexOf(verdicts, reply.verdict) < 0 || !TryNormalize(reply.overview, out string overview)) return false;
            sentence = reply.verdict + ". " + overview;
            return true;
        }

        private static bool TryNormalize(string value, out string sentence)
        {
            sentence = value?.Trim();
            if (string.IsNullOrWhiteSpace(sentence) || sentence.Length > 180) return false;
            if (!sentence.EndsWith(".") && !sentence.EndsWith("!") && !sentence.EndsWith("?")) sentence += ".";
            return true;
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
