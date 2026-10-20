using System;
using System.Collections.Generic;

namespace Ecos.EditorTools.Generation
{
    /// <summary>Cor RGBA de 8 bits independente da Unity (permite gerar arte fora do editor).</summary>
    public struct Rgba : IEquatable<Rgba>
    {
        public byte R, G, B, A;

        public Rgba(byte r, byte g, byte b, byte a = 255)
        {
            R = r; G = g; B = b; A = a;
        }

        public static readonly Rgba Clear = new Rgba(0, 0, 0, 0);
        public static readonly Rgba White = new Rgba(255, 255, 255);

        public static Rgba Hex(string hex)
        {
            hex = hex.TrimStart('#');
            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);
            byte a = hex.Length >= 8 ? Convert.ToByte(hex.Substring(6, 2), 16) : (byte)255;
            return new Rgba(r, g, b, a);
        }

        public bool IsClear => A == 0;

        public Rgba WithAlpha(byte a) => new Rgba(R, G, B, a);

        public static Rgba Lerp(Rgba a, Rgba b, double t)
        {
            t = Math.Max(0, Math.Min(1, t));
            return new Rgba(
                (byte)Math.Round(a.R + (b.R - a.R) * t),
                (byte)Math.Round(a.G + (b.G - a.G) * t),
                (byte)Math.Round(a.B + (b.B - a.B) * t),
                (byte)Math.Round(a.A + (b.A - a.A) * t));
        }

        public Rgba Darker(double amount) => Lerp(this, new Rgba(20, 16, 40, A), amount);
        public Rgba Lighter(double amount) => Lerp(this, new Rgba(255, 255, 240, A), amount);

