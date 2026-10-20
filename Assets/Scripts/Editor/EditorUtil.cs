using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ecos.EditorTools
{
    /// <summary>Utilidades do construtor: pastas, propriedades serializadas e criação de assets.</summary>
    public static class EditorUtil
    {
        public static void EnsureFolder(string assetPath)
        {
            assetPath = assetPath.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(assetPath)) return;
            string parent = Path.GetDirectoryName(assetPath).Replace('\\', '/');
            string name = Path.GetFileName(assetPath);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        /// <summary>Altera um campo serializado (inclusive privado) e registra a mudança (funciona em instâncias de prefab).</summary>
        public static void Set(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"[Ecos] Campo '{field}' não encontrado em {target.GetType().Name}");
                return;
            }
            switch (value)
            {
                case null: prop.objectReferenceValue = null; break;
                case Object o: prop.objectReferenceValue = o; break;
                case bool b: prop.boolValue = b; break;
                case int i:
                    if (prop.propertyType == SerializedPropertyType.LayerMask) prop.intValue = i;
                    else if (prop.propertyType == SerializedPropertyType.Enum) prop.enumValueIndex = i;
                    else if (prop.propertyType == SerializedPropertyType.Float) prop.floatValue = i;
                    else prop.intValue = i;
                    break;
                case float f: prop.floatValue = f; break;
                case double d: prop.floatValue = (float)d; break;
                case string s: prop.stringValue = s; break;
                case Vector2 v2: prop.vector2Value = v2; break;
                case Vector3 v3: prop.vector3Value = v3; break;
                case Color c: prop.colorValue = c; break;
                default:
                    Debug.LogError($"[Ecos] Tipo não suportado para '{field}': {value.GetType()}");
                    return;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static T CreateOrReplaceAsset<T>(T asset, string path) where T : Object
        {
            EnsureFolder(Path.GetDirectoryName(path));
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(asset, existing);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        public static GameObject SavePrefab(GameObject go, string path)
        {
            EnsureFolder(Path.GetDirectoryName(path));
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        public static GameObject Instantiate(GameObject prefab, Transform parent, Vector3 position)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = position;
            return go;
        }

        public static GameObject Child(Transform parent, string name, Vector3 localPosition = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go;
        }

        public static Vector3 Snap(Vector3 p)
        {
            return new Vector3(Mathf.Round(p.x * 16f) / 16f, Mathf.Round(p.y * 16f) / 16f, p.z);
        }
    }
}
