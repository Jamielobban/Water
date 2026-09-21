using UnityEditor;
using UnityEngine;

namespace AdvancedWater.Editor
{
    [CustomEditor(typeof(WaterSurface))]
    public sealed class WaterSurfaceEditor : UnityEditor.Editor
    {
        UnityEditor.Editor profileEditor;
        bool showProfile=true;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            WaterSurface water=(WaterSurface)target;
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Profile: wave motion, quality and underwater settings. Material: colors, normals, foam, refraction and caustics. Keep the surface horizontal.",MessageType.Info);
            if (Vector3.Dot(water.transform.up,Vector3.up)<0.999f)
                EditorGUILayout.HelpBox("Tilted surfaces are unsupported. Reset X/Z rotation for matching rendering, buoyancy and underwater height.",MessageType.Warning);
            if (water.gameObject.layer!=4)
                EditorGUILayout.HelpBox("Set the object to the Water layer (4) before enabling planar reflections.",MessageType.Warning);
            if (!water.profile) return;
            if (GUILayout.Button("Select Appearance Material") && water.profile.material) Selection.activeObject=water.profile.material;
            showProfile=EditorGUILayout.Foldout(showProfile,"Edit Shared Water Profile",true);
            if (showProfile)
            {
                CreateCachedEditor(water.profile,null,ref profileEditor);
                profileEditor.OnInspectorGUI();
            }
        }
        void OnDisable() { if (profileEditor) DestroyImmediate(profileEditor); }
    }
}
