namespace Ecos.EditorTools.Generation
{
    /// <summary>Elementos de interface: painel (9-slice), botão, ícones do HUD e ícone do aplicativo.</summary>
    public static partial class ArtLibrary
    {
        static PixelCanvas DrawPanel()
        {
            const int s = 48;
            var c = new PixelCanvas(s, s);
            var bg = Rgba.Hex("1c1832").WithAlpha(238);
            var bgLight = Rgba.Hex("262044").WithAlpha(238);
            var gold = Rgba.Hex("e8b84a");
            var goldLight = Rgba.Hex("fff0a0");
            var goldDark = Rgba.Hex("8a5a20");
            var edge = Rgba.Hex("0e0b1a");
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    int d = System.Math.Min(System.Math.Min(x, y), System.Math.Min(s - 1 - x, s - 1 - y));
                    bool corner = (x < 2 || x > s - 3) && (y < 2 || y > s - 3);
                    if (corner) continue;
                    Rgba col;
                    if (d == 0) col = edge;
                    else if (d == 1) col = (y < s / 2) ? goldLight : gold;
                    else if (d == 2) col = gold;
                    else if (d == 3) col = goldDark;
                    else if (d == 4) col = edge.WithAlpha(238);
                    else col = y < 12 ? bgLight : bg;
                    c.Set(x, y, col);
                }
            }
            // Ornamentos de folha nos cantos.
            foreach (var (cx, cy) in new[] { (5, 5), (s - 6, 5), (5, s - 6), (s - 6, s - 6) })
            {
                c.Set(cx, cy, Pal.LeafLight);
                c.Set(cx + 1, cy, Pal.LeafBase);
                c.Set(cx, cy + 1, Pal.LeafBase);
                c.Set(cx - 1, cy, Pal.LeafDark);
                c.Set(cx, cy - 1, Pal.LeafDark);
            }
            return c;
        }

        static PixelCanvas DrawButton()
        {
            const int w = 48, h = 24;
            var c = new PixelCanvas(w, h);
            var top = Rgba.Hex("4fd0b0");
            var bottom = Rgba.Hex("238070");
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool corner = (x == 0 || x == w - 1) && (y == 0 || y == h - 1);
                    if (corner) continue;
                    Rgba col;
                    if (x == 0 || y == 0 || x == w - 1 || y == h - 1) col = Rgba.Hex("0e2a26");
                    else if (y == 1) col = Rgba.Hex("a8fff0");
                    else if (y >= h - 4) col = Rgba.Hex("165a4e");
                    else col = Rgba.Lerp(top, bottom, (y - 2) / (double)(h - 6));
                    c.Set(x, y, col);
                }
            }
            c.Set(1, 1, Rgba.Hex("0e2a26"));
            c.Set(w - 2, 1, Rgba.Hex("0e2a26"));
            c.Set(1, h - 2, Rgba.Hex("0e2a26"));
            c.Set(w - 2, h - 2, Rgba.Hex("0e2a26"));
            return c;
        }

        static PixelCanvas DrawAppIcon()
        {
            var c = new PixelCanvas(64, 64);
            c.Ellipse(32, 32, 31, 31, (nx, ny) =>
            {
                double d = nx * nx + ny * ny;
                if (d > 0.88) return Rgba.Hex("1c1832");
                return Rgba.Lerp(Rgba.Hex("7cc4f4"), Rgba.Hex("3f8f5f"), (ny + 1) / 2);
            });
            var lumo = DrawLumo(new LumoPose());
            c.BlitScaled(lumo, 8, 6, 3);
            return c;
        }

        static SheetAsset BuildUISheet()
        {
            var sb = new SheetBuilder(256);
            sb.Add("ui_panel", DrawPanel(), 0.5, 0.5, new[] { 14, 14, 14, 14 });
            sb.Add("ui_button", DrawButton(), 0.5, 0.5, new[] { 8, 8, 8, 8 });
            sb.Add("ui_heart_full", DrawHeart(1.15), 0.5, 0.5);
            sb.Add("ui_heart_empty", DrawHeart(1.15, 16, true), 0.5, 0.5);
            sb.Add("ui_crystal", DrawCrystal(1.0, false, Pal.CrystalWhite, Pal.CrystalLight, Pal.CrystalBase, Pal.CrystalDark, Pal.CrystalDeep, 16, 1), 0.5, 0.5);
            return sb.Build("UI/ui_elements.png");
        }

        static SheetAsset BuildAppIcon() => SheetBuilder.Single("UI/app_icon.png", "app_icon", DrawAppIcon());
    }
}
