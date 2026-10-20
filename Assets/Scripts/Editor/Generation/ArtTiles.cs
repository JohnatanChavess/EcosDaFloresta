using System;

namespace Ecos.EditorTools.Generation
{
    /// <summary>
    /// Tiles dos cenários. Cada tema tem 16 variações de bloco de chão (uma por combinação de lados expostos),
    /// variações extras de preenchimento, plataformas de mão única e a escada.
    /// </summary>
    public static partial class ArtLibrary
    {
        public const int MaskUp = 1, MaskRight = 2, MaskDown = 4, MaskLeft = 8;

        public static string ThemeKey(Theme t) => t.ToString().ToLowerInvariant();

        static PixelCanvas DrawGroundTile(Theme theme, int mask, int variant)
        {
            var tc = ThemeColors.Get(theme);
            var c = new PixelCanvas(16, 16);
            int seed = (int)theme * 1000 + variant * 37 + 11;

            // Preenchimento interno (textura do material).
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    double n = Rng.Hash(x, y, seed);
                    Rgba col = tc.FillBase;
                    switch (theme)
                    {
                        case Theme.Forest:
                            if (n < 0.07) col = tc.FillDark;
                            else if (n > 0.95) col = tc.FillLight;
                            break;
                        case Theme.Cave:
                            if (n < 0.06) col = tc.FillDark;
                            else if (n > 0.96) col = tc.FillLight;
                            break;
                        case Theme.Ruins:
                            col = BrickColor(tc, x, y, seed);
                            break;
                    }
                    c.Set(x, y, col);
                }
            }

            var rng = new Rng(seed);
            if (theme == Theme.Forest)
            {
                // Pedrinhas e raízes.
                for (int i = 0; i < 2 + variant; i++)
                {
                    int px = rng.Range(1, 13), py = rng.Range(5, 13);
                    c.Rect(px, py, 2, 2, tc.FillDark.Lighter(0.1));
                    c.Set(px, py, tc.FillLight);
                    c.Set(px + 1, py + 2, tc.FillDeep);
                    c.Set(px + 2, py + 1, tc.FillDeep);
                }
                if (variant == 2) c.Line(3, 9, 7, 12, tc.FillDeep);
            }
            else if (theme == Theme.Cave)
            {
                // Rachaduras e cristais incrustados.
                for (int i = 0; i < 2; i++)
                {
                    int px = rng.Range(1, 12), py = rng.Range(3, 13);
                    c.Line(px, py, px + rng.Range(2, 4), py + rng.Range(-1, 3), tc.FillDeep);
                }
                if (variant == 1 || variant == 2)
                {
                    int px = rng.Range(3, 11), py = rng.Range(6, 11);
                    c.Set(px, py, tc.Accent);
                    c.Set(px + 1, py - 1, tc.Accent.Lighter(0.5));
                    c.Set(px + 1, py, tc.Accent.Darker(0.3));
                }
            }

            bool up = (mask & MaskUp) != 0, right = (mask & MaskRight) != 0;
            bool down = (mask & MaskDown) != 0, left = (mask & MaskLeft) != 0;

            // Lados expostos: sombra e contorno.
            if (left)
            {
                for (int y = 0; y < 16; y++) { c.Set(0, y, tc.Outline); c.Set(1, y, tc.FillDark); }
            }
            if (right)
            {
                for (int y = 0; y < 16; y++) { c.Set(15, y, tc.Outline); c.Set(14, y, tc.FillDark); }
            }
            if (down)
            {
                for (int x = 0; x < 16; x++) { c.Set(x, 15, tc.Outline); c.Set(x, 14, tc.FillDeep); }
            }
            if (up) DrawTopCap(c, theme, tc, left, right, variant);

