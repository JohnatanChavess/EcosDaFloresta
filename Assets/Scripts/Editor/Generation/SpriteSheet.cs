using System;
using System.Collections.Generic;

namespace Ecos.EditorTools.Generation
{
    /// <summary>Definição de um sprite dentro de uma folha (retângulo em coordenadas de cima para baixo).</summary>
    public class SpriteDef
    {
        public string Name;
        public int X, Y, W, H;
        /// <summary>Pivô normalizado no padrão da Unity (0,0 = canto inferior esquerdo).</summary>
        public double PivotX = 0.5, PivotY = 0.5;
        /// <summary>Bordas para 9-slice (esquerda, baixo, direita, cima) em pixels.</summary>
        public int BorderL, BorderB, BorderR, BorderT;
    }

    /// <summary>Uma imagem PNG gerada e os sprites recortados dela.</summary>
    public class SheetAsset
    {
        public string RelativePath;
        public PixelCanvas Canvas;
        public readonly List<SpriteDef> Sprites = new List<SpriteDef>();
        public bool RepeatWrap;
        public bool FullRectMesh;
        public int PixelsPerUnit = 16;
    }

    /// <summary>
    /// Monta folhas de sprites empacotando imagens em prateleiras, com margem e extrusão de bordas
    /// (evita linhas entre tiles).
    /// </summary>
    public class SheetBuilder
    {
        readonly List<(string name, PixelCanvas canvas, double px, double py, int[] border)> items =
            new List<(string, PixelCanvas, double, double, int[])>();

        readonly int maxWidth;
        readonly int padding;
        readonly bool extrude;

        public SheetBuilder(int maxWidth = 256, int padding = 2, bool extrude = false)
        {
            this.maxWidth = maxWidth;
            this.padding = padding;
            this.extrude = extrude;
        }

        public SheetBuilder Add(string name, PixelCanvas canvas, double pivotX = 0.5, double pivotY = 0.5, int[] border = null)
        {
            items.Add((name, canvas, pivotX, pivotY, border));
            return this;
        }

        public SheetBuilder AddFrames(string baseName, IList<PixelCanvas> frames, double pivotX, double pivotY)
        {
            for (int i = 0; i < frames.Count; i++) Add(baseName + "_" + i, frames[i], pivotX, pivotY);
            return this;
        }

        public SheetAsset Build(string relativePath)
        {
            var placements = new List<(int x, int y)>();
            int x = padding, y = padding, shelfH = 0, usedW = 0;
            foreach (var item in items)
            {
                int w = item.canvas.Width, h = item.canvas.Height;
                if (x + w + padding > maxWidth && x > padding)
                {
                    x = padding;
                    y += shelfH + padding;
                    shelfH = 0;
                }
                placements.Add((x, y));
                x += w + padding;
                usedW = Math.Max(usedW, x);
                shelfH = Math.Max(shelfH, h);
            }
            int totalH = y + shelfH + padding;
            int width = NextPow2(Math.Max(usedW, 8));
            int height = NextPow2(Math.Max(totalH, 8));

            var sheet = new SheetAsset { RelativePath = relativePath, Canvas = new PixelCanvas(width, height) };
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var (px, py) = placements[i];
                sheet.Canvas.Blit(item.canvas, px, py);
                if (extrude) Extrude(sheet.Canvas, item.canvas, px, py);
                var def = new SpriteDef
                {
                    Name = item.name,
                    X = px, Y = py, W = item.canvas.Width, H = item.canvas.Height,
                    PivotX = item.px, PivotY = item.py
                };
                if (item.border != null)
                {
                    def.BorderL = item.border[0];
                    def.BorderB = item.border[1];
                    def.BorderR = item.border[2];
                    def.BorderT = item.border[3];
                }
                sheet.Sprites.Add(def);
            }
            return sheet;
        }

        /// <summary>Folha com uma única imagem (fundos, ícones).</summary>
        public static SheetAsset Single(string relativePath, string name, PixelCanvas canvas, double pivotX = 0.5, double pivotY = 0.5)
        {
            var sheet = new SheetAsset { RelativePath = relativePath, Canvas = canvas };
            sheet.Sprites.Add(new SpriteDef { Name = name, X = 0, Y = 0, W = canvas.Width, H = canvas.Height, PivotX = pivotX, PivotY = pivotY });
            return sheet;
        }

        static void Extrude(PixelCanvas dst, PixelCanvas src, int ox, int oy)
        {
            int w = src.Width, h = src.Height;
            for (int i = 0; i < w; i++)
            {
                dst.Set(ox + i, oy - 1, src.Get(i, 0));
                dst.Set(ox + i, oy + h, src.Get(i, h - 1));
            }
            for (int j = -1; j <= h; j++)
            {
                int sj = Math.Max(0, Math.Min(h - 1, j));
                dst.Set(ox - 1, oy + j, src.Get(0, sj));
                dst.Set(ox + w, oy + j, src.Get(w - 1, sj));
            }
        }

        static int NextPow2(int v)
        {
            int p = 1;
            while (p < v) p <<= 1;
            return p;
        }
    }
}
