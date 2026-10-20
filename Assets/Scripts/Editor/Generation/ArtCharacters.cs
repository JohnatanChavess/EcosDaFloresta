using System;
using System.Collections.Generic;

namespace Ecos.EditorTools.Generation
{
    /// <summary>Sprites do personagem principal e dos inimigos (desenhados proceduralmente, quadro a quadro).</summary>
    public static partial class ArtLibrary
    {
        // ================================================================== LUMO

        public enum LumoEyes { Open, Blink, Hurt, Wide }

        public class LumoPose
        {
            public int Bob;
            public double Squash;     // >0 achata, <0 estica
            public int Lean;
            public int FootLX, FootLY, FootRX, FootRY;
            public int LeafTilt;
            public int ScarfPhase;
            public int ScarfLength = 2;
            public LumoEyes Eyes = LumoEyes.Open;
            public bool Back;
            public int ArmPhase;
            public bool ArmsUp;
        }

        public static PixelCanvas DrawLumo(LumoPose p)
        {
            var c = new PixelCanvas(16, 16);
            double rx = 4.6 + p.Squash * 0.8;
            double ry = 3.9 - p.Squash * 0.8;
            double bottom = 13.0 + p.Bob;
            double cx = 8.0 + p.Lean;
            double cy = bottom - ry;

            // Pés (desenhados antes do corpo para ficarem por trás).
            DrawFoot(c, 5 + p.FootLX, 13 + p.FootLY, Pal.LumoDeep);
            DrawFoot(c, 9 + p.FootRX, 13 + p.FootRY, Pal.LumoDeep);

            // Rabo do cachecol (atrás do corpo).
            int scarfRow = (int)Math.Round(cy + ry * 0.62);
            if (!p.Back)
            {
                int left = (int)Math.Floor(cx - rx) + 1;
                for (int i = 1; i <= p.ScarfLength; i++)
                {
                    int wave = ((p.ScarfPhase + i) % 4) < 2 ? 0 : -1;
                    c.Set(left - i, scarfRow + wave, i == p.ScarfLength ? Pal.ScarfDark : Pal.ScarfBase);
                    if (i == 1) c.Set(left - i, scarfRow + wave + 1, Pal.ScarfDark);
                }
            }

            // Braços (escalada / pulo).
            if (p.Back)
            {
                int up = p.ArmPhase == 0 ? 1 : 0;
                c.Rect((int)(cx - rx) - 1, (int)(cy - 2 - 2 * up), 2, 2, Pal.LumoBase);
                c.Rect((int)(cx + rx) - 1, (int)(cy - 2 - 2 * (1 - up)), 2, 2, Pal.LumoBase);
            }
            else if (p.ArmsUp)
            {
                c.Rect((int)(cx - rx) - 1, (int)cy - 3, 2, 2, Pal.LumoBase);
                c.Rect((int)(cx + rx), (int)cy - 3, 2, 2, Pal.LumoBase);
            }

            // Corpo com sombreamento (luz vindo de cima à esquerda).
            c.Ellipse(cx, cy, rx, ry, (nx, ny) =>
            {
                if (p.Back)
                {
                    if (ny > 0.45) return Pal.LumoDark;
                    if (nx + ny < -0.9) return Pal.LumoLight;
                    return Pal.LumoBase;
                }
                if (ny > 0.55 || (nx > 0.55 && ny > 0.15)) return Pal.LumoDark;
                if (nx + ny < -0.85) return Pal.LumoLight;
                return Pal.LumoBase;
            });
            c.Set((int)Math.Floor(cx - rx * 0.5), (int)Math.Floor(cy - ry * 0.55), Pal.White);

            // Cachecol.
            int x0 = (int)Math.Ceiling(cx - rx + 0.4), x1 = (int)Math.Floor(cx + rx - 0.4);
            for (int x = x0; x <= x1; x++)
            {
                double nx = (x + 0.5 - cx) / rx;
                double ny = (scarfRow + 0.5 - cy) / ry;
                if (nx * nx + ny * ny <= 1.05)
                    c.Set(x, scarfRow, x == x1 || x == x1 - 1 ? Pal.ScarfDark : (x == x0 + 1 ? Pal.ScarfLight : Pal.ScarfBase));
            }
            if (p.Back)
            {
                int mid = (int)Math.Round(cx) - 1;
                c.Set(mid, scarfRow + 1, Pal.ScarfBase);
                c.Set(mid, scarfRow + 2, Pal.ScarfDark);
            }

            // Rosto.
            if (!p.Back)
            {
                int ex = (int)Math.Round(cx) - 2 + (p.Lean > 0 ? 1 : 0);
                int ey = (int)Math.Round(cy) - 2;
                DrawEye(c, ex, ey, p.Eyes);
                DrawEye(c, ex + 3, ey, p.Eyes);
                if (p.Eyes == LumoEyes.Hurt) c.Set(ex + 2, ey + 3, Pal.Outline);
                else c.Set(ex + 2, ey + 3, Pal.LumoDeep);
                c.Set(ex - 1, ey + 3, Pal.Blush);
                if (c.Get(ex + 5, ey + 3).A > 0) c.Set(ex + 5, ey + 3, Pal.Blush);
            }

            // Broto de folhas no topo.
            int top = (int)Math.Floor(cy - ry + 0.5);
            int sx = (int)Math.Round(cx) - 1;
            c.Set(sx, top - 1, Pal.LeafDark);
            c.Set(sx, top - 2, Pal.LeafDark);
            var leaf = Pal.Map(('L', Pal.LeafLight), ('G', Pal.LeafBase), ('D', Pal.LeafDark));
            c.Map(new[] { ".LG.GL.", "GGD.DGG" }, leaf, sx - 3 + p.LeafTilt, top - 4);

            c.Outline(Pal.Outline);
            return c;
        }

