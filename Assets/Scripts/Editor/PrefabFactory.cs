using UnityEditor;
using UnityEngine;

namespace Ecos.EditorTools
{
    public class PrefabSet
    {
        public GameObject Player, Slime, Bat, Thorn;
        public GameObject Crystal, Heart, FinalCrystal;
        public GameObject Checkpoint, Portal, Spikes, CeilingSpikes, Saw, MovingPlatform, CrumbleBlock, Sign;
        public GameObject Sparkle, Poof, AudioManager, LumoDisplay;
        public Material ParticleMaterial;
        public PhysicsMaterial2D NoFriction;
    }

    /// <summary>Cria todos os prefabs do jogo com componentes, colisores e referências configurados.</summary>
    public static class PrefabFactory
    {
        const string Root = "Assets/Prefabs/";

        public static PrefabSet CreateAll(AnimationSet anims)
        {
            var set = new PrefabSet();
            CreateMaterials(set);

            set.Sparkle = CreateSparkle(anims, set.ParticleMaterial);
            set.Poof = CreateEffect("Effects/Poof", "poof_0", anims.Poof, anims.PoofLength);

            set.Player = CreatePlayer(anims, set);
            set.LumoDisplay = CreateLumoDisplay(anims);
            set.Slime = CreateGroundEnemy("Enemies/LodoSombrio", "slime_0", anims.Slime, set, 1.4f, true, new Vector2(0.78f, 0.55f));
            set.Thorn = CreateGroundEnemy("Enemies/Espinhento", "thorn_0", anims.Thorn, set, 2.6f, false, new Vector2(0.82f, 0.62f));
            set.Bat = CreateBat(anims, set);

            set.Crystal = CreateCollectible("Items/Cristal", "crystal_0", anims.Crystal, Collectible.Kind.Crystal, 0.36f, set);
            set.Heart = CreateCollectible("Items/Coracao", "heart_0", anims.Heart, Collectible.Kind.Heart, 0.36f, set);
            set.FinalCrystal = CreateFinalCrystal(anims, set);

            set.Checkpoint = CreateCheckpoint(anims, set);
            set.Portal = CreatePortal(anims);
            set.Spikes = CreateSpikes(false);
            set.CeilingSpikes = CreateSpikes(true);
            set.Saw = CreateSaw();
            set.MovingPlatform = CreateMovingPlatform();
            set.CrumbleBlock = CreateCrumbleBlock();
            set.Sign = CreateSign();
            set.AudioManager = CreateAudioManager();
            return set;
        }

        static void CreateMaterials(PrefabSet set)
        {
            var mat = new Material(Shader.Find("Sprites/Default")) { name = "PixelParticle" };
            mat.mainTexture = AssetGenerator.Sprite("pixel").texture;
            set.ParticleMaterial = EditorUtil.CreateOrReplaceAsset(mat, "Assets/Materials/PixelParticle.mat");

            var phys = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
            set.NoFriction = EditorUtil.CreateOrReplaceAsset(phys, "Assets/Materials/NoFriction.physicsMaterial2D");
        }

        static SpriteRenderer AddVisual(GameObject root, string sprite, int order, RuntimeAnimatorController controller = null, Vector3 offset = default)
        {
            var visual = EditorUtil.Child(root.transform, "Visual", offset);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = AssetGenerator.Sprite(sprite);
            sr.sortingOrder = order;
            if (controller != null)
            {
                var animator = visual.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
            }
            return sr;
        }

        // ------------------------------------------------------------------ Jogador

