using System.Collections.Generic;
using System.Linq;

namespace Ecos.EditorTools.Generation
{
    public class AudioAsset
    {
        public string RelativePath;
        public float[] Samples;
        public bool IsMusic;
    }

    /// <summary>
    /// Efeitos sonoros e trilhas originais compostas para o jogo (geradas por síntese).
    /// </summary>
    public static class AudioLibrary
    {
        public static List<AudioAsset> GenerateAll()
        {
            var list = new List<AudioAsset>
            {
                Sfx("sfx_jump", Jump()),
                Sfx("sfx_collect", Collect()),
                Sfx("sfx_hurt", Hurt()),
                Sfx("sfx_death", Death()),
                Sfx("sfx_gameover", GameOver()),
                Sfx("sfx_victory", Victory()),
                Sfx("sfx_stomp", Stomp()),
                Sfx("sfx_checkpoint", Checkpoint()),
                Sfx("sfx_heart", Heart()),
                Sfx("sfx_levelcomplete", LevelComplete()),
                Sfx("sfx_click", Click()),
                Sfx("sfx_crumble", Crumble()),
                Sfx("sfx_land", Land()),
                Music("music_menu", MenuTheme()),
                Music("music_forest", ForestTheme()),
                Music("music_cave", CaveTheme()),
                Music("music_ruins", RuinsTheme()),
                Music("music_victory", VictoryTheme()),
            };
            return list;
        }

        static AudioAsset Sfx(string name, float[] s) => new AudioAsset { RelativePath = "Audio/SFX/" + name + ".wav", Samples = s };
        static AudioAsset Music(string name, Song song) => new AudioAsset { RelativePath = "Audio/Music/" + name + ".wav", Samples = song.Render(), IsMusic = true };

        static Envelope Pluck(double decay = 0.12, double release = 0.05) => new Envelope(0.002, decay, 0.0, release);

        // ================================================================== EFEITOS

        static float[] Jump()
        {
            var b = SoundSynth.Buffer(0.22);
            SoundSynth.Note(b, 0, 0.16, 260, 640, Wave.Square, 0.5, new Envelope(0.002, 0.16, 0.1, 0.04), 0.25);
            SoundSynth.Finish(b, 0.6);
            return b;
        }

        static float[] Collect()
        {
            var b = SoundSynth.Buffer(0.35);
            SoundSynth.Note(b, 0, 0.06, 988, 988, Wave.Square, 0.4, Pluck(0.06, 0.01), 0.125);
            SoundSynth.Note(b, 0.06, 0.2, 1319, 1319, Wave.Square, 0.4, new Envelope(0.002, 0.25, 0.0, 0.05), 0.125);
            SoundSynth.Note(b, 0.06, 0.2, 2637, 2637, Wave.Sine, 0.15, new Envelope(0.002, 0.25, 0.0, 0.05));
            SoundSynth.Finish(b, 0.6);
            return b;
        }

        static float[] Hurt()
        {
            var b = SoundSynth.Buffer(0.35);
            SoundSynth.Note(b, 0, 0.28, 520, 120, Wave.Saw, 0.45, new Envelope(0.002, 0.28, 0.2, 0.05));
            SoundSynth.Note(b, 0, 0.12, 2000, 800, Wave.Noise, 0.3, Pluck(0.12));
            SoundSynth.Finish(b, 0.7);
            return b;
        }

        static float[] Death()
        {
            var b = SoundSynth.Buffer(1.0);
            string[] notes = { "E5", "C5", "A4", "E4" };
            for (int i = 0; i < notes.Length; i++)
            {
                double f = SoundSynth.Freq(notes[i]);
                SoundSynth.Note(b, i * 0.16, 0.15, f, f * 0.97, Wave.Square, 0.35, Pluck(0.15), 0.5);
            }
            SoundSynth.Note(b, 0.64, 0.3, 1500, 200, Wave.Noise, 0.25, Pluck(0.3, 0.1));
            SoundSynth.Finish(b, 0.7);
            return b;
        }

        static float[] GameOver()
        {
            var b = SoundSynth.Buffer(2.6);
            string[] notes = { "C5", "B4", "A#4", "A4" };
            double[] times = { 0, 0.4, 0.8, 1.2 };
            for (int i = 0; i < notes.Length; i++)
            {
                double f = SoundSynth.Freq(notes[i]);
                double len = i == 3 ? 1.1 : 0.35;
                SoundSynth.Note(b, times[i], len, f, f, Wave.Square, 0.3, new Envelope(0.01, 0.2, 0.5, 0.2), 0.5, i == 3 ? 0.01 : 0);
                SoundSynth.Note(b, times[i], len, f / 2, f / 2, Wave.Triangle, 0.4, new Envelope(0.01, 0.2, 0.6, 0.2));
            }
            SoundSynth.Finish(b, 0.7);
            return b;
        }

