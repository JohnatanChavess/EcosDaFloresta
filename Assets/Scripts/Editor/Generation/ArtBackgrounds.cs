using System;

namespace Ecos.EditorTools.Generation
{
    /// <summary>
    /// Fundos de 384x216 pixels (exatamente a área visível da câmera). As camadas "far" e "mid"
    /// repetem horizontalmente sem emendas, para o efeito parallax.
    /// </summary>
    public static partial class ArtLibrary
    {
        public const int BgWidth = 384;
        public const int BgHeight = 216;

        static readonly int[,] Bayer =
        {
            { 0, 8, 2, 10 },
            { 12, 4, 14, 6 },
            { 3, 11, 1, 9 },
            { 15, 7, 13, 5 }
        };

        static double Wave(int x, int cycles, double phase) => Math.Sin(2 * Math.PI * cycles * x / BgWidth + phase);

        /// <summary>Degradê vertical com pontilhado (dithering) entre várias cores.</summary>
        static void Gradient(PixelCanvas c, Rgba[] stops)
        {
            for (int y = 0; y < c.Height; y++)
            {
                double t = y / (double)(c.Height - 1) * (stops.Length - 1);
                int i = Math.Min(stops.Length - 2, (int)Math.Floor(t));
                double f = t - i;
                // Quantiza em 6 faixas por segmento com pontilhado entre elas.
                double bands = 6;
                double q = f * bands;
                int band = (int)Math.Floor(q);
                double frac = q - band;
                for (int x = 0; x < c.Width; x++)
                {
                    double threshold = (Bayer[y % 4, x % 4] + 0.5) / 16.0;
                    int b = frac > threshold ? band + 1 : band;
                    c.Set(x, y, Rgba.Lerp(stops[i], stops[i + 1], Math.Min(1, b / bands)));
                }
            }
        }

        static void Cloud(PixelCanvas c, int x, int y, int w, Rgba light, Rgba shade)
        {
            c.Ellipse(x, y, w * 0.5, w * 0.2, shade);
            c.Ellipse(x - w * 0.18, y - w * 0.08, w * 0.24, w * 0.18, light);
            c.Ellipse(x + w * 0.12, y - w * 0.12, w * 0.28, w * 0.2, light);
            c.Ellipse(x, y - w * 0.02, w * 0.46, w * 0.13, light);
        }

        /// <summary>Preenche abaixo de uma linha de horizonte (silhueta que se repete horizontalmente).</summary>
        static void Silhouette(PixelCanvas c, Func<int, double> surface, Rgba fill, Rgba edge, Rgba? lowFill = null)
        {
            for (int x = 0; x < c.Width; x++)
            {
                int top = (int)Math.Round(surface(x));
                for (int y = Math.Max(0, top); y < c.Height; y++)
                {
                    Rgba col = fill;
                    if (lowFill.HasValue)
                    {
                        double t = (y - top) / (double)Math.Max(1, c.Height - top);
                        double threshold = (Bayer[y % 4, x % 4] + 0.5) / 16.0;
                        if (t * 1.6 > threshold + 0.4) col = lowFill.Value;
                    }
                    c.Set(x, y, col);
                }
                if (top >= 0 && top < c.Height) c.Set(x, top, edge);
            }
        }

        // ================================================================== FLORESTA

        static PixelCanvas ForestSky()
        {
            var c = new PixelCanvas(BgWidth, BgHeight);
            Gradient(c, new[] { Rgba.Hex("3f96e0"), Rgba.Hex("7cc4f4"), Rgba.Hex("d4f0ff") });
            Cloud(c, 60, 40, 70, Pal.White, Rgba.Hex("cfe8fb"));
            Cloud(c, 220, 28, 54, Pal.White, Rgba.Hex("cfe8fb"));
            Cloud(c, 320, 70, 80, Pal.White, Rgba.Hex("cfe8fb"));
            Cloud(c, 150, 90, 40, Rgba.Hex("f4fbff"), Rgba.Hex("d8eefc"));
            return c;
        }

        static PixelCanvas ForestFar()
        {
            var c = new PixelCanvas(BgWidth, BgHeight);
            Func<int, double> hills = x => 118 + 14 * Wave(x, 2, 0.3) + 7 * Wave(x, 5, 1.2) + 3 * Wave(x, 11, 2.0);
            Silhouette(c, hills, Rgba.Hex("8fcfb4"), Rgba.Hex("b8e6cc"), Rgba.Hex("7cbfa4"));
            // Árvores distantes sobre as colinas.
            for (int x = 6; x < BgWidth; x += 13)
            {
                int baseY = (int)hills(x) + 2;
                double r = 5 + 3 * Math.Abs(Wave(x, 7, 0.5));
                for (int k = 0; k < 2; k++)
                {
                    int ox = k == 0 ? x : x - BgWidth;
                    c.Ellipse(ox, baseY - r * 0.8, r, r, (nx, ny) => nx + ny < -0.6 ? Rgba.Hex("9ad8bc") : Rgba.Hex("78b89c"));
                }
            }
            return c;
        }