        static GameObject CreatePlayer(AnimationSet anims, PrefabSet set)
        {
            var go = new GameObject("Player") { tag = GameConstants.PlayerTag, layer = GameConstants.LayerPlayer };
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 4f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

            var col = go.AddComponent<CapsuleCollider2D>();
            col.direction = CapsuleDirection2D.Vertical;
            col.size = new Vector2(0.62f, 0.92f);
            col.offset = new Vector2(0f, 0.46f);
            col.sharedMaterial = set.NoFriction;

            var sr = AddVisual(go, "lumo_idle_0", SortOrder.Player, anims.Lumo);
            var controller = go.AddComponent<PlayerController>();
            var health = go.AddComponent<PlayerHealth>();
            EditorUtil.Set(controller, "animator", sr.GetComponent<Animator>());
            EditorUtil.Set(controller, "spriteRenderer", sr);
            EditorUtil.Set(controller, "bodyCollider", col);
            EditorUtil.Set(controller, "dustEffectPrefab", set.Poof);
            EditorUtil.Set(controller, "groundMask", 1 << GameConstants.LayerGround);
            EditorUtil.Set(health, "enemyMask", 1 << GameConstants.LayerEnemy);
            EditorUtil.Set(health, "deathEffectPrefab", set.Poof);
            return EditorUtil.SavePrefab(go, Root + "Player/Player.prefab");
        }

        static GameObject CreateLumoDisplay(AnimationSet anims)
        {
            var go = new GameObject("LumoDisplay");
            AddVisual(go, "lumo_idle_0", SortOrder.Player, anims.Lumo);
            return EditorUtil.SavePrefab(go, Root + "Player/LumoDisplay.prefab");
        }

        // ------------------------------------------------------------------ Inimigos

        static GameObject CreateGroundEnemy(string name, string sprite, RuntimeAnimatorController controller, PrefabSet set,
            float speed, bool stompable, Vector2 size)
        {
            var go = new GameObject(System.IO.Path.GetFileName(name)) { layer = GameConstants.LayerEnemy };
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.offset = new Vector2(0f, size.y * 0.5f);
            col.edgeRadius = 0.02f;
            col.sharedMaterial = set.NoFriction;
            var sr = AddVisual(go, sprite, SortOrder.Enemies, controller);
            var enemy = go.AddComponent<PatrolEnemy>();
            EditorUtil.Set(enemy, "speed", speed);
            EditorUtil.Set(enemy, "canBeStomped", stompable);
            EditorUtil.Set(enemy, "deathEffectPrefab", set.Poof);
            EditorUtil.Set(enemy, "spriteRenderer", sr);
            EditorUtil.Set(enemy, "groundMask", 1 << GameConstants.LayerGround);
            return EditorUtil.SavePrefab(go, Root + name + ".prefab");
        }

        static GameObject CreateBat(AnimationSet anims, PrefabSet set)
        {
            var go = new GameObject("MorcegoSombrio") { layer = GameConstants.LayerEnemy };
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.32f;
            var sr = AddVisual(go, "bat_0", SortOrder.Enemies, anims.Bat);
            var enemy = go.AddComponent<FlyingEnemy>();
            EditorUtil.Set(enemy, "deathEffectPrefab", set.Poof);
            EditorUtil.Set(enemy, "spriteRenderer", sr);
            return EditorUtil.SavePrefab(go, Root + "Enemies/MorcegoSombrio.prefab");
        }

        // ------------------------------------------------------------------ Itens

        static GameObject CreateCollectible(string name, string sprite, RuntimeAnimatorController controller, Collectible.Kind kind, float radius, PrefabSet set)
        {
            var go = new GameObject(System.IO.Path.GetFileName(name));
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = radius;
            var sr = AddVisual(go, sprite, SortOrder.Items, controller);
            sr.gameObject.AddComponent<FloatingBob>();
            var c = go.AddComponent<Collectible>();
            EditorUtil.Set(c, "kind", (int)kind);
            EditorUtil.Set(c, "collectEffectPrefab", set.Sparkle);
            return EditorUtil.SavePrefab(go, Root + name + ".prefab");
        }

