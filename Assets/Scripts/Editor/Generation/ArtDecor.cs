using System;
using System.Collections.Generic;

namespace Ecos.EditorTools.Generation
{
    /// <summary>Decorações de cenário (árvores, arbustos, cogumelos, estalagmites, pilares...).</summary>
    public static partial class ArtLibrary
    {
        static readonly Rgba CanopyLight = Rgba.Hex("9be35a");
        static readonly Rgba CanopyBase = Rgba.Hex("4fb23f");
        static readonly Rgba CanopyDark = Rgba.Hex("2c7a35");
        static readonly Rgba CanopyDeep = Rgba.Hex("1d5430");
        static readonly Rgba TrunkLight = Rgba.Hex("a8744a");
        static readonly Rgba TrunkBase = Rgba.Hex("7a4e30");
        static readonly Rgba TrunkDark = Rgba.Hex("4e3020");

        static void DrawCanopyBlob(PixelCanvas c, double cx, double cy, double r)
        {
            c.Ellipse(cx, cy, r, r * 0.9, (nx, ny) =>
            {
                double l = nx * 0.6 + ny;
                if (l > 0.55) return CanopyDark;
                if (l < -0.75) return CanopyLight;
                return CanopyBase;
            });
        }

        static PixelCanvas DrawTree(int w, int h, int seed)
        {
            var c = new PixelCanvas(w, h);
            var rng = new Rng(seed);
            int trunkW = Math.Max(4, w / 8);
            int tx = w / 2 - trunkW / 2;
            int canopyBottom = (int)(h * 0.62);
            // Tronco e raízes.
            for (int y = canopyBottom - 6; y < h; y++)
            {
                int extra = y > h - 4 ? (h - 4 - y) * -1 : 0;
                for (int x = tx - extra; x < tx + trunkW + extra; x++)
                {
                    Rgba col = x < tx + 1 ? TrunkLight : (x >= tx + trunkW - 1 ? TrunkDark : TrunkBase);
                    if (Rng.Hash(x, y, seed) < 0.12) col = TrunkDark;
                    c.Set(x, y, col);
                }
            }
            c.Line(tx + 1, canopyBottom + 2, tx - 3, canopyBottom - 3, TrunkBase);
            c.Line(tx + trunkW - 2, canopyBottom + 4, tx + trunkW + 3, canopyBottom - 2, TrunkBase);
            // Copa formada por vários círculos.
            double cr = w * 0.28;
            var blobs = new List<(double, double, double)>
            {
                (w * 0.5, h * 0.22, cr * 1.05),
                (w * 0.3, h * 0.36, cr),
                (w * 0.7, h * 0.36, cr),
                (w * 0.5, h * 0.45, cr * 1.1),
                (w * 0.22, h * 0.52, cr * 0.7),
                (w * 0.78, h * 0.52, cr * 0.7),
            };
            foreach (var (bx, by, br) in blobs) DrawCanopyBlob(c, bx + rng.Range(-1.0, 1.0), by, br);
            // Sombra da copa por baixo e brilhos.
            for (int x = 0; x < w; x++)
            {
                for (int y = h - 1; y >= 0; y--)
                {
                    var col = c.Get(x, y);
                    if (col.Equals(CanopyBase) || col.Equals(CanopyLight) || col.Equals(CanopyDark))
                    {
                        c.Set(x, y, CanopyDeep);
                        if (c.Get(x, y - 1).A > 0) c.Set(x, y - 1, CanopyDark);
                        break;
                    }
                }
            }
            for (int i = 0; i < w / 4; i++)
            {
                int x = rng.Range(2, w - 2), y = rng.Range(2, (int)(h * 0.5));
                if (c.Get(x, y).Equals(CanopyBase)) c.Set(x, y, CanopyLight);
            }
            // Frutinhas brilhantes.
            for (int i = 0; i < 3; i++)
            {
                int x = rng.Range(4, w - 4), y = rng.Range(6, (int)(h * 0.5));
                if (c.Get(x, y).A > 0) { c.Set(x, y, Pal.ScarfLight); }
            }
            c.Outline(Rgba.Hex("163a26"));
            return c;
        }

        static PixelCanvas DrawBush(int seed)
        {
            var c = new PixelCanvas(32, 16);
            DrawCanopyBlob(c, 9, 10, 6.5);
            DrawCanopyBlob(c, 22, 10, 6.5);
            DrawCanopyBlob(c, 16, 7, 7);
            c.HLine(3, 28, 15, CanopyDeep);
            var rng = new Rng(seed);
            for (int i = 0; i < 3; i++) c.Set(rng.Range(6, 26), rng.Range(4, 11), seed % 2 == 0 ? Pal.Blush : Pal.ScarfLight);
            c.Outline(Rgba.Hex("163a26"));
            return c;
        }

