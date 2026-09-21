using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedWater
{
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(MeshRenderer))]
    public sealed class WaterSurface : MonoBehaviour
    {
        public WaterProfile profile;
        public static readonly List<WaterSurface> Active = new List<WaterSurface>();
        public static float Clock => Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
        public float Level => transform.position.y;
        public Renderer SurfaceRenderer => surfaceRenderer ? surfaceRenderer : surfaceRenderer = GetComponent<Renderer>();
        public float MaxDisplacement => profile ? profile.MaximumDisplacement : 0;

        const int MaxRipples = 32;
        const int MaxWakeSegments = 48;
        readonly Vector4[] wakeStarts = new Vector4[MaxWakeSegments]; // XZ, birth time, reserved
        readonly Vector4[] wakeEnds = new Vector4[MaxWakeSegments];
        readonly Vector4[] wakeData = new Vector4[MaxWakeSegments]; // half width, strength, spread, lifetime
        int nextWake;
        readonly Vector4[] waves = new Vector4[WaterProfile.WaveCount];
        readonly Vector4[] ripples = new Vector4[MaxRipples];
        readonly Vector4[] rippleData = new Vector4[MaxRipples];
        MaterialPropertyBlock block;
        Renderer surfaceRenderer;
        int nextRipple;
        Camera reflectionCamera;
        Texture reflectionTexture;

        void OnEnable()
        {
            if (!Active.Contains(this)) Active.Add(this);
            RenderPipelineManager.beginCameraRendering += BeforeCamera;
            Refresh();
        }
        void OnDisable()
        {
            Active.Remove(this);
            RenderPipelineManager.beginCameraRendering -= BeforeCamera;
            if (SurfaceRenderer) SurfaceRenderer.SetPropertyBlock(null);
        }
        void LateUpdate() => Refresh();
        void BeforeCamera(ScriptableRenderContext context, Camera camera) => Refresh(camera);

        public void Refresh(Camera camera = null)
        {
            if (!profile || !SurfaceRenderer) return;
            if (profile.material && SurfaceRenderer.sharedMaterial != profile.material)
                SurfaceRenderer.sharedMaterial = profile.material;
            block ??= new MaterialPropertyBlock();
            SurfaceRenderer.GetPropertyBlock(block);
            profile.GetWaves(waves);
            block.SetVectorArray("_AW_Waves", waves);
            block.SetFloat("_AW_Time", Clock);
            block.SetFloat("_AW_Steepness", profile.steepness);
            block.SetFloat("_AW_WaveSpeed", profile.waveSpeed);
            block.SetFloat("_AW_ShapeVariation",profile.shapeVariation);
            block.SetFloat("_AW_ShapeScale",profile.shapeScale);
            block.SetFloat("_AW_CrestSharpness",profile.crestSharpness);
            block.SetFloat("_AW_Level", Level);
            block.SetFloat("_AW_Quality", (int)profile.quality);
            block.SetFloat("_AW_RippleStrength", profile.rippleStrength);
            block.SetVectorArray("_AW_Ripples", ripples);
            block.SetVectorArray("_AW_RippleData", rippleData);
            int capacity = profile.quality == WaterQuality.Low ? 4 : profile.quality == WaterQuality.Medium ? 12 : 32;
            int activeCount = 0;
            for (int i = 0; i < capacity; i++)
                if (ripples[i].w > 0 && Clock - ripples[i].z <= rippleData[i].z) activeCount = i + 1;
            block.SetInt("_AW_RippleCount", profile.rippleStrength > 0 ? activeCount : 0);
            int wakeCount=0;
            for (int i=0;i<MaxWakeSegments;i++)
                if (wakeData[i].y>0 && Clock-wakeEnds[i].z<wakeData[i].w) wakeCount=i+1;
            block.SetVectorArray("_AW_WakeStarts",wakeStarts);
            block.SetVectorArray("_AW_WakeEnds",wakeEnds);
            block.SetVectorArray("_AW_WakeData",wakeData);
            block.SetInt("_AW_WakeCount",wakeCount);
            bool reflect = camera && camera == reflectionCamera && reflectionTexture;
            block.SetFloat("_AW_PlanarValid", reflect ? 1 : 0);
            if (reflectionTexture) block.SetTexture("_AW_PlanarTexture", reflectionTexture);
            SurfaceRenderer.SetPropertyBlock(block);
        }

        public void SetReflection(Camera source, Texture texture)
        { reflectionCamera = source; reflectionTexture = texture; }

        public void AddRipple(Vector3 position, float amplitude = 0.12f, float speed = 1, float width = 0.65f, float initialAge = 0)
        {
            if (!profile) return;
            int capacity = profile.quality == WaterQuality.Low ? 4 : profile.quality == WaterQuality.Medium ? 12 : 32;
            float now=Clock;
            // Never replace a visible packet. Under load, skip the new emission
            // rather than abruptly erasing a ring already travelling across the water.
            int slot=-1;
            for (int offset=0;offset<capacity;offset++)
            {
                int candidate=(nextRipple+offset)%capacity;
                if (ripples[candidate].w<=0 || now-ripples[candidate].z>=rippleData[candidate].z)
                { slot=candidate; break; }
            }
            if (slot<0) return;
            ripples[slot] = new Vector4(position.x, position.z, now-Mathf.Max(0,initialAge), Mathf.Max(0, amplitude));
            float seed = Mathf.Repeat(position.x*12.9898f+position.z*78.233f,6.28318f);
            rippleData[slot] = new Vector4(Mathf.Max(0.01f, speed), Mathf.Max(0.05f, width), Mathf.Max(0.1f, profile.rippleLifetime), seed);
            nextRipple = (slot + 1) % capacity;
        }

        // World-space segments remain behind the hull when it turns or stops.
        public void AddWakeSegment(Vector3 start, Vector3 end, float startTime, float endTime,
            float halfWidth, float strength, float spread, float lifetime)
        {
            if (!profile || strength<=0 || endTime<=startTime) return;
            Vector2 delta=new Vector2(end.x-start.x,end.z-start.z);
            if (delta.sqrMagnitude<0.0001f) return;
            for (int offset=0;offset<MaxWakeSegments;offset++)
            {
                int slot=(nextWake+offset)%MaxWakeSegments;
                if (wakeData[slot].y>0 && Clock-wakeEnds[slot].z<wakeData[slot].w) continue;
                wakeStarts[slot]=new Vector4(start.x,start.z,startTime,0);
                wakeEnds[slot]=new Vector4(end.x,end.z,endTime,0);
                wakeData[slot]=new Vector4(Mathf.Max(0.1f,halfWidth),Mathf.Clamp01(strength),
                    Mathf.Max(0,spread),Mathf.Max(0.1f,lifetime));
                nextWake=(slot+1)%MaxWakeSegments;
                return;
            }
        }

        public void ClearRipples()
        {
            System.Array.Clear(ripples,0,ripples.Length);
            System.Array.Clear(rippleData,0,rippleData.Length);
            nextRipple=0;
            System.Array.Clear(wakeData,0,wakeData.Length);
            nextWake=0;
        }

        // Invert the horizontal Gerstner displacement, then evaluate height at that parameter.
        // Keep these equations synchronized with Shaders/WaterWaves.hlsl.
        public bool Sample(Vector3 worldPosition, out float height, out Vector3 normal)
            => Sample(worldPosition, Clock, out height, out normal);

        public bool Sample(Vector3 worldPosition, float time, out float height, out Vector3 normal)
        {
            height = Level; normal = Vector3.up;
            if (!profile || !Contains(worldPosition)) return false;
            profile.GetWaves(waves);
            Vector2 target = new Vector2(worldPosition.x, worldPosition.z), parameter = target;
            for (int i = 0; i < 8; i++)
            {
                Evaluate(parameter, time, out Vector3 displacement, out _, out _);
                parameter = target - new Vector2(displacement.x, displacement.z);
            }
            Evaluate(parameter, time, out Vector3 d, out Vector3 tangent, out Vector3 binormal);
            height += d.y;
            normal = Vector3.Cross(binormal, tangent).normalized;
            return true;
        }

        public Vector3 Displacement(Vector2 parameter, float time)
        {
            if (!profile) return Vector3.zero;
            profile.GetWaves(waves);
            Evaluate(parameter, time, out Vector3 d, out _, out _);
            return d;
        }

        void Evaluate(Vector2 p, float time, out Vector3 displacement, out Vector3 tangent, out Vector3 binormal)
        {
            displacement = Vector3.zero; tangent = Vector3.right; binormal = Vector3.forward;
            for (int i=0;i<waves.Length;i++)
            {
                Vector4 wave=waves[i];
                if (wave.z <= 0) continue;
                float k = 2 * Mathf.PI / wave.w;
                float a = wave.z;
                Vector2 direction=new Vector2(wave.x,wave.y);
                Vector2 perpendicular=new Vector2(-direction.y,direction.x);
                Vector2 diagonal=new Vector2(direction.x-direction.y,direction.x+direction.y).normalized;
                Vector2 modulationDirection=new Vector2(direction.x+direction.y,direction.y-direction.x).normalized;
                float frequency=Mathf.Max(0.01f,profile.shapeScale);
                float seed=i*2.173f;
                float groupTime=time*profile.waveSpeed;
                float phaseA=Vector2.Dot(perpendicular,p)*frequency+seed+groupTime*(0.11f+i*0.017f);
                float phaseB=Vector2.Dot(diagonal,p)*frequency*0.43f+seed*1.91f+1.7f-groupTime*(0.073f+i*0.013f);
                float amplitudePhase=Vector2.Dot(modulationDirection,p)*frequency*0.29f+seed*2.37f+0.8f-groupTime*(0.16f+i*0.021f);
                float variation=profile.shapeVariation;
                float phase=k*Vector2.Dot(direction,p)-Mathf.Sqrt(9.81f*k)*time*profile.waveSpeed
                    +(i >= 4 ? seed : 0)+variation*(Mathf.Sin(phaseA)*0.85f+Mathf.Sin(phaseB)*0.5f);
                Vector2 phaseGradient=k*direction+variation*(Mathf.Cos(phaseA)*0.85f*frequency*perpendicular
                    +Mathf.Cos(phaseB)*0.5f*frequency*0.43f*diagonal);
                float amplitudeFactor=1+variation*(0.35f*Mathf.Sin(amplitudePhase)+0.15f*Mathf.Sin(phaseB));
                Vector2 amplitudeGradient=variation*(0.35f*Mathf.Cos(amplitudePhase)*frequency*0.29f*modulationDirection
                    +0.15f*Mathf.Cos(phaseB)*frequency*0.43f*diagonal);
                float horizontalBase=Mathf.Min(profile.steepness*a,0.16f/k);
                float horizontal=horizontalBase*amplitudeFactor;
                float s = Mathf.Sin(phase), c = Mathf.Cos(phase);
                float sharpness=profile.crestSharpness*0.18f;
                float shape=(s-sharpness*Mathf.Cos(phase*2))/(1+sharpness);
                float shapeDerivative=(c+sharpness*2*Mathf.Sin(phase*2))/(1+sharpness);
                float verticalAmplitude=a*amplitudeFactor;
                displacement+=new Vector3(horizontal*direction.x*c,verticalAmplitude*shape,horizontal*direction.y*c);
                Vector2 horizontalX=direction*(horizontalBase*amplitudeGradient.x*c-horizontal*s*phaseGradient.x);
                Vector2 horizontalZ=direction*(horizontalBase*amplitudeGradient.y*c-horizontal*s*phaseGradient.y);
                float verticalX=a*(amplitudeGradient.x*shape+amplitudeFactor*shapeDerivative*phaseGradient.x);
                float verticalZ=a*(amplitudeGradient.y*shape+amplitudeFactor*shapeDerivative*phaseGradient.y);
                tangent+=new Vector3(horizontalX.x,verticalX,horizontalX.y);
                binormal+=new Vector3(horizontalZ.x,verticalZ,horizontalZ.y);
            }
        }

        public bool Contains(Vector3 worldPosition)
        {
            if (!SurfaceRenderer) return false;
            Bounds b = SurfaceRenderer.bounds;
            return worldPosition.x >= b.min.x && worldPosition.x <= b.max.x
                && worldPosition.z >= b.min.z && worldPosition.z <= b.max.z;
        }

        public static WaterSurface Find(Vector3 position)
        {
            WaterSurface best = null;
            foreach (WaterSurface water in Active)
                if (water && water.isActiveAndEnabled && water.profile && water.Contains(position)
                    && (!best || Mathf.Abs(position.y - water.Level) < Mathf.Abs(position.y - best.Level))) best = water;
            return best;
        }
    }
}
