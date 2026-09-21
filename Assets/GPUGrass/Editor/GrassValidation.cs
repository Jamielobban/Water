using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GPUGrass.Editor
{
    // Run only in an isolated validation project; opens the meadow scene.
    public static class GrassValidation
    {
        public static void Run()
        {
            EditorSceneManager.OpenScene(GrassSceneSetup.ScenePath);
            var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            var target = new RenderTexture(1280, 720, 24);
            target.Create();
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                var previous = RenderTexture.active;
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
                RenderTexture.active = previous;
                File.WriteAllBytes("meadow-preview.png", image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);
                var shader = Resources.Load<Shader>("Grass");
                foreach (var message in ShaderUtil.GetShaderMessages(shader))
                    if (message.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)
                        throw new Exception(message.message);
                var compute = Resources.Load<ComputeShader>("Grass");
                foreach (var message in ShaderUtil.GetComputeShaderMessages(compute))
                    if (message.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)
                        throw new Exception(message.message);
                // Read back only in validation, never in the runtime renderer.
                var field = UnityEngine.Object.FindFirstObjectByType<GpuGrass>();
                var buffers = (ComputeBuffer[])typeof(GpuGrass).GetField("arguments", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(field);
                uint total = 0;
                for (int i = 0; i < buffers.Length; i++)
                {
                    var args = new uint[4]; buffers[i].GetData(args); total += args[1];
                    Debug.Log($"GRASS VALIDATION LOD {i}: {args[1]} blades, {args[0]} vertices per blade");
                    if (args[1] == 0) throw new Exception("Expected visible blades in every LOD.");
                }
                if (total > field.bladeCount) throw new Exception("LOD counts exceed source count.");
                Debug.Log("GRASS VALIDATION PASSED");
            }
            finally { camera.targetTexture = null; target.Release(); UnityEngine.Object.DestroyImmediate(target); }
        }
    }
}
