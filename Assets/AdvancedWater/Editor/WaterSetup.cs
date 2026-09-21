using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace AdvancedWater.Editor
{
    public static class WaterSetup
    {
        public const string Root="Assets/AdvancedWater";
        static readonly string[] BasePresetNames={"Lake","Calm Ocean","Rough Ocean","Tropical","Murky","Stylized"};
        static readonly string[] StylizedPresetNames={"Cel Lagoon","Wind Waker","Painterly Sea","Graphic Ink","Arcane Water","Anime Ocean","Low-Poly","Cozy Pastel","Watercolor Lake","Manga Sea","Retro Pixel Water","Paper-cut Ocean","Bioluminescent Water"};
        static readonly string[] PresetNames=BasePresetNames.Concat(StylizedPresetNames).ToArray();
        static string report;
        static RenderPipelineAsset validationPreviousPipeline;
        static int warmupFrames;
        static int captureCount;

        [MenuItem("Tools/Advanced Water/Regenerate Starter Textures")]
        public static void RegenerateTextures()
        {
            Directory.CreateDirectory(Root+"/Textures"); AssetDatabase.Refresh();
            Texture("Water Normal A",0,true); Texture("Water Normal B",1,true);
            Texture("Water Foam",2,true); Texture("Water Noise",3,true); Texture("Water Caustics",4,true);
        }

        public static void ImproveFoamAndValidate()
        {
            Texture("Water Foam",2,true);
            Texture("Water Caustics",4,true);
            Material rough=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Rough Ocean.mat");
            if (rough)
            {
                rough.SetFloat("_CrestThreshold",0.18f);
                rough.SetFloat("_CrestFoamStrength",0.55f);
                rough.SetFloat("_ShoreFoamWidth",1.8f);
                rough.SetFloat("_ShoreFoamStrength",0.8f);
                rough.SetFloat("_FoamScale",0.32f);
                rough.SetFloat("_FoamDistortion",0.36f);
                EditorUtility.SetDirty(rough);AssetDatabase.SaveAssets();
            }
            foreach (string preset in PresetNames)
            {
                Material water=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+preset+".mat");
                if (!water) continue;
                if (Mathf.Abs(water.GetFloat("_CausticsScale")-0.25f)<0.001f) water.SetFloat("_CausticsScale",0.65f);
                if (Mathf.Abs(water.GetFloat("_CausticsStrength")-0.5f)<0.001f) water.SetFloat("_CausticsStrength",0.3f);
                EditorUtility.SetDirty(water);
            }
            AssetDatabase.SaveAssets();
            BuildAndValidate();
        }

        [MenuItem("Tools/Advanced Water/Generate Starter Assets")]
        public static void GenerateAssets()
        {
            foreach (string folder in new[]{"Materials","Profiles","Textures","Prefabs","Demo","Demo/Settings","Demo/Materials"})
                Directory.CreateDirectory(Root+"/"+folder);
            AssetDatabase.Refresh();
            Texture2D normalA=Texture("Water Normal A",0),normalB=Texture("Water Normal B",1),
                foam=Texture("Water Foam",2),noise=Texture("Water Noise",3),caustics=Texture("Water Caustics",4);
            Shader shader=Shader.Find("Advanced Water/Lake and Ocean");
            if (!shader) throw new InvalidOperationException("Water shader was not imported.");
            for (int i=0;i<BasePresetNames.Length;i++)
            {
                string name=BasePresetNames[i],materialPath=Root+"/Materials/"+name+".mat";
                Material material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (!material)
                {
                    material=new Material(shader){name=name};
                    AssignPresetTextures(material,i,normalA,normalB,foam,noise,caustics);
                    material.EnableKeyword("_REFRACTION"); material.EnableKeyword("_FOAM"); material.EnableKeyword("_CAUSTICS");
                    ConfigureAppearance(material,i);
                    AssetDatabase.CreateAsset(material,materialPath);
                }
                string path=Root+"/Profiles/"+name+".asset";
                if (!AssetDatabase.LoadAssetAtPath<WaterProfile>(path))
                {
                    WaterProfile profile=ScriptableObject.CreateInstance<WaterProfile>();
                    profile.name=name; profile.material=material;
                    ConfigureWaves(profile,i); AssetDatabase.CreateAsset(profile,path);
                }
            }
            IllustratedWaterSetup.Generate();
            CreateDemoPipeline(caustics);
            CreatePrefab(false); CreatePrefab(true);
            AssetDatabase.SaveAssets();
        }

        static void AssignPresetTextures(Material material,int preset,Texture2D fallbackNormalA,Texture2D fallbackNormalB,
            Texture2D fallbackFoam,Texture2D fallbackNoise,Texture2D fallbackCaustics)
        {
            const string textures=Root+"/Textures/Textures/";
            string[] normalA={"SmoothWaves","SmoothWaves","SharpWaves","SmoothWaves","SmoothWaves","SharpWaves"};
            string[] normalB={"SharpWaves","SharpWaves","RoughWaves","SharpWaves","RoughWaves","RoughWaves"};
            string[] foam={"Foam1","Foam1","FoamSea","Foam1","Foam1","WindwakerFoam"};
            string[] shore={"Intersection_Foam","Intersection_Foam","Intersection_Foam","Intersection_Foam","Intersection_Foam","Foam2"};
            string[] caustics={"Caustics_2","Caustics_2","Caustics_1","Caustics_1","Caustics_2","Caustics_2"};
            Texture2D Load(string path,Texture2D fallback)
            {
                Texture2D result=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                return result?result:fallback;
            }
            material.SetTexture("_NormalA",Load(textures+"Normals/"+normalA[preset]+".png",fallbackNormalA));
            material.SetTexture("_NormalB",Load(textures+"Normals/"+normalB[preset]+".png",fallbackNormalB));
            material.SetTexture("_FoamMap",Load(textures+"Foam/"+foam[preset]+".png",fallbackFoam));
            material.SetTexture("_ShoreFoamMap",Load(textures+"Foam/"+shore[preset]+".png",fallbackFoam));
            material.SetTexture("_NoiseMap",Load(textures+"IntersectionNoise.png",fallbackNoise));
            material.SetTexture("_CausticsMap",Load(textures+"Caustics/"+caustics[preset]+".png",fallbackCaustics));
        }

        static void CreatePrefab(bool ocean)
        {
            string name=ocean?"Ocean":"Lake",path=Root+"/Prefabs/"+name+".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path)) return;
            GameObject go=new GameObject(name){layer=4};
            try
            {
                go.AddComponent<MeshFilter>(); var renderer=go.AddComponent<MeshRenderer>();
                renderer.shadowCastingMode=ShadowCastingMode.Off;
                var water=go.AddComponent<WaterSurface>();
                water.profile=AssetDatabase.LoadAssetAtPath<WaterProfile>(Root+"/Profiles/"+(ocean?"Calm Ocean":"Lake")+".asset");
                var mesh=go.AddComponent<WaterMesh>(); mesh.mode=ocean?WaterMesh.SurfaceMode.Ocean:WaterMesh.SurfaceMode.Lake;
                mesh.size=ocean?1600:150;mesh.resolution=ocean?192:128;
                water.Refresh(); go.AddComponent<WaterPlanarReflection>().enabled=false;
                // Generated meshes are reconstructed by OnEnable rather than embedded in prefab assets.
                go.GetComponent<MeshFilter>().sharedMesh=null;
                PrefabUtility.SaveAsPrefabAsset(go,path);
            }
            finally { Object.DestroyImmediate(go); }
        }

        static void ConfigureAppearance(Material m,int i)
        {
            Color[] shallow={new Color(0.08f,0.3f,0.23f),new Color(0.05f,0.3f,0.37f),new Color(0.08f,0.2f,0.24f),
                new Color(0.07f,0.65f,0.54f),new Color(0.28f,0.25f,0.12f),new Color(0.04f,0.68f,0.78f)};
            Color[] deep={new Color(0.012f,0.065f,0.058f),new Color(0.012f,0.055f,0.12f),new Color(0.016f,0.045f,0.068f),
                new Color(0.008f,0.14f,0.23f),new Color(0.045f,0.052f,0.019f),new Color(0.035f,0.12f,0.4f)};
            m.SetColor("_ShallowColor",shallow[i]); m.SetColor("_DeepColor",deep[i]);
            m.SetFloat("_CrestFoamStrength",i==2?0.55f:i==3?0.08f:0);
            m.SetFloat("_CrestThreshold",i==2?0.18f:0.8f);
            m.SetFloat("_NormalStrength",i==0?0.35f:i==2?0.48f:0.55f);
            m.SetFloat("_Smoothness",i==2?0.8f:0.92f);
            m.SetFloat("_ShoreFoamWidth",i==2?1.8f:0.75f);
            m.SetFloat("_ShoreFoamStrength",i==2?0.8f:0.9f);
            Vector3[] normalScale={new Vector3(0.07f,0.14f,0.9f),new Vector3(0.06f,0.16f,1.05f),
                new Vector3(0.07f,0.18f,1.1f),new Vector3(0.085f,0.22f,1.35f),
                new Vector3(0.07f,0.2f,1.1f),new Vector3(0.045f,0.11f,0.65f)};
            float[] foamScale={0.24f,0.28f,0.18f,0.34f,0.24f,0.12f};
            m.SetVector("_NormalScale",normalScale[i]);
            m.SetFloat("_FoamScale",foamScale[i]);
            if (i==2)
            {
                m.SetFloat("_FoamDistortion",0.2f);
                m.SetFloat("_MicroStrength",0.045f);
                m.SetFloat("_ShoreWaveStrength",0.9f);
                m.SetFloat("_Sparkle",0.08f);
            }
            m.SetVector("_Absorption",i==3?new Vector4(0.2f,0.075f,0.055f,0):i==4?new Vector4(0.75f,0.85f,1.2f,0):new Vector4(0.4f,0.17f,0.11f,0));
            if (i==5) { m.SetFloat("_StylizedSteps",5); m.SetFloat("_FoamDistortion",0.05f); }
            if (i==5)
            {
                m.SetColor("_FoamColor",Color.white);
                m.SetFloat("_FoamUnlit",1);
                m.SetFloat("_StylizedFoamCoverage",0.72f);
                m.SetFloat("_CrestThreshold",0.38f);
                m.SetFloat("_CrestFoamStrength",0);
                m.SetFloat("_ShoreFoamWidth",0.35f);
                m.SetFloat("_ShoreFoamStrength",0.75f);
                m.SetFloat("_ReflectionStrength",0.62f);
                m.SetFloat("_PlanarStrength",0.35f);
                m.SetFloat("_SunStrength",0.55f);
                m.SetFloat("_Sparkle",0);
                m.SetFloat("_NormalStrength",0.42f);
            }
            if (i==4) { m.SetFloat("_CausticsStrength",0.1f); m.SetFloat("_DepthDistance",3); }
        }
        static void ConfigureWaves(WaterProfile p,int i)
        {
            float[] amplitude={0.12f,0.55f,1.65f,0.3f,0.16f,0.3f};
            float a=amplitude[i];
            p.wave1=new WaterWave(i==2?-158:15,a,i==2?29:14);
            p.wave2=new WaterWave(i==2?-122:65,i==2?0.68f:a*0.45f,i==2?17.3f:7);
            p.wave3=new WaterWave(-35,i==2?0.26f:a*0.2f,i==2?8.1f:3.5f);
            p.wave4=new WaterWave(110,i==2?0.11f:a*0.1f,i==2?4.7f:1.7f);
            p.steepness=i==2?0.68f:0.45f; p.waveSpeed=i==0?0.9f:i==1?1.08f:i==2?1.12f:i==3?1.05f:i==4?0.95f:1;
            p.directionalSpread=i==5?0:i==0?0.8f:i==3||i==4?0.9f:1;
            p.shapeVariation=i==0?0.4f:i==2?0.85f:i==1?0.68f:i==3?0.55f:i==4?0.5f:0.3f;
            p.shapeScale=i==2?0.12f:i==0?0.09f:0.14f;
            p.crestSharpness=i==0?0.05f:i==2?0.72f:i==1?0.4f:0.3f;
            p.underwaterColor=p.material.GetColor("_DeepColor");
            p.underwaterDensity=i==4?0.4f:i==3?0.065f:0.1f;
        }

        static Texture2D Texture(string name,int kind,bool replace=false)
        {
            string path=Root+"/Textures/"+name+".png";
            Texture2D existing=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing && !replace) return existing;
            const int size=256;
            Texture2D texture=new Texture2D(size,size,TextureFormat.RGBA32,false,true);
            Color[] pixels=new Color[size*size];
            for (int y=0;y<size;y++) for (int x=0;x<size;x++)
            {
                float u=x/(float)size,v=y/(float)size;
                Color color;
                if (kind<2)
                {
                    float step=1f/size;
                    float dx=(NormalHeight(u+step,v,kind)-NormalHeight(u-step,v,kind))*9;
                    float dz=(NormalHeight(u,v+step,kind)-NormalHeight(u,v-step,kind))*9;
                    Vector3 normal=new Vector3(-dx,-dz,1).normalized;
                    color=new Color(normal.x*0.5f+0.5f,normal.y*0.5f+0.5f,normal.z*0.5f+0.5f,1);
                }
                else
                {
                    float n=PeriodicNoise(u,v);
                    float value=n;
                    if (kind==2)
                    {
                        float cloud=TileNoise(u,v,4,51)*0.5f+TileNoise(u,v,9,67)*0.3f+TileNoise(u,v,21,83)*0.2f;
                        float filaments=1-Mathf.Abs(TileNoise(u,v,16,97)*2-1);
                        value=Mathf.SmoothStep(0,1,Mathf.Clamp01((cloud-0.38f)*2.4f))*(0.38f+0.62f*Mathf.Pow(filaments,2));
                    }
                    else if (kind==4)
                    {
                        float cells=8;
                        Vector2 p=new Vector2(u*cells,v*cells);
                        int ix=Mathf.FloorToInt(p.x),iy=Mathf.FloorToInt(p.y);
                        float first=100,second=100;
                        for (int oy=-1;oy<=1;oy++) for (int ox=-1;ox<=1;ox++)
                        {
                            int cx=ix+ox,cy=iy+oy;
                            float hx=Mathf.Repeat(Mathf.Sin(((cx+(int)cells)%(int)cells)*127.1f+((cy+(int)cells)%(int)cells)*311.7f)*43758.5453f,1);
                            float hy=Mathf.Repeat(Mathf.Sin(((cx+(int)cells)%(int)cells)*269.5f+((cy+(int)cells)%(int)cells)*183.3f)*43758.5453f,1);
                            float distance=(p-new Vector2(cx+hx,cy+hy)).sqrMagnitude;
                            if (distance<first) {second=first;first=distance;} else if (distance<second) second=distance;
                        }
                        float edge=Mathf.Sqrt(second)-Mathf.Sqrt(first);
                        value=Mathf.Pow(Mathf.Clamp01(1-edge*5),8);
                    }
                    color=new Color(value,value,value,1);
                }
                pixels[y*size+x]=color;
            }
            texture.SetPixels(pixels); texture.Apply(); File.WriteAllBytes(path,texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=kind<2?TextureImporterType.NormalMap:TextureImporterType.Default;
            importer.sRGBTexture=false; importer.wrapMode=TextureWrapMode.Repeat; importer.mipmapEnabled=true;
            importer.filterMode=FilterMode.Trilinear; importer.anisoLevel=4;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static float PeriodicNoise(float u,float v) => Mathf.Clamp01(0.5f+Mathf.Sin((u*3+v*2)*Mathf.PI*2)*0.23f
            +Mathf.Sin((u*7-v*5)*Mathf.PI*2+1.7f)*0.15f+Mathf.Cos((u*17+v*13)*Mathf.PI*2)*0.08f);
        static float NormalHeight(float u,float v,int kind)
        {
            return TileNoise(u,v,5,kind*23)*0.6f+TileNoise(u,v,11,kind*23+7)*0.27f
                +TileNoise(u,v,23,kind*23+13)*0.1f+TileNoise(u,v,47,kind*23+19)*0.03f;
        }
        static float TileNoise(float u,float v,float frequency,float seed)
        {
            u=Mathf.Repeat(u,1);v=Mathf.Repeat(v,1);
            float sx=u*u*(3-2*u),sy=v*v*(3-2*v);
            float a=Mathf.PerlinNoise(u*frequency+seed,v*frequency+seed);
            float b=Mathf.PerlinNoise((u-1)*frequency+seed,v*frequency+seed);
            float c=Mathf.PerlinNoise(u*frequency+seed,(v-1)*frequency+seed);
            float d=Mathf.PerlinNoise((u-1)*frequency+seed,(v-1)*frequency+seed);
            return Mathf.Lerp(Mathf.Lerp(a,b,sx),Mathf.Lerp(c,d,sx),sy);
        }

        static UniversalRenderPipelineAsset CreateDemoPipeline(Texture2D caustics)
        {
            string rendererPath=Root+"/Demo/Settings/Water Demo Renderer.asset";
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (!renderer)
            {
                // Clone the installed renderer so URP's internal resource references are initialized.
                var source=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
                renderer=source?Object.Instantiate(source):ScriptableObject.CreateInstance<UniversalRendererData>();
                renderer.name="Water Demo Renderer"; renderer.rendererFeatures.Clear();
                AssetDatabase.CreateAsset(renderer,rendererPath);
                InstallFeature(renderer,caustics);
            }
            string pipelinePath=Root+"/Demo/Settings/Water Demo Pipeline.asset";
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (!pipeline)
            {
                var source=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
                pipeline=source?Object.Instantiate(source):UniversalRenderPipelineAsset.Create(renderer);
                pipeline.name="Water Demo Pipeline";
                SerializedObject serialized=new SerializedObject(pipeline);
                SerializedProperty list=serialized.FindProperty("m_RendererDataList");
                list.arraySize=1; list.GetArrayElementAtIndex(0).objectReferenceValue=renderer;
                serialized.FindProperty("m_DefaultRendererIndex").intValue=0;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                pipeline.supportsCameraDepthTexture=true; pipeline.supportsCameraOpaqueTexture=true;
                pipeline.msaaSampleCount=1;
                AssetDatabase.CreateAsset(pipeline,pipelinePath);
            }
            return pipeline;
        }
        static void InstallFeature(UniversalRendererData renderer,Texture2D caustics)
        {
            if (renderer.rendererFeatures.Any(f=>f is WaterUnderwaterFeature)) return;
            var feature=ScriptableObject.CreateInstance<WaterUnderwaterFeature>();
            feature.name="Advanced Water Underwater"; feature.underwaterShader=Shader.Find("Hidden/Advanced Water/Underwater");
            feature.causticsTexture=caustics; feature.Create();
            AssetDatabase.AddObjectToAsset(feature,renderer);
            renderer.rendererFeatures.Add(feature); renderer.SetDirty(); EditorUtility.SetDirty(renderer);
        }
        [MenuItem("Tools/Advanced Water/Install Support on Current Renderer")]
        public static void InstallCurrentRenderer()
        {
            GenerateAssets();
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (!pipeline) { Debug.LogError("Select a URP pipeline before installing water support."); return; }
            SerializedObject serialized=new SerializedObject(pipeline);
            int index=serialized.FindProperty("m_DefaultRendererIndex").intValue;
            var renderer=serialized.FindProperty("m_RendererDataList").GetArrayElementAtIndex(index).objectReferenceValue as UniversalRendererData;
            if (!renderer) { Debug.LogError("Advanced Water needs the Universal Renderer, not the 2D renderer."); return; }
            Undo.RecordObjects(new Object[]{pipeline,renderer},"Install Advanced Water Support");
            pipeline.supportsCameraDepthTexture=true; pipeline.supportsCameraOpaqueTexture=true;
            InstallFeature(renderer,AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/Water Caustics.png"));
            EditorUtility.SetDirty(pipeline); AssetDatabase.SaveAssets();
            Debug.Log("Advanced Water: depth, opaque texture and underwater support enabled on "+pipeline.name);
        }
        [MenuItem("GameObject/Advanced Water/Lake",false,10)] public static void CreateLake() => CreateSurface(false);
        [MenuItem("GameObject/Advanced Water/Ocean",false,11)] public static void CreateOcean() => CreateSurface(true);
        static WaterSurface CreateSurface(bool ocean)
        {
            GenerateAssets();
            GameObject go=new GameObject(ocean?"Ocean":"Lake") {layer=4};
            Undo.RegisterCreatedObjectUndo(go,"Create Water");
            go.AddComponent<MeshFilter>(); var renderer=go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode=ShadowCastingMode.Off;
            var water=go.AddComponent<WaterSurface>();
            water.profile=AssetDatabase.LoadAssetAtPath<WaterProfile>(Root+"/Profiles/"+(ocean?"Calm Ocean":"Lake")+".asset");
            var mesh=go.AddComponent<WaterMesh>(); mesh.mode=ocean?WaterMesh.SurfaceMode.Ocean:WaterMesh.SurfaceMode.Lake;
            mesh.size=ocean?1600:150; mesh.resolution=ocean?192:128; mesh.Rebuild();
            water.Refresh(); Selection.activeGameObject=go;
            return water;
        }
        [MenuItem("Tools/Advanced Water/Open Demo Scene")]
        public static void OpenDemo()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            GenerateAssets();
            string path=Root+"/Demo/Water Demo.unity";
            if (File.Exists(path)) EditorSceneManager.OpenScene(path); else BuildDemo();
        }

        static void BuildDemo()
        {
            Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var settings=new GameObject("Demo Pipeline (Play Mode Only)").AddComponent<WaterDemoPipeline>();
            settings.pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Root+"/Demo/Settings/Water Demo Pipeline.asset");
            WaterSurface water=CreateSurface(false);
            water.name="Water - Select for Settings";
            water.profile=AssetDatabase.LoadAssetAtPath<WaterProfile>(Root+"/Profiles/Tropical.asset");
            water.GetComponent<WaterMesh>().size=200; water.GetComponent<WaterMesh>().resolution=192; water.GetComponent<WaterMesh>().Rebuild();
            water.Refresh(); water.gameObject.AddComponent<WaterPlanarReflection>();
            var sun=new GameObject("Sun").AddComponent<Light>(); sun.type=LightType.Directional;
            sun.transform.rotation=Quaternion.Euler(34,-32,0); sun.intensity=1.5f; sun.color=new Color(1,0.94f,0.82f); sun.shadows=LightShadows.Soft;
            RenderSettings.sun=sun; RenderSettings.ambientMode=AmbientMode.Skybox;
            Material sky=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Demo/Materials/Sky.mat");
            if (!sky)
            {
                sky=new Material(Shader.Find("Skybox/Procedural")); sky.SetFloat("_SunSize",0.025f); sky.SetFloat("_AtmosphereThickness",0.9f);
                sky.SetColor("_SkyTint",new Color(0.5f,0.59f,0.65f)); AssetDatabase.CreateAsset(sky,Root+"/Demo/Materials/Sky.mat");
            }
            RenderSettings.skybox=sky; RenderSettings.ambientIntensity=1;
            RenderSettings.fog=true; RenderSettings.fogMode=FogMode.ExponentialSquared; RenderSettings.fogDensity=0.0025f;
            RenderSettings.fogColor=new Color(0.63f,0.76f,0.8f);
            Material sand=Lit("Sand",new Color(0.65f,0.56f,0.36f));
            Material rock=Lit("Rock",new Color(0.21f,0.25f,0.24f));
            Material wood=Lit("Buoyant Cedar",new Color(0.43f,0.19f,0.065f));
            BuildGround(sand);
            for (int i=0;i<10;i++)
            {
                float z=-10+i*6;
                GameObject stone=GameObject.CreatePrimitive(PrimitiveType.Sphere); stone.name="Shore Rock "+(i+1);
                stone.transform.position=new Vector3(-19-Mathf.Sin(i*1.7f)*3,0.4f,z);
                stone.transform.localScale=new Vector3(3+i%3,2+i%2,3.5f);
                stone.GetComponent<Renderer>().sharedMaterial=rock;
            }
            for (int i=0;i<3;i++)
            {
                GameObject buoy=GameObject.CreatePrimitive(PrimitiveType.Cube); buoy.name="Floating Crate "+(i+1);
                buoy.transform.position=new Vector3(-4+i*4,1,3+i*3);
                buoy.transform.localScale=new Vector3(1.4f,0.7f,1.4f);
                buoy.GetComponent<Renderer>().sharedMaterial=wood;
                buoy.AddComponent<Rigidbody>().mass=40; buoy.AddComponent<WaterBuoyancy>().water=water;
                buoy.AddComponent<WaterInteractor>().water=water;
            }
            GameObject boat=GameObject.CreatePrimitive(PrimitiveType.Cube); boat.name="Wake Demo Boat";
            boat.transform.position=new Vector3(13,0.4f,8); boat.transform.localScale=new Vector3(1.2f,0.55f,2.5f);
            boat.GetComponent<Renderer>().sharedMaterial=Lit("Boat",new Color(0.85f,0.88f,0.8f));
            boat.AddComponent<Rigidbody>().mass=80; boat.AddComponent<WaterBuoyancy>().water=water;
            var interactor=boat.AddComponent<WaterInteractor>(); interactor.water=water; interactor.localOffset=new Vector3(0,0,-0.5f); interactor.amplitude=0.4f; interactor.boatWake=true; interactor.interval=0.55f; interactor.propagationSpeed=0.9f;
            boat.AddComponent<WaterDemoBoat>();
            Camera camera=new GameObject("Main Camera").AddComponent<Camera>(); camera.tag="MainCamera";
            camera.transform.position=new Vector3(17,5.5f,-22); camera.transform.LookAt(new Vector3(-5,0,11));
            camera.nearClipPlane=0.1f; camera.farClipPlane=1200; camera.fieldOfView=58; camera.allowHDR=true;
            camera.gameObject.AddComponent<AudioListener>();
            var cameraData=camera.GetUniversalAdditionalCameraData(); cameraData.renderPostProcessing=true;
            cameraData.requiresColorOption=CameraOverrideOption.On; cameraData.requiresDepthOption=CameraOverrideOption.On;
            var controls=camera.gameObject.AddComponent<WaterDemoController>(); controls.water=water;
            controls.presets=new[]{"Tropical","Lake","Calm Ocean","Rough Ocean","Murky","Stylized",
                "Cel Lagoon","Wind Waker","Painterly Sea","Graphic Ink","Arcane Water","Anime Ocean","Low-Poly","Cozy Pastel","Watercolor Lake","Manga Sea","Retro Pixel Water","Paper-cut Ocean","Bioluminescent Water"}
                .Select(n=>AssetDatabase.LoadAssetAtPath<WaterProfile>(Root+"/Profiles/"+n+".asset")).ToArray();
            EditorSceneManager.SaveScene(scene,Root+"/Demo/Water Demo.unity");
            AssetDatabase.SaveAssets();
        }
        static Material Lit(string name,Color color)
        {
            string path=Root+"/Demo/Materials/"+name+".mat";
            Material material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material) return material;
            material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor",color); material.SetFloat("_Smoothness",0.2f);
            AssetDatabase.CreateAsset(material,path); return material;
        }
        static void BuildGround(Material sand)
        {
            const int resolution=100; const float size=240;
            var vertices=new Vector3[(resolution+1)*(resolution+1)]; var uv=new Vector2[vertices.Length];
            var triangles=new int[resolution*resolution*6];
            for (int z=0;z<=resolution;z++) for (int x=0;x<=resolution;x++)
            {
                float px=(x/(float)resolution-0.5f)*size,pz=(z/(float)resolution-0.5f)*size;
                float shore=-19+Mathf.Sin(pz*0.06f)*5;
                float height=Mathf.Clamp((shore-px)*0.2f,-7,6)+Mathf.Sin(px*0.2f)*Mathf.Sin(pz*0.17f)*0.3f;
                vertices[z*(resolution+1)+x]=new Vector3(px,height,pz); uv[z*(resolution+1)+x]=new Vector2(px,pz)*0.1f;
            }
            int index=0;
            for (int z=0;z<resolution;z++) for (int x=0;x<resolution;x++)
            {
                int a=z*(resolution+1)+x,b=a+resolution+1;
                triangles[index++]=a;triangles[index++]=b;triangles[index++]=a+1;
                triangles[index++]=a+1;triangles[index++]=b;triangles[index++]=b+1;
            }
            string path=Root+"/Demo/Shore Mesh.asset";
            Mesh mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (!mesh) { mesh=new Mesh{name="Sloping Shore"}; mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();AssetDatabase.CreateAsset(mesh,path); }
            GameObject ground=new GameObject("Sloping Shore and Lake Bed"); ground.AddComponent<MeshFilter>().sharedMesh=mesh;
            ground.AddComponent<MeshRenderer>().sharedMaterial=sand; ground.AddComponent<MeshCollider>().sharedMesh=mesh;
        }

        // Entry point used by headless validation; no scene or pipeline settings outside this package are saved.
        public static void BuildAndValidate()
        {
            report=Path.GetFullPath("Logs/AdvancedWaterValidation.txt");
            try
            {
                GenerateAssets();
                if (File.Exists(Root+"/Demo/Water Demo.unity")) EditorSceneManager.OpenScene(Root+"/Demo/Water Demo.unity"); else BuildDemo();
                ValidateWaves();
                validationPreviousPipeline=QualitySettings.renderPipeline;
                QualitySettings.renderPipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Root+"/Demo/Settings/Water Demo Pipeline.asset");
                warmupFrames=0;
                EditorApplication.update+=WarmupValidation;
                EditorApplication.QueuePlayerLoopUpdate();
            }
            catch (Exception e) { File.WriteAllText(report,e.ToString()); Debug.LogException(e); EditorApplication.Exit(1); }
        }
        public static void RefineAndValidate()
        {
            RegenerateTextures();
            BuildAndValidate();
        }
        static void WarmupValidation()
        {
            EditorApplication.QueuePlayerLoopUpdate();
            if (++warmupFrames<12) return;
            EditorApplication.update-=WarmupValidation;
            CaptureValidation();
        }
        static void ValidateWaves()
        {
            WaterSurface water=Object.FindFirstObjectByType<WaterSurface>();
            WaterProfile original=water.profile;
            float maximumError=0;
            foreach (string name in PresetNames)
            {
                water.profile=AssetDatabase.LoadAssetAtPath<WaterProfile>(Root+"/Profiles/"+name+".asset");
                for (int i=0;i<100;i++)
                {
                    Vector2 parameter=new Vector2(Mathf.Sin(i*1.37f)*30,Mathf.Cos(i*0.83f)*30);
                    float time=i*0.39f;
                    Vector3 d=water.Displacement(parameter,time);
                    Vector3 displaced=new Vector3(parameter.x+d.x,water.Level+d.y,parameter.y+d.z);
                    if (!water.Sample(displaced,time,out float height,out Vector3 normal)) throw new Exception("Wave sample unexpectedly outside surface.");
                    float error=Mathf.Abs(displaced.y-height); maximumError=Mathf.Max(maximumError,error);
                    if (error>0.01f || Mathf.Abs(normal.magnitude-1)>0.001f || normal.y<=0)
                        throw new Exception($"Wave inversion/normal failed: {name}, sample {i}, error {error}");
                }
            }
            water.profile=original; water.Refresh();
            File.WriteAllText(report,$"{PresetNames.Length*100} wave round-trip samples passed. Maximum height error: {maximumError:F6} m.\n");
        }
        static void CaptureValidation()
        {
            RenderPipelineAsset previous=validationPreviousPipeline;
            try
            {
                captureCount=0;
                Camera camera=Camera.main;
                camera.aspect=1280f/720;
                WaterSurface water=Object.FindFirstObjectByType<WaterSurface>();
                Directory.CreateDirectory("Logs/WaterPreviews");
                foreach (string name in PresetNames)
                {
                    water.profile=AssetDatabase.LoadAssetAtPath<WaterProfile>(Root+"/Profiles/"+name+".asset"); water.Refresh();
                    water.GetComponent<WaterMesh>().Rebuild();
                    Capture(camera,"Logs/WaterPreviews/"+name+".png");
                }
                ValidateVariants(water,camera);
                water.profile=AssetDatabase.LoadAssetAtPath<WaterProfile>(Root+"/Profiles/Tropical.asset");
                water.GetComponent<WaterPlanarReflection>().RenderReflection(camera);
                Capture(camera,"Logs/WaterPreviews/Planar Reflection.png");
                camera.transform.position=new Vector3(6,-1.5f,-5); camera.transform.LookAt(new Vector3(-8,-2,15));
                water.profile.underwater=false;
                Capture(camera,"Logs/WaterPreviews/Underwater Without Effect.png");
                water.profile.underwater=true;
                Capture(camera,"Logs/WaterPreviews/Underwater.png");
                camera.transform.position=new Vector3(6,0,-5); camera.transform.LookAt(new Vector3(-8,0,15));
                Capture(camera,"Logs/WaterPreviews/Waterline.png");
                camera.transform.position=new Vector3(6,5,-3); camera.transform.LookAt(new Vector3(0,water.Level,3));
                water.AddRipple(new Vector3(0,water.Level,3),0.72f,2.4f,0.36f,1.05f);water.Refresh(camera);
                water.GetComponent<WaterPlanarReflection>().RenderReflection(camera);
                Capture(camera,"Logs/WaterPreviews/Ripple.png");water.ClearRipples();water.Refresh(camera);
                File.AppendAllText(report,$"Underwater RenderGraph passes recorded: {WaterUnderwaterFeature.RecordedPassCount}. Pipeline: {GraphicsSettings.currentRenderPipeline.name}\n");
                if (WaterUnderwaterFeature.RecordedPassCount<2) throw new Exception("Underwater RenderGraph pass did not run for both submerged and waterline captures.");
                foreach (string shaderName in new[]{"Advanced Water/Lake and Ocean","Advanced Water/Stylized Water","Hidden/Advanced Water/Underwater"})
                {
                    Shader shader=Shader.Find(shaderName);
                    var errors=ShaderUtil.GetShaderMessages(shader).Where(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).ToArray();
                    if (errors.Length>0) throw new Exception(shaderName+": "+string.Join("\n",errors.Select(e=>e.message)));
                }
                File.AppendAllText(report,$"Shader error check passed. {captureCount} GPU preview renders completed.\n");
                Debug.Log("Advanced Water validation passed: "+report);
                QualitySettings.renderPipeline=previous;
                WaterPlayValidation.Run();
            }
            catch (Exception e)
            {
                QualitySettings.renderPipeline=previous;
                File.AppendAllText(report,e.ToString()); Debug.LogException(e); EditorApplication.Exit(1);
            }
        }
        static void ValidateVariants(WaterSurface water,Camera camera)
        {
            WaterProfile original=water.profile;
            WaterProfile temporary=Object.Instantiate(original);
            Material material=Object.Instantiate(original.material); temporary.material=material;
            Texture2D flow=null;
            WaterMesh mesh=water.GetComponent<WaterMesh>();
            WaterMesh.SurfaceMode oldMode=mesh.mode; float oldSize=mesh.size;
            try
            {
                water.profile=temporary;
                foreach (WaterQuality quality in Enum.GetValues(typeof(WaterQuality)))
                {
                    temporary.quality=quality; water.Refresh();
                    Capture(camera,"Logs/WaterPreviews/Quality "+quality+".png");
                }
                mesh.mode=WaterMesh.SurfaceMode.Ocean; mesh.size=1600; mesh.Rebuild();
                Capture(camera,"Logs/WaterPreviews/Ocean Geometry.png");
                mesh.mode=oldMode; mesh.size=oldSize; mesh.Rebuild();
                flow=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
                flow.SetPixels(new[]{new Color(0.9f,0.4f,1),new Color(0.3f,0.8f,1),new Color(0.6f,0.6f,1),new Color(0.2f,0.3f,1)}); flow.Apply();
                material.SetTexture("_FlowMap",flow); material.EnableKeyword("_FLOWMAP");
                Capture(camera,"Logs/WaterPreviews/Flow Map.png");
            }
            finally
            {
                water.profile=original;water.Refresh();mesh.mode=oldMode;mesh.size=oldSize;mesh.Rebuild();
                Object.DestroyImmediate(temporary);Object.DestroyImmediate(material);if(flow)Object.DestroyImmediate(flow);
            }
        }
        internal static void Capture(Camera camera,string path)
        {
            var target=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);
            target.Create();
            RenderTexture old=RenderTexture.active;
            Texture2D pixels=null;
            try
            {
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest {destination=target});
                RenderTexture.active=target; pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0); pixels.Apply(); File.WriteAllBytes(path,pixels.EncodeToPNG());
                captureCount++;
            }
            finally { RenderTexture.active=old; if (pixels) Object.DestroyImmediate(pixels); target.Release(); Object.DestroyImmediate(target); }
        }
    }
}
