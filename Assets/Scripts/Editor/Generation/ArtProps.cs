using System;
using System.Collections.Generic;

namespace Ecos.EditorTools.Generation
{
    /// <summary>Itens coletáveis, objetos de fase, armadilhas e efeitos visuais.</summary>
    public static partial class ArtLibrary
    {
        // ================================================================== CRISTAL DE ENERGIA

        static PixelCanvas DrawCrystal(double widthFactor, bool flipShade, Rgba white, Rgba light, Rgba mid, Rgba dark, Rgba deep, int size = 16, int sparkle = -1)
        {
            var c = new PixelCanvas(size, size);
            double s = size / 16.0;
            double cx = size / 2.0, top = 1.5 * s, midY = 6.0 * s, bottom = 14.5 * s;
            double hw = Math.Max(0.6, 4.6 * s * widthFactor);
            Rgba leftCol = flipShade ? mid : light;
            Rgba rightCol = flipShade ? light : mid;
            // Coroa (parte de cima).
            c.Triangle(cx - hw, midY, cx, top, cx, midY, leftCol);
            c.Triangle(cx, top, cx + hw, midY, cx, midY, rightCol);
            // Pavilhão (parte de baixo).
            c.Triangle(cx - hw, midY, cx, midY, cx, bottom, flipShade ? dark : mid);
            c.Triangle(cx, midY, cx + hw, midY, cx, bottom, flipShade ? deep : dark);
            // Faixa de brilho.
            if (hw > 1.5)
            {
                int my = (int)Math.Floor(midY);
                for (int x = (int)Math.Ceiling(cx - hw + 0.5); x < cx + hw - 0.5; x++)
                    if (c.Get(x, my).A > 0) c.Set(x, my, x < cx ? white : light);
                c.Set((int)Math.Floor(cx - hw * 0.45), (int)Math.Floor(midY - 2 * s), white);
            }
            c.Outline(Pal.Outline);
            if (sparkle >= 0)
            {
                int sx = (int)(cx + hw * 0.6) + 1, sy = (int)(top + 2 * s);
                if (sparkle == 1)
                {
                    c.Set(sx, sy, Pal.White);
                    c.Set(sx - 1, sy, white);
                    c.Set(sx + 1, sy, white);
                    c.Set(sx, sy - 1, white);
                    c.Set(sx, sy + 1, white);
                }
                else if (sparkle == 2) c.Set(sx, sy, Pal.White);
            }
            return c;
        }

        static List<PixelCanvas> CrystalFrames()
        {
            double[] w = { 1.0, 0.75, 0.4, 0.2, 0.4, 0.75 };
            var frames = new List<PixelCanvas>();
            for (int i = 0; i < w.Length; i++)
                frames.Add(DrawCrystal(w[i], i >= 3, Pal.CrystalWhite, Pal.CrystalLight, Pal.CrystalBase, Pal.CrystalDark, Pal.CrystalDeep, 16,
                    i == 0 ? 1 : i == 1 ? 2 : -1));
            return frames;
        }

        static List<PixelCanvas> FinalCrystalFrames()
        {
            var frames = new List<PixelCanvas>();
            for (int i = 0; i < 6; i++)
            {
                var c = new PixelCanvas(32, 32);
                // Aura pulsante.
                double r = 13 + Math.Sin(i / 6.0 * Math.PI * 2) * 1.5;
                c.Ellipse(16, 16, r, r, (nx, ny) =>
                {
                    double d = Math.Sqrt(nx * nx + ny * ny);
                    if (d < 0.7) return Rgba.Clear;
                    return Pal.Magic.WithAlpha((byte)(d > 0.9 ? 60 : 110));
                });
                var gem = DrawCrystal(1.0, false, Pal.GoldWhite, Pal.GoldLight, Pal.GoldBase, Pal.GoldDark, Pal.GoldDark.Darker(0.3), 24, -1);
                c.Blit(gem, 4, 3);
                // Faíscas girando ao redor.
                for (int k = 0; k < 4; k++)
                {
                    double a = (i / 6.0 + k / 4.0) * Math.PI * 2;
                    int sx = (int)Math.Round(16 + Math.Cos(a) * 13), sy = (int)Math.Round(16 + Math.Sin(a) * 13);
                    c.Set(sx, sy, Pal.White);
                    c.Set(sx + 1, sy, Pal.GoldLight);
                    c.Set(sx - 1, sy, Pal.GoldLight);
                    c.Set(sx, sy + 1, Pal.GoldLight);
                    c.Set(sx, sy - 1, Pal.GoldLight);
                }
                frames.Add(c);
            }
            return frames;
        }