            // Cantos arredondados.
            if (up && left) { c.Set(0, 0, Rgba.Clear); c.Set(1, 0, tc.Outline); c.Set(0, 1, tc.Outline); }
            if (up && right) { c.Set(15, 0, Rgba.Clear); c.Set(14, 0, tc.Outline); c.Set(15, 1, tc.Outline); }
            if (down && left) { c.Set(0, 15, Rgba.Clear); c.Set(1, 15, tc.Outline); c.Set(0, 14, tc.Outline); }
            if (down && right) { c.Set(15, 15, Rgba.Clear); c.Set(14, 15, tc.Outline); c.Set(15, 14, tc.Outline); }
            return c;
        }

        static Rgba BrickColor(ThemeColors tc, int x, int y, int seed)
        {
            int row = y / 4;
            int offset = (row % 2) * 4;
            int bx = (x + offset) / 8;
            int localX = (x + offset) % 8;
            int localY = y % 4;
            if (localY == 3 || localX == 7) return tc.FillDeep;
            double tone = Rng.Hash(bx, row, seed / 37);
            Rgba b = tone < 0.33 ? tc.FillDark.Lighter(0.15) : tone < 0.66 ? tc.FillBase : tc.FillBase.Lighter(0.12);
            if (localY == 0 && localX < 6) b = b.Lighter(0.18);
            if (Rng.Hash(x, y, seed) < 0.05) b = tc.FillDark;
            return b;
        }

        static void DrawTopCap(PixelCanvas c, Theme theme, ThemeColors tc, bool left, bool right, int variant)
        {
            for (int x = 0; x < 16; x++)
            {
                double h = Rng.Hash(x, 3, (int)theme + variant * 5);
                int depth;
                switch (theme)
                {
                    case Theme.Forest:
                        depth = 4 + (h > 0.55 ? 1 : 0) + (x % 5 == 2 ? 1 : 0);
                        c.Set(x, 0, tc.Outline);
                        c.Set(x, 1, tc.TopLight);
                        for (int y = 2; y < depth; y++) c.Set(x, y, tc.TopBase);
                        c.Set(x, depth, tc.TopDark);
                        c.Set(x, depth + 1, tc.FillDark);
                        if (h > 0.8) c.Set(x, 2, tc.TopLight);
                        break;
                    case Theme.Cave:
                        depth = 3 + (h > 0.7 ? 1 : 0);
                        c.Set(x, 0, tc.Outline);
                        c.Set(x, 1, tc.TopLight);
                        for (int y = 2; y < depth; y++) c.Set(x, y, tc.TopBase);
                        c.Set(x, depth, tc.TopDark);
                        if (h > 0.82) c.Set(x, 1, tc.Accent);
                        if (h < 0.1) { c.Set(x, 1, tc.Accent.Lighter(0.4)); c.Set(x, 2, tc.Accent); }
                        break;
                    default:
                        c.Set(x, 0, tc.Outline);
                        c.Set(x, 1, tc.FillLight.Lighter(0.3));
                        c.Set(x, 2, tc.FillLight);
                        c.Set(x, 3, tc.FillDeep);
                        // Musgo sobre a pedra.
                        int mossLen = h > 0.45 ? (h > 0.8 ? 3 : 2) : 0;
                        if (variant == 1) mossLen = Math.Max(mossLen, 1);
                        for (int y = 1; y <= mossLen; y++) c.Set(x, y, y == 1 ? tc.TopLight : tc.TopBase);
                        break;
                }
            }
            if (theme == Theme.Forest && variant == 1)
            {
                // Florzinhas na grama.
                c.Set(4, 1, Rgba.Hex("ff8fab"));
                c.Set(11, 1, Rgba.Hex("fff3a0"));
            }
            if (left)
            {
                for (int y = 1; y < 6; y++) c.Set(0, y, tc.Outline);
                for (int y = 1; y < 5; y++) c.Set(1, y, theme == Theme.Ruins ? tc.FillLight : tc.TopBase);
            }
            if (right)
            {
                for (int y = 1; y < 6; y++) c.Set(15, y, tc.Outline);
                for (int y = 1; y < 5; y++) c.Set(14, y, theme == Theme.Ruins ? tc.FillDark : tc.TopDark);
            }
        }

        /// <summary>Plataforma de mão única. part: 0 = esquerda, 1 = meio, 2 = direita, 3 = única.</summary>
        static PixelCanvas DrawPlatformTile(Theme theme, int part)
        {
            var c = new PixelCanvas(16, 16);
            bool capLeft = part == 0 || part == 3;
            bool capRight = part == 2 || part == 3;
            Rgba light, mid, dark, outline, accent;
            switch (theme)
            {
                case Theme.Cave:
                    light = Rgba.Hex("9aa2d8"); mid = Rgba.Hex("6a70a8"); dark = Rgba.Hex("3f4478"); outline = Rgba.Hex("120f26"); accent = Rgba.Hex("5ff0d8");
                    break;
                case Theme.Ruins:
                    light = Rgba.Hex("f4e6cc"); mid = Rgba.Hex("d8c4a2"); dark = Rgba.Hex("9a8466"); outline = Rgba.Hex("2a1a24"); accent = Pal.GoldBase;
                    break;
                default:
                    light = Pal.WoodLight; mid = Pal.WoodBase; dark = Pal.WoodDark; outline = Rgba.Hex("2a1a1e"); accent = Pal.WoodDeep;
                    break;
            }

            int x0 = capLeft ? 1 : 0, x1 = capRight ? 14 : 15;
            for (int x = x0; x <= x1; x++)
            {
                c.Set(x, 0, outline);
                c.Set(x, 1, light);
                c.Set(x, 2, mid);
                c.Set(x, 3, theme == Theme.Forest ? mid : accent);
                c.Set(x, 4, dark);
                c.Set(x, 5, outline);
            }
            if (theme == Theme.Forest)
            {
                // Veios da madeira e pregos.
                for (int x = x0; x <= x1; x++) if (Rng.Hash(x, part, 7) < 0.25) c.Set(x, 3, dark);
                if (capLeft) c.Set(3, 2, Pal.MetalBase);
                if (capRight) c.Set(12, 2, Pal.MetalBase);
                if (part == 1) c.Set(8, 2, accent);
                // Suportes abaixo.
                if (capLeft) { c.VLine(3, 6, 8, outline); c.VLine(4, 6, 7, dark); }
                if (capRight) { c.VLine(12, 6, 8, outline); c.VLine(11, 6, 7, dark); }
            }
            else if (theme == Theme.Cave)
            {
                for (int x = x0; x <= x1; x++) if (Rng.Hash(x, part, 3) < 0.6) c.Set(x, 3, mid);
                c.Set(6, 6, dark);
                c.Set(9, 6, dark);
                c.Set(7, 7, outline);
            }
            else
            {
                if (capLeft) { c.Set(2, 2, accent); c.Set(2, 3, Pal.GoldDark); }
                if (capRight) { c.Set(13, 2, accent); c.Set(13, 3, Pal.GoldDark); }
            }
            if (capLeft) { c.VLine(0, 1, 4, outline); c.Set(1, 4, dark); }
            if (capRight) { c.VLine(15, 1, 4, outline); c.Set(14, 4, dark); }
            return c;
        }

        static PixelCanvas DrawLadderTile()
        {
            var c = new PixelCanvas(16, 16);
            var outline = Rgba.Hex("2a1a1e");
            // Degraus.
            foreach (int ry in new[] { 3, 11 })
            {
                c.HLine(4, 11, ry - 1, outline);
                c.HLine(4, 11, ry, Pal.WoodLight);
                c.HLine(4, 11, ry + 1, Pal.WoodDark);
                c.HLine(4, 11, ry + 2, outline);
            }
            // Laterais.
            foreach (int rx in new[] { 2, 12 })
            {
                c.VLine(rx - 1, 0, 15, outline);
                c.VLine(rx, 0, 15, Pal.WoodLight);
                c.VLine(rx + 1, 0, 15, Pal.WoodBase);
                c.VLine(rx + 2, 0, 15, outline);
                c.Set(rx + 1, 6, Pal.WoodDark);
                c.Set(rx + 1, 13, Pal.WoodDark);
            }
            return c;
        }

        static SheetAsset BuildTileSheet(Theme theme)
        {
            string key = ThemeKey(theme);
            var sb = new SheetBuilder(256, 2, true);
            for (int mask = 0; mask < 16; mask++) sb.Add(key + "_ground_" + mask, DrawGroundTile(theme, mask, 0));
            sb.Add(key + "_ground_fill_b", DrawGroundTile(theme, 0, 1));
            sb.Add(key + "_ground_fill_c", DrawGroundTile(theme, 0, 2));
            sb.Add(key + "_ground_top_b", DrawGroundTile(theme, MaskUp, 1));
            sb.Add(key + "_ground_top_c", DrawGroundTile(theme, MaskUp, 2));
            for (int part = 0; part < 4; part++) sb.Add(key + "_platform_" + part, DrawPlatformTile(theme, part));
            if (theme == Theme.Forest) sb.Add("ladder", DrawLadderTile());
            return sb.Build("Sprites/Tiles/tiles_" + key + ".png");
        }
    }
}