        static PixelCanvas ForestMid()
        {
            var c = new PixelCanvas(BgWidth, BgHeight);
            Func<int, double> line = x => 150 + 6 * Wave(x, 3, 1.0) + 4 * Wave(x, 7, 0.2);
            Silhouette(c, line, Rgba.Hex("3f8f5f"), Rgba.Hex("57a872"), Rgba.Hex("2f7550"));
            int i = 0;
            for (int x = 0; x < BgWidth; x += 24, i++)
            {
                double r = 14 + 5 * Math.Abs(Wave(x, 5, 0.7));
                int baseY = (int)line(x);
                for (int k = -1; k <= 1; k++)
                {
                    int ox = x + k * BgWidth;
                    // Tronco.
                    c.Rect(ox - 2, baseY - 4, 4, 30, Rgba.Hex("35573f"));
                    c.Ellipse(ox, baseY - r * 0.9, r, r * 1.05, (nx, ny) =>
                    {
                        double l = nx * 0.5 + ny;
                        if (l > 0.6) return Rgba.Hex("2f7550");
                        if (l < -0.7) return Rgba.Hex("62b27a");
                        return Rgba.Hex("448f5e");
                    });
                }
            }
            return c;
        }

        // ================================================================== CAVERNA

        static PixelCanvas CaveSky()
        {
            var c = new PixelCanvas(BgWidth, BgHeight);
            Gradient(c, new[] { Rgba.Hex("0a0818"), Rgba.Hex("171334"), Rgba.Hex("231c48") });
            var rng = new Rng(77);
            for (int i = 0; i < 70; i++)
            {
                int x = rng.Range(0, BgWidth), y = rng.Range(0, BgHeight);
                c.Set(x, y, rng.Chance(0.5) ? Rgba.Hex("5ff0d8").WithAlpha(140) : Rgba.Hex("b98cff").WithAlpha(120));
            }
            return c;
        }

        static PixelCanvas CaveFar()
        {
            var c = new PixelCanvas(BgWidth, BgHeight);
            Rgba col = Rgba.Hex("261f4e"), edge = Rgba.Hex("342b66");
            // Estalactites distantes.
            for (int x = 0; x < BgWidth; x++)
            {
                double len = 30 + 22 * Wave(x, 6, 0.4) + 14 * Wave(x, 13, 1.7) + 8 * Wave(x, 29, 0.9);
                for (int y = 0; y < len; y++) c.Set(x, y, col);
                c.Set(x, (int)len, edge);
            }
            Func<int, double> floor = x => 160 + 10 * Wave(x, 4, 0.2) + 6 * Wave(x, 9, 2.2);
            Silhouette(c, floor, col, edge);
            var rng = new Rng(5);
            for (int i = 0; i < 24; i++)
            {
                int x = rng.Range(0, BgWidth);
                int y = (int)floor(x) + rng.Range(4, 40);
                c.Set(x, y, Rgba.Hex("5ff0d8").WithAlpha(180));
            }
            return c;
        }

        static PixelCanvas CaveMid()
        {
            var c = new PixelCanvas(BgWidth, BgHeight);
            Rgba col = Rgba.Hex("181434"), edge = Rgba.Hex("2a2458");
            for (int x = 0; x < BgWidth; x++)
            {
                double spike = Math.Max(0, Wave(x, 5, 0.0)) * 50 + Math.Max(0, Wave(x, 11, 1.3)) * 22;
                double len = 12 + spike;
                for (int y = 0; y < len; y++) c.Set(x, y, col);
                c.Set(x, (int)len, edge);
            }
            Func<int, double> floor = x => 176 - Math.Max(0, Wave(x, 4, 2.0)) * 40 - Math.Max(0, Wave(x, 9, 0.5)) * 18;
            Silhouette(c, floor, col, edge);
            // Aglomerados de cristais brilhantes.
            for (int x = 20; x < BgWidth; x += 64)
            {
                int baseY = (int)floor(x) + 2;
                for (int k = 0; k < 3; k++)
                {
                    double cx = x + (k - 1) * 4;
                    double h = k == 1 ? 16 : 10;
                    c.Triangle(cx - 2.5, baseY, cx, baseY - h, cx + 2.5, baseY, (px, py) => px < cx ? Rgba.Hex("7ff8e4") : Rgba.Hex("2fa8a0"));
                }
                c.Ellipse(x, baseY - 8, 12, 10, (nx, ny) => Rgba.Hex("5ff0d8").WithAlpha(40));
            }
            return c;
        }

