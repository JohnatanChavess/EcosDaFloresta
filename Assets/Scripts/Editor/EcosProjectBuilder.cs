using System;
using System.Collections.Generic;
using System.IO;
using Ecos.EditorTools.Generation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ecos.EditorTools
{
    /// <summary>
    /// Gera o projeto completo: arte, áudio, tiles, animações, prefabs, cenas e configurações.
    /// Menu: "Ecos da Floresta > Gerar projeto completo". Também roda em modo batch:
    /// Unity.exe -batchmode -projectPath . -executeMethod Ecos.EditorTools.EcosProjectBuilder.BatchBuildAll -quit
    /// </summary>
    [InitializeOnLoad]
    public static class EcosProjectBuilder
    {
        static EcosProjectBuilder()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += OfferBuildIfMissing;
        }

        static void OfferBuildIfMissing()
        {
            if (File.Exists(SceneFactory.SceneFolder + GameConstants.SceneMainMenu + ".unity")) return;
            if (EditorUtility.DisplayDialog("Ecos da Floresta",
                    "As cenas do jogo ainda não foram geradas neste projeto.\nDeseja gerar agora (arte, áudio, prefabs e as 3 fases)?",
                    "Gerar agora", "Depois"))
                BuildAll();
        }

        [MenuItem("Ecos da Floresta/1. Gerar projeto completo", priority = 1)]
        public static void BuildAll()
        {
            var start = DateTime.Now;
            try
            {
                Step("Configurando o projeto", 0.02f);
                ProjectConfigurator.Configure();

                Step("Gerando sprites em pixel art", 0.08f);
                AssetGenerator.GenerateArt();

                Step("Gerando músicas e efeitos sonoros", 0.25f);
                AssetGenerator.GenerateAudio();

                Step("Aplicando ícone e opções do executável", 0.35f);
                ProjectConfigurator.Configure();

                Step("Criando tiles", 0.4f);
                foreach (var theme in new[] { Theme.Forest, Theme.Cave, Theme.Ruins }) TileFactory.Create(theme);
                AssetDatabase.SaveAssets();

                Step("Criando animações", 0.48f);
                var anims = AnimationFactory.CreateAll();

                Step("Criando prefabs", 0.56f);
                var prefabs = PrefabFactory.CreateAll(anims);
                var levelCanvas = UIBuilder.CreateLevelCanvasPrefab();
                AssetDatabase.SaveAssets();

                var scenes = new List<string>();
                Step("Montando o menu principal", 0.64f);
                scenes.Add(SceneFactory.BuildMainMenu(prefabs, anims.Crystal));

                var levels = LevelDesigns.All();
                for (int i = 0; i < levels.Count; i++)
                {
                    var level = levels[i];
                    Step("Montando a fase " + level.Number + ": " + level.DisplayName, 0.7f + i * 0.08f);
                    foreach (var warning in level.Validate()) Debug.LogWarning("[Ecos] " + level.SceneName + ": " + warning);
                    bool final = i == levels.Count - 1;
                    string next = final ? GameConstants.SceneVictory : levels[i + 1].SceneName;
                    scenes.Add(SceneFactory.BuildLevel(level, prefabs, levelCanvas, next, final));
                }

                Step("Montando a tela de vitória", 0.95f);
                scenes.Add(SceneFactory.BuildVictory(prefabs, anims.Crystal));

                var buildScenes = new List<EditorBuildSettingsScene>();
                foreach (var s in scenes) buildScenes.Add(new EditorBuildSettingsScene(s, true));
                EditorBuildSettings.scenes = buildScenes.ToArray();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorSceneManager.OpenScene(scenes[0]);
                Debug.Log($"[Ecos] Projeto gerado com sucesso em {(DateTime.Now - start).TotalSeconds:0.0}s. Cenas: {string.Join(", ", scenes)}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        static void Step(string message, float progress)
        {
            Debug.Log("[Ecos] " + message + "...");
            if (!Application.isBatchMode) EditorUtility.DisplayProgressBar("Ecos da Floresta", message, progress);
        }

        /// <summary>Entrada para linha de comando (sai com código 1 em caso de erro).</summary>
        public static void BatchBuildAll()
        {
            try
            {
                BuildAll();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("[Ecos] Falha ao gerar o projeto: " + e);
                EditorApplication.Exit(1);
            }
        }
    }
}
