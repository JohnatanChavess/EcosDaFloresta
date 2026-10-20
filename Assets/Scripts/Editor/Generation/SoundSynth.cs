using System;
using System.Collections.Generic;
using System.IO;

namespace Ecos.EditorTools.Generation
{
    public enum Wave { Square, Triangle, Saw, Sine, Noise }

    /// <summary>Envelope ADSR simples (segundos e nível de sustentação 0..1).</summary>
    public struct Envelope
    {
        public double Attack, Decay, Sustain, Release;

        public Envelope(double attack, double decay, double sustain, double release)
        {
            Attack = attack; Decay = decay; Sustain = sustain; Release = release;
        }

        public double Level(double t, double noteLength)
        {
            double level;
            if (t < Attack) level = t / Math.Max(1e-5, Attack);
            else if (t < Attack + Decay) level = 1 - (1 - Sustain) * (t - Attack) / Math.Max(1e-5, Decay);
            else level = Sustain;
            if (t > noteLength) level *= Math.Max(0, 1 - (t - noteLength) / Math.Max(1e-5, Release));
            return level;
        }
    }

    /// <summary>
    /// Sintetizador estilo chiptune (onda quadrada, triangular, serra, senoide e ruído)
    /// usado para gerar todos os efeitos sonoros e músicas originais do jogo.
    /// </summary>
    public static class SoundSynth
    {
        public const int SampleRate = 22050;

        public static float[] Buffer(double seconds) => new float[(int)Math.Ceiling(seconds * SampleRate)];

        static double Osc(Wave wave, double phase, double duty, ref uint noise, ref double noiseValue, double lastPhase)
        {
            double p = phase - Math.Floor(phase);
            switch (wave)
            {
                case Wave.Square: return p < duty ? 1.0 : -1.0;
                case Wave.Triangle: return 4.0 * Math.Abs(p - 0.5) - 1.0;
                case Wave.Saw: return 2.0 * p - 1.0;
                case Wave.Sine: return Math.Sin(2 * Math.PI * p);
                default:
                    // Ruído que muda de valor a cada ciclo (soa como ruído "8 bits").
                    if (Math.Floor(phase) != Math.Floor(lastPhase))
                    {
                        noise ^= noise << 13; noise ^= noise >> 17; noise ^= noise << 5;
                        noiseValue = (noise & 0xFFFF) / 32768.0 - 1.0;
                    }
                    return noiseValue;
            }
        }

        /// <summary>
        /// Adiciona uma nota ao buffer. A frequência pode deslizar de freqStart a freqEnd (exponencialmente).
        /// </summary>
        public static void Note(float[] buffer, double start, double length, double freqStart, double freqEnd, Wave wave,
            double volume, Envelope env, double duty = 0.5, double vibratoDepth = 0, double vibratoRate = 5.5)
        {
            int s0 = (int)(start * SampleRate);
            double total = length + env.Release;
            int count = (int)(total * SampleRate);
            double phase = 0, lastPhase = -1;
            uint noise = 0x12345u ^ (uint)(freqStart * 1000);
            double noiseValue = 0;
            for (int i = 0; i < count; i++)
            {
                int idx = s0 + i;
                if (idx < 0) continue;
                if (idx >= buffer.Length) break;
                double t = i / (double)SampleRate;
                double k = Math.Min(1.0, t / Math.Max(1e-5, length));
                double freq = freqStart * Math.Pow(freqEnd / freqStart, k);
                if (vibratoDepth > 0 && t > 0.12) freq *= 1 + vibratoDepth * Math.Sin(2 * Math.PI * vibratoRate * t);
                lastPhase = phase;
                phase += freq / SampleRate;
                double v = Osc(wave, phase, duty, ref noise, ref noiseValue, lastPhase);
                buffer[idx] += (float)(v * volume * env.Level(t, length));
            }
        }

        public static void Kick(float[] b, double t, double vol)
        {
            Note(b, t, 0.12, 150, 40, Wave.Sine, vol, new Envelope(0.001, 0.1, 0.0, 0.02));
            Note(b, t, 0.02, 900, 400, Wave.Noise, vol * 0.25, new Envelope(0.001, 0.02, 0.0, 0.01));
        }

        public static void Snare(float[] b, double t, double vol)
        {
            Note(b, t, 0.12, 3500, 2500, Wave.Noise, vol, new Envelope(0.001, 0.12, 0.0, 0.02));
            Note(b, t, 0.06, 220, 160, Wave.Triangle, vol * 0.6, new Envelope(0.001, 0.06, 0.0, 0.02));
        }

        public static void Hat(float[] b, double t, double vol)
        {
            Note(b, t, 0.03, 9000, 9000, Wave.Noise, vol, new Envelope(0.001, 0.03, 0.0, 0.01));
        }

        /// <summary>Filtro passa-baixa de um polo (suaviza ruídos).</summary>
        public static void LowPass(float[] b, double cutoff)
        {
            double rc = 1.0 / (2 * Math.PI * cutoff);
            double dt = 1.0 / SampleRate;
            double a = dt / (rc + dt);
            double y = 0;
            for (int i = 0; i < b.Length; i++)
            {
                y += a * (b[i] - y);
                b[i] = (float)y;
            }
        }