        static void DrawFoot(PixelCanvas c, int x, int y, Rgba color)
        {
            c.Rect(x, y, 2, 2, color);
            c.Set(x, y, color.Lighter(0.2));
        }

        static void DrawEye(PixelCanvas c, int x, int y, LumoEyes eyes)
        {
            switch (eyes)
            {
                case LumoEyes.Blink:
                    c.HLine(x, x + 1, y + 2, Pal.Outline);
                    break;
                case LumoEyes.Hurt:
                    c.Set(x, y + 1, Pal.Outline);
                    c.Set(x + 1, y + 2, Pal.Outline);
                    c.Set(x, y + 2, Pal.Outline);
                    break;
                case LumoEyes.Wide:
                    c.Rect(x, y, 2, 3, Pal.White);
                    c.Set(x + 1, y + 1, Pal.Outline);
                    break;
                default:
                    c.Rect(x, y, 2, 3, Pal.White);
                    c.Set(x + 1, y + 1, Pal.Outline);
                    c.Set(x + 1, y + 2, Pal.Outline);
                    break;
            }
        }

        static List<PixelCanvas> LumoIdle()
        {
            return new List<PixelCanvas>
            {
                DrawLumo(new LumoPose()),
                DrawLumo(new LumoPose { LeafTilt = 1, ScarfPhase = 1 }),
                DrawLumo(new LumoPose { Bob = 0, Squash = 0.35, LeafTilt = 1, ScarfPhase = 2 }),
                DrawLumo(new LumoPose { Squash = 0.35, ScarfPhase = 3 }),
                DrawLumo(new LumoPose { LeafTilt = -1, ScarfPhase = 0 }),
                DrawLumo(new LumoPose { Eyes = LumoEyes.Blink, ScarfPhase = 1 }),
            };
        }

        static List<PixelCanvas> LumoWalk()
        {
            int[] lx = { -1, 0, 1, 1, 0, -1 };
            int[] ly = { 0, 0, 0, -1, -1, 0 };
            int[] bob = { 0, -1, 0, 0, -1, 0 };
            var frames = new List<PixelCanvas>();
            for (int i = 0; i < 6; i++)
            {
                int j = (i + 3) % 6;
                frames.Add(DrawLumo(new LumoPose
                {
                    Bob = bob[i], FootLX = lx[i], FootLY = ly[i], FootRX = lx[j], FootRY = ly[j],
                    LeafTilt = bob[i] < 0 ? -1 : 0, ScarfPhase = i, ScarfLength = 2
                }));
            }
            return frames;
        }

        static List<PixelCanvas> LumoRun()
        {
            int[] lx = { -2, -1, 1, 2, 1, -1 };
            int[] ly = { 0, 0, 0, -1, -2, -1 };
            int[] bob = { 0, -1, -1, 0, -1, -1 };
            var frames = new List<PixelCanvas>();
            for (int i = 0; i < 6; i++)
            {
                int j = (i + 3) % 6;
                frames.Add(DrawLumo(new LumoPose
                {
                    Bob = bob[i], Lean = 1, Squash = -0.15, FootLX = lx[i], FootLY = ly[i], FootRX = lx[j], FootRY = ly[j],
                    LeafTilt = -1, ScarfPhase = i, ScarfLength = 3
                }));
            }
            return frames;
        }

        static PixelCanvas LumoJump() => DrawLumo(new LumoPose
        {
            Bob = -1, Squash = -0.45, FootLX = 0, FootLY = -1, FootRX = 0, FootRY = 0, LeafTilt = -1, ScarfPhase = 2, ScarfLength = 3, ArmsUp = true
        });