        static GameObject CreateFinalCrystal(AnimationSet anims, PrefabSet set)
        {
            var go = new GameObject("CristalPrimordial");
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.85f;
            col.offset = new Vector2(0f, 1.2f);
            var sr = AddVisual(go, "final_crystal_0", SortOrder.Items, anims.FinalCrystal, new Vector3(0f, 1.2f, 0f));
            sr.gameObject.AddComponent<FloatingBob>();
            var pulse = EditorUtil.Child(go.transform, "Halo", new Vector3(0f, 1.2f, 0f));
            var halo = pulse.AddComponent<SpriteRenderer>();
            halo.sprite = AssetGenerator.Sprite("sparkle_2");
            halo.color = new Color(1f, 0.9f, 0.5f, 0.6f);
            halo.sortingOrder = SortOrder.Items - 1;
            pulse.transform.localScale = Vector3.one * 3f;
            pulse.AddComponent<PulseGlow>();
            var c = go.AddComponent<Collectible>();
            EditorUtil.Set(c, "kind", (int)Collectible.Kind.FinalCrystal);
            EditorUtil.Set(c, "collectEffectPrefab", set.Sparkle);
            return EditorUtil.SavePrefab(go, Root + "Items/CristalPrimordial.prefab");
        }

        // ------------------------------------------------------------------ Objetos de fase

        static GameObject CreateCheckpoint(AnimationSet anims, PrefabSet set)
        {
            var go = new GameObject("Checkpoint");
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 2f);
            col.offset = new Vector2(0f, 1f);
            var sr = AddVisual(go, "checkpoint_off_0", SortOrder.DecorationBack + 5, anims.Checkpoint);
            var cp = go.AddComponent<Checkpoint>();
            EditorUtil.Set(cp, "animator", sr.GetComponent<Animator>());
            EditorUtil.Set(cp, "activateEffectPrefab", set.Sparkle);
            return EditorUtil.SavePrefab(go, Root + "Level/Checkpoint.prefab");
        }

