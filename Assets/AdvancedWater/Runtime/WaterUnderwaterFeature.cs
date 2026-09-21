using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace AdvancedWater
{
    // Native Unity 6 RenderGraph pass; deliberately does not enable compatibility mode.
    public sealed class WaterUnderwaterFeature : ScriptableRendererFeature
    {
        public static int RecordedPassCount { get; private set; }
        [Tooltip("Assign Advanced Water/Underwater shader so builds retain it.")]
        public Shader underwaterShader;
        public Texture2D causticsTexture;
        Material material;
        UnderwaterPass pass;
        public override void Create()
        {
            CoreUtils.Destroy(material);
            if (!underwaterShader) underwaterShader=Shader.Find("Hidden/Advanced Water/Underwater");
            if (underwaterShader) material=CoreUtils.CreateEngineMaterial(underwaterShader);
            pass=new UnderwaterPass {renderPassEvent=RenderPassEvent.BeforeRenderingPostProcessing};
        }
        public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData renderingData)
        {
            Camera camera=renderingData.cameraData.camera;
            if (!material || renderingData.cameraData.renderType==CameraRenderType.Overlay
                || camera.cameraType==CameraType.Reflection || camera.cameraType==CameraType.Preview || camera.stereoEnabled) return;
            WaterSurface water=WaterSurface.Find(camera.transform.position);
            if (!water || !water.profile.underwater || camera.transform.position.y>water.Level+water.MaxDisplacement+camera.nearClipPlane+0.5f) return;
            material.SetTexture("_CausticsMap",causticsTexture?causticsTexture:Texture2D.blackTexture);
            pass.Setup(material);
            renderer.EnqueuePass(pass);
        }
        protected override void Dispose(bool disposing) { CoreUtils.Destroy(material); material=null; }

        sealed class UnderwaterPass : ScriptableRenderPass
        {
            Material material;
            public void Setup(Material value)
            {
                material=value;
                requiresIntermediateTexture=true;
                ConfigureInput(ScriptableRenderPassInput.Depth);
            }
            public override void RecordRenderGraph(RenderGraph graph,ContextContainer frameData)
            {
                var resources=frameData.Get<UniversalResourceData>();
                var cameraData=frameData.Get<UniversalCameraData>();
                WaterSurface water=WaterSurface.Find(cameraData.camera.transform.position);
                if (!water || !material || resources.isActiveTargetBackBuffer || !resources.cameraDepthTexture.IsValid()) return;
                RecordedPassCount++;
                WaterProfile profile=water.profile;
                // Each pass gets immutable per-camera properties (including split screen / Scene view).
                var properties=new MaterialPropertyBlock();
                Vector4[] waves=new Vector4[WaterProfile.WaveCount]; profile.GetWaves(waves);
                properties.SetVectorArray("_AW_Waves",waves);
                properties.SetFloat("_AW_Level",water.Level);
                properties.SetFloat("_AW_Steepness",profile.steepness);
                properties.SetFloat("_AW_WaveSpeed",profile.waveSpeed);
                properties.SetFloat("_AW_ShapeVariation",profile.shapeVariation);
                properties.SetFloat("_AW_ShapeScale",profile.shapeScale);
                properties.SetFloat("_AW_CrestSharpness",profile.crestSharpness);
                properties.SetFloat("_AW_Time",WaterSurface.Clock);
                properties.SetColor("_UnderwaterColor",profile.underwaterColor);
                properties.SetVector("_UnderwaterParams",new Vector4(profile.underwaterDensity,profile.underwaterDistortion,
                    profile.waterlineWidth,cameraData.camera.nearClipPlane));
                Bounds bounds=water.SurfaceRenderer.bounds;
                properties.SetVector("_WaterBounds",new Vector4(bounds.min.x,bounds.min.z,bounds.max.x,bounds.max.z));
                Material surface=profile.material;
                bool stylized=surface && surface.HasProperty("_StyleMode");
                properties.SetFloat("_UnderwaterStyle",stylized?surface.GetFloat("_StyleMode"):-1);
                properties.SetFloat("_UnderwaterSteps",stylized?surface.GetFloat("_ColorSteps"):0);
                properties.SetColor("_UnderwaterAccent",stylized?surface.GetColor("_AccentColor"):profile.underwaterColor);
                properties.SetFloat("_UnderwaterBrush",stylized?surface.GetFloat("_BrushStrength"):0);
                properties.SetFloat("_CausticsStrength",surface && surface.IsKeywordEnabled("_CAUSTICS") && profile.quality>=WaterQuality.Medium
                    ? surface.GetFloat("_CausticsStrength"):0);
                properties.SetFloat("_CausticsScale",surface?surface.GetFloat("_CausticsScale"):0.25f);
                var source=resources.activeColorTexture;
                var desc=graph.GetTextureDesc(source);
                desc.name="Advanced Water Underwater"; desc.clearBuffer=false;
                var destination=graph.CreateTexture(desc);
                var parameters=new RenderGraphUtils.BlitMaterialParameters(source,destination,material,0) {propertyBlock=properties};
                using (var builder=graph.AddBlitPass(parameters,passName:"Advanced Water Underwater",returnBuilder:true))
                    builder.UseTexture(resources.cameraDepthTexture,AccessFlags.Read);
                resources.cameraColor=destination;
            }
        }
    }
}
