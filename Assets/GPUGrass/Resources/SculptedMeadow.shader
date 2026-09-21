Shader "GPU Grass/Sculpted Meadow"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
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
            float4 _StyleRoot, _StyleTip, _StyleMotion;
            float _Wind, _DebugLOD, _StyleEmission;
            int _Segments, _Sides, _Shape, _Lod;
            struct Varyings
            {
                float4 positionCS:SV_POSITION;
                float3 positionWS:TEXCOORD0;
                float3 normalWS:TEXCOORD1;
                float3 color:TEXCOORD2;
                float3 detail:TEXCOORD3;
                float fog:TEXCOORD4;
            };
            Varyings Vert(uint vertex:SV_VertexID, uint instance:SV_InstanceID)
            {
                Blade b=_Blades[instance];
                float height=b.shape.x;
                float angle=b.shape.w;
                float3 right=float3(cos(angle),0,sin(angle));
                float3 forward=float3(-sin(angle),0,cos(angle));
                float3 wind=MeadowWind(b.positionSeed.xz,b.positionSeed.w,_Time.y*_StyleMotion.y)*_Wind*_StyleMotion.z;
                float3 pos=0, normal=float3(0,1,0);
                float t=0, edge=0, head=0;
                uint stemCount=(uint)_Segments*6;
                if (_Shape==3 || (_Shape==2 && vertex>=stemCount))
                {
                    // Faceted closed-sided stalk head or crystal prism with a pointed crown.
                    uint v=_Shape==3 ? vertex : vertex-stemCount;
                    uint face=v/9, c=v%9;
                    float a=angle+face*6.283185/(float)_Sides;
                    float a2=angle+(face+1)*6.283185/(float)_Sides;
                    bool second=(c==1 || c==3 || c==4 || c==7);
                    float theta=second ? a2 : a;
                    float upper=(c==2 || c==4 || c==5 || c>=6) ? 1 : 0;
                    bool apex=c==8;
                    float bottom=_Shape==3 ? 0 : .73;
                    float top=_Shape==3 ? .72 : .96;
                    t=apex ? 1 : lerp(bottom,top,upper);
                    float radius=_Shape==3 ? b.shape.y : .06+height*.016;
                    float3 radial=float3(cos(theta),0,sin(theta));
                    pos=float3(0,t*height,0)+radial*(apex ? 0 : radius);
                    if (_Shape==2) pos+=wind*t*t*height;
                    float mid=(a+a2)*.5;
                    normal=normalize(float3(cos(mid),c>=6 ? .5 : 0,sin(mid)));
                    head=_Shape==2 ? 1 : 0;
                    edge=c>=6 ? 1 : 0;
                }
                else
                {
                    uint c=vertex%6;
                    float upper=(c==2 || c==4 || c==5) ? 1 : 0;
                    // Both triangles wind the same way: BL, BR, TL / BR, TR, TL.
                    float side=(c==1 || c==3 || c==4) ? 1 : -1;
                    t=(vertex/6+upper)/(float)_Segments;
                    edge=side;
                    if (_Shape==1)
                    {
                        // A broad ribbon whose centerline loops forward and down at its tip.
                        float arc=t*2.5;
                        float curl=(1-cos(arc))*.42;
                        float y=sin(arc)*.85;
                        float twist=t*2+b.positionSeed.w;
                        float3 strip=right*cos(twist)+forward*sin(twist);
                        float width=b.shape.y*(.3+.7*sin(t*3.14159));
                        pos=forward*curl*height+float3(0,y*height,0)+strip*side*width+wind*t*t*height;
                        float3 tangent=forward*sin(arc)*1.05+float3(0,cos(arc)*2.125,0)+wind*2*t;
                        normal=normalize(cross(strip,tangent));
                    }
                    else if (_Shape==4)
                    {
                        // Long pointed leaves, swept in a common breeze with a subtle S curve.
                        float3 sweep=forward*.16+float3(.3,0,.12)+wind;
                        float curve=t*t;
                        float width=b.shape.y*(1-t)*(.85+.15*sin(t*3.14159));
                        pos=float3(0,(t-.15*t*t*t)*height,0)+sweep*curve*height
                            +right*(side*width+sin(t*3.14159)*.055*height);
                        float3 tangent=float3(0,1-.45*t*t,0)+sweep*2*t
                            +right*cos(t*3.14159)*.173;
                        normal=normalize(cross(right,tangent));
                    }
                    else
                    {
                        float width=_Shape==2 ? .009 : b.shape.y*(1-t);
                        float3 lean=_Shape==2 ? wind : forward*.18+wind;
                        pos=float3(0,t*height,0)+lean*t*t*height+right*side*width;
                        normal=normalize(cross(right,float3(0,1,0)+lean*2*t));
                    }
                }
                pos+=b.positionSeed.xyz;
                float3 color=lerp(_StyleRoot.rgb,_StyleTip.rgb,t);
                if (_Shape==0) color=t<.38 ? _StyleRoot.rgb : _StyleTip.rgb;
                if (_Shape==1) color*=lerp(float3(.75,.85,1),float3(1,.9,.7),b.shape.z*.5);
                if (head>.5) color=lerp(float3(.20,.095,.035),float3(.48,.28,.09),b.positionSeed.w);
                if (_Shape==3) color=lerp(color,color.brg,b.shape.z*.22);
                if (_Shape==4)
                {
                    float patch=.5+.5*sin(b.positionSeed.x*.10+sin(b.positionSeed.z*.09)*2);
                    color=lerp(_StyleRoot.rgb,_StyleTip.rgb,smoothstep(.05,.9,t));
                    color*=lerp(float3(.82,1,1.03),float3(1.08,1,.83),patch);
                    if (b.shape.z>1.5) color=lerp(color,color*float3(1.18,1,.72),.55);
                }
                if (_DebugLOD>.5) color=_Lod==0 ? float3(.1,1,.3) : (_Lod==1 ? float3(1,.65,.05) : float3(.3,.45,1));
                pos=b.positionSeed.xyz+SurfaceRotate(pos-b.positionSeed.xyz,b.surfaceNormal.xyz);
                normal=SurfaceRotate(normal,b.surfaceNormal.xyz);
                Varyings o;
                o.positionCS=TransformWorldToHClip(pos); o.positionWS=pos; o.normalWS=normal; o.color=color;
                o.detail=float3(t,edge,b.positionSeed.w); o.fog=ComputeFogFactor(o.positionCS.z); return o;
            }
            half4 Frag(Varyings i, bool front:SV_IsFrontFace):SV_Target
            {
                float3 n=normalize(front ? i.normalWS : -i.normalWS);
                Light sun=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float shade=saturate(dot(n,sun.direction)*.5+.5);
                if (_Shape==0) shade=shade>.55 ? 1 : .45;
                float3 color=i.color*(SampleSH(float3(0,1,0))*.5+sun.color*(.35+.65*shade)*lerp(.4,1,sun.shadowAttenuation));
                if (_Shape==1) color*=lerp(.6,1,smoothstep(.0,.12,1-abs(i.detail.y)));
                if (_Shape==4 && _DebugLOD<.5)
                {
                    // Three deliberate lighting bands and a fine warm highlight on the leaf edge.
                    // Thin leaves transmit light on both sides; avoid camera-facing sign flips.
                    float leafLight=.55+.45*abs(dot(normalize(i.normalWS),sun.direction));
                    float lit=leafLight*sun.shadowAttenuation;
                    float3 band=lerp(float3(.40,.65,.65),float3(.80,.94,.80),smoothstep(.28,.40,lit));
                    band=lerp(band,float3(1.08,1.04,.87),smoothstep(.65,.77,lit));
                    color=i.color*band*(SampleSH(float3(0,1,0))*.45+sun.color*.85);
                    float edgeLine=smoothstep(.78,.95,abs(i.detail.y));
                    float highlight=edgeLine*smoothstep(.35,.9,i.detail.x)*smoothstep(.65,.77,lit);
                    color+=float3(.30,.27,.10)*highlight;
                }
                if (_Shape==3 && _DebugLOD<.5)
                {
                    float3 view=SafeNormalize(GetCameraPositionWS()-i.positionWS);
                    float rim=pow(1-saturate(abs(dot(n,view))),3);
                    float glint=pow(saturate(dot(n,SafeNormalize(view+sun.direction))),40);
                    color+=i.color*_StyleEmission*(.3+i.detail.x*.7)+rim*float3(.2,.5,.8)+glint*.7;
                }
                return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
    }
}

