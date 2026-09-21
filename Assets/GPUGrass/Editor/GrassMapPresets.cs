using UnityEditor;
using UnityEngine;

namespace GPUGrass.Editor
{
    public static class GrassMapPresets
    {
        public const string PathPreset = "Assets/GPUGrass/Presets/Three Grasses - Winding Path.asset";

        // Always duplicate the template, so painting never edits the saved preset.
        public static void ApplyPathPreset(GpuGrass field)
        {
            var template = AssetDatabase.LoadAssetAtPath<GrassPaintMap>(PathPreset);
            if (!template)
            {
                Debug.LogError("Missing grass map preset: " + PathPreset);
                return;
            }
            var map = Object.Instantiate(template);
            map.name = "Winding Path Paint";
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/GPUGrass/Presets/Winding Path Paint.asset");
            AssetDatabase.CreateAsset(map, path);
            Undo.RecordObject(field, "Apply winding path grass preset");
            field.paintMap = map;
            field.RefreshPaint();
            EditorUtility.SetDirty(field);
            PrefabUtility.RecordPrefabInstancePropertyModifications(field);
            AssetDatabase.SaveAssetIfDirty(map);
            SceneView.RepaintAll();
        }
    }
}