        // ================================================================== RUÍNAS

        static PixelCanvas RuinsSky()
        {
            var c = new PixelCanvas(BgWidth, BgHeight);
            Gradient(c, new[] { Rgba.Hex("2a1f5c"), Rgba.Hex("7a3a86"), Rgba.Hex("e0707a"), Rgba.Hex("ffc07a") });
            // Sol poente com halo.
            c.Ellipse(285, 150, 40, 40, (nx, ny) => Rgba.Hex("ffe0a0").WithAlpha(50));
            c.Ellipse(285, 150, 28, 28, (nx, ny) => Rgba.Hex("ffe8b0").WithAlpha(90));
            c.Circle(285, 150, 18, Rgba.Hex("fff2c8"));
            // Faixas de nuvens.
            for (int i = 0; i < 4; i++)
            {
                int y = 100 + i * 18;
                int x0 = (i * 97) % BgWidth;
                for (int x = 0; x < 110 + i * 20; x++)
                {
                    int px = (x0 + x) % BgWidth;
                    c.Set(px, y, Rgba.Hex("c8608a"));
                    if (x > 6 && x < 90) c.Set(px, y + 1, Rgba.Hex("a84a80"));
                }
            }
            return c;
        }

        static PixelCanvas RuinsFar()
        {
            var c = new PixelCanvas(BgWidth, BgHeight);
            Func<int, double> mountains = x =>
            {
                double peaks = Math.Abs(Wave(x, 3, 0.4)) * 40 + Math.Abs(Wave(x, 7, 1.1)) * 16;
                return 150 - peaks;
            };
            Silhouette(c, mountains, Rgba.Hex("6a3f7a"), Rgba.Hex("9a6aa8"), Rgba.Hex("5a3470"));
            return c;
        }

        static PixelCanvas RuinsMid()
        {
            var c = new PixelCanvas(BgWidth, BgHeight);
            Rgba col = Rgba.Hex("3a2450"), edge = Rgba.Hex("54366e");
            Func<int, double> ground = x => 180 + 4 * Wave(x, 3, 0.5);
            Silhouette(c, ground, col, edge);
            // Colunas e arcos em ruínas.
            int[] columnX = { 20, 64, 120, 170, 236, 300, 350 };
            int[] heights = { 90, 60, 110, 40, 96, 70, 50 };
            for (int i = 0; i < columnX.Length; i++)
            {
                int x = columnX[i], h = heights[i];
                int baseY = (int)ground(x);
                c.Rect(x - 6, baseY - h, 12, h, col);
                c.Rect(x - 8, baseY - h - 4, 16, 4, col);
                c.VLine(x - 6, baseY - h, baseY, edge);
                // Topo quebrado.
                if (i % 2 == 1)
                {
                    c.Rect(x - 8, baseY - h - 4, 7, 3, Rgba.Clear);
                    c.Set(x + 4, baseY - h - 5, col);
                }
            }
            // Arco entre duas colunas.
            c.Ring(92, (int)ground(92) - 60, 34, 30, 6, a => a < 0 ? col : Rgba.Clear);
            c.Ring(268, (int)ground(268) - 70, 38, 34, 6, a => a < 0 ? col : Rgba.Clear);
            // Plantas penduradas.
            var rng = new Rng(13);
            for (int i = 0; i < 18; i++)
            {
                int x = rng.Range(0, BgWidth);
                int y0 = (int)ground(x) - rng.Range(30, 100);
                if (c.Get(x, y0).A == 0) continue;
                for (int y = y0; y < y0 + rng.Range(4, 14); y++) c.Set(x, y, Rgba.Hex("4a6a4a"));
            }
            return c;
        }

        static SheetAsset Bg(string key, PixelCanvas canvas)
        {
            var sheet = SheetBuilder.Single("Sprites/Backgrounds/" + key + ".png", key, canvas, 0.5, 0.5);
            sheet.RepeatWrap = true;
            sheet.FullRectMesh = true;
            return sheet;
        }

        static SheetAsset[] BuildBackgrounds()
        {
            return new[]
            {
                Bg("bg_forest_sky", ForestSky()), Bg("bg_forest_far", ForestFar()), Bg("bg_forest_mid", ForestMid()),
                Bg("bg_cave_sky", CaveSky()), Bg("bg_cave_far", CaveFar()), Bg("bg_cave_mid", CaveMid()),
                Bg("bg_ruins_sky", RuinsSky()), Bg("bg_ruins_far", RuinsFar()), Bg("bg_ruins_mid", RuinsMid()),
            };
        }
    }
}
