Shader "GPU Grass/Meadow"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "MeadowWind.hlsl"
            #include "GrassSurface.hlsl"
            struct Blade { float4 positionSeed; float4 shape; float4 surfaceNormal; };
            StructuredBuffer<Blade> _Blades;
            float _Wind, _DebugLOD;
            float4 _StyleRoot, _StyleTip, _StyleMotion;
            float _StyleEmission;
            int _Segments, _Lod;
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float3 color:TEXCOORD2; float fog:TEXCOORD3; };
            Varyings Vert(uint vertex:SV_VertexID,uint instance:SV_InstanceID)
            {
                Blade b=_Blades[instance];
                uint corner=vertex%6;
                float upper=(corner==2 || corner==4 || corner==5) ? 1 : 0;
                float side=(corner==1 || corner==3 || corner==5) ? 1 : -1;
                float t=(vertex/6+upper)/(float)_Segments;
                float angle=b.shape.w;
                float3 right=float3(cos(angle),0,sin(angle));
                float curve=b.shape.z<.5 ? .16 : (b.shape.z<1.5 ? .34 : .24);
                curve*=_StyleMotion.x;
                float3 bend=float3(sin(angle),0,-cos(angle))*curve;
                bend+=MeadowWind(b.positionSeed.xz,b.positionSeed.w,_Time.y*_StyleMotion.y)*_Wind*_StyleMotion.z;
                float3 pos=b.positionSeed.xyz+float3(0,t*b.shape.x,0)+bend*t*t*b.shape.x;
                float profile=b.shape.z<.5 ? (1-t) : (b.shape.z<1.5 ? (1-t)*(.6+.85*sin(t*3.14159)) : (1-t)*(.9+.15*sin(t*3.14159)));
                pos+=right*side*b.shape.y*profile;
                pos=b.positionSeed.xyz+SurfaceRotate(pos-b.positionSeed.xyz,b.surfaceNormal.xyz);
                Varyings o;
                o.positionCS=TransformWorldToHClip(pos); o.positionWS=pos;
                o.normalWS=normalize(cross(right,float3(0,1,0)+2*bend*t));
                float3 baseColor=_StyleRoot.rgb*(b.shape.z<.5 ? .85 : 1);
                float3 tip=_StyleTip.rgb*(b.shape.z<.5 ? .88 : 1);
                if (b.shape.z>1.5) { baseColor*=float3(1.3,1,.85); tip=lerp(tip,tip*float3(1.25,1,.72),.65); }
                float patchTone=.5+.5*sin(b.positionSeed.x*.17+sin(b.positionSeed.z*.12)*2);
                o.color=lerp(baseColor,tip,smoothstep(0,1,t))*lerp(.94,1.06,b.positionSeed.w);
                o.color=lerp(o.color,o.color*float3(.9,1.02,1.08),patchTone*.35);
                if (_DebugLOD>.5) o.color=_Lod==0 ? float3(.1,1,.3) : (_Lod==1 ? float3(1,.65,.05) : float3(.3,.45,1));
                o.normalWS=SurfaceRotate(o.normalWS,b.surfaceNormal.xyz);
                o.fog=ComputeFogFactor(o.positionCS.z); return o;
            }
            half4 Frag(Varyings i, bool front:SV_IsFrontFace):SV_Target
            {
                float3 n=normalize(front ? i.normalWS : -i.normalWS);
                Light light=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float diffuse=.55+.45*abs(dot(n,light.direction));
                diffuse=lerp(diffuse,floor(diffuse*3)/3,_StyleMotion.w);
                float3 color=i.color*(SampleSH(float3(0,1,0))*.8+light.color*diffuse*lerp(.5,1,light.shadowAttenuation));
                if (_DebugLOD<.5) color+=i.color*_StyleEmission;
                return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
    }
}

