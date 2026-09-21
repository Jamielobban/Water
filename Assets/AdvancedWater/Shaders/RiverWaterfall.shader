Shader "Advanced Water/River and Waterfall"
{
    Properties
    {
        _WaterColor("River Color", Color)=(0.025,0.3,0.32,1)
        _FallColor("Waterfall Color", Color)=(0.25,0.66,0.7,1)
        _FoamColor("Foam Color", Color)=(0.87,0.98,0.97,1)
        _FlowSpeed("Downstream Speed", Range(0,12))=3
        _FoamStrength("Foam Strength", Range(0,1))=0.8
        _Smoothness("Smoothness", Range(0,1))=0.85
        [Enum(Natural,0,Cel Lagoon,1,Watercolor,2,Graphic Ink,3,Arcane,4)] _Style("River Style", Float)=0
        _FallFoam("Waterfall Foam Coverage", Range(0,1))=0.22
        _PoolFoam("Landing Foam Coverage", Range(0,1))=0.55
        _BankFoam("Bank Foam Coverage", Range(0,1))=0.35
        _MarkScale("Pattern Scale", Range(0.2,3))=1
        _AccentColor("Style Accent", Color)=(0.12,0.65,0.64,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _WaterColor, _FallColor, _FoamColor;
                float _FlowSpeed, _FoamStrength, _Smoothness;
                float _Style, _FallFoam, _PoolFoam, _BankFoam, _MarkScale;
                half4 _AccentColor;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2; half4 color:COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionWS=TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS=TransformWorldToHClip(o.positionWS);
                o.normalWS=TransformObjectToWorldNormal(input.normalOS);
                o.uv=input.uv; o.color=input.color; return o;
            }
            float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float Noise(float2 p)
            {
                float2 i=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);
            }
            half4 Frag(Varyings input):SV_Target
            {
                float t=_Time.y*_FlowSpeed;
                float2 flow=float2(input.uv.x*20,input.uv.y-t)*_MarkScale;
                float noise=Noise(flow*float2(1,0.75));
                float fine=Noise(flow*float2(2.7,1.8)+7);
                float streak=Noise(float2(flow.x*2.2,flow.y*0.22));
                float bank=1-smoothstep(0.01,0.085,min(input.uv.x,1-input.uv.x));
                float falling=smoothstep(0.12,0.65,input.color.g);
                // Coverage sets a threshold, rather than adding white until the mask saturates.
                // Keep colored gaps through the lip and curtain; reserve broader foam for the pool.
                float coverage=lerp(input.color.r*_PoolFoam,_FallFoam,falling);
                coverage=max(coverage,bank*_BankFoam)*_FoamStrength;
                float pattern=lerp(noise*0.72+fine*0.28,streak,falling);
                float threshold=1-saturate(coverage)*0.65;
                float foam=smoothstep(threshold-0.065,threshold+0.065,pattern)*saturate(coverage*8);
                half3 water=lerp(_WaterColor.rgb,_FallColor.rgb,falling*0.7);
                water*=0.83+noise*0.3;
                float marks=0;
                if (_Style>0.5 && _Style<1.5)
                {
                    float bands=floor(noise*3)/2;
                    water=lerp(water,_AccentColor.rgb,bands*0.4);
                    foam=smoothstep(0.4,0.6,foam);
                    marks=smoothstep(0.92,0.98,sin(flow.y*2+noise*4))*0.12*(1-falling);
                }
                else if (_Style>1.5 && _Style<2.5)
                {
                    float wash=Noise(flow*float2(0.35,0.6));
                    float grain=Noise(input.uv*float2(750,100));
                    water=lerp(water,_AccentColor.rgb,wash*0.42)*(0.94+grain*0.12);
                    foam*=0.55+fine*0.35;
                }
                else if (_Style>2.5 && _Style<3.5)
                {
                    water=lerp(_WaterColor.rgb,_FallColor.rgb,step(0.52,noise)*0.45+falling*0.3);
                    float hatch=1-smoothstep(0.04,0.12,abs(sin(flow.x*4+flow.y*2)));
                    water=lerp(water,_AccentColor.rgb,hatch*0.5);
                    foam=step(0.5,foam);
                }
                else if (_Style>3.5)
                {
                    float ribbon=sin(flow.x*2+sin(flow.y*0.6)*2);
                    marks=pow(saturate(ribbon),16)*(0.25+noise*0.65);
                    water=lerp(water,_AccentColor.rgb,marks*0.45);
                }
                half3 normal=normalize(input.normalWS);
                half3 view=GetWorldSpaceNormalizeViewDir(input.positionWS);
                if (dot(normal,view)<0) normal=-normal;
                Light light=GetMainLight();
                half diffuse=saturate(dot(normal,light.direction));
                if (_Style>0.5 && _Style<1.5) diffuse=floor(diffuse*3)/3;
                half spec=pow(saturate(dot(normal,normalize(light.direction+view))),lerp(16,180,_Smoothness));
                half fresnel=pow(1-saturate(dot(normal,view)),4);
                half3 color=water*(SampleSH(normal)+light.color*(0.35+0.65*diffuse));
                color+=light.color*spec*(0.08+fine*0.2)*(1-foam)+fresnel*_FallColor.rgb*0.25;
                color+=_AccentColor.rgb*marks*(_Style>3.5?1.5:0.4);
                return half4(lerp(color,_FoamColor.rgb,foam),1);
            }
            ENDHLSL
        }
    }
}
