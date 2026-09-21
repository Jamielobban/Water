Shader "Hidden/Advanced Water/Underwater"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Underwater"
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "WaterWaves.hlsl"
            TEXTURE2D(_CausticsMap); SAMPLER(sampler_CausticsMap);
            float4 _UnderwaterColor, _UnderwaterParams, _WaterBounds, _UnderwaterAccent;
            float _CausticsStrength,_CausticsScale,_UnderwaterStyle,_UnderwaterSteps,_UnderwaterBrush;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv=input.texcoord;
                float rawDepth=SampleSceneDepth(uv);
                #if UNITY_REVERSED_Z
                    float deviceDepth=rawDepth,nearDepth=1;
                #else
                    float deviceDepth=lerp(UNITY_NEAR_CLIP_VALUE,1,rawDepth),nearDepth=UNITY_NEAR_CLIP_VALUE;
                #endif
                float3 world=ComputeWorldSpacePosition(uv,deviceDepth,UNITY_MATRIX_I_VP);
                float3 nearWorld=ComputeWorldSpacePosition(uv,nearDepth,UNITY_MATRIX_I_VP);
                float nearHeight=AWSurfaceHeight(nearWorld.xz);
                float signedDepth=nearHeight-nearWorld.y;
                float mask=smoothstep(-_UnderwaterParams.z,_UnderwaterParams.z,signedDepth);
                float inside=step(_WaterBounds.x,nearWorld.x)*step(nearWorld.x,_WaterBounds.z)
                    *step(_WaterBounds.y,nearWorld.z)*step(nearWorld.z,_WaterBounds.w);
                mask*=inside;
                float2 distortion=float2(sin(uv.y*31+_AW_Time*1.6),cos(uv.x*27+_AW_Time*1.3))*_UnderwaterParams.y*mask;
                float3 original=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv).rgb;
                float3 color=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,saturate(uv+distortion)).rgb;
                float3 ray=world-nearWorld;
                float path=min(length(ray),300);
                float3 direction=normalize(ray);
                // Clip the underwater path at the surface for upward-facing rays.
                if (direction.y>0.0001) path=min(path,max(0,signedDepth)/direction.y);
                // Clip against rectangular lake bounds so distant land beyond a lake is not fully fogged.
                float2 exitDistance=float2(1e5,1e5);
                if (abs(direction.x)>0.0001) exitDistance.x=((direction.x>0?_WaterBounds.z:_WaterBounds.x)-nearWorld.x)/direction.x;
                if (abs(direction.z)>0.0001) exitDistance.y=((direction.z>0?_WaterBounds.w:_WaterBounds.y)-nearWorld.z)/direction.z;
                path=min(path,max(0,min(exitDistance.x,exitDistance.y)));
                float below=saturate((_AW_Level-world.y)*2);
                float2 cuv=world.xz*_CausticsScale;
                float caustic=min(SAMPLE_TEXTURE2D(_CausticsMap,sampler_CausticsMap,cuv+_AW_Time*0.035).r,
                    SAMPLE_TEXTURE2D(_CausticsMap,sampler_CausticsMap,cuv*1.17-_AW_Time*0.026).r);
                color+=caustic*_CausticsStrength*below*exp(-max(0,_AW_Level-world.y)*0.16);
                // Preferentially absorb red light; clear presets retain nearby floor detail.
                float3 transmission=exp(-path*_UnderwaterParams.x*float3(1.8,0.85,0.55));
                color=color*transmission+_UnderwaterColor.rgb*(1-transmission);
                if (_UnderwaterStyle>=0)
                {
                    float steps=max(2,_UnderwaterSteps);
                    color=floor(saturate(color)*steps+0.5)/steps;
                    float lightRibbon=pow(saturate(sin(world.x*0.12+world.z*0.09-_AW_Time*0.7)*0.5+0.5),8);
                    float accentStrength=lerp(0.035,0.14,step(3.5,_UnderwaterStyle))*max(0.2,_UnderwaterBrush);
                    color+=_UnderwaterAccent.rgb*lightRibbon*accentStrength*exp(-max(0,_AW_Level-world.y)*0.12);
                }
                float waterline=pow(saturate(1-abs(signedDepth)/max(_UnderwaterParams.z,0.001)),3)*inside;
                color=lerp(original,color,mask);
                color*=1-waterline*0.16;
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