        static PixelCanvas LumoFall() => DrawLumo(new LumoPose
        {
            Bob = -1, Squash = -0.2, FootLX = -1, FootLY = 0, FootRX = 1, FootRY = 0, LeafTilt = 1, ScarfPhase = 0, ScarfLength = 2, Eyes = LumoEyes.Wide, ArmsUp = true
        });

        static List<PixelCanvas> LumoClimb()
        {
            return new List<PixelCanvas>
            {
                DrawLumo(new LumoPose { Back = true, ArmPhase = 0, FootLY = -1, FootRY = 0 }),
                DrawLumo(new LumoPose { Back = true, ArmPhase = 0, Bob = -1, FootLY = -1, FootRY = -1 }),
                DrawLumo(new LumoPose { Back = true, ArmPhase = 1, FootLY = 0, FootRY = -1 }),
                DrawLumo(new LumoPose { Back = true, ArmPhase = 1, Bob = -1, FootLY = -1, FootRY = -1 }),
            };
        }

        static PixelCanvas LumoHurt() => DrawLumo(new LumoPose
        {
            Squash = 0.5, LeafTilt = 1, Eyes = LumoEyes.Hurt, FootLX = -1, FootRX = 1, ScarfPhase = 1, ScarfLength = 1
        });

        static SheetAsset BuildLumoSheet()
        {
            const double pivotY = 1.0 / 16.0;
            var sb = new SheetBuilder(128);
            sb.AddFrames("lumo_idle", LumoIdle(), 0.5, pivotY);
            sb.AddFrames("lumo_walk", LumoWalk(), 0.5, pivotY);
            sb.AddFrames("lumo_run", LumoRun(), 0.5, pivotY);
            sb.Add("lumo_jump_0", LumoJump(), 0.5, pivotY);
            sb.Add("lumo_fall_0", LumoFall(), 0.5, pivotY);
            sb.AddFrames("lumo_climb", LumoClimb(), 0.5, pivotY);
            sb.Add("lumo_hurt_0", LumoHurt(), 0.5, pivotY);
            return sb.Build("Sprites/Characters/lumo.png");
        }

        // ================================================================== LODO SOMBRIO (slime)

        static PixelCanvas DrawSlime(double rx, double ry, int eyeOffsetY, int drip)
        {
            var c = new PixelCanvas(16, 16);
            double cx = 8, bottom = 14.0;
            double cy = bottom;
            c.Ellipse(cx, cy, rx, ry * 2, (nx, ny) =>
            {
                if (ny > 0) return Rgba.Clear;
                if (ny > -0.18) return Pal.SlimeDark;
                if (nx + ny < -1.05) return Pal.SlimeLight;
                if (nx > 0.6) return Pal.SlimeDark;
                return Pal.SlimeBase;
            });
            // Base achatada.
            c.HLine((int)(cx - rx + 1), (int)(cx + rx - 1), 13, Pal.SlimeDark);
            // Brilho.
            int topY = (int)Math.Ceiling(bottom - ry * 2);
            c.Set((int)(cx - rx * 0.45), topY + 2, Pal.White);
            c.Set((int)(cx - rx * 0.45) + 1, topY + 2, Pal.SlimeLight);
            // Manchas corrompidas.
            c.Set((int)cx - 3, 11, Pal.SlimeDeep);
            c.Set((int)cx + 2, 12, Pal.SlimeDeep);
            if (drip > 0) c.Set((int)cx - 1, 14, Pal.SlimeDark);
            // Olhos zangados (olhando para a direita).
            int ey = topY + 3 + eyeOffsetY;
            int ex = (int)cx;
            c.Rect(ex, ey, 2, 2, Pal.White);
            c.Rect(ex + 3, ey, 2, 2, Pal.White);
            c.Set(ex + 1, ey + 1, Pal.EvilRed);
            c.Set(ex + 4, ey + 1, Pal.EvilRed);
            c.Set(ex, ey - 1, Pal.SlimeDeep);
            c.Set(ex + 1, ey, Pal.SlimeDeep);
            c.Set(ex + 4, ey - 1, Pal.SlimeDeep);
            c.Set(ex + 3, ey, Pal.SlimeDeep);
            c.Outline(Pal.Outline);
            return c;
        }

        static List<PixelCanvas> SlimeFrames() => new List<PixelCanvas>
        {
            DrawSlime(6.0, 4.6, 0, 0),
            DrawSlime(6.6, 4.1, 1, 1),
            DrawSlime(7.0, 3.7, 1, 1),
            DrawSlime(5.6, 5.0, 0, 0),
        };

        // ================================================================== MORCEGO SOMBRIO