        static float[] Victory()
        {
            var b = SoundSynth.Buffer(3.2);
            string[] run = { "C5", "E5", "G5", "C6", "E6", "G6" };
            for (int i = 0; i < run.Length; i++)
            {
                double f = SoundSynth.Freq(run[i]);
                SoundSynth.Note(b, i * 0.08, 0.08, f, f, Wave.Square, 0.25, Pluck(0.1), 0.25);
            }
            string[] chord = { "C5", "E5", "G5", "C6" };
            foreach (var n in chord)
            {
                double f = SoundSynth.Freq(n);
                SoundSynth.Note(b, 0.55, 0.35, f, f, Wave.Square, 0.16, new Envelope(0.01, 0.1, 0.7, 0.1), 0.25);
                SoundSynth.Note(b, 1.0, 1.6, f * 1.0, f, Wave.Square, 0.16, new Envelope(0.01, 0.3, 0.6, 0.5), 0.25, 0.008);
            }
            SoundSynth.Note(b, 0.55, 0.35, SoundSynth.Freq("G3"), SoundSynth.Freq("G3"), Wave.Triangle, 0.5, new Envelope(0.01, 0.1, 0.7, 0.1));
            SoundSynth.Note(b, 1.0, 1.6, SoundSynth.Freq("C3"), SoundSynth.Freq("C3"), Wave.Triangle, 0.5, new Envelope(0.01, 0.3, 0.7, 0.5));
            SoundSynth.Kick(b, 0.55, 0.5);
            SoundSynth.Kick(b, 1.0, 0.6);
            SoundSynth.Snare(b, 1.0, 0.3);
            SoundSynth.Finish(b, 0.75);
            return b;
        }

        static float[] Stomp()
        {
            var b = SoundSynth.Buffer(0.25);
            SoundSynth.Note(b, 0, 0.15, 420, 70, Wave.Square, 0.5, Pluck(0.15), 0.5);
            SoundSynth.Note(b, 0, 0.08, 1200, 300, Wave.Noise, 0.35, Pluck(0.08));
            SoundSynth.Finish(b, 0.7);
            return b;
        }

        static float[] Checkpoint()
        {
            var b = SoundSynth.Buffer(0.8);
            string[] notes = { "C5", "E5", "G5", "C6" };
            for (int i = 0; i < notes.Length; i++)
            {
                double f = SoundSynth.Freq(notes[i]);
                SoundSynth.Note(b, i * 0.09, 0.3, f, f, Wave.Triangle, 0.45, new Envelope(0.002, 0.3, 0.0, 0.1));
                SoundSynth.Note(b, i * 0.09, 0.3, f * 2, f * 2, Wave.Sine, 0.12, new Envelope(0.002, 0.3, 0.0, 0.1));
            }
            SoundSynth.Finish(b, 0.65);
            return b;
        }

        static float[] Heart()
        {
            var b = SoundSynth.Buffer(0.6);
            string[] notes = { "E5", "G#5", "B5", "E6" };
            for (int i = 0; i < notes.Length; i++)
            {
                double f = SoundSynth.Freq(notes[i]);
                SoundSynth.Note(b, i * 0.07, 0.18, f, f, Wave.Square, 0.25, Pluck(0.2), 0.125);
            }
            SoundSynth.Finish(b, 0.6);
            return b;
        }

        static float[] LevelComplete()
        {
            var b = SoundSynth.Buffer(2.0);
            string[] notes = { "G4", "C5", "E5", "G5", "E5", "G5", "C6" };
            double[] t = { 0, 0.12, 0.24, 0.36, 0.6, 0.72, 0.9 };
            for (int i = 0; i < notes.Length; i++)
            {
                double f = SoundSynth.Freq(notes[i]);
                double len = i == notes.Length - 1 ? 0.8 : 0.11;
                SoundSynth.Note(b, t[i], len, f, f, Wave.Square, 0.28, new Envelope(0.005, 0.1, 0.6, 0.15), 0.25, i == notes.Length - 1 ? 0.01 : 0);
            }
            SoundSynth.Note(b, 0.9, 0.8, SoundSynth.Freq("C3"), SoundSynth.Freq("C3"), Wave.Triangle, 0.45, new Envelope(0.01, 0.2, 0.7, 0.2));
            SoundSynth.Finish(b, 0.7);
            return b;
        }

