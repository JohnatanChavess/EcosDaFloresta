using System;
using System.Collections.Generic;
using System.Linq;

namespace Ecos.EditorTools.Generation
{
    public enum Cell : byte { Empty, Ground, Platform, Ladder }

    public enum EntityType
    {
        PlayerStart, Crystal, Heart, FinalCrystal, Checkpoint, Goal,
        Slime, Bat, Thorn, Spikes, CeilingSpikes, Saw, Mover, Crumble, Sign
    }

    public class LevelEntity
    {
        public EntityType Type;
        public int X, Y;
        public double Dx, Dy;
        public string Text;
        public bool Flag;
    }

    /// <summary>
    /// Descrição de uma fase em grade (1 célula = 1 unidade = 16 px). y = 0 é a linha de baixo.
    /// Os métodos formam uma pequena linguagem para montar o nível; o construtor de cenas
    /// transforma isso em Tilemaps e prefabs na Unity.
    /// </summary>
    public class LevelData
    {
        public readonly string SceneName;
        public readonly int Number;
        public readonly string DisplayName;
        public readonly Theme Theme;
        public readonly int Width, Height;
        public readonly List<LevelEntity> Entities = new List<LevelEntity>();
        readonly Cell[,] cells;

        public LevelData(string sceneName, int number, string displayName, Theme theme, int width, int height)
        {
            SceneName = sceneName;
            Number = number;
            DisplayName = displayName;
            Theme = theme;
            Width = width;
            Height = height;
            cells = new Cell[width, height];
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
        public Cell Get(int x, int y) => InBounds(x, y) ? cells[x, y] : Cell.Empty;
        public void Set(int x, int y, Cell c) { if (InBounds(x, y)) cells[x, y] = c; }
        public bool IsSolid(int x, int y) => Get(x, y) == Cell.Ground;

        // ------------------------------------------------------------------ Terreno

        /// <summary>Chão das colunas x0..x1 com altura h (os pés ficam em y = h).</summary>
        public LevelData Ground(int x0, int x1, int h) => Block(x0, x1, 0, h - 1);

        public LevelData Block(int x0, int x1, int y0, int y1)
        {
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    Set(x, y, Cell.Ground);
            return this;
        }

        /// <summary>Plataforma de mão única na linha y (os pés ficam em y + 1).</summary>
        public LevelData Plat(int x, int y, int length)
        {
            for (int i = 0; i < length; i++) Set(x + i, y, Cell.Platform);
            return this;
        }

        /// <summary>Escada na coluna x, de y0 até y1 (y1 fica na mesma linha do chão que ela alcança).</summary>
        public LevelData Ladder(int x, int y0, int y1)
        {
            for (int y = y0; y <= y1; y++) Set(x, y, Cell.Ladder);
            return this;
        }

        // ------------------------------------------------------------------ Entidades

        LevelData Add(EntityType type, int x, int y, double dx = 0, double dy = 0, string text = null, bool flag = false)
        {
            Entities.Add(new LevelEntity { Type = type, X = x, Y = y, Dx = dx, Dy = dy, Text = text, Flag = flag });
            return this;
        }

        public LevelData Start(int x, int y) => Add(EntityType.PlayerStart, x, y);
        public LevelData Crystal(int x, int y) => Add(EntityType.Crystal, x, y);

        public LevelData Crystals(int x0, int x1, int y)
        {
            for (int x = x0; x <= x1; x++) Crystal(x, y);
            return this;
        }

        public LevelData Heart(int x, int y) => Add(EntityType.Heart, x, y);
        public LevelData FinalCrystal(int x, int y) => Add(EntityType.FinalCrystal, x, y);
        public LevelData Checkpoint(int x, int y) => Add(EntityType.Checkpoint, x, y);
        public LevelData Goal(int x, int y) => Add(EntityType.Goal, x, y);
        public LevelData Slime(int x, int y, bool startRight = false) => Add(EntityType.Slime, x, y, flag: startRight);
        public LevelData Thorn(int x, int y, bool startRight = false) => Add(EntityType.Thorn, x, y, flag: startRight);
        /// <summary>Morcego centrado na célula (x, y) voando ±range na horizontal.</summary>
        public LevelData Bat(int x, int y, double range) => Add(EntityType.Bat, x, y, range);
        public LevelData Sign(int x, int y, string text) => Add(EntityType.Sign, x, y, text: text);

        /// <summary>Espinhos de chão de x0 a x1 apoiados em y (respawn = volta ao checkpoint).</summary>
        public LevelData Spikes(int x0, int x1, int y, bool respawn = false)
        {
            for (int x = x0; x <= x1; x++) Add(EntityType.Spikes, x, y, flag: respawn);
            return this;
        }

        /// <summary>Espinhos pendurados na célula y (presos ao bloco acima).</summary>
        public LevelData CeilingSpikes(int x0, int x1, int y)
        {
            for (int x = x0; x <= x1; x++) Add(EntityType.CeilingSpikes, x, y);
            return this;
        }

        /// <summary>Esfera de espinhos que se move (dx, dy) a partir da célula (x, y).</summary>
        public LevelData Saw(int x, int y, double dx, double dy) => Add(EntityType.Saw, x, y, dx, dy);

        /// <summary>Plataforma móvel de 3 células; x = célula da esquerda, y = altura dos pés sobre ela.</summary>
        public LevelData Mover(int x, int y, double dx, double dy) => Add(EntityType.Mover, x, y, dx, dy);

        /// <summary>Blocos que desmoronam na linha y, de x0 a x1 (pés em y + 1).</summary>
        public LevelData Crumble(int x0, int x1, int y)
        {
            for (int x = x0; x <= x1; x++) Add(EntityType.Crumble, x, y);
            return this;
        }

        // ------------------------------------------------------------------ Consultas

        public LevelEntity PlayerStart => Entities.FirstOrDefault(e => e.Type == EntityType.PlayerStart);
        public int CrystalCount => Entities.Count(e => e.Type == EntityType.Crystal);

        /// <summary>Altura do terreno sólido mais alto da coluna (para decorações e limites).</summary>
        public int SurfaceHeight(int x)
        {
            for (int y = Height - 1; y >= 0; y--)
                if (IsSolid(x, y) && !IsSolid(x, y + 1)) return y + 1;
            return 0;
        }

        // ------------------------------------------------------------------ Validação

        bool SupportAt(int x, int y, HashSet<(int, int)> extra)
        {
            var below = Get(x, y - 1);
            if (below == Cell.Ground || below == Cell.Platform) return true;
            if (below == Cell.Ladder && Get(x, y) != Cell.Ladder) return true;
            return extra.Contains((x, y));
        }

        bool Free(int x, int y) => InBounds(x, y) ? !IsSolid(x, y) : y >= Height;

        /// <summary>
        /// Verificação aproximada de alcançabilidade usando o envelope de pulo do personagem
        /// (altura máx. ~3,6 células, ~4 células andando e ~7 correndo). Retorna avisos.
        /// </summary>
        public List<string> Validate()
        {
            var warnings = new List<string>();
            var extraSupport = new HashSet<(int, int)>();
            foreach (var e in Entities)
            {
                if (e.Type == EntityType.Crumble) extraSupport.Add((e.X, e.Y + 1));
                if (e.Type == EntityType.Mover)
                {
                    int steps = (int)Math.Round(Math.Max(Math.Abs(e.Dx), Math.Abs(e.Dy)));
                    for (int s = 0; s <= steps; s++)
                    {
                        int ox = e.X + (int)Math.Round(e.Dx * s / Math.Max(1, steps));
                        int oy = e.Y + (int)Math.Round(e.Dy * s / Math.Max(1, steps));
                        for (int i = 0; i < 3; i++) extraSupport.Add((ox + i, oy));
                    }
                }
            }

            var start = PlayerStart;
            if (start == null)
            {
                warnings.Add("Sem posição inicial do jogador");
                return warnings;
            }

            var reached = new HashSet<(int, int)>();
            var queue = new Queue<(int, int)>();
            reached.Add((start.X, start.Y));
            queue.Enqueue((start.X, start.Y));
            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                bool onLadder = Get(x, y) == Cell.Ladder;
                bool standing = SupportAt(x, y, extraSupport);

                // Subir/descer escadas.
                if (onLadder || Get(x, y - 1) == Cell.Ladder)
                {
                    foreach (int dy in new[] { -1, 1 })
                    {
                        int ny = y + dy;
                        if (Free(x, ny) && (Get(x, ny) == Cell.Ladder || Get(x, ny - 1) == Cell.Ladder) && reached.Add((x, ny)))
                            queue.Enqueue((x, ny));
                    }
                }
                if (!standing && !onLadder) continue;

                // Pulos e quedas: qualquer célula ocupável dentro do envelope.
                for (int dy = 3; dy >= -12; dy--)
                {
                    int reach = dy == 3 ? 4 : dy == 2 ? 5 : dy >= 0 ? 6 : Math.Min(9, 6 + (-dy) / 2);
                    // Teto: precisa de espaço acima para pular.
                    int headroom = 0;
                    while (headroom < 4 && Free(x, y + headroom + 1)) headroom++;
                    if (dy > 0 && headroom < dy) continue;
                    for (int dx = -reach; dx <= reach; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (!InBounds(nx, ny) || !Free(nx, ny)) continue;
                        bool target = SupportAt(nx, ny, extraSupport) || Get(nx, ny) == Cell.Ladder;
                        if (!target) continue;
                        if (reached.Add((nx, ny))) queue.Enqueue((nx, ny));
                    }
                }
            }

            foreach (var e in Entities)
            {
                bool needsReach = e.Type == EntityType.Crystal || e.Type == EntityType.Heart || e.Type == EntityType.Checkpoint ||
                                  e.Type == EntityType.Goal || e.Type == EntityType.FinalCrystal;
                if (!needsReach) continue;
                if (IsSolid(e.X, e.Y)) warnings.Add($"{e.Type} dentro de bloco sólido em ({e.X},{e.Y})");
                bool ok = reached.Any(p => Math.Abs(p.Item1 - e.X) <= (e.Type == EntityType.Crystal ? 3 : 1) && e.Y - p.Item2 <= 3 && e.Y - p.Item2 >= 0);
                if (!ok) warnings.Add($"{e.Type} possivelmente inalcançável em ({e.X},{e.Y})");
            }
            foreach (var e in Entities.Where(e => e.Type == EntityType.Spikes))
                if (!IsSolid(e.X, e.Y - 1)) warnings.Add($"Espinho sem chão embaixo em ({e.X},{e.Y})");
            foreach (var e in Entities.Where(e => e.Type == EntityType.Slime || e.Type == EntityType.Thorn))
                if (!IsSolid(e.X, e.Y - 1)) warnings.Add($"{e.Type} sem chão embaixo em ({e.X},{e.Y})");
            return warnings;
        }
    }
}
