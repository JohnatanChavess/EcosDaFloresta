using System.Collections.Generic;
using System.Linq;
using Ecos.EditorTools.Generation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Ecos.EditorTools
{
    /// <summary>Monta e salva as cenas: menu principal, as três fases e a tela de vitória.</summary>
    public static class SceneFactory
    {
        public const string SceneFolder = "Assets/Scenes/";
        const float OrthoSize = 6.75f;

        static readonly Dictionary<Theme, Color> CameraColor = new Dictionary<Theme, Color>
        {
            { Theme.Forest, new Color32(0x7C, 0xC4, 0xF4, 0xFF) },
            { Theme.Cave, new Color32(0x14, 0x10, 0x2E, 0xFF) },
            { Theme.Ruins, new Color32(0x7A, 0x3A, 0x86, 0xFF) },
        };

        static readonly Dictionary<Theme, MusicId> ThemeMusic = new Dictionary<Theme, MusicId>
        {
            { Theme.Forest, MusicId.Forest }, { Theme.Cave, MusicId.Cave }, { Theme.Ruins, MusicId.Ruins }
        };

        // ================================================================== FASES

        public static string BuildLevel(LevelData L, PrefabSet prefabs, GameObject levelCanvas, string nextScene, bool finalLevel)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // Abrir uma cena nova descarrega assets recém-criados da memória: recarrega os tiles do disco.
            var tiles = TileFactory.Load(L.Theme);

            // Câmera e fundos.
            var cam = CreateCamera(CameraColor[L.Theme]);
            var follow = cam.gameObject.AddComponent<CameraFollow>();
            CreateBackgrounds(cam.transform, ArtLibrary.ThemeKey(L.Theme), L.Width);

            // Grade e tilemaps.
            var grid = new GameObject("Grid", typeof(Grid));
            var ground = CreateTilemap(grid.transform, "Chao", SortOrder.Ground, GameConstants.LayerGround);
            var platforms = CreateTilemap(grid.transform, "Plataformas", SortOrder.Platforms, GameConstants.LayerGround);
            var ladders = CreateTilemap(grid.transform, "Escadas", SortOrder.Ladders, 0);
            PaintTiles(L, tiles, ground, platforms, ladders);
            AddSolidCollider(ground.gameObject, false);
            AddSolidCollider(platforms.gameObject, true);
            CreateLadderObjects(L);
            CreateBoundaryWalls(L);

            // Entidades.
            var parents = new Dictionary<string, Transform>();
            Transform Group(string name)
            {
                if (!parents.TryGetValue(name, out var t))
                {
                    t = new GameObject(name).transform;
                    parents[name] = t;
                }
                return t;
            }

            GameObject playerGo = null;
            Transform spawn = null;
            foreach (var e in L.Entities)
            {
                Vector3 cellBottom = new Vector3(e.X + 0.5f, e.Y, 0f);
                Vector3 cellCenter = new Vector3(e.X + 0.5f, e.Y + 0.5f, 0f);
                GameObject go;
                switch (e.Type)
                {
                    case EntityType.PlayerStart:
                        playerGo = EditorUtil.Instantiate(prefabs.Player, null, cellBottom);
                        spawn = new GameObject("PontoDeInicio").transform;
                        spawn.position = cellBottom + new Vector3(0f, 0.05f, 0f);
                        break;
                    case EntityType.Crystal:
                        EditorUtil.Instantiate(prefabs.Crystal, Group("Cristais"), cellCenter);
                        break;
                    case EntityType.Heart:
                        EditorUtil.Instantiate(prefabs.Heart, Group("Itens"), cellCenter);
                        break;
                    case EntityType.FinalCrystal:
                        EditorUtil.Instantiate(prefabs.FinalCrystal, Group("Itens"), cellBottom);
                        break;
                    case EntityType.Checkpoint:
                        EditorUtil.Instantiate(prefabs.Checkpoint, Group("Checkpoints"), cellBottom);
                        break;
                    case EntityType.Goal:
                        EditorUtil.Instantiate(prefabs.Portal, Group("Objetivo"), cellBottom + new Vector3(0.5f, 0f, 0f));
                        break;
                    case EntityType.Slime:
                        go = EditorUtil.Instantiate(prefabs.Slime, Group("Inimigos"), cellBottom);
                        EditorUtil.Set(go.GetComponent<PatrolEnemy>(), "startMovingRight", e.Flag);
                        break;
                    case EntityType.Thorn:
                        go = EditorUtil.Instantiate(prefabs.Thorn, Group("Inimigos"), cellBottom);
                        EditorUtil.Set(go.GetComponent<PatrolEnemy>(), "startMovingRight", e.Flag);
                        break;
                    case EntityType.Bat:
                        go = EditorUtil.Instantiate(prefabs.Bat, Group("Inimigos"), cellCenter);
                        EditorUtil.Set(go.GetComponent<FlyingEnemy>(), "range", (float)e.Dx);
                        break;
                    case EntityType.Spikes:
                        go = EditorUtil.Instantiate(prefabs.Spikes, Group("Armadilhas"), cellBottom);
                        if (e.Flag) EditorUtil.Set(go.GetComponent<Hazard>(), "respawnOnHit", true);
                        break;
                    case EntityType.CeilingSpikes:
                        EditorUtil.Instantiate(prefabs.CeilingSpikes, Group("Armadilhas"), cellBottom + Vector3.up);
                        break;
                    case EntityType.Saw:
                        go = EditorUtil.Instantiate(prefabs.Saw, Group("Armadilhas"), cellCenter);
                        EditorUtil.Set(go.GetComponent<PathMover>(), "travel", new Vector2((float)e.Dx, (float)e.Dy));
                        break;
                    case EntityType.Mover:
                        go = EditorUtil.Instantiate(prefabs.MovingPlatform, Group("PlataformasMoveis"), new Vector3(e.X + 1.5f, e.Y, 0f));
                        EditorUtil.Set(go.GetComponent<MovingPlatform>(), "travel", new Vector2((float)e.Dx, (float)e.Dy));
                        break;
                    case EntityType.Crumble:
                        EditorUtil.Instantiate(prefabs.CrumbleBlock, Group("BlocosRachados"), cellCenter);
                        break;
                    case EntityType.Sign:
                        go = EditorUtil.Instantiate(prefabs.Sign, Group("Placas"), cellBottom);
                        EditorUtil.Set(go.GetComponent<TutorialSign>(), "message", e.Text);
                        break;
                }
            }

            PlaceDecorations(L);

            // Câmera segue o jogador dentro dos limites da fase.
            EditorUtil.Set(follow, "target", playerGo.transform);
            EditorUtil.Set(follow, "boundsMin", new Vector2(0f, 0f));
            EditorUtil.Set(follow, "boundsMax", new Vector2(L.Width, Mathf.Max(L.Height, OrthoSize * 2f)));
            cam.transform.position = new Vector3(playerGo.transform.position.x + 8f, OrthoSize, -10f);

            // Interface, áudio e gerenciador.
            var canvasGo = (GameObject)PrefabUtility.InstantiatePrefab(levelCanvas);
            UIFactory.CreateEventSystem();
            PrefabUtility.InstantiatePrefab(prefabs.AudioManager);

            var gmGo = new GameObject("GameManager");
            var gm = gmGo.AddComponent<GameManager>();
            EditorUtil.Set(gm, "levelNumber", L.Number);
            EditorUtil.Set(gm, "levelDisplayName", L.DisplayName);
            EditorUtil.Set(gm, "nextSceneName", nextScene);
            EditorUtil.Set(gm, "isFinalLevel", finalLevel);
            EditorUtil.Set(gm, "music", (int)ThemeMusic[L.Theme]);
            EditorUtil.Set(gm, "killHeight", -4f);
            EditorUtil.Set(gm, "player", playerGo.GetComponent<PlayerController>());
            EditorUtil.Set(gm, "playerHealth", playerGo.GetComponent<PlayerHealth>());
            EditorUtil.Set(gm, "hud", canvasGo.GetComponentInChildren<HUDController>(true));
            EditorUtil.Set(gm, "levelUI", canvasGo.GetComponent<LevelUI>());
            EditorUtil.Set(gm, "cameraFollow", follow);
            EditorUtil.Set(gm, "spawnPoint", spawn);

            string path = SceneFolder + L.SceneName + ".unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        static Camera CreateCamera(Color background)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = OrthoSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;
            go.transform.position = new Vector3(0f, OrthoSize, -10f);
            go.AddComponent<AudioListener>();
            return cam;
        }

        /// <summary>Céu preso à câmera + duas camadas com parallax (distante e intermediária).</summary>
        static void CreateBackgrounds(Transform cam, string themeKey, float levelWidth, bool parallax = true)
        {
            var sky = EditorUtil.Child(cam, "Ceu", new Vector3(0f, 0f, 20f));
            var skyRenderer = sky.AddComponent<SpriteRenderer>();
            skyRenderer.sprite = AssetGenerator.Sprite("bg_" + themeKey + "_sky");
            skyRenderer.drawMode = SpriteDrawMode.Tiled;
            skyRenderer.size = new Vector2(24f * 3f, 13.5f);
            skyRenderer.sortingOrder = SortOrder.Sky;

            CreateParallax(cam, "FundoDistante", "bg_" + themeKey + "_far", SortOrder.FarBackground, 0.15f, levelWidth, 15f, parallax);
            CreateParallax(cam, "FundoIntermediario", "bg_" + themeKey + "_mid", SortOrder.MidBackground, 0.4f, levelWidth, 10f, parallax);
        }

        static void CreateParallax(Transform cam, string name, string sprite, int order, float factor, float levelWidth, float z, bool parallax)
        {
            var go = new GameObject(name);
            go.transform.position = new Vector3(0f, cam.position.y, z);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetGenerator.Sprite(sprite);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(Mathf.Ceil((levelWidth * 2f + 72f) / 24f) * 24f, 13.5f);
            sr.sortingOrder = order;
            var layer = go.AddComponent<ParallaxLayer>();
            EditorUtil.Set(layer, "cameraTransform", cam);
            EditorUtil.Set(layer, "horizontalFactor", parallax ? factor : 0f);
            EditorUtil.Set(layer, "verticalFactor", 1f);
        }

        static Tilemap CreateTilemap(Transform grid, string name, int order, int layer)
        {
            var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer)) { layer = layer };
            go.transform.SetParent(grid, false);
            var renderer = go.GetComponent<TilemapRenderer>();
            renderer.sortingOrder = order;
            renderer.mode = TilemapRenderer.Mode.Chunk;
            return go.GetComponent<Tilemap>();
        }

        static void AddSolidCollider(GameObject go, bool oneWay)
        {
            var tilemapCollider = go.AddComponent<TilemapCollider2D>();
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            var composite = go.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            if (oneWay)
            {
                composite.usedByEffector = true;
                var effector = go.AddComponent<PlatformEffector2D>();
                effector.useOneWay = true;
                effector.surfaceArc = 170f;
            }
        }

        static void PaintTiles(LevelData L, ThemeTiles tiles, Tilemap ground, Tilemap platforms, Tilemap ladders)
        {
            for (int x = 0; x < L.Width; x++)
            {
                for (int y = 0; y < L.Height; y++)
                {
                    var cell = L.Get(x, y);
                    var pos = new Vector3Int(x, y, 0);
                    switch (cell)
                    {
                        case Cell.Ground:
                            ground.SetTile(pos, GroundTile(L, tiles, x, y));
                            break;
                        case Cell.Platform:
                            bool left = L.Get(x - 1, y) == Cell.Platform;
                            bool right = L.Get(x + 1, y) == Cell.Platform;
                            int part = left && right ? 1 : left ? 2 : right ? 0 : 3;
                            platforms.SetTile(pos, tiles.Platform[part]);
                            break;
                        case Cell.Ladder:
                            ladders.SetTile(pos, tiles.Ladder);
                            break;
                    }
                }
            }
        }

        static TileBase GroundTile(LevelData L, ThemeTiles tiles, int x, int y)
        {
            bool Solid(int cx, int cy)
            {
                if (cx < 0 || cx >= L.Width || cy < 0) return true;
                if (cy >= L.Height) return false;
                return L.IsSolid(cx, cy);
            }

            int mask = 0;
            if (!Solid(x, y + 1)) mask |= ArtLibrary.MaskUp;
            if (!Solid(x + 1, y)) mask |= ArtLibrary.MaskRight;
            if (!Solid(x, y - 1)) mask |= ArtLibrary.MaskDown;
            if (!Solid(x - 1, y)) mask |= ArtLibrary.MaskLeft;

            int variant = (int)(Rng.Hash(x, y, L.Number) * 3);
            if (mask == 0) return tiles.FillVariants[variant];
            if (mask == ArtLibrary.MaskUp) return tiles.TopVariants[variant];
            return tiles.Ground[mask];
        }

        static void CreateLadderObjects(LevelData L)
        {
            var parent = new GameObject("Escadas (logica)").transform;
            for (int x = 0; x < L.Width; x++)
            {
                int y = 0;
                while (y < L.Height)
                {
                    if (L.Get(x, y) != Cell.Ladder)
                    {
                        y++;
                        continue;
                    }
                    int y0 = y;
                    while (y < L.Height && L.Get(x, y) == Cell.Ladder) y++;
                    int height = y - y0;

                    var ladderGo = new GameObject("Escada_" + x + "_" + y0);
                    ladderGo.transform.SetParent(parent, false);
                    ladderGo.transform.position = new Vector3(x + 0.5f, y0, 0f);
                    ladderGo.AddComponent<BoxCollider2D>();
                    var ladder = ladderGo.AddComponent<Ladder>();

                    var top = EditorUtil.Child(ladderGo.transform, "Topo", new Vector3(0f, height, 0f));
                    top.layer = GameConstants.LayerGround;
                    var topCol = top.AddComponent<BoxCollider2D>();
                    topCol.size = new Vector2(1f, 0.2f);
                    topCol.offset = new Vector2(0f, -0.1f);
                    topCol.usedByEffector = true;
                    var effector = top.AddComponent<PlatformEffector2D>();
                    effector.useOneWay = true;
                    effector.surfaceArc = 170f;

                    ladder.Configure(height, topCol);
                }
            }
        }

        static void CreateBoundaryWalls(LevelData L)
        {
            var parent = new GameObject("Limites").transform;
            float h = L.Height + 30f;
            foreach (float x in new[] { -0.5f, L.Width + 0.5f })
            {
                var wall = new GameObject(x < 0 ? "ParedeEsquerda" : "ParedeDireita") { layer = GameConstants.LayerGround };
                wall.transform.SetParent(parent, false);
                wall.transform.position = new Vector3(x, L.Height * 0.5f, 0f);
                wall.AddComponent<BoxCollider2D>().size = new Vector2(1f, h);
            }
        }

        // ================================================================== DECORAÇÕES

        static void PlaceDecorations(LevelData L, int clearFrom = -1, int clearTo = -1)
        {
            var parent = new GameObject("Decoracao").transform;
            var rng = new Rng(L.Number * 97 + 5);
            var blocked = new HashSet<(int, int)>();
            foreach (var e in L.Entities)
            {
                for (int dx = -1; dx <= 1; dx++) blocked.Add((e.X + dx, e.Y));
            }
            // Área livre (ex.: onde o Lumo fica no menu). Árvores ocupam ~3 células para cada lado.
            for (int x = clearFrom - 3; x <= clearTo + 3 && clearFrom >= 0; x++)
                for (int y = 0; y < L.Height; y++)
                    blocked.Add((x, y));

            void Put(string sprite, float x, float y, int order, bool flip = false, Color? tint = null)
            {
                var go = new GameObject(sprite);
                go.transform.SetParent(parent, false);
                go.transform.position = EditorUtil.Snap(new Vector3(x, y, 0f));
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = AssetGenerator.Sprite(sprite);
                sr.sortingOrder = order;
                sr.flipX = flip;
                if (tint.HasValue) sr.color = tint.Value;
            }

            int lastTreeX = -10;
            for (int x = 1; x < L.Width - 1; x++)
            {
                for (int y = 0; y < L.Height; y++)
                {
                    bool floor = L.IsSolid(x, y - 1) && L.Get(x, y) == Cell.Empty;
                    bool ceiling = L.IsSolid(x, y + 1) && L.Get(x, y) == Cell.Empty && y + 1 < L.Height;
                    float px = x + 0.5f + (float)rng.Range(-0.25, 0.25);
                    bool flip = rng.Chance(0.5);
                    double r = rng.Next();

                    if (floor && !blocked.Contains((x, y)))
                    {
                        bool flatAround = L.IsSolid(x - 1, y - 1) && L.IsSolid(x + 1, y - 1);
                        int headroom = 0;
                        while (headroom < 6 && !L.IsSolid(x, y + headroom) && L.Get(x, y + headroom) == Cell.Empty) headroom++;

                        switch (L.Theme)
                        {
                            case Theme.Forest:
                                if (r < 0.05 && flatAround && headroom >= 5 && x - lastTreeX > 6)
                                {
                                    Put(rng.Chance(0.5) ? "forest_tree_big" : "forest_tree_small", px, y, SortOrder.DecorationBack - 5, flip);
                                    lastTreeX = x;
                                }
                                else if (r < 0.11 && flatAround) Put(rng.Chance(0.5) ? "forest_bush_a" : "forest_bush_b", px, y, SortOrder.DecorationBack, flip);
                                else if (r < 0.22) Put(new[] { "forest_flower_red", "forest_flower_blue", "forest_flower_yellow" }[rng.Range(0, 3)], px, y, SortOrder.DecorationBack + 1, flip);
                                else if (r < 0.36) Put("forest_grass", px, y, rng.Chance(0.35) ? SortOrder.DecorationFront : SortOrder.DecorationBack + 1, flip);
                                else if (r < 0.39) Put("forest_mushroom", px, y, SortOrder.DecorationBack + 1, flip);
                                else if (r < 0.42) Put("forest_rock", px, y, SortOrder.DecorationBack + 1, flip);
                                break;
                            case Theme.Cave:
                                if (r < 0.05 && headroom >= 3) Put("cave_stalagmite", px, y, SortOrder.DecorationBack - 2, flip);
                                else if (r < 0.12) Put("cave_mushroom", px, y, SortOrder.DecorationBack + 1, flip);
                                else if (r < 0.17) Put("cave_crystals", px, y, SortOrder.DecorationBack + 1, flip);
                                else if (r < 0.21) Put("cave_rock", px, y, SortOrder.DecorationBack + 1, flip);
                                else if (r < 0.31) Put("cave_grass", px, y, rng.Chance(0.3) ? SortOrder.DecorationFront : SortOrder.DecorationBack + 1, flip);
                                break;
                            case Theme.Ruins:
                                if (r < 0.04 && flatAround && headroom >= 5) Put("ruins_pillar_tall", px, y, SortOrder.DecorationBack - 5, false, new Color(0.85f, 0.8f, 0.85f));
                                else if (r < 0.08 && headroom >= 3) Put("ruins_pillar_broken", px, y, SortOrder.DecorationBack - 4, flip, new Color(0.9f, 0.85f, 0.9f));
                                else if (r < 0.12) Put("ruins_rubble", px, y, SortOrder.DecorationBack + 1, flip);
                                else if (r < 0.18) Put("ruins_flower", px, y, SortOrder.DecorationBack + 1, flip);
                                else if (r < 0.32) Put("ruins_grass", px, y, rng.Chance(0.35) ? SortOrder.DecorationFront : SortOrder.DecorationBack + 1, flip);
                                break;
                        }
                    }
                    else if (ceiling && r < 0.16)
                    {
                        if (L.Theme == Theme.Cave) Put("cave_stalactite", px, y + 1, SortOrder.DecorationBack - 1, flip);
                        else if (L.Theme == Theme.Ruins) Put("ruins_vine", px, y + 1, SortOrder.DecorationBack, flip);
                    }
                }
            }
        }

        // ================================================================== MENU E VITÓRIA

        static void BuildScenery(Theme theme, int width, int seed, int clearFrom, int clearTo)
        {
            var tiles = TileFactory.Load(theme);
            var grid = new GameObject("Grid", typeof(Grid));
            var ground = CreateTilemap(grid.transform, "Chao", SortOrder.Ground, GameConstants.LayerGround);
            var L = new LevelData("Cenario", seed, "", theme, width, 14);
            L.Ground(0, width - 1, 3);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < 3; y++)
                    ground.SetTile(new Vector3Int(x, y, 0), GroundTile(L, tiles, x, y));
            PlaceDecorations(L, clearFrom, clearTo);
        }

        public static string BuildMainMenu(PrefabSet prefabs, RuntimeAnimatorController crystalAnim)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = CreateCamera(CameraColor[Theme.Forest]);
            cam.transform.position = new Vector3(12f, OrthoSize, -10f);
            CreateBackgrounds(cam.transform, "forest", 24f, false);
            BuildScenery(Theme.Forest, 26, 11, 15, 20);

            var lumo = EditorUtil.Instantiate(prefabs.LumoDisplay, null, new Vector3(17.5f, 3f, 0f));
            lumo.transform.localScale = Vector3.one * 3f;
            for (int i = 0; i < 5; i++)
            {
                var crystal = new GameObject("CristalDecorativo");
                crystal.transform.position = new Vector3(14.5f + i * 1.6f, 7.5f + (i % 2) * 0.8f, 0f);
                var sr = crystal.AddComponent<SpriteRenderer>();
                sr.sprite = AssetGenerator.Sprite("crystal_0");
                sr.sortingOrder = SortOrder.Items;
                crystal.AddComponent<Animator>().runtimeAnimatorController = crystalAnim;
                crystal.AddComponent<FloatingBob>();
            }

            PrefabUtility.InstantiatePrefab(prefabs.AudioManager);
            UIFactory.CreateEventSystem();
            var host = new GameObject("MainMenu");
            UIBuilder.BuildMainMenuUI(host);

            string path = SceneFolder + GameConstants.SceneMainMenu + ".unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        public static string BuildVictory(PrefabSet prefabs, RuntimeAnimatorController crystalAnim)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = CreateCamera(CameraColor[Theme.Forest]);
            cam.transform.position = new Vector3(12f, OrthoSize, -10f);
            CreateBackgrounds(cam.transform, "forest", 24f, false);
            BuildScenery(Theme.Forest, 26, 23, 15, 21);

            var lumo = EditorUtil.Instantiate(prefabs.LumoDisplay, null, new Vector3(18f, 3f, 0f));
            lumo.transform.localScale = Vector3.one * 3f;
            var final = new GameObject("CristalPrimordial");
            final.transform.position = new Vector3(18f, 8.6f, 0f);
            var fsr = final.AddComponent<SpriteRenderer>();
            fsr.sprite = AssetGenerator.Sprite("final_crystal_0");
            fsr.sortingOrder = SortOrder.Items;
            final.transform.localScale = Vector3.one * 1.6f;
            final.AddComponent<Animator>().runtimeAnimatorController =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Items/FinalCrystal.controller");
            final.AddComponent<FloatingBob>();
            final.AddComponent<PulseGlow>();
            for (int i = 0; i < 8; i++)
            {
                var crystal = new GameObject("CristalDecorativo");
                float a = i / 8f * Mathf.PI * 2f;
                crystal.transform.position = new Vector3(18f + Mathf.Cos(a) * 3.2f, 8.6f + Mathf.Sin(a) * 2.2f, 0f);
                var sr = crystal.AddComponent<SpriteRenderer>();
                sr.sprite = AssetGenerator.Sprite("crystal_0");
                sr.sortingOrder = SortOrder.Items;
                crystal.AddComponent<Animator>().runtimeAnimatorController = crystalAnim;
                crystal.AddComponent<FloatingBob>();
            }

            PrefabUtility.InstantiatePrefab(prefabs.AudioManager);
            UIFactory.CreateEventSystem();
            var host = new GameObject("Victory");
            UIBuilder.BuildVictoryUI(host);

            string path = SceneFolder + GameConstants.SceneVictory + ".unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }
    }
}
