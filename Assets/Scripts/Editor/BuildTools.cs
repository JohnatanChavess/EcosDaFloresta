using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ecos.EditorTools
{
    /// <summary>Gera o executável para Windows e capturas da visão geral das fases.</summary>
    public static class BuildTools
    {
        public const string WindowsBuildPath = "Build/Windows/EcosDaFloresta.exe";

        [MenuItem("Ecos da Floresta/2. Gerar executável para Windows", priority = 20)]
        public static void BuildWindows()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) throw new Exception("Nenhuma cena nas Build Settings. Gere o projeto primeiro.");
            Directory.CreateDirectory(Path.GetDirectoryName(WindowsBuildPath));
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = WindowsBuildPath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[Ecos] Build {summary.result}: {summary.totalSize / (1024f * 1024f):0.0} MB, {summary.totalErrors} erros, {summary.totalWarnings} avisos, " +
                      $"tempo {summary.totalTime.TotalSeconds:0}s -> {Path.GetFullPath(WindowsBuildPath)}");
            if (summary.result != BuildResult.Succeeded) throw new Exception("O build falhou: " + summary.result);
        }

        public static void BatchBuildWindows()
        {
            try
            {
                BuildWindows();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("[Ecos] " + e);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Renderiza cada fase inteira em um PNG (visão geral do level design).</summary>
        [MenuItem("Ecos da Floresta/3. Capturar visão geral das fases", priority = 40)]
        public static void CaptureLevelOverviews()
        {
            string folder = Path.GetFullPath("Screenshots");
            Directory.CreateDirectory(folder);
            foreach (string scene in new[] { GameConstants.SceneLevel1, GameConstants.SceneLevel2, GameConstants.SceneLevel3 })
            {
                EditorSceneManager.OpenScene(SceneFactory.SceneFolder + scene + ".unity");
                var grid = UnityEngine.Object.FindAnyObjectByType<Grid>();
                var ground = grid.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>().First();
                ground.CompressBounds();
                var bounds = ground.cellBounds;
                float width = bounds.xMax, height = Mathf.Max(bounds.yMax + 3, 14);

                foreach (var p in UnityEngine.Object.FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None)) p.gameObject.SetActive(false);
                var mainCam = Camera.main;
                var sky = mainCam.transform.Find("Ceu");
                if (sky != null) sky.gameObject.SetActive(false);

                var go = new GameObject("CapturaTemp");
                var cam = go.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = height / 2f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = mainCam.backgroundColor;
                go.transform.position = new Vector3(width / 2f, height / 2f, -10f);
                int pxW = Mathf.RoundToInt(width * 16), pxH = Mathf.RoundToInt(height * 16);
                var rt = new RenderTexture(pxW, pxH, 24) { filterMode = FilterMode.Point };
                cam.aspect = width / height;
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(pxW, pxH, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, pxW, pxH), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(folder, scene + "_visao_geral.png"), tex.EncodeToPNG());
                cam.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(go);
                Debug.Log("[Ecos] Captura salva: " + scene);
            }
            EditorSceneManager.OpenScene(SceneFactory.SceneFolder + GameConstants.SceneMainMenu + ".unity");
        }

        public static void BatchCapture()
        {
            try
            {
                CaptureLevelOverviews();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("[Ecos] " + e);
                EditorApplication.Exit(1);
            }
        }
    }
}
