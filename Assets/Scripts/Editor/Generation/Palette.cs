using System.Collections.Generic;

namespace Ecos.EditorTools.Generation
{
    /// <summary>Paleta de cores do jogo (mantém o visual coerente entre todos os sprites).</summary>
    public static class Pal
    {
        static Rgba H(string hex) => Rgba.Hex(hex);

        public static readonly Rgba Outline = H("1e1a2e");
        public static readonly Rgba White = H("ffffff");
        public static readonly Rgba OffWhite = H("f4f0e8");

        // Lumo
        public static readonly Rgba LumoLight = H("b4f8de");
        public static readonly Rgba LumoBase = H("5ee0b4");
        public static readonly Rgba LumoDark = H("2fa889");
        public static readonly Rgba LumoDeep = H("1f7a66");
        public static readonly Rgba LeafLight = H("c8f27a");
        public static readonly Rgba LeafBase = H("82d64a");
        public static readonly Rgba LeafDark = H("3f9a2f");
        public static readonly Rgba ScarfLight = H("ffe27a");
        public static readonly Rgba ScarfBase = H("ffc44a");
        public static readonly Rgba ScarfDark = H("e08a2a");
        public static readonly Rgba Blush = H("ff8fab");

        // Inimigos
        public static readonly Rgba SlimeLight = H("c39af5");
        public static readonly Rgba SlimeBase = H("8d4bd6");
        public static readonly Rgba SlimeDark = H("5a2a99");
        public static readonly Rgba SlimeDeep = H("3a1a6a");
        public static readonly Rgba EvilRed = H("ff4a5a");
        public static readonly Rgba EvilGlow = H("ffb0b8");
        public static readonly Rgba BatLight = H("8a6fb8");
        public static readonly Rgba BatBase = H("5b4485");
        public static readonly Rgba BatDark = H("362a55");
        public static readonly Rgba BatWing = H("7a5aa8");
        public static readonly Rgba ThornLight = H("e0646e");
        public static readonly Rgba ThornBase = H("a8323e");
        public static readonly Rgba ThornDark = H("6b1d2a");
        public static readonly Rgba Bone = H("efe6d2");
        public static readonly Rgba BoneDark = H("a89c86");
        public static readonly Rgba EyeYellow = H("ffd23f");

        // Itens
        public static readonly Rgba CrystalWhite = H("e8ffff");
        public static readonly Rgba CrystalLight = H("8ff4ff");
        public static readonly Rgba CrystalBase = H("3cc8f0");
        public static readonly Rgba CrystalDark = H("2378c8");
        public static readonly Rgba CrystalDeep = H("1d4a8f");
        public static readonly Rgba HeartLight = H("ffa0b4");
        public static readonly Rgba HeartBase = H("ff4d6d");
        public static readonly Rgba HeartDark = H("b81e45");
        public static readonly Rgba GoldWhite = H("fffbe0");
        public static readonly Rgba GoldLight = H("ffe66d");
        public static readonly Rgba GoldBase = H("f5b83a");
        public static readonly Rgba GoldDark = H("c0701a");
        public static readonly Rgba Magic = H("b98cff");

        // Metal e pedra
        public static readonly Rgba MetalLight = H("e8eef6");
        public static readonly Rgba MetalBase = H("a8b4c8");
        public static readonly Rgba MetalDark = H("5f6a82");
        public static readonly Rgba StoneLight = H("b8b4c8");
        public static readonly Rgba StoneBase = H("7f7a94");
        public static readonly Rgba StoneDark = H("4e4a62");
        public static readonly Rgba WoodLight = H("d8a066");
        public static readonly Rgba WoodBase = H("a8703f");
        public static readonly Rgba WoodDark = H("6e4428");
        public static readonly Rgba WoodDeep = H("452a1c");

        public static Dictionary<char, Rgba> Map(params (char, Rgba)[] entries)
        {
            var d = new Dictionary<char, Rgba>();
            foreach (var (k, v) in entries) d[k] = v;
            return d;
        }
    }

    /// <summary>Temas visuais das três fases.</summary>
    public enum Theme { Forest, Cave, Ruins }

    public class ThemeColors
    {
        public Rgba Outline, FillLight, FillBase, FillDark, FillDeep, TopLight, TopBase, TopDark, Accent;

        public static ThemeColors Get(Theme theme)
        {
            switch (theme)
            {
                case Theme.Cave:
                    return new ThemeColors
                    {
                        Outline = Rgba.Hex("120f26"),
                        FillLight = Rgba.Hex("6a70a8"), FillBase = Rgba.Hex("474c7c"), FillDark = Rgba.Hex("31345a"), FillDeep = Rgba.Hex("23254a"),
                        TopLight = Rgba.Hex("a6acd8"), TopBase = Rgba.Hex("7a80b8"), TopDark = Rgba.Hex("565c94"),
                        Accent = Rgba.Hex("5ff0d8")
                    };
                case Theme.Ruins:
                    return new ThemeColors
                    {
                        Outline = Rgba.Hex("2a1a24"),
                        FillLight = Rgba.Hex("dcc09a"), FillBase = Rgba.Hex("b89a7a"), FillDark = Rgba.Hex("8a6c56"), FillDeep = Rgba.Hex("5e4636"),
                        TopLight = Rgba.Hex("a6e07a"), TopBase = Rgba.Hex("6cbf5a"), TopDark = Rgba.Hex("3f8a44"),
                        Accent = Rgba.Hex("f5b83a")
                    };
                default:
                    return new ThemeColors
                    {
                        Outline = Rgba.Hex("2a1a1e"),
                        FillLight = Rgba.Hex("b07a50"), FillBase = Rgba.Hex("8a5a3a"), FillDark = Rgba.Hex("664028"), FillDeep = Rgba.Hex("44281c"),
                        TopLight = Rgba.Hex("b4f064"), TopBase = Rgba.Hex("64c83c"), TopDark = Rgba.Hex("2f8a2a"),
                        Accent = Rgba.Hex("ffd84a")
                    };
            }
        }
    }
}
