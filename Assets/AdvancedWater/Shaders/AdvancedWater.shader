Shader "Advanced Water/Lake and Ocean"

{

    Properties

    {

        [Header(Color and Absorption)]

        _ShallowColor("Shallow Color", Color) = (0.08,0.55,0.52,1)

        _DeepColor("Deep Color", Color) = (0.012,0.09,0.15,1)

        _Absorption("Absorption RGB per meter", Vector) = (0.35,0.12,0.07,0)

        _DepthDistance("Color Depth Distance", Float) = 8

        _EdgeFade("Contact Edge Fade", Range(0.01,2)) = 0.25

        [Header(Normals and Current)]

        [Normal] _NormalA("Normal A", 2D) = "bump" {}

        [Normal] _NormalB("Normal B", 2D) = "bump" {}

        _NormalScale("Normal Tiling A B Micro", Vector) = (0.12,0.23,1.5,0)

        _NormalStrength("Normal Strength", Range(0,3)) = 0.65

        _MicroStrength("Micro Ripple Strength", Range(0,1)) = 0.15

        _Current("Current X Z and Speed", Vector) = (1,0.3,0.15,0)

        [Toggle(_FLOWMAP)] _UseFlow("Use Flow Map", Float) = 0

        _FlowMap("Flow RG direction B speed", 2D) = "gray" {}

        _FlowBounds("Flow Map Origin XZ and Size XZ", Vector) = (-50,-50,100,100)

        _DetailFade("Normal Fade Start End", Vector) = (60,350,0,0)

        [Header(Refraction and Reflection)]

        [Toggle(_REFRACTION)] _UseRefraction("Refraction", Float) = 1

        _RefractionStrength("Refraction Strength", Range(0,0.1)) = 0.025

        _Smoothness("Smoothness", Range(0,1)) = 0.9

        _ReflectionStrength("Reflection Strength", Range(0,2)) = 1

        _PlanarStrength("Planar Reflection Blend", Range(0,1)) = 0.85

        [HDR] _SpecularColor("Sun Highlight", Color) = (1,0.96,0.85,1)

        _SunStrength("Sun Strength", Range(0,5)) = 1

        _Sparkle("Micro Sparkle", Range(0,2)) = 0.3

        [Header(Foam)]

        [Toggle(_FOAM)] _UseFoam("Foam", Float) = 1

        _FoamMap("Foam Mask R", 2D) = "white" {}

        _ShoreFoamMap("Contact Foam Mask R", 2D) = "white" {}

        _NoiseMap("Foam Distortion Noise R", 2D) = "gray" {}

        _FoamColor("Foam Color", Color) = (0.9,0.98,0.98,1)

        _FoamUnlit("Pure Unlit Foam", Range(0,1)) = 0

        _FoamScale("Foam Tiling", Float) = 0.45

        _FoamDistortion("Foam Distortion", Range(0,1)) = 0.18

        _ShoreFoamWidth("Contact Foam Depth", Float) = 0.75

        _ShoreFoamStrength("Contact Foam Strength", Range(0,3)) = 0.9

        _IntersectionFoamWidth("Object Foam Depth", Range(0.01,1)) = 0.16

        _IntersectionFoamStrength("Object Ring Strength", Range(0,3)) = 1

        _CrestThreshold("Crest Foam Threshold", Range(0,1)) = 0.7

        _CrestFoamStrength("Crest Foam Strength", Range(0,3)) = 0.8

        _StylizedFoamCoverage("Stylized Crest Coverage", Range(0,1)) = 0

        _ShoreWaveStrength("Shore Wash Foam", Range(0,2)) = 0.5

        _ShoreWaveSpeed("Shore Wash Speed", Float) = 0.6

        _InteractionFoamStrength("Ripple and Wake Foam", Range(0,2)) = 0

        [Header(Caustics)]

        [Toggle(_CAUSTICS)] _UseCaustics("Caustics", Float) = 1

        _CausticsMap("Caustics R", 2D) = "black" {}

        _CausticsScale("Caustics Tiling", Float) = 0.65

        _CausticsStrength("Caustics Strength", Range(0,3)) = 0.3

        _CausticsDepth("Caustics Maximum Depth", Float) = 10

        [Header(Art Direction)]

        _ScatterStrength("Backlit Wave Scattering", Range(0,3)) = 0.65

        _StylizedSteps("Color Bands 0 for Smooth", Range(0,8)) = 0

    }

    SubShader

    {

        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }

        Pass

        {

            Name "WaterForward"

            Tags { "LightMode"="UniversalForwardOnly" }

            Blend SrcAlpha OneMinusSrcAlpha

            ZWrite Off

            Cull Off

            HLSLPROGRAM

            #pragma target 3.5

            #pragma vertex Vert

            #pragma fragment Frag

            #pragma shader_feature_local_fragment _REFRACTION

            #pragma shader_feature_local_fragment _FOAM

            #pragma shader_feature_local_fragment _FLOWMAP

            #pragma shader_feature_local_fragment _CAUSTICS

            #pragma multi_compile_fog

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING

            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION

            #define _SURFACE_TYPE_TRANSPARENT 1

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            #include "WaterWaves.hlsl"



            TEXTURE2D(_NormalA); SAMPLER(sampler_NormalA);

            TEXTURE2D(_NormalB); SAMPLER(sampler_NormalB);

            TEXTURE2D(_FoamMap); SAMPLER(sampler_FoamMap);

            TEXTURE2D(_ShoreFoamMap); SAMPLER(sampler_ShoreFoamMap);

            TEXTURE2D(_NoiseMap); SAMPLER(sampler_NoiseMap);

            TEXTURE2D(_FlowMap); SAMPLER(sampler_FlowMap);

            TEXTURE2D(_CausticsMap); SAMPLER(sampler_CausticsMap);

            TEXTURE2D(_AW_PlanarTexture); SAMPLER(sampler_AW_PlanarTexture);

            float _AW_PlanarValid;

            CBUFFER_START(UnityPerMaterial)

                float4 _ShallowColor, _DeepColor, _Absorption, _NormalScale, _Current, _FlowBounds, _DetailFade;

                float4 _SpecularColor, _FoamColor;

                float _DepthDistance, _EdgeFade, _NormalStrength, _MicroStrength;

                float _RefractionStrength, _Smoothness, _ReflectionStrength, _PlanarStrength, _SunStrength, _Sparkle;

                float _FoamScale, _FoamDistortion, _ShoreFoamWidth, _ShoreFoamStrength, _FoamUnlit;

                float _IntersectionFoamWidth, _IntersectionFoamStrength;

                float _CrestThreshold, _CrestFoamStrength, _StylizedFoamCoverage;

                float _ShoreWaveStrength, _ShoreWaveSpeed, _InteractionFoamStrength, _CausticsScale, _CausticsStrength, _CausticsDepth;

                float _ScatterStrength, _StylizedSteps;

            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };

            struct Varyings

            {

                float4 positionCS : SV_POSITION;

                float3 positionWS : TEXCOORD0;

                float3 normalWS : TEXCOORD1;

                float3 waveInfo : TEXCOORD2; // height, horizontal compression, fog

                UNITY_VERTEX_OUTPUT_STEREO

            };

            Varyings Vert(Attributes input)

            {

                Varyings o = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 p = TransformObjectToWorld(input.positionOS.xyz);

                float3 d,t,b; AWDisplace(p.xz,d,t,b);

                o.positionWS = p+d;

                o.positionCS = TransformWorldToHClip(o.positionWS);

                float3 waveCross=cross(b,t);

                o.normalWS = normalize(waveCross);

                float total = 0.001;

                [unroll] for (int i=0; i<12; i++) total += _AW_Waves[i].z;

                float compression=saturate((1-waveCross.y)*1.35);

                o.waveInfo = float3(saturate(d.y/total*0.5+0.5),compression,ComputeFogFactor(o.positionCS.z));

                return o;

            }

            float FoamHash(float2 cell)

            {

                float3 h=frac(float3(cell.x,cell.y,cell.x)*0.1031);

                h+=dot(h,h.yzx+33.33);

                return frac((h.x+h.y)*h.z);

            }

            float FoamNoise(float2 position)

            {

                float2 cell=floor(position), f=frac(position);

                f=f*f*(3-2*f);

                return lerp(lerp(FoamHash(cell),FoamHash(cell+float2(1,0)),f.x),

                    lerp(FoamHash(cell+float2(0,1)),FoamHash(cell+float2(1,1)),f.x),f.y);

            }

            float3 SceneWorld(float2 uv)

            {

                float depth = SampleSceneDepth(uv);

                #if !UNITY_REVERSED_Z

                    depth = lerp(UNITY_NEAR_CLIP_VALUE,1,depth);

                #endif

                return ComputeWorldSpacePosition(uv,depth,UNITY_MATRIX_I_VP);

            }

            half4 Frag(Varyings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target

            {

                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 p = input.positionWS;

                float2 uv = GetNormalizedScreenSpaceUV(input.positionCS);

                float3 view = GetWorldSpaceNormalizeViewDir(p);

                float frontFace = IS_FRONT_VFACE(face,1.0,0.0);

                float distanceToCamera = distance(_WorldSpaceCameraPos,p);

                float detail = 1-smoothstep(_DetailFade.x,max(_DetailFade.x+1,_DetailFade.y),distanceToCamera);

                float2 current = _Current.xy * _Current.z;

                #if defined(_FLOWMAP)

                    float3 flow = SAMPLE_TEXTURE2D(_FlowMap,sampler_FlowMap,(p.xz-_FlowBounds.xy)/max(_FlowBounds.zw,0.01)).rgb;

                    current = (flow.rg*2-1)*flow.b*_Current.z;

                #endif

                // Dual-phase advection avoids unbounded texture stretching when a flow map is used.

                float phase0 = frac(_AW_Time*0.08), phase1 = frac(_AW_Time*0.08+0.5);

                float blend = abs(phase0*2-1);

                float2 baseUV = p.xz*_NormalScale.x;

                float3 a = lerp(UnpackNormal(SAMPLE_TEXTURE2D(_NormalA,sampler_NormalA,baseUV-current*phase0*12.5)),

                                UnpackNormal(SAMPLE_TEXTURE2D(_NormalA,sampler_NormalA,baseUV-current*phase1*12.5)),blend);

                // Cross the second layer at an irrational-looking angle so directional maps cannot form parallel bands.

                float2 crossUV=float2(p.x*0.614-p.z*0.789,p.x*0.789+p.z*0.614)*_NormalScale.y;

                float3 b = UnpackNormal(SAMPLE_TEXTURE2D(_NormalB,sampler_NormalB,crossUV+float2(-current.y,current.x)*_AW_Time*0.7));

                float2 bSlope=float2(b.x*0.614+b.y*0.789,-b.x*0.789+b.y*0.614);

                // Keep the secondary layer subordinate to the broad surface motion.

                float2 slope = (a.xy+bSlope*0.55)*_NormalStrength*detail;

                if (_AW_Quality >= 2)

                {

                    float2 microUV=float2(p.x*0.927+p.z*0.375,-p.x*0.375+p.z*0.927)*_NormalScale.z;

                    float2 micro=UnpackNormal(SAMPLE_TEXTURE2D(_NormalB,sampler_NormalB,microUV-current*_AW_Time*1.7)).xy;

                    micro=float2(micro.x*0.927-micro.y*0.375,micro.x*0.375+micro.y*0.927);

                    float microFade=1-smoothstep(_DetailFade.x*0.25,max(_DetailFade.x*0.25+1,_DetailFade.x),distanceToCamera);

                    slope += micro*_MicroStrength*microFade;

                }

                float3 ripple = AWRipple(p.xz);

                float3 wake = AWWake(p.xz);

                slope += ripple.xy+wake.xy;

                float3 waveNormal=normalize(input.normalWS);

                // Perturb the wave's height gradient instead of adding to a unit normal.

                float3 normal = normalize(waveNormal+float3(-slope.x,0,-slope.y)*max(waveNormal.y,0.1));

                normal *= IS_FRONT_VFACE(face,1,-1);

                float3 scene = SceneWorld(uv);

                float3 contactScene = scene;

                float sceneSurfaceY = scene.y;

                float waterEye = -TransformWorldToView(p).z;

                float gap = max(0,-TransformWorldToView(scene).z-waterEye);

                float pathLength = max(0,dot(scene-p,-view));

                float2 refractedUV = uv;

                #if defined(_REFRACTION)

                    if (_AW_Quality >= 1)

                    {

                        float2 offset = mul((float3x3)UNITY_MATRIX_V,normal).xy*_RefractionStrength*saturate(gap);

                        float2 testUV = clamp(uv+offset,0.002,0.998);

                        float3 testScene = SceneWorld(testUV);

                        // Do not refract foreground objects across the surface silhouette.

                        if (-TransformWorldToView(testScene).z > waterEye+0.03)

                        { refractedUV=testUV; scene=testScene; pathLength=distance(scene,p); }

                    }

                #endif

                float colorDepth = saturate(pathLength/max(0.01,_DepthDistance));

                if (_StylizedSteps >= 2) colorDepth = floor(colorDepth*_StylizedSteps)/_StylizedSteps;

                float3 waterColor = lerp(_ShallowColor.rgb,_DeepColor.rgb,colorDepth);

                float3 transmission = exp(-max(_Absorption.rgb,0.001)*min(pathLength,500));

                float3 background = SampleSceneColor(refractedUV);

                Light sun = GetMainLight(TransformWorldToShadowCoord(p));

                #if defined(_CAUSTICS)

                    if (_AW_Quality >= 1 && frontFace > 0.5)

                    {

                        float2 cuv = scene.xz*_CausticsScale;

                        float caustic = min(SAMPLE_TEXTURE2D(_CausticsMap,sampler_CausticsMap,cuv+_AW_Time*0.035).r,

                            SAMPLE_TEXTURE2D(_CausticsMap,sampler_CausticsMap,cuv*1.17-_AW_Time*0.026).r);

                        float submerged = max(0,p.y-scene.y);

                        background += caustic*_CausticsStrength*saturate(1-submerged/max(_CausticsDepth,0.01))

                            *saturate(submerged*3)*sun.color*sun.shadowAttenuation;

                    }

                #endif

                float3 color = background*transmission+waterColor*(1-transmission);

                // Below the surface the opaque background is beyond the water/air

                // interface. The underwater pass handles camera-to-surface absorption.

                color=lerp(background,color,frontFace);

                float cosView=saturate(dot(normal,view));

                float fresnel = 0.02+0.98*pow(1-cosView,5);

                if (frontFace < 0.5)

                {

                    // Water-to-air refraction closes the viewing window at the critical angle.

                    float sinTransmittedSq=1.333*1.333*(1-cosView*cosView);

                    float cosTransmitted=sqrt(saturate(1-sinTransmittedSq));

                    fresnel=lerp(0.02+0.98*pow(1-cosTransmitted,5),1,smoothstep(0.97,1,sinTransmittedSq));

                }

                if (_StylizedSteps >= 2)

                {

                    float toonLight=floor(saturate(dot(normal,sun.direction)*0.5+0.5)*3)/2;

                    color*=lerp(0.78,1.1,toonLight);

                    fresnel=floor(saturate(fresnel)*4+0.5)/4;

                }

                float3 reflection = GlossyEnvironmentReflection(reflect(-view,normal),p,1-_Smoothness,1,uv);

                // The above-water planar capture is not an underwater reflection.

                if (_AW_PlanarValid > 0.5 && _AW_Quality >= 2 && frontFace > 0.5)

                {

                    float2 ruv=uv+mul((float3x3)UNITY_MATRIX_V,normal).xy*_RefractionStrength*0.12;

                    float valid=smoothstep(0,0.03,min(min(ruv.x,ruv.y),min(1-ruv.x,1-ruv.y)));

                    float3 planar = SAMPLE_TEXTURE2D(_AW_PlanarTexture,sampler_AW_PlanarTexture,saturate(ruv)).rgb;

                    float planarFlatness=smoothstep(0.38,0.82,abs(normal.y));

                    reflection = lerp(reflection,planar,_PlanarStrength*valid*planarFlatness);

                }

                // Approximate submerged reflected radiance until underwater reflections exist.

                if (frontFace < 0.5) reflection=lerp(_DeepColor.rgb,_ShallowColor.rgb,0.35);

                color = lerp(color,reflection,saturate(fresnel*_ReflectionStrength));

                float3 halfDir = SafeNormalize(view+sun.direction);

                float exponent = exp2(4+_Smoothness*8);

                float specular = pow(saturate(dot(normal,halfDir)),exponent)*_SunStrength;

                specular += pow(saturate(dot(normal,halfDir)),1500)*_Sparkle*detail;

                if (_StylizedSteps >= 2) specular=smoothstep(0.18,0.34,specular);

                color += specular*sun.color*_SpecularColor.rgb*sun.shadowAttenuation*frontFace;

                color += pow(saturate(dot(view,-sun.direction)),4)*input.waveInfo.x*_ScatterStrength

                    *_ShallowColor.rgb*sun.color*(1-fresnel)*sun.shadowAttenuation;

                float foam = 0;

                #if defined(_FOAM)

                    float noise = SAMPLE_TEXTURE2D(_NoiseMap,sampler_NoiseMap,p.xz*_FoamScale*0.27+_AW_Time*0.012).r;

                    float mask = SAMPLE_TEXTURE2D(_FoamMap,sampler_FoamMap,p.xz*_FoamScale-current*_AW_Time*0.25+noise*_FoamDistortion).r;

                    float shoreMask = SAMPLE_TEXTURE2D(_ShoreFoamMap,sampler_ShoreFoamMap,

                        p.xz*_FoamScale*0.72+float2(noise,-noise)*_FoamDistortion*0.45).r;

                    float fineMask = SAMPLE_TEXTURE2D(_NoiseMap,sampler_NoiseMap,p.xz*_FoamScale*1.9-current*_AW_Time*0.19).r;

                    float submergedGround=max(0,p.y-sceneSurfaceY);

                    // Screen-depth alone cannot distinguish shallow terrain from a submerged hull.

                    // Reconstruct the opaque surface orientation from the existing world position:

                    // upward-facing surfaces retain the broad shore band, while steep surfaces use

                    // the narrow eye-depth contact ring below. No additional depth samples are needed.

                    float3 sceneDx=ddx(contactScene);

                    float3 sceneDy=ddy(contactScene);

                    float3 sceneCross=cross(sceneDx,sceneDy);

                    float sceneUp=abs(sceneCross.y)*rsqrt(max(dot(sceneCross,sceneCross),1e-8));

                    float terrainWeight=smoothstep(0.45,0.82,sceneUp);

                    float groundOnly=smoothstep(0.015,0.08,submergedGround)*exp(-submergedGround*1.8);

                    float shore = saturate(1-submergedGround/max(_ShoreFoamWidth,0.01))*groundOnly*terrainWeight;

                    // Object intersection foam uses WORLD-SPACE vertical depth, not a
                    // screen-space dilation. This keeps the contact stable with camera angle and
                    // avoids repeated/offset copies of the silhouette.
                    //
                    // signedContactDepth > 0 means the opaque scene surface is below the local
                    // displaced water surface. Surfaces above the waterline are rejected.
                    float signedContactDepth=p.y-contactScene.y;

                    float objectBelowWater=smoothstep(0.002,0.025,signedContactDepth);

                    float intersectionDepth=1-smoothstep(0.0,max(_IntersectionFoamWidth,0.01),

                        max(signedContactDepth,0.0));

                    // Keep broad shoreline foam on upward-facing terrain. The dedicated object
                    // contact foam is aimed at steep surfaces such as hulls, rocks and pillars.
                    float objectWeight=1-terrainWeight;

                    float intersectionMask=intersectionDepth*objectBelowWater*objectWeight;

                    // Use the depth mask to REVEAL the existing foam texture instead of drawing
                    // a solid white depth band. Close to the contact line the cutoff drops and
                    // more foam survives; farther away only sparse texture islands remain.
                    float intersectionTexture=saturate(shoreMask*0.62+fineMask*0.28+noise*0.10);

                    float intersectionCutoff=lerp(0.84,0.30,saturate(intersectionMask));

                    // Small world-space/noise modulation stops the cutoff from becoming a perfectly
                    // uniform stripe while retaining the same foam language as the rest of the shader.
                    intersectionCutoff+=(0.5-noise)*0.10;

                    float intersectionAA=max(fwidth(intersectionTexture),0.015);

                    float intersectionPattern=smoothstep(intersectionCutoff-intersectionAA,

                        intersectionCutoff+intersectionAA,intersectionTexture);

                    float intersectionFoam=saturate(intersectionMask*_IntersectionFoamStrength)

                        *intersectionPattern;

                    float wash = pow(saturate(sin(gap*4-_AW_Time*_ShoreWaveSpeed*3)*0.5+0.5),5);

                    float crest = smoothstep(_CrestThreshold,min(_CrestThreshold+0.16,1.001),input.waveInfo.y);

                    if (_StylizedSteps >= 2 && _StylizedFoamCoverage > 0)

                    {

                        float heightThreshold=lerp(0.9,0.48,_StylizedFoamCoverage);

                        float graphicCrest=smoothstep(heightThreshold,min(heightThreshold+0.1,1),input.waveInfo.x);

                        crest=max(crest,graphicCrest);

                    }

                    if (_StylizedSteps >= 2) crest *= smoothstep(0.3,0.76,mask*0.72+fineMask*0.45);

                    float pattern = saturate(mask*0.72+fineMask*0.36);

                    float shoreCoverage=shore*(_ShoreFoamStrength+wash*_ShoreWaveStrength);

                    float crestCoverage=crest*_CrestFoamStrength;

                    float shorePattern=saturate(shoreMask*0.76+fineMask*0.24);

                    float shoreFoam=smoothstep(0.16,0.76,shoreCoverage*(0.42+shorePattern*0.58));

                    float crestFoam=smoothstep(0.12,0.95,crestCoverage*pattern);

                    if (_StylizedSteps < 2)

                    {

                        // Broad, non-tiled activation patches interrupt the crest stripes.

                        // These evolve continuously; foam is procedural, not a persisted simulation.

                        float2 foamPosition=p.xz-current*_AW_Time*0.45;

                        float breakup=FoamNoise(foamPosition*0.075+float2(_AW_Time*0.023,-_AW_Time*0.017));

                        float breakupFine=FoamNoise(foamPosition*0.19+float2(7.3,19.1));

                        float activation=smoothstep(0.48,0.73,breakup*0.75+breakupFine*0.25);

                        // Procedural foam lace replaces the cloudy photographic mask

                        // on realistic crests. World-space warping prevents a stamped tile.

                        float2 laceUV=foamPosition*max(_FoamScale,0.01)*6;

                        laceUV+=float2(breakup,breakupFine)*1.7;

                        float lace=FoamNoise(laceUV)+0.3*FoamNoise(laceUV*2.13+13.7);

                        float ridge=abs(lace-0.65);

                        float aa=max(fwidth(lace),0.008);

                        float strands=1-smoothstep(0.025,0.075+aa,ridge);

                        float holes=smoothstep(0.25,0.6,FoamNoise(laceUV*0.71+31.2));

                        float softPattern=strands*lerp(0.25,1,holes);

                        crestFoam=saturate(crestCoverage)*activation*softPattern;



                        // Depth contours approximate the shoreline. Positive time moves

                        // the wash bands toward shallower ground, independent of camera depth.

                        float shoreDepth=max(0,_AW_Level-sceneSurfaceY);

                        float shoreBand=(1-smoothstep(_ShoreFoamWidth*0.35,_ShoreFoamWidth,shoreDepth))

                            *smoothstep(0.01,0.12,submergedGround)*terrainWeight;

                        float washPhase=shoreDepth*5+_AW_Time*_ShoreWaveSpeed*2+breakup*2;

                        float washBand=pow(saturate(sin(washPhase)*0.5+0.5),4);

                        float washTail=pow(saturate(sin(washPhase-0.7)*0.5+0.5),2)*0.25;

                        shoreFoam=saturate(shoreBand*(_ShoreFoamStrength*0.15+

                            (washBand+washTail)*_ShoreWaveStrength)*lerp(0.25,1,shorePattern));

                    }

                    if (_StylizedSteps >= 2) crestFoam=smoothstep(0.28,0.76,crestCoverage*pattern);

                    foam=max(max(max(shoreFoam,intersectionFoam),crestFoam),ripple.z*_InteractionFoamStrength*pattern);

                    foam=max(foam,wake.z);

                    foam*=lerp(0.18,1,frontFace);

                    float3 litFoam=_FoamColor.rgb*(0.4+0.6*sun.color*sun.shadowAttenuation);

                    color = lerp(color,lerp(litFoam,_FoamColor.rgb,_FoamUnlit),foam);

                #endif

                color = MixFog(color,input.waveInfo.z);

                return half4(color,saturate(gap/max(_EdgeFade,0.01)));

            }

            ENDHLSL

        }

    }

    CustomEditor "AdvancedWater.Editor.AdvancedWaterShaderGUI"

    FallBack Off

}
