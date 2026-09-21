Shader "GPU Grass/Flowers"
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
            int _Segments, _Lod, _Petals;
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float3 color:TEXCOORD2; float fog:TEXCOORD3; };
            float3 Stem(Blade b, float3 bend, float t)
            {
                return b.positionSeed.xyz+float3(0,t*b.shape.x,0)+bend*t*t*b.shape.x;
            }
            Varyings Vert(uint vertex:SV_VertexID, uint instance:SV_InstanceID)
            {
                Blade b=_Blades[instance];
                float phase=b.shape.w;
                float3 bend=float3(sin(phase),0,-cos(phase))*.08;
                bend+=MeadowWind(b.positionSeed.xz,b.positionSeed.w,_Time.y)*_Wind*.65;
                float3 right=float3(cos(phase),0,sin(phase));
                float3 pos, normal, color;
                uint stemVertices=(uint)_Segments*6;
                uint leafVertices=_Lod<2 ? 12u : 0u;
                if (vertex<stemVertices)
                {
                    uint c=vertex%6;
                    float upper=(c==2 || c==4 || c==5) ? 1 : 0;
                    float side=(c==1 || c==3 || c==5) ? 1 : -1;
                    float t=(vertex/6+upper)/(float)_Segments;
                    pos=Stem(b,bend,t)+right*side*b.shape.y*(1-.45*t);
                    normal=normalize(cross(right,float3(0,1,0)+2*bend*t));
                    color=lerp(float3(.035,.12,.025),float3(.21,.38,.06),t);
                }
                else if (vertex<stemVertices+leafVertices)
                {
                    uint v=vertex-stemVertices, leaf=v/6, c=v%6;
                    float3 outward=right*(leaf==0 ? 1 : -1);
                    float3 across=normalize(cross(outward,float3(0,1,0)));
                    float t=leaf==0 ? .38 : .63;
                    float along=(c==0 || c==3) ? 0 : ((c==2 || c==4) ? 1 : .5);
                    float side=c==1 ? 1 : (c==5 ? -1 : 0);
                    pos=Stem(b,bend,t)+outward*along*.3+float3(0,along*.16,0)+across*side*.065;
                    normal=normalize(float3(0,1,0)-outward*.5);
                    color=lerp(float3(.055,.2,.025),float3(.28,.43,.08),along);
                }
                else
                {
                    uint v=vertex-stemVertices-leafVertices;
                    float3 axis=normalize(float3(bend.x*.7,1,bend.z*.7));
                    float3 u=normalize(cross(float3(0,0,1),axis));
                    float3 w=cross(axis,u);
                    float3 head=Stem(b,bend,1);
                    float scale=lerp(.8,1.2,b.positionSeed.w);
                    float centerRadius=(b.shape.z<.5 ? .065 : .055)*scale;
                    if (v<(uint)_Petals*6)
                    {
                        uint petal=v/6, c=v%6;
                        float angle=phase+petal*6.283185/(float)_Petals;
                        float3 radial=u*cos(angle)+w*sin(angle);
                        float3 tangent=-u*sin(angle)+w*cos(angle);
                        float along=(c==0 || c==3) ? 0 : ((c==2 || c==4) ? 1 : .55);
                        float side=c==1 ? 1 : (c==5 ? -1 : 0);
                        float length=(b.shape.z<.5 ? .23 : (b.shape.z<1.5 ? .28 : .26))*scale;
                        float width=(b.shape.z<.5 ? .045 : .12)*scale*12/(float)_Petals;
                        float cup=b.shape.z<.5 ? -.04 : (b.shape.z<1.5 ? .14 : .055);
                        pos=head+radial*(centerRadius*.65+length*along)+tangent*side*width+axis*(cup*along*along+.025*sin(along*3.14159));
                        normal=normalize(axis-radial*cup*3);
                        float3 petalColor=b.shape.z<.5 ? float3(1,.96,.83) : (b.shape.z<1.5 ? float3(.88,.055,.035) : lerp(float3(.68,.14,.64),float3(1,.43,.68),b.positionSeed.w));
                        color=lerp(petalColor*.6,petalColor,saturate(along*2));
                    }
                    else
                    {
                        uint c=(v-(uint)_Petals*6)%3, slice=(v-(uint)_Petals*6)/3;
                        float angle=phase+(slice+(c==2 ? 1 : 0))*6.283185/(float)_Petals;
                        float radius=c==0 ? 0 : centerRadius;
                        pos=head+(u*cos(angle)+w*sin(angle))*radius+axis*(c==0 ? .045 : .018);
                        normal=axis;
                        color=b.shape.z>.5 && b.shape.z<1.5 ? float3(.085,.035,.045) : float3(.95,.58,.045);
                    }
                }
                pos=b.positionSeed.xyz+SurfaceRotate(pos-b.positionSeed.xyz,b.surfaceNormal.xyz);
                normal=SurfaceRotate(normal,b.surfaceNormal.xyz);
                Varyings o;
                o.positionCS=TransformWorldToHClip(pos); o.positionWS=pos; o.normalWS=normal;
                o.color=color;
                if (_DebugLOD>.5) o.color=_Lod==0 ? float3(.1,1,.3) : (_Lod==1 ? float3(1,.65,.05) : float3(.3,.45,1));
                o.fog=ComputeFogFactor(o.positionCS.z); return o;
            }
            half4 Frag(Varyings i, bool front:SV_IsFrontFace):SV_Target
            {
                float3 n=normalize(front ? i.normalWS : -i.normalWS);
                Light light=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float diffuse=.4+.6*abs(dot(n,light.direction));
                float3 color=i.color*(SampleSH(float3(0,1,0))*.7+light.color*diffuse*lerp(.4,1,light.shadowAttenuation));
                return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
    }
}

