using System.Collections.Generic;

namespace Ecos.EditorTools.Generation
{
    /// <summary>
    /// Ponto de entrada da geração de arte. Todos os sprites do jogo são desenhados por código
    /// (arte original, sem assets de terceiros) e exportados como PNG.
    /// </summary>
    public static partial class ArtLibrary
    {
        public static List<SheetAsset> GenerateAll()
        {
            var sheets = new List<SheetAsset>
            {
                BuildLumoSheet(),
                BuildEnemySheet(),
                BuildTileSheet(Theme.Forest),
                BuildTileSheet(Theme.Cave),
                BuildTileSheet(Theme.Ruins),
                BuildItemSheet(),
                BuildLevelObjectSheet(),
                BuildEffectSheet(),
                BuildDecorSheet(),
                BuildUISheet(),
                BuildAppIcon(),
            };
            sheets.AddRange(BuildBackgrounds());
            return sheets;
        }
    }
}
