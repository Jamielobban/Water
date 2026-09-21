using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AdvancedWater
{
    [DisallowMultipleComponent, RequireComponent(typeof(WaterSurface))]
    public sealed class WaterPlanarReflection : MonoBehaviour
    {
        [Tooltip("Defaults to MainCamera. Reflection is only applied when this camera renders.")]
        public Camera sourceCamera;
        [Tooltip("All water should be on layer 4 (Water), excluded here to prevent feedback.")]
        public LayerMask reflectLayers=~(1<<4);
        [Range(128,2048)] public int resolution=512;
        [Range(1,8)] public int updateEveryFrames=2;
        [Min(1)] public float maxDistance=800;
        [Range(0.001f,0.3f)] public float clipOffset=0.04f;
        WaterSurface water;
        Camera mirror;
        RenderTexture target;
        static bool rendering;
        int lastFrame=-1;
        void OnEnable() => water=GetComponent<WaterSurface>();
        void LateUpdate()
        {
            if (!water || !water.profile || water.profile.quality<WaterQuality.High)
            { if (water) water.SetReflection(null,null); return; }
            Camera source=sourceCamera?sourceCamera:Camera.main;
            if (!source || !source.isActiveAndEnabled || rendering || Time.frameCount==lastFrame) return;
            int interval=water.profile.quality==WaterQuality.Cinematic?1:Mathf.Max(1,updateEveryFrames);
            if (Time.frameCount%interval!=0) return;
            RenderReflection(source);
        }
        public void RenderReflection(Camera source)
        {
            if (rendering || !source || source.stereoEnabled) return;
            if (!water) water=GetComponent<WaterSurface>();
            if (source.transform.position.y<=water.Level) { water.SetReflection(null,null); return; }
            EnsureResources(source.aspect);
            mirror.CopyFrom(source);
            mirror.enabled=false; mirror.cameraType=CameraType.Reflection;
            mirror.targetTexture=target;
            mirror.cullingMask=source.cullingMask & reflectLayers.value & ~(1<<gameObject.layer);
            mirror.farClipPlane=Mathf.Min(source.farClipPlane,maxDistance);
            mirror.allowMSAA=false;
            var data=mirror.GetUniversalAdditionalCameraData();
            data.renderPostProcessing=false; data.renderShadows=false;
            data.requiresColorOption=CameraOverrideOption.Off; data.requiresDepthOption=CameraOverrideOption.Off;
            data.antialiasing=AntialiasingMode.None;
            Matrix4x4 reflection=Matrix4x4.identity;
            reflection.m11=-1; reflection.m13=2*water.Level;
            mirror.transform.position=reflection.MultiplyPoint(source.transform.position);
            mirror.transform.rotation=Quaternion.LookRotation(reflection.MultiplyVector(source.transform.forward),reflection.MultiplyVector(source.transform.up));
            mirror.worldToCameraMatrix=source.worldToCameraMatrix*reflection;
            Vector3 planePoint=mirror.worldToCameraMatrix.MultiplyPoint(new Vector3(0,water.Level+clipOffset,0));
            Vector3 planeNormal=mirror.worldToCameraMatrix.MultiplyVector(Vector3.up).normalized;
            Vector4 clip=new Vector4(planeNormal.x,planeNormal.y,planeNormal.z,-Vector3.Dot(planePoint,planeNormal));
            mirror.projectionMatrix=source.CalculateObliqueMatrix(clip);
            bool inverted=GL.invertCulling;
            try
            {
                rendering=true; GL.invertCulling=!inverted;
                // Submit outside camera-render callbacks: avoids nesting a RenderGraph execution.
                RenderPipeline.SubmitRenderRequest(mirror,new UniversalRenderPipeline.SingleCameraRequest {destination=target});
                water.SetReflection(source,target);
                lastFrame=Time.frameCount;
            }
            finally { GL.invertCulling=inverted; rendering=false; }
        }
        void EnsureResources(float aspect)
        {
            if (!mirror)
            {
                GameObject go=new GameObject("Water Reflection Camera") {hideFlags=HideFlags.HideAndDontSave};
                mirror=go.AddComponent<Camera>(); mirror.enabled=false;
            }
            int actual=Mathf.Clamp(Mathf.ClosestPowerOfTwo(resolution),128,2048);
            int height=Mathf.Max(64,Mathf.RoundToInt(actual/Mathf.Max(0.1f,aspect)));
            if (target && target.width==actual && target.height==height) return;
            if (target) { target.Release(); if (Application.isPlaying) Destroy(target); else DestroyImmediate(target); }
            target=new RenderTexture(actual,height,24,RenderTextureFormat.ARGBHalf)
            {name="Water Planar Reflection",hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear};
            target.Create();
        }
        void OnDisable()
        {
            if (water) water.SetReflection(null,null);
            if (target) { target.Release(); if (Application.isPlaying) Destroy(target); else DestroyImmediate(target); }
            if (mirror) { if (Application.isPlaying) Destroy(mirror.gameObject); else DestroyImmediate(mirror.gameObject); }
            target=null; mirror=null;
        }
    }
}
