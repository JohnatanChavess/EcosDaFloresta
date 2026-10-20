using System;
using System.IO;
using System.IO.Compression;

namespace Ecos.EditorTools.Generation
{
    /// <summary>Codificador PNG mínimo (RGBA 8 bits) sem dependências da Unity.</summary>
    public static class PngEncoder
    {
        static readonly uint[] CrcTable = BuildCrcTable();

        public static byte[] Encode(PixelCanvas canvas)
        {
            int w = canvas.Width, h = canvas.Height;
            var raw = new byte[(w * 4 + 1) * h];
            var px = canvas.RawTopDown;
            int k = 0;
            for (int y = 0; y < h; y++)
            {
                raw[k++] = 0;
                for (int x = 0; x < w; x++)
                {
                    var c = px[y * w + x];
                    raw[k++] = c.R;
                    raw[k++] = c.G;
                    raw[k++] = c.B;
                    raw[k++] = c.A;
                }
            }

            using (var ms = new MemoryStream())
            {
                ms.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, 0, 8);
                var ihdr = new byte[13];
                WriteBE(ihdr, 0, (uint)w);
                WriteBE(ihdr, 4, (uint)h);
                ihdr[8] = 8;
                ihdr[9] = 6;
                WriteChunk(ms, "IHDR", ihdr);
                WriteChunk(ms, "IDAT", ZlibCompress(raw));
                WriteChunk(ms, "IEND", new byte[0]);
                return ms.ToArray();
            }
        }

        public static void Save(PixelCanvas canvas, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, Encode(canvas));
        }

        static byte[] ZlibCompress(byte[] data)
        {
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0x78);
                ms.WriteByte(0x9C);
                using (var deflate = new DeflateStream(ms, CompressionLevel.Optimal, true))
                    deflate.Write(data, 0, data.Length);
                uint a = 1, b = 0;
                foreach (byte d in data)
                {
                    a = (a + d) % 65521;
                    b = (b + a) % 65521;
                }
                uint adler = (b << 16) | a;
                var tail = new byte[4];
                WriteBE(tail, 0, adler);
                ms.Write(tail, 0, 4);
                return ms.ToArray();
            }
        }

        static void WriteChunk(Stream s, string type, byte[] data)
        {
            var len = new byte[4];
            WriteBE(len, 0, (uint)data.Length);
            s.Write(len, 0, 4);
            var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
            s.Write(typeBytes, 0, 4);
            s.Write(data, 0, data.Length);
            uint crc = 0xFFFFFFFF;
            foreach (byte t in typeBytes) crc = CrcTable[(crc ^ t) & 0xFF] ^ (crc >> 8);
            foreach (byte d in data) crc = CrcTable[(crc ^ d) & 0xFF] ^ (crc >> 8);
            crc ^= 0xFFFFFFFF;
            var crcBytes = new byte[4];
            WriteBE(crcBytes, 0, crc);
            s.Write(crcBytes, 0, 4);
        }

        static void WriteBE(byte[] buffer, int offset, uint value)
        {
            buffer[offset] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }

        static uint[] BuildCrcTable()
        {
            var table = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                uint c = n;
                for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                table[n] = c;
            }
            return table;
        }
    }
}