        public bool Equals(Rgba other) => R == other.R && G == other.G && B == other.B && A == other.A;
        public override bool Equals(object obj) => obj is Rgba o && Equals(o);
        public override int GetHashCode() => (R << 24) | (G << 16) | (B << 8) | A;
    }

    /// <summary>
    /// Tela de pixels com origem no canto superior esquerdo (y cresce para baixo) e
    /// primitivas de desenho próprias para pixel art.
    /// </summary>
    public class PixelCanvas
    {
        public readonly int Width;
        public readonly int Height;
        readonly Rgba[] pixels;

        public PixelCanvas(int width, int height)
        {
            Width = width;
            Height = height;
            pixels = new Rgba[width * height];
        }

        public bool Inside(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public Rgba Get(int x, int y) => Inside(x, y) ? pixels[y * Width + x] : Rgba.Clear;

        public void Set(int x, int y, Rgba c)
        {
            if (Inside(x, y)) pixels[y * Width + x] = c;
        }

        /// <summary>Desenha com mistura alfa.</summary>
        public void Blend(int x, int y, Rgba c)
        {
            if (!Inside(x, y) || c.A == 0) return;
            if (c.A == 255)
            {
                pixels[y * Width + x] = c;
                return;
            }
            var dst = pixels[y * Width + x];
            double a = c.A / 255.0;
            double outA = a + dst.A / 255.0 * (1 - a);
            if (outA <= 0) return;
            byte Mix(byte s, byte d) => (byte)Math.Round((s * a + d * (dst.A / 255.0) * (1 - a)) / outA);
            pixels[y * Width + x] = new Rgba(Mix(c.R, dst.R), Mix(c.G, dst.G), Mix(c.B, dst.B), (byte)Math.Round(outA * 255));
        }

        public void Fill(Rgba c)
        {
            for (int i = 0; i < pixels.Length; i++) pixels[i] = c;
        }

        public void Rect(int x, int y, int w, int h, Rgba c)
        {
            for (int j = y; j < y + h; j++)
                for (int i = x; i < x + w; i++)
                    Set(i, j, c);
        }

        public void HLine(int x0, int x1, int y, Rgba c)
        {
            if (x0 > x1) (x0, x1) = (x1, x0);
            for (int x = x0; x <= x1; x++) Set(x, y, c);
        }

        public void VLine(int x, int y0, int y1, Rgba c)
        {
            if (y0 > y1) (y0, y1) = (y1, y0);
            for (int y = y0; y <= y1; y++) Set(x, y, c);
        }

        public void Line(int x0, int y0, int x1, int y1, Rgba c)
        {
            int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            while (true)
            {
                Set(x0, y0, c);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        /// <summary>Elipse preenchida. A função de cor recebe as coordenadas normalizadas (-1..1).</summary>
        public void Ellipse(double cx, double cy, double rx, double ry, Func<double, double, Rgba> shader)
        {
            int x0 = (int)Math.Floor(cx - rx - 1), x1 = (int)Math.Ceiling(cx + rx + 1);
            int y0 = (int)Math.Floor(cy - ry - 1), y1 = (int)Math.Ceiling(cy + ry + 1);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    double nx = (x + 0.5 - cx) / rx;
                    double ny = (y + 0.5 - cy) / ry;
                    if (nx * nx + ny * ny <= 1.0)
                    {
                        var c = shader(nx, ny);
                        if (!c.IsClear) Set(x, y, c);
                    }
                }
            }
        }

        public void Ellipse(double cx, double cy, double rx, double ry, Rgba c) => Ellipse(cx, cy, rx, ry, (a, b) => c);

        public void Circle(double cx, double cy, double r, Rgba c) => Ellipse(cx, cy, r, r, c);

        /// <summary>Contorno de elipse (anel) com espessura em pixels.</summary>
        public void Ring(double cx, double cy, double rx, double ry, double thickness, Func<double, Rgba> shaderByAngle)
        {
            int x0 = (int)Math.Floor(cx - rx - 1), x1 = (int)Math.Ceiling(cx + rx + 1);
            int y0 = (int)Math.Floor(cy - ry - 1), y1 = (int)Math.Ceiling(cy + ry + 1);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    double dx = x + 0.5 - cx, dy = y + 0.5 - cy;
                    double outer = (dx * dx) / (rx * rx) + (dy * dy) / (ry * ry);
                    double irx = Math.Max(0.1, rx - thickness), iry = Math.Max(0.1, ry - thickness);
                    double inner = (dx * dx) / (irx * irx) + (dy * dy) / (iry * iry);
                    if (outer <= 1.0 && inner > 1.0)
                    {
                        var c = shaderByAngle(Math.Atan2(dy, dx));
                        if (!c.IsClear) Set(x, y, c);
                    }
                }
            }
        }

        /// <summary>Triângulo preenchido (usado em espinhos e cristais).</summary>
        public void Triangle(double ax, double ay, double bx, double by, double cx, double cy, Func<int, int, Rgba> shader)
        {
            int minX = (int)Math.Floor(Math.Min(ax, Math.Min(bx, cx)));
            int maxX = (int)Math.Ceiling(Math.Max(ax, Math.Max(bx, cx)));
            int minY = (int)Math.Floor(Math.Min(ay, Math.Min(by, cy)));
            int maxY = (int)Math.Ceiling(Math.Max(ay, Math.Max(by, cy)));
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    double px = x + 0.5, py = y + 0.5;
                    double d1 = (px - bx) * (ay - by) - (ax - bx) * (py - by);
                    double d2 = (px - cx) * (by - cy) - (bx - cx) * (py - cy);
                    double d3 = (px - ax) * (cy - ay) - (cx - ax) * (py - ay);
                    bool neg = d1 < 0 || d2 < 0 || d3 < 0;
                    bool pos = d1 > 0 || d2 > 0 || d3 > 0;
                    if (!(neg && pos))
                    {
                        var c = shader(x, y);
                        if (!c.IsClear) Set(x, y, c);
                    }
                }
            }
        }

        public void Triangle(double ax, double ay, double bx, double by, double cx, double cy, Rgba c) =>
            Triangle(ax, ay, bx, by, cx, cy, (x, y) => c);

        /// <summary>Desenha um mapa de caracteres (pixel art feita à mão).</summary>
        public void Map(string[] rows, IDictionary<char, Rgba> palette, int ox, int oy, bool flipX = false)
        {
            for (int j = 0; j < rows.Length; j++)
            {
                string row = rows[j];
                for (int i = 0; i < row.Length; i++)
                {
                    char ch = row[flipX ? row.Length - 1 - i : i];
                    if (palette.TryGetValue(ch, out var c)) Set(ox + i, oy + j, c);
                }
            }
        }

        /// <summary>Adiciona contorno de 1 pixel ao redor de todos os pixels opacos.</summary>
        public void Outline(Rgba color, bool diagonals = false)
        {
            var copy = (Rgba[])pixels.Clone();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (copy[y * Width + x].A != 0) continue;
                    bool near = false;
                    for (int dy = -1; dy <= 1 && !near; dy++)
                    {
                        for (int dx = -1; dx <= 1 && !near; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            if (!diagonals && dx != 0 && dy != 0) continue;
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= Width || ny >= Height) continue;
                            if (copy[ny * Width + nx].A > 128) near = true;
                        }
                    }
                    if (near) pixels[y * Width + x] = color;
                }
            }
        }

        /// <summary>Copia outra tela para esta (com alfa), opcionalmente espelhada.</summary>
        public void Blit(PixelCanvas src, int ox, int oy, bool flipX = false, bool flipY = false)
        {
            for (int y = 0; y < src.Height; y++)
            {
                for (int x = 0; x < src.Width; x++)
                {
                    int sx = flipX ? src.Width - 1 - x : x;
                    int sy = flipY ? src.Height - 1 - y : y;
                    var c = src.Get(sx, sy);
                    if (c.A > 0) Blend(ox + x, oy + y, c);
                }
            }
        }

        /// <summary>Copia com escala inteira (vizinho mais próximo).</summary>
        public void BlitScaled(PixelCanvas src, int ox, int oy, int scale)
        {
            for (int y = 0; y < src.Height; y++)
                for (int x = 0; x < src.Width; x++)
                {
                    var c = src.Get(x, y);
                    if (c.A == 0) continue;
                    for (int j = 0; j < scale; j++)
                        for (int i = 0; i < scale; i++)
                            Blend(ox + x * scale + i, oy + y * scale + j, c);
                }
        }

        /// <summary>Substitui cada pixel opaco usando uma função.</summary>
        public void Recolor(Func<int, int, Rgba, Rgba> f)
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    var c = pixels[y * Width + x];
                    if (c.A > 0) pixels[y * Width + x] = f(x, y, c);
                }
        }

        public PixelCanvas Clone()
        {
            var c = new PixelCanvas(Width, Height);
            Array.Copy(pixels, c.pixels, pixels.Length);
            return c;
        }

        /// <summary>Retorna os pixels em ordem de baixo para cima (padrão PNG é de cima para baixo).</summary>
        public Rgba[] RawTopDown => pixels;
    }

    /// <summary>Gerador pseudoaleatório determinístico (a arte sai sempre igual).</summary>
    public class Rng
    {
        uint state;

        public Rng(int seed)
        {
            state = (uint)seed * 2654435761u + 1013904223u;
            if (state == 0) state = 1;
        }

        public uint NextUInt()
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return state;
        }

        public double Next() => (NextUInt() & 0xFFFFFF) / (double)0x1000000;
        public int Range(int min, int maxExclusive) => min + (int)(Next() * (maxExclusive - min));
        public double Range(double min, double max) => min + Next() * (max - min);
        public bool Chance(double p) => Next() < p;

        public static double Hash(int x, int y, int seed = 0)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (double)0x1000000;
            }
        }
    }
}