        /// <summary>Eco simples (delay com realimentação).</summary>
        public static void Echo(float[] b, double delaySeconds, double feedback, double mix)
        {
            int d = (int)(delaySeconds * SampleRate);
            var wet = new float[b.Length];
            for (int i = 0; i < b.Length; i++)
            {
                float delayed = i >= d ? wet[i - d] : 0f;
                wet[i] = b[i] + (float)(delayed * feedback);
            }
            for (int i = 0; i < b.Length; i++) b[i] = (float)(b[i] * (1 - mix) + wet[i] * mix);
        }

        public static void MixInto(float[] dst, float[] src, double gain = 1.0)
        {
            int n = Math.Min(dst.Length, src.Length);
            for (int i = 0; i < n; i++) dst[i] += (float)(src[i] * gain);
        }

        /// <summary>Normaliza o pico e aplica saturação suave para evitar distorção.</summary>
        public static void Finish(float[] b, double peak = 0.85, bool fadeEdges = true)
        {
            double max = 1e-6;
            foreach (float s in b) max = Math.Max(max, Math.Abs(s));
            double g = peak / max;
            for (int i = 0; i < b.Length; i++) b[i] = (float)Math.Tanh(b[i] * g * 1.1) * 0.95f;
            if (!fadeEdges) return;
            int fade = Math.Min(b.Length / 4, SampleRate / 200);
            for (int i = 0; i < fade; i++)
            {
                b[i] *= i / (float)fade;
                b[b.Length - 1 - i] *= i / (float)fade;
            }
        }

        public static double Freq(string note)
        {
            // Formato: C4, C#4, Db4 ...
            string[] names = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
            string name = note.Substring(0, note.Length - 1);
            int octave = note[note.Length - 1] - '0';
            name = name.Replace("Db", "C#").Replace("Eb", "D#").Replace("Gb", "F#").Replace("Ab", "G#").Replace("Bb", "A#");
            int idx = Array.IndexOf(names, name);
            if (idx < 0) throw new ArgumentException("Nota inválida: " + note);
            int midi = (octave + 1) * 12 + idx;
            return 440.0 * Math.Pow(2, (midi - 69) / 12.0);
        }

        public static byte[] ToWav(float[] samples)
        {
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                int dataLen = samples.Length * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' });
                w.Write(36 + dataLen);
                w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                w.Write(16);
                w.Write((short)1);
                w.Write((short)1);
                w.Write(SampleRate);
                w.Write(SampleRate * 2);
                w.Write((short)2);
                w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' });
                w.Write(dataLen);
                foreach (float s in samples)
                {
                    float c = Math.Max(-1f, Math.Min(1f, s));
                    w.Write((short)(c * 32767));
                }
                w.Flush();
                return ms.ToArray();
            }
        }
    }

    /// <summary>Um canal de música escrito como sequência de notas em colcheias.</summary>
    public class Channel
    {
        public string Notes;
        public Wave Wave = Wave.Square;
        public double Duty = 0.5;
        public double Volume = 0.2;
        public Envelope Env = new Envelope(0.005, 0.08, 0.6, 0.05);
        public double Vibrato;
        public bool Drums;
        public bool EchoBus;
    }

    /// <summary>Música: andamento + canais. Tokens: nota (C5), "." pausa, "-" sustenta; bateria: k, s, h.</summary>
    public class Song
    {
        public double Bpm = 120;
        public int StepsPerBeat = 2;
        public readonly List<Channel> Channels = new List<Channel>();
        public double EchoDelay = 0.3, EchoFeedback = 0.3, EchoMix = 0.3;

        public float[] Render()
        {
            double step = 60.0 / Bpm / StepsPerBeat;
            int steps = 0;
            foreach (var ch in Channels) steps = Math.Max(steps, Tokens(ch.Notes).Length);
            double length = steps * step;
            var dry = SoundSynth.Buffer(length);
            var echoBus = SoundSynth.Buffer(length);

            foreach (var ch in Channels)
            {
                var target = ch.EchoBus ? echoBus : dry;
                var tokens = Tokens(ch.Notes);
                for (int i = 0; i < tokens.Length; i++)
                {
                    string tk = tokens[i];
                    double t = i * step;
                    if (ch.Drums)
                    {
                        foreach (char d in tk)
                        {
                            if (d == 'k') SoundSynth.Kick(target, t, ch.Volume * 1.6);
                            else if (d == 's') SoundSynth.Snare(target, t, ch.Volume);
                            else if (d == 'h') SoundSynth.Hat(target, t, ch.Volume * 0.4);
                        }
                        continue;
                    }
                    if (tk == "." || tk == "-") continue;
                    int len = 1;
                    while (i + len < tokens.Length && tokens[i + len] == "-") len++;
                    double f = SoundSynth.Freq(tk);
                    double noteLen = len * step * 0.92;
                    // Evita que a cauda passe do fim do loop.
                    double release = Math.Min(ch.Env.Release, Math.Max(0, length - t - noteLen));
                    var env = new Envelope(ch.Env.Attack, ch.Env.Decay, ch.Env.Sustain, Math.Max(0.005, release));
                    SoundSynth.Note(target, t, noteLen, f, f, ch.Wave, ch.Volume, env, ch.Duty, ch.Vibrato);
                }
            }

            SoundSynth.Echo(echoBus, EchoDelay, EchoFeedback, EchoMix);
            SoundSynth.MixInto(dry, echoBus);
            SoundSynth.Finish(dry, 0.8, false);
            return dry;
        }

        static string[] Tokens(string s) => s.Split(new[] { ' ', '|', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
    }
}