        // ================================================================== CORAÇÃO

        static PixelCanvas DrawHeart(double scale, int size = 16, bool empty = false)
        {
            var c = new PixelCanvas(size, size);
            double cx = size / 2.0, cy = size / 2.0;
            double r = 3.1 * scale * size / 16.0;
            Rgba light = empty ? Rgba.Hex("6a6480") : Pal.HeartLight;
            Rgba mid = empty ? Rgba.Hex("4a4560") : Pal.HeartBase;
            Rgba dark = empty ? Rgba.Hex("332f45") : Pal.HeartDark;
            Func<double, double, Rgba> shade = (nx, ny) => ny > 0.3 ? dark : mid;
            c.Ellipse(cx - r * 0.95, cy - r * 0.45, r, r, shade);
            c.Ellipse(cx + r * 0.95, cy - r * 0.45, r, r, (nx, ny) => nx > 0.3 || ny > 0.3 ? dark : mid);
            c.Triangle(cx - r * 1.95, cy - r * 0.2, cx + r * 1.95, cy - r * 0.2, cx, cy + r * 2.0, (x, y) => x > cx ? dark : mid);
            c.Set((int)(cx - r * 1.2), (int)(cy - r * 0.8), light);
            c.Set((int)(cx - r * 1.2) + 1, (int)(cy - r * 0.8), light);
            c.Set((int)(cx - r * 1.2), (int)(cy - r * 0.8) + 1, light);
            if (!empty) c.Set((int)(cx - r * 1.2), (int)(cy - r * 0.8) - 1, Pal.White);
            c.Outline(Pal.Outline);
            return c;
        }

        // ================================================================== CHECKPOINT (totem)

        static PixelCanvas DrawCheckpoint(bool active, int frame)
        {
            var c = new PixelCanvas(16, 32);
            // Pedestal.
            c.Rect(3, 28, 10, 4, Pal.StoneDark);
            c.HLine(3, 12, 28, Pal.StoneBase);
            // Pilar com runa.
            for (int y = 12; y < 28; y++)
            {
                c.Set(5, y, Pal.StoneLight);
                c.Rect(6, y, 4, 1, Pal.StoneBase);
                c.Set(10, y, Pal.StoneDark);
            }
            Rgba rune = active ? Pal.CrystalLight : Pal.StoneDark;
            c.VLine(8, 15, 24, rune);
            c.HLine(7, 9, 18, rune);
            c.HLine(7, 9, 22, rune);
            // Suporte do cristal.
            c.Rect(4, 10, 8, 2, Pal.StoneBase);
            c.HLine(4, 11, 10, Pal.StoneLight);
            c.Set(4, 9, Pal.StoneBase);
            c.Set(11, 9, Pal.StoneBase);
            // Cristal.
            Rgba g1 = active ? Pal.LeafLight : Rgba.Hex("8a8aa0");
            Rgba g2 = active ? Pal.LumoBase : Rgba.Hex("5f5f78");
            Rgba g3 = active ? Pal.LumoDeep : Rgba.Hex("3f3f55");
            int bob = active && (frame % 2 == 1) ? -1 : 0;
            c.Triangle(5, 6 + bob, 8, 1 + bob, 8, 6 + bob, g1);
            c.Triangle(8, 1 + bob, 11, 6 + bob, 8, 6 + bob, g2);
            c.Triangle(5, 6 + bob, 8, 6 + bob, 8, 10 + bob, g2);
            c.Triangle(8, 6 + bob, 11, 6 + bob, 8, 10 + bob, g3);
            c.Outline(Pal.Outline);
            if (active)
            {
                // Brilho cintilante.
                var glow = Pal.LeafLight.WithAlpha(150);
                int r = frame % 2 == 0 ? 1 : 2;
                c.Blend(8, 0, glow);
                c.Blend(2 - (r - 1), 5 + bob, glow);
                c.Blend(13 + (r - 1), 5 + bob, glow);
                if (frame == 1 || frame == 3) c.Set(12, 2, Pal.White);
                if (frame == 0 || frame == 2) c.Set(3, 3, Pal.White);
            }
            return c;
        }