        static float[] Click()
        {
            var b = SoundSynth.Buffer(0.06);
            SoundSynth.Note(b, 0, 0.04, 900, 700, Wave.Square, 0.4, Pluck(0.04, 0.01), 0.25);
            SoundSynth.Finish(b, 0.45);
            return b;
        }

        static float[] Crumble()
        {
            var b = SoundSynth.Buffer(0.45);
            SoundSynth.Note(b, 0, 0.4, 600, 150, Wave.Noise, 0.6, new Envelope(0.01, 0.4, 0.1, 0.05));
            SoundSynth.Note(b, 0, 0.3, 90, 50, Wave.Triangle, 0.4, Pluck(0.3));
            SoundSynth.LowPass(b, 1800);
            SoundSynth.Finish(b, 0.6);
            return b;
        }

        static float[] Land()
        {
            var b = SoundSynth.Buffer(0.1);
            SoundSynth.Note(b, 0, 0.07, 400, 150, Wave.Noise, 0.5, Pluck(0.07, 0.01));
            SoundSynth.LowPass(b, 1200);
            SoundSynth.Finish(b, 0.4);
            return b;
        }

        // ================================================================== MÚSICAS

        static string Rep(string s, int n) => string.Join(" ", Enumerable.Repeat(s, n));

        static Channel Lead(string notes, double vol = 0.2, double duty = 0.25, bool echo = false) => new Channel
        {
            Notes = notes, Wave = Wave.Square, Duty = duty, Volume = vol, Vibrato = 0.006,
            Env = new Envelope(0.005, 0.1, 0.55, 0.06), EchoBus = echo
        };

        static Channel Arp(string notes, double vol = 0.08, Wave wave = Wave.Square, bool echo = false) => new Channel
        {
            Notes = notes, Wave = wave, Duty = 0.125, Volume = vol, Env = new Envelope(0.002, 0.12, 0.2, 0.03), EchoBus = echo
        };

        static Channel Bass(string notes, double vol = 0.32) => new Channel
        {
            Notes = notes, Wave = Wave.Triangle, Volume = vol, Env = new Envelope(0.004, 0.1, 0.75, 0.04)
        };

        static Channel Drums(string notes, double vol = 0.25) => new Channel { Notes = notes, Drums = true, Volume = vol };

        /// <summary>Floresta Encantada — Dó maior, alegre e saltitante.</summary>
        static Song ForestTheme()
        {
            string bC = "C3 . C3 G2 C3 . G2 .", bAm = "A2 . A2 E2 A2 . E2 .", bF = "F2 . F2 C3 F2 . C3 .", bG = "G2 . G2 D3 G2 . D3 .";
            string bEm = "E2 . E2 B2 E2 . B2 .";
            string aC = "C5 E5 G5 E5 C5 E5 G5 E5", aAm = "A4 C5 E5 C5 A4 C5 E5 C5", aF = "F4 A4 C5 A4 F4 A4 C5 A4", aG = "G4 B4 D5 B4 G4 B4 D5 B4";
            string aEm = "E4 G4 B4 G4 E4 G4 B4 G4";

            string partA = string.Join(" | ", bC, bAm, bF, bG);
            string bass = string.Join(" | ", partA, partA, bF, bG, bEm, bAm, bF, bG, bC, bC);
            string arpA = string.Join(" | ", aC, aAm, aF, aG);
            string arp = string.Join(" | ", arpA, arpA, aF, aG, aEm, aAm, aF, aG, aC, aC);
            string lead = string.Join(" | ",
                "E5 - - G5 E5 - D5 C5", "E5 - - A5 G5 - E5 -", "F5 - E5 F5 A5 - G5 F5", "D5 - - E5 D5 - B4 -",
                "E5 - - G5 E5 - D5 C5", "E5 - - A5 G5 - E5 -", "F5 - A5 - G5 - F5 D5", "B4 - C5 - D5 - . .",
                "A5 - - G5 F5 - E5 F5", "G5 - - F5 E5 - D5 E5", "E5 - G5 - B5 - G5 -", "A5 - - - E5 - C5 -",
                "F5 - A5 - C6 - A5 -", "G5 - B5 - D6 - B5 -", "C6 - - - G5 - E5 -", "C5 - - - . . . .");
            string drumBar = "k . h . s . h .";
            string drums = Rep(drumBar, 7) + " k . h k s . s s " + Rep(drumBar, 7) + " k . h k s s s s";
            var song = new Song { Bpm = 132 };
            song.Channels.Add(Bass(bass));
            song.Channels.Add(Arp(arp));
            song.Channels.Add(Lead(lead));
            song.Channels.Add(Drums(drums));
            return song;
        }