        static PixelCanvas DrawFlower(Rgba petal, Rgba center, int stemHeight)
        {
            var c = new PixelCanvas(16, 16);
            int top = 15 - stemHeight;
            c.VLine(8, top + 1, 15, Pal.LeafDark);
            c.Set(9, 13, Pal.LeafBase);
            c.Set(10, 12, Pal.LeafBase);
            c.Set(7, top, petal);
            c.Set(9, top, petal);
            c.Set(8, top - 1, petal);
            c.Set(8, top + 1, petal);
            c.Set(8, top, center);
            c.Outline(Rgba.Hex("163a26"));
            return c;
        }

        static PixelCanvas DrawGrassTuft(Rgba light, Rgba dark)
        {
            var c = new PixelCanvas(16, 8);
            int[] heights = { 3, 5, 4, 6, 3, 5 };
            for (int i = 0; i < heights.Length; i++)
            {
                int x = 3 + i * 2;
                c.VLine(x, 8 - heights[i], 7, i % 2 == 0 ? light : dark);
            }
            return c;
        }

        static PixelCanvas DrawMushroom(Rgba cap, Rgba capDark, Rgba dots, bool glow)
        {
            var c = new PixelCanvas(16, 16);
            c.Rect(7, 10, 3, 6, Pal.OffWhite);
            c.Set(9, 11, Rgba.Hex("c8c0b0"));
            c.Ellipse(8.5, 10, 5.5, 4, (nx, ny) => ny > 0 ? Rgba.Clear : (nx > 0.4 ? capDark : cap));
            c.HLine(4, 13, 9, capDark);
            c.Set(6, 7, dots);
            c.Set(10, 8, dots);
            c.Set(8, 6, dots);
            c.Outline(Rgba.Hex("1e1a2e"));
            if (glow)
            {
                // Esporos brilhantes flutuando.
                c.Set(3, 3, cap.Lighter(0.5));
                c.Set(13, 2, cap.Lighter(0.5));
                c.Set(11, 4, cap.WithAlpha(160));
            }
            return c;
        }

        static PixelCanvas DrawRock(Rgba light, Rgba mid, Rgba dark)
        {
            var c = new PixelCanvas(16, 8);
            c.Ellipse(8, 8, 6, 5, (nx, ny) => ny > 0 ? Rgba.Clear : (nx + ny < -0.8 ? light : (nx > 0.4 ? dark : mid)));
            c.Outline(Rgba.Hex("1e1a2e"));
            return c;
        }

        static PixelCanvas DrawStalagmite(bool hanging)
        {
            var tc = ThemeColors.Get(Theme.Cave);
            var c = new PixelCanvas(16, 32);
            c.Triangle(2, 31, 8, 3, 14, 31, (x, y) => x < 7 ? tc.FillLight : (x < 10 ? tc.FillBase : tc.FillDark));
            c.Triangle(9, 31, 12, 18, 15, 31, tc.FillDark);
            c.Set(6, 20, tc.Accent);
            c.Set(5, 26, tc.Accent.Lighter(0.4));
            c.Outline(tc.Outline);
            if (!hanging) return c;
            var flipped = new PixelCanvas(16, 32);
            flipped.Blit(c, 0, 0, false, true);
            return flipped;
        }

        static PixelCanvas DrawCrystalCluster()
        {
            var c = new PixelCanvas(16, 16);
            Rgba[] cols = { Rgba.Hex("b98cff"), Rgba.Hex("8f5ae0"), Rgba.Hex("5ff0d8"), Rgba.Hex("2fa8a0") };
            c.Triangle(2, 15, 5, 5, 8, 15, (x, y) => x < 5 ? cols[0] : cols[1]);
            c.Triangle(6, 15, 9, 1, 12, 15, (x, y) => x < 9 ? cols[2] : cols[3]);
            c.Triangle(10, 15, 13, 8, 15, 15, (x, y) => x < 13 ? cols[0] : cols[1]);
            c.Set(8, 5, Pal.White);
            c.Outline(Rgba.Hex("120f26"));
            return c;
        }

        static PixelCanvas DrawPillar(int height, bool broken)
        {
            var tc = ThemeColors.Get(Theme.Ruins);
            var c = new PixelCanvas(16, height);
            int top = broken ? 6 : 3;
            c.Rect(1, height - 3, 14, 3, tc.FillDark);
            c.HLine(1, 14, height - 3, tc.FillLight);
            for (int y = top + 2; y < height - 3; y++)
            {
                c.Set(3, y, tc.FillLight);
                c.Rect(4, y, 2, 1, tc.FillBase);
                c.Set(6, y, tc.FillDark);
                c.Rect(7, y, 2, 1, tc.FillBase);
                c.Set(9, y, tc.FillDark);
                c.Rect(10, y, 2, 1, tc.FillBase);
                c.Set(12, y, tc.FillDark);
            }
            if (broken)
            {
                // Topo quebrado irregular.
                int[] jag = { 3, 1, 4, 2, 0, 3, 5, 2, 1, 4 };
                for (int x = 3; x <= 12; x++)
                    for (int y = top; y < top + 2 + jag[x - 3]; y++) c.Set(x, y, Rgba.Clear);
            }
            else
            {
                c.Rect(1, top - 2, 14, 4, tc.FillBase);
                c.HLine(1, 14, top - 2, tc.FillLight);
                c.HLine(1, 14, top + 1, tc.FillDark);
            }
            // Trepadeira.
            var rng = new Rng(height);
            int vx = 4;
            for (int y = top + 6; y < height - 4; y++)
            {
                vx += rng.Range(-1, 2);
                vx = Math.Max(3, Math.Min(12, vx));
                c.Set(vx, y, tc.TopBase);
                if (rng.Chance(0.3)) c.Set(vx + 1, y, tc.TopLight);
            }
            c.Outline(tc.Outline);
            return c;
        }