        // ================================================================== PORTAL

        static PixelCanvas DrawPortal(int frame)
        {
            var c = new PixelCanvas(32, 40);
            double cx = 16, cy = 19;
            // Interior translúcido.
            c.Ellipse(cx, cy, 9.5, 15.5, (nx, ny) =>
            {
                double d = Math.Sqrt(nx * nx + ny * ny);
                double swirl = Math.Sin(Math.Atan2(ny, nx) * 3 + d * 8 - frame * Math.PI / 3);
                Rgba a = Rgba.Lerp(Rgba.Hex("2a1a5e"), Rgba.Hex("49d6c8"), d);
                if (swirl > 0.6) a = Rgba.Lerp(a, Rgba.Hex("c8fff4"), 0.5);
                return a.WithAlpha((byte)(170 + 60 * d));
            });
            // Anel externo.
            c.Ring(cx, cy, 11.5, 17.5, 2.2, ang =>
            {
                double t = Math.Sin(ang * 2 + frame * Math.PI / 3);
                return t > 0.3 ? Rgba.Hex("c8fff4") : (t > -0.4 ? Rgba.Hex("5ff0d8") : Rgba.Hex("2fa8a0"));
            });
            // Faíscas orbitando.
            for (int k = 0; k < 3; k++)
            {
                double a = frame / 6.0 * Math.PI * 2 + k * Math.PI * 2 / 3;
                int sx = (int)Math.Round(cx + Math.Cos(a) * 13), sy = (int)Math.Round(cy + Math.Sin(a) * 19);
                c.Set(sx, sy, Pal.White);
                c.Set(sx, sy - 1, Pal.CrystalLight);
            }
            // Base de pedra.
            c.Rect(5, 36, 22, 4, Pal.StoneDark);
            c.HLine(5, 26, 36, Pal.StoneLight);
            c.Rect(8, 35, 16, 1, Pal.StoneBase);
            var final = new PixelCanvas(32, 40);
            final.Blit(c, 0, 0);
            final.Outline(Pal.Outline);
            return final;
        }

        // ================================================================== ARMADILHAS

        static PixelCanvas DrawSpikes()
        {
            var c = new PixelCanvas(16, 16);
            for (int i = 0; i < 4; i++)
            {
                double x0 = i * 4 + 0.2;
                c.Triangle(x0, 14, x0 + 1.8, 6, x0 + 3.6, 14, (x, y) => x < x0 + 1.8 ? Pal.MetalLight : (x < x0 + 2.6 ? Pal.MetalBase : Pal.MetalDark));
            }
            c.Rect(0, 13, 16, 3, Pal.MetalDark);
            c.HLine(0, 15, 13, Pal.MetalBase);
            var o = new PixelCanvas(16, 16);
            o.Blit(c, 0, 0);
            o.Outline(Pal.Outline);
            // O contorno lateral deve tocar a borda do tile.
            return o;
        }

