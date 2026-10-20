using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Ecos.EditorTools
{
    /// <summary>Configura camadas, física 2D, qualidade e as opções do executável.</summary>
    public static class ProjectConfigurator
    {
        public static void Configure()
        {
            ConfigureLayers();
            ConfigurePhysics();
            ConfigureQuality();
            ConfigurePlayer();
            ConfigureGraphics();
        }

        /// <summary>
        /// Garante que os shaders da interface e dos sprites entrem no executável
        /// (sem isso a UI aparece magenta no build).
        /// </summary>
        static void ConfigureGraphics()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (assets == null || assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            var list = so.FindProperty("m_AlwaysIncludedShaders");
            string[] names =
            {
                "UI/Default", "UI/Default Font", "Sprites/Default", "Sprites/Mask", "UI/DefaultETC1",
                "Legacy Shaders/Diffuse", "Hidden/CubeBlur", "Hidden/CubeCopy", "Hidden/CubeBlend", "Hidden/VideoDecode"
            };
            foreach (var name in names)
            {
                var shader = Shader.Find(name);
                if (shader == null) continue;
                bool present = false;
                for (int i = 0; i < list.arraySize; i++)
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader) present = true;
                if (present) continue;
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
            }
            var spriteMaterial = so.FindProperty("m_SpritesDefaultMaterial");
            if (spriteMaterial != null && spriteMaterial.objectReferenceValue == null)
                spriteMaterial.objectReferenceValue = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ConfigureLayers()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;
            var tagManager = new SerializedObject(assets[0]);
            var layers = tagManager.FindProperty("layers");
            SetLayer(layers, GameConstants.LayerGround, "Ground");
            SetLayer(layers, GameConstants.LayerPlayer, "Player");
            SetLayer(layers, GameConstants.LayerEnemy, "Enemy");
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetLayer(SerializedProperty layers, int index, string name)
        {
            var element = layers.GetArrayElementAtIndex(index);
            element.stringValue = name;
        }

        static void ConfigurePhysics()
        {
            Physics2D.gravity = new Vector2(0f, -9.81f);
            Physics2D.queriesStartInColliders = false;
            Physics2D.IgnoreLayerCollision(GameConstants.LayerPlayer, GameConstants.LayerEnemy, true);
            Physics2D.IgnoreLayerCollision(GameConstants.LayerEnemy, GameConstants.LayerEnemy, true);
        }

        static void ConfigureQuality()
        {
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.antiAliasing = 0;
                QualitySettings.vSyncCount = 1;
            }
            QualitySettings.SetQualityLevel(current, false);
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Ecos Studio";
            PlayerSettings.productName = "Ecos da Floresta";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Standard);

            var icon = AssetGenerator.Texture("Assets/UI/app_icon.png");
            if (icon != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        }
    }
}
