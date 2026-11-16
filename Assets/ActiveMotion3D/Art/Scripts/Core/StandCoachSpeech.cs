using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ActiveMotion3D.EndlessRunner.Core
{
    // The ABI below matches sherpa-onnx v1.13.5's Windows x64 C API.
    internal sealed class StandCoachSpeech : IDisposable
    {
        internal sealed class Wave
        {
            public readonly float[] Samples;
            public readonly int SampleRate;

            public Wave(float[] samples, int sampleRate)
            {
                Samples = samples;
                SampleRate = sampleRate;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Vits
        {
            public IntPtr Model, Lexicon, Tokens, DataDirectory;
            public float NoiseScale, NoiseScaleW, LengthScale;
            public IntPtr Dictionary;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Matcha
        {
            public IntPtr AcousticModel, Vocoder, Lexicon, Tokens, DataDirectory;
            public float NoiseScale, LengthScale;
            public IntPtr Dictionary;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Kokoro
        {
            [MarshalAs(UnmanagedType.LPUTF8Str)] public string Model;
            [MarshalAs(UnmanagedType.LPUTF8Str)] public string Voices;
            [MarshalAs(UnmanagedType.LPUTF8Str)] public string Tokens;
            [MarshalAs(UnmanagedType.LPUTF8Str)] public string DataDirectory;
            public float LengthScale;
            public IntPtr Dictionary, Lexicon;
            [MarshalAs(UnmanagedType.LPUTF8Str)] public string Language;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Kitten
        {
            public IntPtr Model, Voices, Tokens, DataDirectory;
            public float LengthScale;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Zipvoice
        {
            public IntPtr Tokens, Encoder, Decoder, Vocoder, DataDirectory, Lexicon;
            public float FeatureScale, TimeShift, TargetRms, GuidanceScale;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Pocket
        {
            public IntPtr Flow, Main, Encoder, Decoder, Conditioner, Vocabulary, Scores;
            public int CacheCapacity;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Supertonic
        {
            public IntPtr Duration, Encoder, Estimator, Vocoder, Config, Indexer, Style;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ModelConfig
        {
            public Vits Vits;
            public int Threads, Debug;
            public IntPtr Provider;
            public Matcha Matcha;
            public Kokoro Kokoro;
            public Kitten Kitten;
            public Zipvoice Zipvoice;
            public Pocket Pocket;
            public Supertonic Supertonic;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Config
        {
            public ModelConfig Model;
            public IntPtr RuleFsts;
            public int MaxSentences;
            public IntPtr RuleFars;
            public float SilenceScale;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Audio
        {
            public IntPtr Samples;
            public int Count, SampleRate;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr CreateSpeech(ref Config config);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr GenerateSpeech(IntPtr tts, [MarshalAs(UnmanagedType.LPUTF8Str)] string text,
            int speaker, float speed);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void DestroyNative(IntPtr handle);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibraryEx(string path, IntPtr file, uint flags);
        [DllImport("kernel32", ExactSpelling = true)]
        private static extern IntPtr GetProcAddress(IntPtr module, string symbol);
        [DllImport("kernel32")]
        private static extern bool FreeLibrary(IntPtr module);

        private readonly object speechLock = new object();
        private readonly GenerateSpeech generate;
        private readonly DestroyNative destroySpeech;
        private readonly DestroyNative destroyAudio;
        private IntPtr library;
        private IntPtr tts;

        public StandCoachSpeech(string directory)
        {
            // Resolve native dependencies beside the bundled DLL and in Windows system locations.
            library = LoadLibraryEx(Path.Combine(directory, "sherpa-onnx-c-api.dll"), IntPtr.Zero, 0x1100);
            if (library == IntPtr.Zero)
                throw new InvalidOperationException($"Could not load Stand Coach speech (Windows error {Marshal.GetLastWin32Error()}).");

            try
            {
                var create = GetFunction<CreateSpeech>("SherpaOnnxCreateOfflineTts");
                generate = GetFunction<GenerateSpeech>("SherpaOnnxOfflineTtsGenerate");
                destroySpeech = GetFunction<DestroyNative>("SherpaOnnxDestroyOfflineTts");
                destroyAudio = GetFunction<DestroyNative>("SherpaOnnxDestroyOfflineTtsGeneratedAudio");
                var config = new Config
                {
                    Model = new ModelConfig
                    {
                        Threads = Math.Min(4, Math.Max(1, System.Environment.ProcessorCount / 2)),
                        Kokoro = new Kokoro
                        {
                            Model = Path.Combine(directory, "model.int8.onnx"),
                            Voices = Path.Combine(directory, "voices.bin"),
                            Tokens = Path.Combine(directory, "tokens.txt"),
                            DataDirectory = Path.Combine(directory, "espeak-ng-data"),
                            Language = "en",
                            LengthScale = 1f
                        }
                    },
                    MaxSentences = 1,
                    SilenceScale = 0.6f
                };
                tts = create(ref config);
                if (tts == IntPtr.Zero) throw new InvalidOperationException("Could not initialize the Kokoro voice model.");
            }
            catch
            {
                FreeLibrary(library);
                library = IntPtr.Zero;
                throw;
            }
        }

        public Wave Synthesize(string sentence, int voice, float speed)
        {
            lock (speechLock)
            {
                if (tts == IntPtr.Zero) throw new ObjectDisposedException(nameof(StandCoachSpeech));
                IntPtr audio = generate(tts, sentence, voice, speed);
                if (audio == IntPtr.Zero) throw new InvalidOperationException("Stand Coach speech generation failed.");
                try
                {
                    Audio result = Marshal.PtrToStructure<Audio>(audio);
                    if (result.Count <= 0 || result.SampleRate <= 0)
                        throw new InvalidOperationException("Stand Coach speech was empty.");
                    var samples = new float[result.Count];
                    Marshal.Copy(result.Samples, samples, 0, result.Count);
                    return new Wave(samples, result.SampleRate);
                }
                finally
                {
                    destroyAudio(audio);
                }
            }
        }

        internal static void PreparePlayback(Wave wave, bool killerQueen)
        {
            float[] samples = wave.Samples;
            float rumbleRate = 1f - (float)Math.Exp(-2 * Math.PI * 85 / wave.SampleRate);
            float presenceRate = 1f - (float)Math.Exp(-2 * Math.PI * 1650 / wave.SampleRate);
            float presence = killerQueen ? 0.36f : 0.29f;
            float rumble = 0f, body = 0f, peak = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                rumble += rumbleRate * (samples[i] - rumble);
                float voice = samples[i] - rumble;
                body += presenceRate * (voice - body);
                samples[i] = voice + presence * (voice - body);
                peak = Math.Max(peak, Math.Abs(samples[i]));
            }

            // Measure the voiced samples so pauses do not drive the gain up.
            float gate = Math.Max(0.002f, peak * 0.075f);
            double energy = 0;
            int active = 0;
            foreach (float sample in samples)
            {
                if (Math.Abs(sample) < gate) continue;
                energy += sample * sample;
                active++;
            }
            if (active == 0) return;

            float target = killerQueen ? 0.28f : 0.25f;
            float gain = Math.Min(8f, target / (float)Math.Sqrt(energy / active));
            const float knee = 0.61f;
            float headroom = (killerQueen ? 0.95f : 0.92f) - knee;
            for (int i = 0; i < samples.Length; i++)
            {
                float sample = samples[i] * gain;
                float aboveKnee = Math.Abs(sample) - knee;
                // Smoothly approach the ceiling to keep headroom without squaring off peaks.
                samples[i] = aboveKnee <= 0f ? sample
                    : Math.Sign(sample) * (knee + headroom * aboveKnee / (headroom + aboveKnee));
            }
        }

        internal static Wave ShapeDelivery(Wave wave, bool killerQueen, int lineIndex)
        {
            float[] source = wave.Samples;
            var output = new float[(int)(source.Length / 0.88f) + 1];
            double position = 0;
            int count = 0;
            while (position < source.Length - 1)
            {
                double progress = position / (source.Length - 1);
                double arc = Math.Sin(progress * Math.PI);
                double lateStress = Math.Exp(-Math.Pow((progress - 0.69) / 0.16, 2));
                // Onyx hits the opening and drops sharply. George builds to a later
                // accent and settles at the end of the phrase.
                double pitch = killerQueen
                    ? 0.98 + 0.045 * arc + 0.055 * lateStress - 0.035 * progress
                    : 1.045 - 0.085 * progress + 0.035 * arc;
                if (lineIndex == 2) pitch += 0.025 * arc;
                float emphasis = (float)(killerQueen ? 0.94 + 0.09 * lateStress : 1.04 - 0.09 * progress);
                int index = (int)position;
                float sample = source[index] + (source[index + 1] - source[index]) * (float)(position - index);
                output[count++] = sample * emphasis;
                position += pitch;
            }
            if (count == 0) return wave;
            Array.Resize(ref output, count);
            return new Wave(output, wave.SampleRate);
        }

        private T GetFunction<T>(string name) where T : Delegate
        {
            IntPtr address = GetProcAddress(library, name);
            if (address == IntPtr.Zero) throw new EntryPointNotFoundException(name);
            return Marshal.GetDelegateForFunctionPointer<T>(address);
        }

        public void Dispose()
        {
            lock (speechLock)
            {
                if (library == IntPtr.Zero) return;
                if (tts != IntPtr.Zero) destroySpeech(tts);
                tts = IntPtr.Zero;
                FreeLibrary(library);
                library = IntPtr.Zero;
            }
        }
    }
}
