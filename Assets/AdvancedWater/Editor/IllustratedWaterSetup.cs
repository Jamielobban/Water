using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AdvancedWater.Editor
{
    public static class IllustratedWaterSetup
    {
        static readonly string[] Names={"Anime Ocean","Low-Poly","Cozy Pastel","Watercolor Lake","Manga Sea","Retro Pixel Water","Paper-cut Ocean","Bioluminescent Water"};
        const string Root=WaterSetup.Root;

        [MenuItem("Tools/Advanced Water/Add Illustrated Presets")]
        public static void Generate()
        {
            Shader shader=Shader.Find("Advanced Water/Stylized Water");
            if (!shader) throw new InvalidOperationException("Stylized Water shader is missing.");
            Directory.CreateDirectory(Root+"/Materials");
            Directory.CreateDirectory(Root+"/Profiles");
            AssetDatabase.Refresh();
            for (int i=0;i<Names.Length;i++)
            {
                string name=Names[i], path=Root+"/Materials/"+name+".mat";
                Material material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!material)
                {
                    material=new Material(shader){name=name};
                    material.SetFloat("_StyleMode",5+i);
                    material.SetColor("_ShallowColor",i==0?new Color(0.025f,0.57f,0.8f):i==1?new Color(0.12f,0.72f,0.68f):new Color(0.56f,0.88f,0.78f));
                    material.SetColor("_DeepColor",i==0?new Color(0.012f,0.09f,0.38f):i==1?new Color(0.025f,0.22f,0.36f):new Color(0.38f,0.38f,0.65f));
                    material.SetColor("_ShadowColor",i==2?new Color(0.42f,0.34f,0.56f):new Color(0.12f,0.22f,0.4f));
                    material.SetColor("_AccentColor",i==2?new Color(0.96f,0.79f,0.9f):new Color(0.3f,0.8f,0.87f));
                    material.SetColor("_FoamColor",i==2?new Color(1,0.95f,0.79f):new Color(0.95f,0.99f,1));
                    material.SetFloat("_ColorDepth",i==2?5:9);
                    material.SetFloat("_ColorSteps",i==0?3:4);
                    material.SetFloat("_NormalStrength",i==0?0.12f:0);
                    material.SetTexture("_NormalA",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/Textures/Normals/SmoothWaves.png"));
                    material.SetTexture("_NoiseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/Water Noise.png"));
                    material.SetTexture("_FoamMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/Textures/Foam/Foam1.png"));
                    material.SetTexture("_ShoreFoamMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/Textures/Foam/Foam2.png"));
                    material.SetFloat("_BrushStrength",0);
                    material.SetFloat("_ReflectionStrength",i==1?0.12f:0.18f);
                    material.SetFloat("_HighlightStrength",i==0?0.35f:i==1?0.2f:0.1f);
                    material.SetFloat("_MarkScale",i==0?0.65f:0.42f);
                    material.SetFloat("_MarkStrength",i==1?0:0.85f);
                    material.SetFloat("_MarkSpeed",i==0?0.1f:0.035f);
                    material.SetFloat("_SparkleDensity",0.24f);
                    material.SetFloat("_CrestFoamStrength",0);
                    material.SetFloat("_ShoreFoamWidth",i==2?0.65f:0.9f);
                    material.SetFloat("_ShoreFoamStrength",i==1?0.6f:1.1f);
                    material.SetFloat("_ShoreWaveStrength",i==0?0.35f:0);
                    material.SetFloat("_InteractionFoamStrength",i==2?0.4f:0.1f);
                    material.SetFloat("_EdgeFade",0.12f);
                    if (i>=3) ConfigurePrintMaterial(material,i);
                    if (i>=6) ConfigureFantasyMaterial(material,i);
                    material.EnableKeyword("_FOAM");
                    AssetDatabase.CreateAsset(material,path);
                }
                path=Root+"/Profiles/"+name+".asset";
                if (AssetDatabase.LoadAssetAtPath<WaterProfile>(path)) continue;
                WaterProfile profile=ScriptableObject.CreateInstance<WaterProfile>();
                profile.name=name; profile.material=material;
                float amplitude=i==0?0.55f:i==1?0.65f:0.09f;
                profile.wave1=new WaterWave(18,amplitude,18);
                profile.wave2=new WaterWave(76,amplitude*0.42f,10);
                profile.wave3=new WaterWave(-35,amplitude*0.15f,5.5f);
                profile.wave4=new WaterWave(110,amplitude*0.04f,3);
                profile.waveSpeed=i==2?0.45f:0.8f;
                profile.steepness=i==2?0.2f:0.5f;
                profile.crestSharpness=i==0?0.65f:i==1?0.25f:0;
                profile.shapeVariation=i==2?0.2f:0.4f;
                profile.rippleStrength=i==1?0.25f:0.65f;
                profile.meshResolutionOverride=i==1?48:0;
                if (i>=3) ConfigurePrintWaves(profile,i);
                if (i>=6) ConfigureFantasyWaves(profile,i);
                profile.underwaterColor=material.GetColor("_DeepColor");
                AssetDatabase.CreateAsset(profile,path);
            }
            AssetDatabase.SaveAssets();
        }

        static void ConfigurePrintMaterial(Material material,int index)
        {
            switch (index)
            {
                case 3:
                    material.SetFloat("_BrushScale",0.16f);
                    material.SetFloat("_BrushStrength",0.65f);
                    material.SetFloat("_PaperScale",7.0f);
                    material.SetFloat("_PaperStrength",0.28f);
                    material.SetFloat("_PigmentStrength",0.65f);
                    material.SetFloat("_MarkStrength",0.0f);
                    material.SetFloat("_NormalStrength",0.0f);
                    material.SetFloat("_ReflectionStrength",0.0f);
                    material.SetFloat("_HighlightStrength",0.0f);
                    material.SetFloat("_ColorDepth",7.0f);
                    material.SetFloat("_ShoreFoamStrength",0.8f);
                    material.SetFloat("_ShoreFoamWidth",0.8f);
                    material.SetFloat("_InteractionFoamStrength",0.15f);
                    material.SetColor("_ShallowColor",new Color(0.62f,0.84f,0.77f));
                    material.SetColor("_DeepColor",new Color(0.16f,0.35f,0.49f));
                    material.SetColor("_ShadowColor",new Color(0.35f,0.43f,0.51f));
                    material.SetColor("_AccentColor",new Color(0.55f,0.65f,0.82f));
                    material.SetColor("_FoamColor",new Color(0.96f,0.92f,0.8f));
                    break;
                case 4:
                    material.SetFloat("_BrushStrength",0.0f);
                    material.SetFloat("_MarkStrength",0.0f);
                    material.SetFloat("_NormalStrength",0.08f);
                    material.SetFloat("_HatchScale",1.6f);
                    material.SetFloat("_HatchStrength",0.85f);
                    material.SetFloat("_OutlineStrength",0.85f);
                    material.SetFloat("_ReflectionStrength",0.0f);
                    material.SetFloat("_HighlightStrength",0.0f);
                    material.SetFloat("_ColorDepth",10.0f);
                    material.SetFloat("_ColorSteps",3.0f);
                    material.SetFloat("_CrestFoamStrength",0.3f);
                    material.SetFloat("_ShoreFoamStrength",1.1f);
                    material.SetFloat("_ShoreFoamWidth",1.1f);
                    material.SetFloat("_ShoreWaveStrength",0.22f);
                    material.SetFloat("_InteractionFoamStrength",0.2f);
                    material.SetColor("_ShallowColor",new Color(0.9f,0.88f,0.79f));
                    material.SetColor("_DeepColor",new Color(0.38f,0.4f,0.41f));
                    material.SetColor("_ShadowColor",new Color(0.018f,0.028f,0.045f));
                    material.SetColor("_AccentColor",new Color(0.68f,0.72f,0.73f));
                    material.SetColor("_FoamColor",new Color(1.0f,0.98f,0.9f));
                    break;
                case 5:
                    material.SetFloat("_BrushStrength",0.0f);
                    material.SetFloat("_MarkScale",0.75f);
                    material.SetFloat("_MarkStrength",0.8f);
                    material.SetFloat("_MarkSpeed",0.35f);
                    material.SetFloat("_NormalStrength",0.0f);
                    material.SetFloat("_PixelSize",0.28f);
                    material.SetFloat("_PixelFPS",8.0f);
                    material.SetFloat("_DitherStrength",0.65f);
                    material.SetFloat("_ReflectionStrength",0.0f);
                    material.SetFloat("_HighlightStrength",0.0f);
                    material.SetFloat("_ColorDepth",7.0f);
                    material.SetFloat("_ColorSteps",3.0f);
                    material.SetFloat("_ShoreFoamStrength",0.9f);
                    material.SetFloat("_ShoreFoamWidth",0.7f);
                    material.SetFloat("_InteractionFoamStrength",0.25f);
                    material.SetColor("_ShallowColor",new Color(0.1f,0.62f,0.73f));
                    material.SetColor("_DeepColor",new Color(0.035f,0.09f,0.3f));
                    material.SetColor("_ShadowColor",new Color(0.025f,0.045f,0.14f));
                    material.SetColor("_AccentColor",new Color(0.4f,0.93f,0.9f));
                    material.SetColor("_FoamColor",new Color(0.83f,1.0f,0.94f));
                    break;
            }
        }

        static void ConfigurePrintWaves(WaterProfile profile,int index)
        {
            float amplitude=0.1f;
            switch (index)
            {
                case 3:
                    amplitude=0.16f; profile.waveSpeed=0.5f;
                    profile.steepness=0.25f; profile.crestSharpness=0.1f;
                    profile.shapeVariation=0.35f; break;
                case 4:
                    amplitude=0.5f; profile.waveSpeed=0.75f;
                    profile.steepness=0.55f; profile.crestSharpness=0.6f;
                    profile.shapeVariation=0.45f; break;
                case 5:
                    amplitude=0.12f; profile.waveSpeed=0.6f;
                    profile.steepness=0.2f; profile.crestSharpness=0.15f;
                    profile.shapeVariation=0.15f; break;
            }
            profile.wave1=new WaterWave(18,amplitude,18);
            profile.wave2=new WaterWave(76,amplitude*0.42f,10);
            profile.wave3=new WaterWave(-35,amplitude*0.15f,5.5f);
            profile.wave4=new WaterWave(110,amplitude*0.04f,3);
        }

        static void ConfigureFantasyMaterial(Material material,int index)
        {
            switch (index)
            {
                case 6:
                    material.SetFloat("_LayerScale",0.13f);
                    material.SetFloat("_LayerShadow",0.45f);
                    material.SetFloat("_LayerShadowWidth",0.07f);
                    material.SetFloat("_ScallopScale",0.55f);
                    material.SetFloat("_ScallopSize",0.045f);
                    material.SetFloat("_PaperEdgeWidth",0.045f);
                    material.SetFloat("_PaperScale",6.0f);
                    material.SetFloat("_PaperStrength",0.22f);
                    material.SetFloat("_MarkSpeed",0.065f);
                    material.SetFloat("_MarkStrength",0.0f);
                    material.SetFloat("_BrushStrength",0.0f);
                    material.SetFloat("_NormalStrength",0.0f);
                    material.SetFloat("_ReflectionStrength",0.0f);
                    material.SetFloat("_HighlightStrength",0.0f);
                    material.SetFloat("_ColorDepth",9.0f);
                    material.SetFloat("_ShoreFoamStrength",0.8f);
                    material.SetFloat("_ShoreFoamWidth",0.8f);
                    material.SetFloat("_InteractionFoamStrength",0.1f);
                    material.SetColor("_ShallowColor",new Color(0.15f,0.61f,0.83f));
                    material.SetColor("_DeepColor",new Color(0.035f,0.12f,0.34f));
                    material.SetColor("_ShadowColor",new Color(0.018f,0.045f,0.12f));
                    material.SetColor("_AccentColor",new Color(0.36f,0.77f,0.93f));
                    material.SetColor("_FoamColor",new Color(0.94f,0.95f,0.86f));
                    break;
                case 7:
                    material.SetFloat("_BioGlowStrength",3.0f);
                    material.SetFloat("_BioTrailStrength",4.0f);
                    material.SetFloat("_BioDensity",0.35f);
                    material.SetFloat("_BioScale",1.2f);
                    material.SetFloat("_BioFleckSize",0.045f);
                    material.SetFloat("_BioPulseSpeed",0.8f);
                    material.SetFloat("_MarkStrength",0.0f);
                    material.SetFloat("_BrushStrength",0.0f);
                    material.SetFloat("_NormalStrength",0.06f);
                    material.SetFloat("_ReflectionStrength",0.18f);
                    material.SetFloat("_HighlightStrength",0.0f);
                    material.SetFloat("_ColorDepth",8.0f);
                    material.SetFloat("_ShoreFoamStrength",0.0f);
                    material.SetFloat("_InteractionFoamStrength",0.0f);
                    material.SetColor("_ShallowColor",new Color(0.012f,0.065f,0.1f));
                    material.SetColor("_DeepColor",new Color(0.002f,0.006f,0.022f));
                    material.SetColor("_ShadowColor",new Color(0.001f,0.002f,0.009f));
                    material.SetColor("_AccentColor",new Color(0.035f,0.26f,0.4f));
                    material.SetColor("_FoamColor",new Color(0.04f,0.5f,0.65f));
                    material.SetColor("_BioGlowColor",new Color(0.06f,0.85f,1.0f));
                    break;
            }
        }

        static void ConfigureFantasyWaves(WaterProfile profile,int index)
        {
            float amplitude=0.2f;
            switch (index)
            {
                case 6:
                    amplitude=0.2f; profile.waveSpeed=0.45f;
                    profile.steepness=0.18f; profile.crestSharpness=0.15f;
                    profile.shapeVariation=0.2f; profile.rippleStrength=0.5f;
                    profile.rippleLifetime=5.0f; break;
                case 7:
                    amplitude=0.18f; profile.waveSpeed=0.55f;
                    profile.steepness=0.3f; profile.crestSharpness=0.2f;
                    profile.shapeVariation=0.35f; profile.rippleStrength=1.0f;
                    profile.rippleLifetime=7.0f; break;
            }
            profile.wave1=new WaterWave(18,amplitude,18);
            profile.wave2=new WaterWave(76,amplitude*0.42f,10);
            profile.wave3=new WaterWave(-35,amplitude*0.15f,5.5f);
            profile.wave4=new WaterWave(110,amplitude*0.04f,3);
        }

        static void ValidateMeshSwitching()
        {
            var water=UnityEngine.Object.FindFirstObjectByType<WaterSurface>();
            var mesh=water.GetComponent<WaterMesh>();
            var filter=water.GetComponent<MeshFilter>();
            var original=water.profile;
            int originalResolution=mesh.resolution;
            try
            {
                foreach (string name in new[]{"Low-Poly","Anime Ocean","Low-Poly","Cozy Pastel"})
                {
                    water.profile=AssetDatabase.LoadAssetAtPath<WaterProfile>(Root+"/Profiles/"+name+".asset");
                    mesh.Rebuild();
                    int resolution=water.profile.meshResolutionOverride>0
                        ? Mathf.Clamp(water.profile.meshResolutionOverride,16,256):Mathf.Clamp(originalResolution,16,256);
                    if (filter.sharedMesh.vertexCount!=(resolution+1)*(resolution+1) || mesh.resolution!=originalResolution)
                        throw new InvalidOperationException("Profile mesh switching failed: "+name);
                }
                Debug.Log("Illustrated water mesh switching passed: coarse grid and original resolution restored.");
            }
            finally { water.profile=original; water.Refresh(); mesh.Rebuild(); }
        }

        // Preserve the existing demo scene and append only missing profiles.
        public static void InstallAndValidate()
        {
            Generate();
            var scene=EditorSceneManager.OpenScene(Root+"/Demo/Water Demo.unity");
            foreach (var controller in UnityEngine.Object.FindObjectsByType<WaterDemoController>(FindObjectsSortMode.None))
            {
                var profiles=(controller.presets??Array.Empty<WaterProfile>()).ToList();
                foreach (string name in Names)
                {
                    var profile=AssetDatabase.LoadAssetAtPath<WaterProfile>(Root+"/Profiles/"+name+".asset");
                    if (!profiles.Contains(profile)) profiles.Add(profile);
                }
                controller.presets=profiles.ToArray();
                EditorUtility.SetDirty(controller);
            }
            ValidateMeshSwitching();
            EditorSceneManager.SaveScene(scene);
            WaterSetup.BuildAndValidate();
        }
    }
}