        static GameObject CreatePortal(AnimationSet anims)
        {
            var go = new GameObject("PortalFinal");
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.1f, 2f);
            col.offset = new Vector2(0f, 1.2f);
            AddVisual(go, "portal_0", SortOrder.DecorationBack + 5, anims.Portal);
            go.AddComponent<LevelGoal>();
            return EditorUtil.SavePrefab(go, Root + "Level/PortalFinal.prefab");
        }

        static GameObject CreateSpikes(bool ceiling)
        {
            var go = new GameObject(ceiling ? "EspinhosTeto" : "Espinhos");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetGenerator.Sprite("spikes");
            sr.sortingOrder = SortOrder.Hazards;
            sr.flipY = ceiling;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 0.4f);
            col.offset = new Vector2(0f, ceiling ? -0.24f : 0.24f);
            go.AddComponent<Hazard>();
            return EditorUtil.SavePrefab(go, Root + "Level/" + go.name + ".prefab");
        }

        static GameObject CreateSaw()
        {
            var go = new GameObject("EsferaDeEspinhos");
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.36f;
            var sr = AddVisual(go, "saw", SortOrder.Hazards);
            sr.gameObject.AddComponent<Spinner>();
            go.AddComponent<Hazard>();
            go.AddComponent<PathMover>();
            return EditorUtil.SavePrefab(go, Root + "Level/EsferaDeEspinhos.prefab");
        }

        static GameObject CreateMovingPlatform()
        {
            var go = new GameObject("PlataformaMovel") { layer = GameConstants.LayerGround };
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetGenerator.Sprite("mover_platform");
            sr.sortingOrder = SortOrder.Platforms;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(3f, 0.4f);
            col.offset = new Vector2(0f, -0.2f);
            col.usedByEffector = true;
            var effector = go.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 160f;
            go.AddComponent<MovingPlatform>();
            return EditorUtil.SavePrefab(go, Root + "Level/PlataformaMovel.prefab");
        }

        static GameObject CreateCrumbleBlock()
        {
            var go = new GameObject("BlocoRachado") { layer = GameConstants.LayerGround };
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            go.AddComponent<BoxCollider2D>().size = new Vector2(1f, 1f);
            var sr = AddVisual(go, "crumble_block", SortOrder.Platforms);
            var fp = go.AddComponent<FallingPlatform>();
            EditorUtil.Set(fp, "spriteRenderer", sr);
            return EditorUtil.SavePrefab(go, Root + "Level/BlocoRachado.prefab");
        }

        static GameObject CreateSign()
        {
            var go = new GameObject("PlacaDeDica");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetGenerator.Sprite("sign");
            sr.sortingOrder = SortOrder.DecorationBack + 4;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(3.5f, 3f);
            col.offset = new Vector2(0f, 1.4f);
            go.AddComponent<TutorialSign>();
            return EditorUtil.SavePrefab(go, Root + "Level/PlacaDeDica.prefab");
        }

        // ------------------------------------------------------------------ Efeitos

        static GameObject CreateEffect(string name, string sprite, RuntimeAnimatorController controller, float length)
        {
            var go = new GameObject(System.IO.Path.GetFileName(name));
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetGenerator.Sprite(sprite);
            sr.sortingOrder = SortOrder.Effects;
            go.AddComponent<Animator>().runtimeAnimatorController = controller;
            var auto = go.AddComponent<AutoDestroy>();
            EditorUtil.Set(auto, "lifetime", length);
            return EditorUtil.SavePrefab(go, Root + name + ".prefab");
        }

        static GameObject CreateSparkle(AnimationSet anims, Material particleMaterial)
        {
            var go = new GameObject("BrilhoColeta");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetGenerator.Sprite("sparkle_0");
            sr.sortingOrder = SortOrder.Effects;
            go.AddComponent<Animator>().runtimeAnimatorController = anims.Sparkle;
            var auto = go.AddComponent<AutoDestroy>();
            EditorUtil.Set(auto, "lifetime", 0.9f);

            // Partículas de energia.
            var psGo = EditorUtil.Child(go.transform, "Particulas");
            var ps = psGo.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.55f, 0.95f, 1f), new Color(1f, 1f, 0.8f));
            main.gravityModifier = 0.6f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 32;
            main.playOnAwake = true;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.15f;
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = gradient;
            var renderer = psGo.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = particleMaterial;
            renderer.sortingOrder = SortOrder.Effects + 1;
            return EditorUtil.SavePrefab(go, Root + "Effects/BrilhoColeta.prefab");
        }

        // ------------------------------------------------------------------ Áudio

        static GameObject CreateAudioManager()
        {
            var go = new GameObject("AudioManager");
            var music = go.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            var am = go.AddComponent<AudioManager>();
            EditorUtil.Set(am, "musicSource", music);
            EditorUtil.Set(am, "menuMusic", AssetGenerator.Audio("music_menu"));
            EditorUtil.Set(am, "forestMusic", AssetGenerator.Audio("music_forest"));
            EditorUtil.Set(am, "caveMusic", AssetGenerator.Audio("music_cave"));
            EditorUtil.Set(am, "ruinsMusic", AssetGenerator.Audio("music_ruins"));
            EditorUtil.Set(am, "victoryMusic", AssetGenerator.Audio("music_victory"));
            EditorUtil.Set(am, "jumpSfx", AssetGenerator.Audio("sfx_jump"));
            EditorUtil.Set(am, "collectSfx", AssetGenerator.Audio("sfx_collect"));
            EditorUtil.Set(am, "hurtSfx", AssetGenerator.Audio("sfx_hurt"));
            EditorUtil.Set(am, "deathSfx", AssetGenerator.Audio("sfx_death"));
            EditorUtil.Set(am, "gameOverSfx", AssetGenerator.Audio("sfx_gameover"));
            EditorUtil.Set(am, "victorySfx", AssetGenerator.Audio("sfx_victory"));
            EditorUtil.Set(am, "stompSfx", AssetGenerator.Audio("sfx_stomp"));
            EditorUtil.Set(am, "checkpointSfx", AssetGenerator.Audio("sfx_checkpoint"));
            EditorUtil.Set(am, "heartSfx", AssetGenerator.Audio("sfx_heart"));
            EditorUtil.Set(am, "levelCompleteSfx", AssetGenerator.Audio("sfx_levelcomplete"));
            EditorUtil.Set(am, "clickSfx", AssetGenerator.Audio("sfx_click"));
            EditorUtil.Set(am, "crumbleSfx", AssetGenerator.Audio("sfx_crumble"));
            EditorUtil.Set(am, "landSfx", AssetGenerator.Audio("sfx_land"));
            return EditorUtil.SavePrefab(go, Root + "Managers/AudioManager.prefab");
        }
    }
}