        /// <summary>Cavernas Luminosas — Lá menor, misteriosa, com eco.</summary>
        static Song CaveTheme()
        {
            string Hold(string n) => n + " - - - - - - -";
            string bass = string.Join(" | ",
                Hold("A2"), Hold("F2"), Hold("G2"), Hold("E2"), Hold("A2"), Hold("F2"), Hold("G2"), Hold("E2"),
                Hold("D2"), Hold("A2"), Hold("E2"), Hold("E2"));
            string arp = string.Join(" | ",
                "A4 . E5 . C5 . E5 .", "F4 . C5 . A4 . C5 .", "G4 . D5 . B4 . D5 .", "E4 . B4 . G4 . B4 .",
                "A4 . E5 . C5 . E5 .", "F4 . C5 . A4 . C5 .", "G4 . D5 . B4 . D5 .", "E4 . B4 . G#4 . B4 .",
                "D4 . A4 . F4 . A4 .", "A4 . E5 . C5 . E5 .", "E4 . B4 . G#4 . B4 .", "E4 . G#4 . B4 . E5 .");
            string lead = string.Join(" | ",
                "E5 - - - - - D5 C5", "C5 - - - A4 - - -", "B4 - - - D5 - - -", "E5 - - - - - - -",
                "E5 - - - G5 - E5 D5", "C5 - - - A4 - C5 -", "B4 - - - G4 - B4 -", "E5 - - - - - - -",
                "F5 - - - E5 - D5 -", "C5 - - - E5 - A5 -", "G#5 - - - B5 - G#5 -", "E5 - - - - - - -");
            string drums = Rep("k . . . h . . . ", 4) + Rep("k . . . h . . h ", 4) + Rep("k . . . h . h . ", 4);
            var song = new Song { Bpm = 96, EchoDelay = 0.47, EchoFeedback = 0.38, EchoMix = 0.35 };
            song.Channels.Add(Bass(bass, 0.3));
            song.Channels.Add(Arp(arp, 0.07, Wave.Square, true));
            song.Channels.Add(new Channel
            {
                Notes = lead, Wave = Wave.Triangle, Volume = 0.34, Vibrato = 0.01,
                Env = new Envelope(0.02, 0.2, 0.7, 0.15), EchoBus = true
            });
            song.Channels.Add(Drums(drums, 0.18));
            return song;
        }

        /// <summary>Ruínas da Energia — Ré menor, rápida e tensa.</summary>
        static string Pump(string low, string high) => string.Join(" ", Enumerable.Repeat(low + " " + high, 4));

        static Song RuinsTheme()
        {
            string bDm = Pump("D2", "D3"), bBb = Pump("A#1", "A#2"), bC = Pump("C2", "C3"), bA = Pump("A1", "A2"), bGm = Pump("G1", "G2");
            string aDm = "D5 F5 A5 F5 D5 F5 A5 F5", aBb = "A#4 D5 F5 D5 A#4 D5 F5 D5", aC = "C5 E5 G5 E5 C5 E5 G5 E5", aA = "A4 C#5 E5 C#5 A4 C#5 E5 C#5";
            string aGm = "G4 A#4 D5 A#4 G4 A#4 D5 A#4";
            string bass = string.Join(" | ", bDm, bBb, bC, bA, bDm, bBb, bC, bA, bGm, bDm, bBb, bA, bGm, bDm, bBb, bA);
            string arp = string.Join(" | ", aDm, aBb, aC, aA, aDm, aBb, aC, aA, aGm, aDm, aBb, aA, aGm, aDm, aBb, aA);
            string lead = string.Join(" | ",
                "D5 - E5 F5 . E5 D5 .", "D5 - F5 A#5 . A5 F5 .", "E5 - G5 C6 . A#5 G5 .", "C#5 - E5 A5 - - - .",
                "A5 - G5 F5 . E5 F5 .", "F5 - E5 D5 . C5 D5 .", "E5 - F5 G5 . A5 A#5 .", "A5 - - - C#6 - - -",
                "G5 - A#5 - D6 - A#5 -", "A5 - F5 - D5 - F5 -", "F5 - D5 - A#4 - D5 F5", "E5 - C#5 - A4 - - -",
                "G5 - A#5 - D6 - F6 -", "F6 - E6 D6 A5 - F5 -", "F5 - G5 A5 A#5 - D6 -", "C#6 - - - E6 - - -");
            string drumBar = "k . h k s . h .";
            string drums = Rep(drumBar, 7) + " k . s s s . s s " + Rep(drumBar, 7) + " k k s s s s s s";
            var song = new Song { Bpm = 144 };
            song.Channels.Add(Bass(bass, 0.3));
            song.Channels.Add(Arp(arp, 0.06));
            song.Channels.Add(Lead(lead, 0.18, 0.5));
            song.Channels.Add(Drums(drums, 0.26));
            return song;
        }