        static PixelCanvas DrawSaw()
        {
            var c = new PixelCanvas(16, 16);
            for (int k = 0; k < 8; k++)
            {
                double a = k * Math.PI / 4;
                double bx = 8 + Math.Cos(a) * 3.5, by = 8 + Math.Sin(a) * 3.5;
                double tx = 8 + Math.Cos(a) * 7.3, ty = 8 + Math.Sin(a) * 7.3;
                double px = -Math.Sin(a) * 1.7, py = Math.Cos(a) * 1.7;
                c.Triangle(bx - px, by - py, tx, ty, bx + px, by + py, Pal.MetalBase);
            }
            c.Circle(8, 8, 4.6, Pal.SlimeDeep);
            c.Ellipse(8, 8, 4.6, 4.6, (nx, ny) => nx + ny < -0.7 ? Pal.SlimeBase : (nx + ny > 0.8 ? Pal.Outline.Lighter(0.1) : Pal.SlimeDeep));
            c.Circle(8, 8, 1.8, Pal.EvilRed);
            c.Set(7, 7, Pal.EvilGlow);
            c.Outline(Pal.Outline);
            return c;
        }

        static PixelCanvas DrawMoverPlatform()
        {
            var c = new PixelCanvas(48, 10);
            c.Rect(1, 1, 46, 7, Pal.StoneBase);
            c.HLine(1, 46, 1, Pal.StoneLight);
            c.HLine(1, 46, 2, Pal.GoldLight);
            c.HLine(1, 46, 7, Pal.StoneDark);
            c.HLine(1, 46, 6, Pal.StoneDark.Lighter(0.1));
            for (int x = 6; x < 46; x += 12)
            {
                c.Set(x, 4, Pal.CrystalLight);
                c.Set(x + 1, 4, Pal.CrystalBase);
                c.Set(x + 1, 3, Pal.CrystalWhite);
            }
            c.Rect(20, 8, 8, 1, Pal.StoneDark);
            c.Set(23, 9, Pal.CrystalBase);
            c.Set(24, 9, Pal.CrystalBase);
            c.Outline(Pal.Outline);
            return c;
        }

        static PixelCanvas DrawCrumbleBlock()
        {
            var tc = ThemeColors.Get(Theme.Ruins);
            var c = new PixelCanvas(16, 16);
            c.Rect(1, 1, 14, 14, tc.FillBase);
            c.HLine(1, 14, 1, tc.FillLight.Lighter(0.2));
            c.HLine(1, 14, 2, tc.FillLight);
            c.HLine(1, 14, 14, tc.FillDark);
            c.VLine(14, 1, 14, tc.FillDark);
            // Rachaduras.
            c.Line(4, 3, 7, 7, tc.FillDeep);
            c.Line(7, 7, 6, 11, tc.FillDeep);
            c.Line(7, 7, 11, 9, tc.FillDeep);
            c.Line(11, 9, 12, 13, tc.FillDeep);
            c.Set(3, 12, tc.FillDeep);
            c.Set(9, 4, tc.FillDark);
            c.Outline(tc.Outline);
            for (int i = 0; i < 16; i++)
            {
                c.Set(i, 0, tc.Outline);
                c.Set(i, 15, tc.Outline);
                c.Set(0, i, tc.Outline);
                c.Set(15, i, tc.Outline);
            }
            return c;
        }

        static PixelCanvas DrawSign()
        {
            var c = new PixelCanvas(16, 16);
            c.Rect(7, 9, 2, 7, Pal.WoodDark);
            c.Set(7, 9, Pal.WoodBase);
            c.Rect(1, 1, 14, 8, Pal.WoodBase);
            c.HLine(1, 14, 1, Pal.WoodLight);
            c.HLine(1, 14, 8, Pal.WoodDark);
            c.HLine(3, 12, 3, Pal.WoodDeep);
            c.HLine(3, 9, 5, Pal.WoodDeep);
            c.Set(12, 5, Pal.LeafBase);
            c.Set(11, 6, Pal.LeafDark);
            c.Outline(Rgba.Hex("2a1a1e"));
            return c;
        }

        // ================================================================== EFEITOS