        static PixelCanvas DrawBat(int wing)
        {
            var c = new PixelCanvas(16, 16);
            double cx = 8, cy = 8;
            // Asas: 0 = alto, 1 = meio, 2 = baixo, 3 = meio.
            int tipY = wing == 0 ? 2 : wing == 2 ? 13 : 7;
            int midY = wing == 0 ? 5 : wing == 2 ? 11 : 8;
            Func<int, int, Rgba> wingShade = (x, y) => y > midY ? Pal.BatDark : Pal.BatWing;
            c.Triangle(cx - 2, cy - 1, 0.5, tipY, cx - 2, cy + 2, wingShade);
            c.Triangle(cx - 2, cy + 2, 0.5, tipY, 3.5, midY + 2, wingShade);
            c.Triangle(cx + 2, cy - 1, 15.5, tipY, cx + 2, cy + 2, wingShade);
            c.Triangle(cx + 2, cy + 2, 15.5, tipY, 12.5, midY + 2, wingShade);
            // Orelhas.
            c.Set(6, 4, Pal.BatBase);
            c.Set(6, 5, Pal.BatBase);
            c.Set(9, 4, Pal.BatBase);
            c.Set(9, 5, Pal.BatBase);
            // Corpo.
            c.Ellipse(cx, cy, 3.2, 3.2, (nx, ny) => nx + ny < -0.8 ? Pal.BatLight : (ny > 0.5 ? Pal.BatDark : Pal.BatBase));
            // Olhos e presas.
            c.Set(7, 7, Pal.EvilRed);
            c.Set(9, 7, Pal.EvilRed);
            c.Set(7, 6, Pal.EvilGlow);
            c.Set(8, 10, Pal.Bone);
            c.Outline(Pal.Outline);
            return c;
        }

        static List<PixelCanvas> BatFrames() => new List<PixelCanvas> { DrawBat(0), DrawBat(1), DrawBat(2), DrawBat(1) };

        // ================================================================== ESPINHENTO (besouro de espinhos)

        static PixelCanvas DrawThornback(int step)
        {
            var c = new PixelCanvas(16, 16);
            // Patas.
            int[] legX = { 4, 7, 10 };
            for (int i = 0; i < 3; i++)
            {
                int lift = ((i + step) % 2 == 0) ? 0 : 1;
                int dx = ((i + step) % 2 == 0) ? 0 : 1;
                c.VLine(legX[i] + dx, 12, 14 - lift, Pal.Outline.Lighter(0.15));
            }
            // Espinhos no casco (atrás do casco, apontando para fora da cúpula).
            int bob = step % 2;
            double[] angles = { -2.5, -1.95, -1.4, -0.85 };
            foreach (double a in angles)
            {
                double bx = 6.5 + Math.Cos(a) * 4.2, by = 12.5 + Math.Sin(a) * 4.6;
                double tx = 6.5 + Math.Cos(a) * (8.2 + bob * 0.4), ty = 12.5 + Math.Sin(a) * (8.4 + bob * 0.4);
                double px = -Math.Sin(a) * 1.6, py = Math.Cos(a) * 1.6;
                c.Triangle(bx - px, by - py, tx, ty, bx + px, by + py, (x, y) => (x - tx) * py - (y - ty) * px > 0 ? Pal.BoneDark : Pal.Bone);
            }
            // Casco em cúpula.
            c.Ellipse(6.5, 12.5, 5.6, 5.4, (nx, ny) =>
            {
                if (ny > 0.05) return Rgba.Clear;
                if (ny > -0.2) return Pal.ThornDark;
                if (nx + ny < -0.95) return Pal.ThornLight;
                if (nx > 0.55) return Pal.ThornDark;
                return Pal.ThornBase;
            });
            c.Set(4, 9, Pal.ThornLight.Lighter(0.4));
            c.HLine(2, 11, 12, Pal.ThornDark);
            // Cabeça.
            c.Ellipse(12.6, 11.2, 2.4, 2.2, (nx, ny) => ny > 0.4 ? Pal.ThornDark : Pal.ThornBase.Darker(0.3));
            c.Set(13, 10, Pal.EyeYellow);
            c.Set(14, 10, Pal.EyeYellow);
            c.Set(14, 12, Pal.Bone);
            c.Outline(Pal.Outline);
            return c;
        }

        static List<PixelCanvas> ThornFrames() => new List<PixelCanvas>
        {
            DrawThornback(0), DrawThornback(1), DrawThornback(0), DrawThornback(1)
        };

        static SheetAsset BuildEnemySheet()
        {
            const double pivotY = 1.0 / 16.0;
            var sb = new SheetBuilder(128);
            sb.AddFrames("slime", SlimeFrames(), 0.5, pivotY);
            sb.AddFrames("bat", BatFrames(), 0.5, 0.5);
            var thorn = ThornFrames();
            // Os quadros 2 e 3 alternam a postura dos espinhos.
            thorn[2] = DrawThornback(2);
            thorn[3] = DrawThornback(3);
            sb.AddFrames("thorn", thorn, 0.5, pivotY);
            return sb.Build("Sprites/Characters/enemies.png");
        }
    }
}
