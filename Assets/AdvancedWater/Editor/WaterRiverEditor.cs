using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedWater.Editor
{
    [CustomEditor(typeof(WaterRiver))]
    public sealed class WaterRiverEditor : UnityEditor.Editor
    {
        [MenuItem("Tools/Advanced Water/Open River Demo Scene")]
        static void OpenDemo()
        {
            if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/AdvancedWater/Demo/River/River Demo.unity");
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("Points run downstream in local space. Move the scene handles to shape the river. Foam controls rapids and the waterfall landing. This surface has its own shader; lake/ocean buoyancy and underwater effects do not apply.",MessageType.Info);
        }
        void OnSceneGUI()
        {
            var river=(WaterRiver)target;
            if (river.points==null) return;
            for (int i=0;i<river.points.Length;i++)
            {
                Vector3 world=river.transform.TransformPoint(river.points[i].position);
                Handles.Label(world,"River "+i);
                EditorGUI.BeginChangeCheck();
                Vector3 moved=Handles.PositionHandle(world,river.transform.rotation);
                if (!EditorGUI.EndChangeCheck()) continue;
                Undo.RecordObject(river,"Move River Point");
                river.points[i].position=river.transform.InverseTransformPoint(moved);
                river.Rebuild(); EditorUtility.SetDirty(river);
                PrefabUtility.RecordPrefabInstancePropertyModifications(river);
            }
        }

        [MenuItem("GameObject/Advanced Water/River with Waterfall",false,10)]
        static void Create(MenuCommand command)
        {
            Shader shader=Shader.Find("Advanced Water/River and Waterfall");
            if (!shader) { Debug.LogError("River and Waterfall shader is not imported yet."); return; }
            const string path="Assets/AdvancedWater/Materials/River and Waterfall.mat";
            Material material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material=new Material(shader) { name="River and Waterfall" };
                AssetDatabase.CreateAsset(material,path);
            }
            var go=new GameObject("River with Waterfall") { layer=4 };
            GameObjectUtility.SetParentAndAlign(go,command.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go,"Create River with Waterfall");
            var river=go.AddComponent<WaterRiver>();
            river.material=material; river.Rebuild();
            go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            Selection.activeGameObject=go;
            SceneView.lastActiveSceneView?.FrameSelected();
        }
    }
}