        static PixelCanvas DrawVine(int length)
        {
            var tc = ThemeColors.Get(Theme.Ruins);
            var c = new PixelCanvas(16, length);
            var rng = new Rng(length * 7);
            int x = 8;
            for (int y = 0; y < length - 1; y++)
            {
                if (y % 3 == 0) x += rng.Range(-1, 2);
                x = Math.Max(4, Math.Min(11, x));
                c.Set(x, y, tc.TopDark);
                if (y % 4 == 1) { c.Set(x - 1, y, tc.TopBase); c.Set(x - 2, y + 1, tc.TopLight); }
                if (y % 4 == 3) { c.Set(x + 1, y, tc.TopBase); c.Set(x + 2, y + 1, tc.TopLight); }
            }
            c.Set(x, length - 1, Pal.Blush);
            return c;
        }

        static PixelCanvas DrawRubble()
        {
            var tc = ThemeColors.Get(Theme.Ruins);
            var c = new PixelCanvas(16, 8);
            c.Rect(2, 4, 5, 4, tc.FillBase);
            c.HLine(2, 6, 4, tc.FillLight);
            c.Rect(8, 2, 6, 6, tc.FillDark.Lighter(0.1));
            c.HLine(8, 13, 2, tc.FillLight);
            c.Rect(6, 6, 3, 2, tc.FillBase);
            c.Outline(tc.Outline);
            return c;
        }

        static SheetAsset BuildDecorSheet()
        {
            var sb = new SheetBuilder(256);
            // Floresta.
            sb.Add("forest_tree_big", DrawTree(48, 64, 3), 0.5, 0.0);
            sb.Add("forest_tree_small", DrawTree(32, 44, 9), 0.5, 0.0);
            sb.Add("forest_bush_a", DrawBush(2), 0.5, 0.0);
            sb.Add("forest_bush_b", DrawBush(5), 0.5, 0.0);
            sb.Add("forest_flower_red", DrawFlower(Rgba.Hex("ff6b81"), Pal.ScarfLight, 4), 0.5, 0.0);
            sb.Add("forest_flower_blue", DrawFlower(Rgba.Hex("7ab8ff"), Pal.White, 5), 0.5, 0.0);
            sb.Add("forest_flower_yellow", DrawFlower(Pal.ScarfLight, Rgba.Hex("e08a2a"), 3), 0.5, 0.0);
            sb.Add("forest_grass", DrawGrassTuft(Rgba.Hex("9ef05a"), Rgba.Hex("4fb23f")), 0.5, 0.0);
            sb.Add("forest_mushroom", DrawMushroom(Rgba.Hex("ff5a5a"), Rgba.Hex("b82e3e"), Pal.White, false), 0.5, 0.0);
            sb.Add("forest_rock", DrawRock(Pal.StoneLight, Pal.StoneBase, Pal.StoneDark), 0.5, 0.0);
            // Caverna.
            sb.Add("cave_stalagmite", DrawStalagmite(false), 0.5, 0.0);
            sb.Add("cave_stalactite", DrawStalagmite(true), 0.5, 1.0);
            sb.Add("cave_mushroom", DrawMushroom(Rgba.Hex("5ff0d8"), Rgba.Hex("2fa8a0"), Pal.White, true), 0.5, 0.0);
            sb.Add("cave_crystals", DrawCrystalCluster(), 0.5, 0.0);
            sb.Add("cave_rock", DrawRock(Rgba.Hex("8a90c8"), Rgba.Hex("5a60a0"), Rgba.Hex("363a70")), 0.5, 0.0);
            sb.Add("cave_grass", DrawGrassTuft(Rgba.Hex("5ff0d8"), Rgba.Hex("2fa8a0")), 0.5, 0.0);
            // Ruínas.
            sb.Add("ruins_pillar_broken", DrawPillar(40, true), 0.5, 0.0);
            sb.Add("ruins_pillar_tall", DrawPillar(64, false), 0.5, 0.0);
            sb.Add("ruins_vine", DrawVine(24), 0.5, 1.0);
            sb.Add("ruins_grass", DrawGrassTuft(Rgba.Hex("a6e07a"), Rgba.Hex("6cbf5a")), 0.5, 0.0);
            sb.Add("ruins_rubble", DrawRubble(), 0.5, 0.0);
            sb.Add("ruins_flower", DrawFlower(Rgba.Hex("ffb0e0"), Pal.ScarfLight, 4), 0.5, 0.0);
            return sb.Build("Sprites/Decor/decor.png");
        }
    }
}
