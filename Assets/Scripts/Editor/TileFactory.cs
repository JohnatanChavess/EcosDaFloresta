using Ecos.EditorTools.Generation;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Ecos.EditorTools
{
    /// <summary>Conjunto de tiles de um tema (chão com 16 variações de borda, plataformas e escada).</summary>
    public class ThemeTiles
    {
        public Tile[] Ground = new Tile[16];
        public Tile[] FillVariants = new Tile[3];
        public Tile[] TopVariants = new Tile[3];
        public Tile[] Platform = new Tile[4];
        public Tile Ladder;
    }

    /// <summary>Cria os assets de Tile usados nos Tilemaps das fases.</summary>
    public static class TileFactory
    {
        public static ThemeTiles Create(Theme theme) => Build(theme, true);

        /// <summary>Carrega do disco os tiles já criados (necessário após abrir uma cena nova).</summary>
        public static ThemeTiles Load(Theme theme) => Build(theme, false);

        static ThemeTiles Build(Theme theme, bool create)
        {
            string key = ArtLibrary.ThemeKey(theme);
            string folder = "Assets/Tilemaps/" + char.ToUpperInvariant(key[0]) + key.Substring(1) + "/";
            var set = new ThemeTiles();
            for (int mask = 0; mask < 16; mask++)
                set.Ground[mask] = Make(create, folder + key + "_ground_" + mask + ".asset", key + "_ground_" + mask, Tile.ColliderType.Grid);
            set.FillVariants[0] = set.Ground[0];
            set.FillVariants[1] = Make(create, folder + key + "_ground_fill_b.asset", key + "_ground_fill_b", Tile.ColliderType.Grid);
            set.FillVariants[2] = Make(create, folder + key + "_ground_fill_c.asset", key + "_ground_fill_c", Tile.ColliderType.Grid);
            set.TopVariants[0] = set.Ground[ArtLibrary.MaskUp];
            set.TopVariants[1] = Make(create, folder + key + "_ground_top_b.asset", key + "_ground_top_b", Tile.ColliderType.Grid);
            set.TopVariants[2] = Make(create, folder + key + "_ground_top_c.asset", key + "_ground_top_c", Tile.ColliderType.Grid);
            for (int part = 0; part < 4; part++)
                set.Platform[part] = Make(create, folder + key + "_platform_" + part + ".asset", key + "_platform_" + part, Tile.ColliderType.Grid);
            set.Ladder = Make(create, "Assets/Tilemaps/Shared/ladder.asset", "ladder", Tile.ColliderType.None);
            return set;
        }

        static Tile Make(bool create, string path, string spriteName, Tile.ColliderType collider)
        {
            if (!create)
            {
                var existing = UnityEditor.AssetDatabase.LoadAssetAtPath<Tile>(path);
                if (existing == null) Debug.LogError("[Ecos] Tile não encontrado: " + path);
                return existing;
            }
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = AssetGenerator.Sprite(spriteName);
            tile.colliderType = collider;
            tile.color = Color.white;
            tile.name = System.IO.Path.GetFileNameWithoutExtension(path);
            return EditorUtil.CreateOrReplaceAsset(tile, path);
        }
    }
}