        static List<PixelCanvas> SparkleFrames()
        {
            var frames = new List<PixelCanvas>();
            for (int f = 0; f < 5; f++)
            {
                var c = new PixelCanvas(16, 16);
                int len = new[] { 2, 4, 6, 7, 7 }[f];
                byte alpha = (byte)new[] { 255, 255, 230, 160, 80 }[f];
                var bright = Pal.White.WithAlpha(alpha);
                var tint = Pal.CrystalLight.WithAlpha(alpha);
                int start = f >= 3 ? f - 1 : 0;
                for (int i = start; i <= len; i++)
                {
                    var col = i < len - 1 ? bright : tint;
                    c.Set(8 + i, 8, col);
                    c.Set(7 - i, 8, col);
                    c.Set(8, 8 + i, col);
                    c.Set(8, 7 - i, col);
                }
                int d = new[] { 1, 2, 4, 5, 6 }[f];
                c.Set(8 + d, 8 + d, tint);
                c.Set(7 - d, 8 + d, tint);
                c.Set(8 + d, 7 - d, tint);
                c.Set(7 - d, 7 - d, tint);
                if (f < 2) c.Rect(7, 7, 2, 2, bright);
                frames.Add(c);
            }
            return frames;
        }

        static List<PixelCanvas> PoofFrames()
        {
            var frames = new List<PixelCanvas>();
            double[] radius = { 2.0, 3.2, 4.2, 4.8, 5.0 };
            byte[] alpha = { 255, 240, 200, 140, 70 };
            for (int f = 0; f < 5; f++)
            {
                var c = new PixelCanvas(16, 16);
                for (int k = 0; k < 5; k++)
                {
                    double a = k * Math.PI * 2 / 5 + 0.4;
                    double dist = f * 1.1;
                    double r = radius[f] * (k % 2 == 0 ? 0.55 : 0.45);
                    double px = 8 + Math.Cos(a) * dist, py = 8 + Math.Sin(a) * dist;
                    c.Ellipse(px, py, r, r, (nx, ny) => (nx + ny < -0.4 ? Pal.White : Rgba.Hex("d8d4e8")).WithAlpha(alpha[f]));
                }
                if (f < 2) c.Circle(8, 8, radius[f] * 0.6, Pal.White.WithAlpha(alpha[f]));
                frames.Add(c);
            }
            return frames;
        }

        static SheetAsset BuildItemSheet()
        {
            var sb = new SheetBuilder(256);
            sb.AddFrames("crystal", CrystalFrames(), 0.5, 0.5);
            sb.AddFrames("heart", new List<PixelCanvas> { DrawHeart(1.0), DrawHeart(1.12), DrawHeart(1.0), DrawHeart(0.92) }, 0.5, 0.5);
            sb.AddFrames("final_crystal", FinalCrystalFrames(), 0.5, 0.5);
            return sb.Build("Sprites/Items/items.png");
        }

        static SheetAsset BuildLevelObjectSheet()
        {
            var sb = new SheetBuilder(256);
            sb.Add("checkpoint_off_0", DrawCheckpoint(false, 0), 0.5, 0.0);
            for (int i = 0; i < 4; i++) sb.Add("checkpoint_on_" + i, DrawCheckpoint(true, i), 0.5, 0.0);
            for (int i = 0; i < 6; i++) sb.Add("portal_" + i, DrawPortal(i), 0.5, 0.0);
            sb.Add("spikes", DrawSpikes(), 0.5, 0.0);
            sb.Add("saw", DrawSaw(), 0.5, 0.5);
            sb.Add("mover_platform", DrawMoverPlatform(), 0.5, 1.0);
            sb.Add("crumble_block", DrawCrumbleBlock(), 0.5, 0.5);
            sb.Add("sign", DrawSign(), 0.5, 0.0);
            return sb.Build("Sprites/Level/level_objects.png");
        }

        static SheetAsset BuildEffectSheet()
        {
            var sb = new SheetBuilder(128);
            sb.AddFrames("sparkle", SparkleFrames(), 0.5, 0.5);
            sb.AddFrames("poof", PoofFrames(), 0.5, 0.5);
            var px = new PixelCanvas(4, 4);
            px.Fill(Pal.White);
            sb.Add("pixel", px, 0.5, 0.5);
            return sb.Build("Sprites/Effects/effects.png");
        }
    }
}
