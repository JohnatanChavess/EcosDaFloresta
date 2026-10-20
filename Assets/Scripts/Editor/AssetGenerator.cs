using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ecos.EditorTools.Generation;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Ecos.EditorTools
{
    /// <summary>
    /// Exporta a arte e o áudio gerados para a pasta Assets e configura os importadores
    /// (sprites em pixel art com filtro Point, recortes das folhas, compressão de áudio).
    /// </summary>
    public static class AssetGenerator
    {
        static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, AudioClip> audioCache = new Dictionary<string, AudioClip>();
        static List<string> sheetPaths = new List<string>();

        public static void GenerateArt()
        {
            var sheets = ArtLibrary.GenerateAll();
            sheetPaths = sheets.Select(s => "Assets/" + s.RelativePath).ToList();

            foreach (var sheet in sheets)
            {
                string full = Path.Combine(Application.dataPath, sheet.RelativePath);
                PngEncoder.Save(sheet.Canvas, full);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            foreach (var sheet in sheets) ConfigureTexture(sheet);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            spriteCache.Clear();
        }

        static void ConfigureTexture(SheetAsset sheet)
        {
            string path = "Assets/" + sheet.RelativePath;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("[Ecos] Importador não encontrado: " + path);
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = sheet.PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096;
            importer.wrapMode = sheet.RepeatWrap ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = sheet.FullRectMesh ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
            settings.spriteExtrude = 0;
            settings.spriteGenerateFallbackPhysicsShape = false;

            bool single = sheet.Sprites.Count == 1 && sheet.Sprites[0].W == sheet.Canvas.Width && sheet.Sprites[0].H == sheet.Canvas.Height;
            if (single)
            {
                var def = sheet.Sprites[0];
                settings.spriteMode = (int)SpriteImportMode.Single;
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2((float)def.PivotX, (float)def.PivotY);
                settings.spriteBorder = new Vector4(def.BorderL, def.BorderB, def.BorderR, def.BorderT);
                importer.SetTextureSettings(settings);
                importer.spriteImportMode = SpriteImportMode.Single;
            }
            else
            {
                settings.spriteMode = (int)SpriteImportMode.Multiple;
                importer.SetTextureSettings(settings);
                importer.spriteImportMode = SpriteImportMode.Multiple;
                ApplySpriteRects(importer, sheet);
            }
            importer.SaveAndReimport();
        }

        static void ApplySpriteRects(TextureImporter importer, SheetAsset sheet)
        {
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            // Reaproveita os IDs existentes para não quebrar referências ao regenerar.
            var existing = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
            int h = sheet.Canvas.Height;
            var rects = new List<SpriteRect>();
            foreach (var def in sheet.Sprites)
            {
                rects.Add(new SpriteRect
                {
                    name = def.Name,
                    spriteID = existing.TryGetValue(def.Name, out var id) ? id : GUID.Generate(),
                    rect = new Rect(def.X, h - def.Y - def.H, def.W, def.H),
                    alignment = SpriteAlignment.Custom,
                    pivot = new Vector2((float)def.PivotX, (float)def.PivotY),
                    border = new Vector4(def.BorderL, def.BorderB, def.BorderR, def.BorderT)
                });
            }
            provider.SetSpriteRects(rects.ToArray());

            var nameProvider = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (nameProvider != null)
                nameProvider.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());

            provider.Apply();
        }

        public static void GenerateAudio()
        {
            var clips = AudioLibrary.GenerateAll();
            foreach (var clip in clips)
            {
                string full = Path.Combine(Application.dataPath, clip.RelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(full));
                File.WriteAllBytes(full, SoundSynth.ToWav(clip.Samples));
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            foreach (var clip in clips)
            {
                string path = "Assets/" + clip.RelativePath;
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) continue;
                importer.forceToMono = true;
                importer.loadInBackground = clip.IsMusic;
                var s = importer.defaultSampleSettings;
                if (clip.IsMusic)
                {
                    s.loadType = AudioClipLoadType.CompressedInMemory;
                    s.compressionFormat = AudioCompressionFormat.Vorbis;
                    s.quality = 0.6f;
                }
                else
                {
                    s.loadType = AudioClipLoadType.DecompressOnLoad;
                    s.compressionFormat = AudioCompressionFormat.ADPCM;
                }
                importer.defaultSampleSettings = s;
                importer.SaveAndReimport();
            }
            audioCache.Clear();
        }

        // ------------------------------------------------------------------ Consulta

        public static Sprite Sprite(string name)
        {
            if (spriteCache.Count == 0) LoadSprites();
            if (spriteCache.TryGetValue(name, out var s)) return s;
            Debug.LogError("[Ecos] Sprite não encontrado: " + name);
            return null;
        }

        public static Sprite[] Frames(string baseName, int count)
        {
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++) frames[i] = Sprite(baseName + "_" + i);
            return frames;
        }

        static void LoadSprites()
        {
            var paths = sheetPaths.Count > 0
                ? sheetPaths
                : AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Sprites", "Assets/UI" }).Select(AssetDatabase.GUIDToAssetPath).ToList();
            foreach (var path in paths)
                foreach (var s in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
                    spriteCache[s.name] = s;
        }

        public static AudioClip Audio(string name)
        {
            if (audioCache.TryGetValue(name, out var clip)) return clip;
            string[] folders = { "Assets/Audio/SFX/", "Assets/Audio/Music/" };
            foreach (var f in folders)
            {
                clip = AssetDatabase.LoadAssetAtPath<AudioClip>(f + name + ".wav");
                if (clip != null)
                {
                    audioCache[name] = clip;
                    return clip;
                }
            }
            Debug.LogError("[Ecos] Áudio não encontrado: " + name);
            return null;
        }

        public static Texture2D Texture(string assetPath) => AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
    }
}