        /// <summary>Menu — Fá maior, calma e acolhedora.</summary>
        static Song MenuTheme()
        {
            string bF = "F2 - - - C3 - - -", bDm = "D2 - - - A2 - - -", bBb = "A#1 - - - F2 - - -", bC = "C2 - - - G2 - - -";
            string aF = "F4 A4 C5 F5 C5 A4 F4 A4", aDm = "D4 F4 A4 D5 A4 F4 D4 F4", aBb = "A#3 D4 F4 A#4 F4 D4 A#3 D4", aC = "C4 E4 G4 C5 G4 E4 C4 E4";
            string bass = Rep(string.Join(" | ", bF, bDm, bBb, bC), 2);
            string arp = Rep(string.Join(" | ", aF, aDm, aBb, aC), 2);
            string lead = string.Join(" | ",
                "A5 - - G5 F5 - C5 -", "D5 - - E5 F5 - A5 -", "A#5 - A5 G5 F5 - D5 -", "E5 - - - G5 - - -",
                "A5 - - G5 F5 - C5 -", "D5 - F5 - A5 - D6 -", "C6 - A#5 A5 G5 - E5 -", "F5 - - - - - . .");
            string drums = Rep("k . h . s . h h", 8);
            var song = new Song { Bpm = 100 };
            song.Channels.Add(Bass(bass, 0.3));
            song.Channels.Add(Arp(arp, 0.11, Wave.Triangle));
            song.Channels.Add(Lead(lead, 0.13, 0.125));
            song.Channels.Add(Drums(drums, 0.14));
            return song;
        }

        /// <summary>Vitória — Dó maior, triunfante.</summary>
        static Song VictoryTheme()
        {
            string bass = string.Join(" | ",
                "C3 . C3 . G2 . C3 .", "F2 . F2 . C3 . F2 .", "G2 . G2 . D3 . G2 .", "C3 . G2 . C3 . C3 .",
                "A2 . A2 . E2 . A2 .", "F2 . F2 . C3 . F2 .", "G2 . G2 . D3 . G2 .", "C3 . G2 . C3 . . .");
            string arp = string.Join(" | ",
                "C5 E5 G5 E5 C5 E5 G5 E5", "F4 A4 C5 A4 F4 A4 C5 A4", "G4 B4 D5 B4 G4 B4 D5 B4", "C5 E5 G5 E5 C5 E5 G5 E5",
                "A4 C5 E5 C5 A4 C5 E5 C5", "F4 A4 C5 A4 F4 A4 C5 A4", "G4 B4 D5 B4 G4 B4 D5 B4", "C5 E5 G5 C6 G5 E5 C5 .");
            string lead = string.Join(" | ",
                "G5 - C6 - E6 - D6 C6", "A5 - C6 - F6 - E6 D6", "B5 - D6 - G6 - F6 D6", "E6 - - - C6 - - -",
                "E6 - D6 C6 A5 - C6 -", "F6 - E6 D6 C6 - A5 -", "G5 - B5 - D6 - F6 -", "E6 - - - - - . .");
            string drums = Rep("k . h . s . h .", 3) + " k . s . s s s s " + Rep("k . h . s . h .", 3) + " k . s s k . . .";
            var song = new Song { Bpm = 120 };
            song.Channels.Add(Bass(bass, 0.32));
            song.Channels.Add(Arp(arp, 0.07));
            song.Channels.Add(Lead(lead, 0.15, 0.25));
            song.Channels.Add(Drums(drums, 0.22));
            return song;
        }
    }
}
