using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace GPUGrass.Editor
{
    public static class GrassSceneSetup
    {
        public const string ScenePath = "Assets/GPUGrass/Demo/Grass Meadow.unity";
        [MenuItem("Tools/GPU Grass/Create Meadow Scene")]
        public static void Create()
        {
            Directory.CreateDirectory("Assets/GPUGrass/Demo");
            AssetDatabase.Refresh();
            Scene previous = SceneManager.GetActiveScene();
            if (!Application.isBatchMode && string.IsNullOrEmpty(previous.path))
            {
                Debug.LogError("Save the current untitled scene before creating the meadow.");
                return;
            }
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var field = new GameObject("GPU Grass - 240k blades").AddComponent<GpuGrass>();
                var camera = new GameObject("Meadow Camera").AddComponent<Camera>();
                camera.tag = "MainCamera"; camera.nearClipPlane = .1f; camera.farClipPlane = 180;
                camera.transform.position = new Vector3(8, 3, -30);
                camera.transform.rotation = Quaternion.Euler(8, -12, 0);
                camera.backgroundColor = new Color(.56f, .72f, .82f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.gameObject.AddComponent<AudioListener>();
                camera.gameObject.AddComponent<GrassDemo>().grass = field;
                var sun = new GameObject("Warm afternoon sun").AddComponent<Light>();
                sun.type = LightType.Directional; sun.intensity = 1.6f;
                sun.color = new Color(1, .91f, .74f); sun.shadows = LightShadows.Soft;
                sun.transform.rotation = Quaternion.Euler(38, -35, 0);
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Meadow soil"; ground.transform.localScale = Vector3.one * 10.5f;
                ground.transform.position = new Vector3(0, -.025f, 0);
                const string materialPath = "Assets/GPUGrass/Demo/Meadow Soil.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (!material)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    material.SetColor("_BaseColor", new Color(.16f, .19f, .07f));
                    material.SetFloat("_Smoothness", 0);
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                ground.GetComponent<Renderer>().sharedMaterial = material;
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(.48f, .59f, .7f);
                RenderSettings.ambientEquatorColor = new Color(.32f, .36f, .25f);
                RenderSettings.ambientGroundColor = new Color(.12f, .15f, .07f);
                RenderSettings.sun = sun; RenderSettings.fog = true;
                RenderSettings.fogColor = camera.backgroundColor; RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogStartDistance = 55; RenderSettings.fogEndDistance = 115;
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
            Debug.Log("Grass meadow scene created: " + ScenePath);
        }

        public static void CreateAndValidate() { Create(); GrassValidation.Run(); }
    }
}
