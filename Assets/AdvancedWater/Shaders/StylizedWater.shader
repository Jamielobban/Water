Shader "Advanced Water/Stylized Water"
{
    Properties
    {
        [Header(Graphic Palette)]
        _ShallowColor("Shallow Color", Color)=(0.02,0.75,0.82,1)
        _DeepColor("Deep Color", Color)=(0.015,0.12,0.42,1)
        _ShadowColor("Shadow Band", Color)=(0.01,0.05,0.2,1)
        _AccentColor("Wave Accent", Color)=(0.2,0.95,1,1)
        _ColorDepth("Color Depth", Float)=7
        _ColorSteps("Color Steps", Range(2,8))=4
        _StyleMode("Style Mode", Range(0,12))=0
        [Header(Illustrated Details)]
        _MarkScale("Illustration Tiling", Float)=0.3
        _MarkStrength("Illustration Strength", Range(0,1))=0.65
        _MarkSpeed("Illustration Drift", Range(0,1))=0.12
        _SparkleDensity("Sparkle Density", Range(0,1))=0.3
        [Header(Print and Pixel Details)]
        _PaperScale("Paper Grain Tiling", Float)=6
        _PaperStrength("Paper Grain Strength", Range(0,1))=0.15
        _PigmentStrength("Pigment Pooling", Range(0,1))=0.55
        _HatchScale("Ink Hatch Tiling", Float)=2.5
        _HatchStrength("Ink Hatch Strength", Range(0,1))=0.7
        _PixelSize("Pixel Size in Meters", Float)=0.2
        _PixelFPS("Pixel Animation FPS", Range(1,24))=8
        _DitherStrength("Pixel Dither", Range(0,1))=0.5
        [Header(Layered Paper)]
        _LayerScale("Paper Band Tiling", Float)=0.13
        _LayerShadow("Paper Shadow Strength", Range(0,1))=0.45
        _LayerShadowWidth("Paper Shadow Width", Range(0.005,0.25))=0.07
        _ScallopScale("Scallop Tiling", Float)=0.8
        _ScallopSize("Scallop Depth", Range(0,0.15))=0.04
        _PaperEdgeWidth("Scalloped Foam Width", Range(0.005,0.2))=0.045
        [Header(Bioluminescence)]
        [HDR] _BioGlowColor("Plankton Glow Color", Color)=(0.06,0.85,1,1)
        _BioGlowStrength("Plankton Glow Strength", Range(0,10))=3
        _BioTrailStrength("Interaction Glow Strength", Range(0,10))=4
        _BioDensity("Plankton Density", Range(0,1))=0.35
        _BioScale("Plankton Tiling", Float)=1.2
        _BioFleckSize("Plankton Fleck Size", Range(0.01,0.12))=0.045
        _BioPulseSpeed("Plankton Pulse Speed", Range(0,3))=0.8
        [Header(Surface Marks)]
        [Normal] _NormalA("Broad Normal", 2D)="bump" {}
        [Normal] _NormalB("Detail Normal", 2D)="bump" {}
        _NormalScale("Normal Tiling A B", Vector)=(0.06,0.18,0,0)
        _NormalStrength("Normal Strength", Range(0,2))=0.4
        _Current("Current X Z and Speed", Vector)=(1,0.3,0.15,0)
        _NoiseMap("Brush Noise", 2D)="gray" {}
        _BrushScale("Brush Tiling", Float)=0.13
        _BrushStrength("Brush Strength", Range(0,1))=0.25
        [Header(Graphic Foam)]
        [Toggle(_FOAM)] _UseFoam("Foam", Float)=1
        _FoamMap("Crest Foam", 2D)="white" {}
        _ShoreFoamMap("Contact Foam", 2D)="white" {}
        [HDR] _FoamColor("Foam Color", Color)=(1,1,1,1)
        _FoamScale("Foam Tiling", Float)=0.12
        _FoamCutoff("Foam Cutoff", Range(0,1))=0.5
        _FoamCoverage("Persistent Crest Coverage", Range(0,1))=0.65
        _CrestFoamStrength("Crest Foam Strength", Range(0,1))=0
        _ShoreFoamWidth("Contact Foam Width", Float)=0.8
        _ShoreFoamStrength("Contact Foam Strength", Range(0,3))=1.2
        [Header(Surface Foam Pattern)]
        _SurfaceFoamMap("Surface Foam Texture", 2D)="black" {}
        _SurfaceFoamColor("Surface Foam Color", Color)=(1,0.9,0.72,1)
        _SurfaceFoamShadowColor("Surface Foam Shadow", Color)=(0.08,0.02,0.45,1)
        _SurfaceFoamScale("Surface Foam Tiling", Float)=0.05
        _SurfaceFoamStrength("Surface Foam Strength", Range(0,1))=0
        _SurfaceFoamCutoff("Surface Foam Cutoff", Range(0,1))=0.82
        _SurfaceFoamVariation("Surface Anti-Tiling", Range(0,1))=0.25
        _SurfaceFoamDistortion("Surface Distortion", Range(0,0.5))=0.12
        [Header(Shore Break and Interaction)]
        _ShoreWaveStrength("Moving Shore Breaks", Range(0,2))=0
        _ShoreWaveSpacing("Shore Break Spacing", Float)=0.38
        _ShoreWaveSpeed("Shore Break Speed", Float)=0.65
        _InteractionFoamStrength("Ripple and Wake Foam", Range(0,2))=0
        [Header(Toon Lighting)]
        _ReflectionStrength("Reflection Strength", Range(0,1))=0.25
        _HighlightStrength("Hard Highlight", Range(0,2))=0.5
        _OutlineStrength("Dark Wave Lines", Range(0,1))=0
        _FresnelSteps("Fresnel Steps", Range(1,6))=3
        _EdgeFade("Contact Edge Fade", Range(0.01,2))=0.25
        [HideInInspector] _CausticsMap("Caustics",2D)="black" {}
        [HideInInspector] _CausticsScale("Caustics Scale",Float)=0.3
        [HideInInspector] _CausticsStrength("Caustics Strength",Float)=0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "StylizedWaterForward"
            Tags { "LightMode"="UniversalForwardOnly" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local_fragment _FOAM
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "WaterWaves.hlsl"

            TEXTURE2D(_NormalA); SAMPLER(sampler_NormalA);
            TEXTURE2D(_NormalB); SAMPLER(sampler_NormalB);
            TEXTURE2D(_NoiseMap); SAMPLER(sampler_NoiseMap);
            TEXTURE2D(_FoamMap); SAMPLER(sampler_FoamMap);
            TEXTURE2D(_ShoreFoamMap); SAMPLER(sampler_ShoreFoamMap);
            TEXTURE2D(_SurfaceFoamMap); SAMPLER(sampler_SurfaceFoamMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor,_DeepColor,_ShadowColor,_AccentColor,_FoamColor;
                float4 _SurfaceFoamColor,_SurfaceFoamShadowColor;
                float4 _NormalScale,_Current;
                float _ColorDepth,_ColorSteps,_StyleMode,_NormalStrength,_BrushScale,_BrushStrength;
                float _FoamScale,_FoamCutoff,_FoamCoverage,_CrestFoamStrength,_ShoreFoamWidth,_ShoreFoamStrength;
                float _SurfaceFoamScale,_SurfaceFoamStrength,_SurfaceFoamCutoff,_SurfaceFoamVariation,_SurfaceFoamDistortion;
                float _ShoreWaveStrength,_ShoreWaveSpacing,_ShoreWaveSpeed,_InteractionFoamStrength;
                float _ReflectionStrength,_HighlightStrength,_OutlineStrength,_FresnelSteps,_EdgeFade;
                float _CausticsScale,_CausticsStrength;
                float _MarkScale,_MarkStrength,_MarkSpeed,_SparkleDensity;
                float _PaperScale,_PaperStrength,_PigmentStrength,_HatchScale,_HatchStrength;
                float _PixelSize,_PixelFPS,_DitherStrength;
                float _LayerScale,_LayerShadow,_LayerShadowWidth,_ScallopScale,_ScallopSize,_PaperEdgeWidth;
                float4 _BioGlowColor;
                float _BioGlowStrength,_BioTrailStrength,_BioDensity,_BioScale,_BioFleckSize,_BioPulseSpeed;
            CBUFFER_END

            struct Attributes { float4 positionOS:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            {
                float4 positionCS:SV_POSITION;
                float3 positionWS:TEXCOORD0;
                float3 normalWS:TEXCOORD1;
                float3 waveInfo:TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings o=(Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 p=TransformObjectToWorld(input.positionOS.xyz);
                float3 d,t,b; AWDisplace(p.xz,d,t,b);
                o.positionWS=p+d;
                o.positionCS=TransformWorldToHClip(o.positionWS);
                float3 waveCross=cross(b,t);
                o.normalWS=normalize(waveCross);
                float total=max(_AW_Waves[0].z+_AW_Waves[1].z+_AW_Waves[2].z+_AW_Waves[3].z,0.001);
                o.waveInfo=float3(saturate(d.y/total*0.5+0.5),saturate((1-length(waveCross))*1.35),ComputeFogFactor(o.positionCS.z));
                return o;
            }

            float3 SceneWorld(float2 uv)
            {
                float depth=SampleSceneDepth(uv);
                #if !UNITY_REVERSED_Z
                    depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
                #endif
                return ComputeWorldSpacePosition(uv,depth,UNITY_MATRIX_I_VP);
            }

            float Hash21(float2 p)
            {
                p=frac(p*float2(123.34,456.21));
                p+=dot(p,p+45.32);
                return frac(p.x*p.y);
            }

            float WashNoise(float2 uv)
            {
                float2 cell=floor(uv), f=frac(uv);
                f=f*f*(3-2*f);
                return lerp(lerp(Hash21(cell),Hash21(cell+float2(1,0)),f.x),
                            lerp(Hash21(cell+float2(0,1)),Hash21(cell+float2(1,1)),f.x),f.y);
            }

            float InkHatch(float phase)
            {
                // Differentiate the continuous phase, never the wrapped cell index.
                float footprint=fwidth(phase);
                float hatchMask=1-smoothstep(0.72-max(footprint,0.02),0.72+max(footprint,0.02),cos(phase));
                return (1-hatchMask)*(1-smoothstep(0.7,2.5,footprint));
            }

            float Bayer4(float2 cell)
            {
                float2 a=fmod(fmod(cell,4)+4,4);
                float2 low=fmod(a,2), high=floor(a/2);
                float first=2*low.x+3*low.y-4*low.x*low.y;
                float second=2*high.x+3*high.y-4*high.x*high.y;
                return (4*first+second+0.5)/16;
            }

            float Plankton(float2 world)
            {
                float2 uv=world*max(_BioScale,0.01)+float2(_AW_Time*0.018,-_AW_Time*0.011);
                // Use unwrapped coordinates for the footprint to avoid cell-border artifacts.
                float footprint=max(length(ddx(uv)),length(ddy(uv)));
                float2 cell=floor(uv), q=frac(uv)-0.5;
                float seed=Hash21(cell);
                q-=float2(seed-0.5,Hash21(cell+31.7)-0.5)*0.28;
                float radius=max(_BioFleckSize,0.005)*lerp(0.6,1.15,seed);
                float dist=length(q);
                float aa=max(footprint,0.002);
                float core=1-smoothstep(max(0,radius-aa),radius+aa,dist);
                float halo=exp(-dot(q,q)/max(radius*radius*5,0.0001))*0.22;
                float pulse=0.55+0.45*sin(_AW_Time*_BioPulseSpeed+seed*TWO_PI);
                float visibility=1-smoothstep(0.1,0.4,footprint);
                return (core+halo)*step(seed,_BioDensity)*pulse*visibility;
            }

            float IllustrationShape(float2 q, float seed, float size, bool cozy)
            {
                if (cozy)
                {
                    float pulse=0.65+0.35*sin(_AW_Time*1.7+seed*TWO_PI);
                    q/=lerp(0.65,1.15,seed)*pulse;
                    return sqrt(abs(q.x))+sqrt(abs(q.y))-0.53;
                }
                q.y+=sin(q.x*7+seed*TWO_PI)*0.045;
                return length(q/(float2(0.34,0.055)*size))-1;
            }

            // Derive the pixel footprint before wrapping into randomized cells.
            // Differentiating the final shape crosses cell boundaries and creates false outlines.
            float Illustration(float2 world, bool cozy)
            {
                float2 uv=world*max(_MarkScale,0.001);
                uv+=float2(_AW_Time*_MarkSpeed, _AW_Time*_MarkSpeed*0.19);
                if (!cozy) uv*=float2(0.42,1.65);
                float2 pixelX=ddx(uv), pixelY=ddy(uv);
                float2 cell=floor(uv), q=frac(uv)-0.5;
                float seed=Hash21(cell);
                float size=lerp(0.55,1.0,Hash21(cell+19.7));
                q-=float2(seed-0.5,Hash21(cell+7.1)-0.5)*0.35;
                float shape=IllustrationShape(q,seed,size,cozy);
                // Sample only this mark's shape, keeping its random seed and size fixed.
                float dx=IllustrationShape(q+pixelX*0.5,seed,size,cozy)
                        -IllustrationShape(q-pixelX*0.5,seed,size,cozy);
                float dy=IllustrationShape(q+pixelY*0.5,seed,size,cozy)
                        -IllustrationShape(q-pixelY*0.5,seed,size,cozy);
                float aa=max(abs(dx)+abs(dy),0.005);
                float detailFade=1-smoothstep(0.15,0.5,max(length(pixelX),length(pixelY)));
                return (1-smoothstep(-aa,aa,shape))*step(seed,cozy?_SparkleDensity:0.46)*detailFade;
            }

            half4 Frag(Varyings input,FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 p=input.positionWS;
                bool watercolor=_StyleMode>7.5 && _StyleMode<8.5;
                bool manga=_StyleMode>8.5 && _StyleMode<9.5;
                bool retro=_StyleMode>9.5 && _StyleMode<10.5;
                bool paperCut=_StyleMode>10.5 && _StyleMode<11.5;
                bool bioluminescent=_StyleMode>11.5;
                float styleTime=retro?floor(_AW_Time*max(_PixelFPS,1))/max(_PixelFPS,1):_AW_Time;
                float2 artPosition=retro?(floor(p.xz/max(_PixelSize,0.01))+0.5)*max(_PixelSize,0.01):p.xz;
                float2 screenUV=GetNormalizedScreenSpaceUV(input.positionCS);
                float3 view=GetWorldSpaceNormalizeViewDir(p);
                float2 motion=_Current.xy*_Current.z*styleTime;
                float2 uvA=p.xz*_NormalScale.x-motion;
                float2 uvB=float2(p.x*0.643-p.z*0.766,p.x*0.766+p.z*0.643)*_NormalScale.y+motion*0.6;
                float2 na=UnpackNormal(SAMPLE_TEXTURE2D(_NormalA,sampler_NormalA,uvA)).xy;
                float2 nb=UnpackNormal(SAMPLE_TEXTURE2D(_NormalB,sampler_NormalB,uvB)).xy;
                nb=float2(nb.x*0.643+nb.y*0.766,-nb.x*0.766+nb.y*0.643);
                float3 ripple=AWRipple(p.xz);
                float3 wake=AWWake(p.xz);
                float2 slope=(na+nb)*_NormalStrength+ripple.xy+wake.xy;
                float3 normal=normalize(input.normalWS+float3(-slope.x,0,-slope.y));
                if (_StyleMode>5.5 && _StyleMode<6.5)
                {
                    // Derivatives of displaced geometry give one true normal per triangle.
                    normal=normalize(cross(ddy(p),ddx(p)));
                    normal*=normal.y<0?-1:1;
                }
                normal*=IS_FRONT_VFACE(face,1,-1);

                float3 scene=SceneWorld(screenUV);
                float waterEye=-TransformWorldToView(p).z;
                float gap=max(0,-TransformWorldToView(scene).z-waterEye);
                float path=max(0,dot(scene-p,-view));
                float depthBand=saturate(path/max(_ColorDepth,0.01));
                if (_StyleMode<6.5 || manga || retro) depthBand=floor(depthBand*max(_ColorSteps,2)+0.5)/max(_ColorSteps,2);
                float3 baseColor=lerp(_ShallowColor.rgb,_DeepColor.rgb,depthBand);
                Light sun=GetMainLight(TransformWorldToShadowCoord(p));
                float ndl=saturate(dot(normal,sun.direction)*0.5+0.5);
                float lightBand=saturate(floor(ndl*3)/2);
                if (_StyleMode>6.5 && _StyleMode<8.5) lightBand=smoothstep(0.1,0.95,ndl);
                if (_StyleMode>5.5 && _StyleMode<6.5) lightBand=ndl;
                baseColor=lerp(baseColor*_ShadowColor.rgb*2.2,baseColor,lightBand);

                float noise=SAMPLE_TEXTURE2D(_NoiseMap,sampler_NoiseMap,p.xz*_BrushScale+motion*0.035).r;
                if (_StyleMode>1.5 && _StyleMode<2.5)
                {
                    float paint=floor(noise*4)/3-0.5;
                    baseColor=lerp(baseColor,_AccentColor.rgb,saturate(paint*_BrushStrength*2));
                    baseColor*=1+paint*_BrushStrength*0.35;
                }
                float waveLine=smoothstep(0.48,0.72,input.waveInfo.x)*smoothstep(0.05,0.42,input.waveInfo.y+noise*0.12);
                if (_StyleMode>0.5 && _StyleMode<1.5)
                    baseColor=lerp(baseColor,_AccentColor.rgb,waveLine*0.35);
                if (_StyleMode>2.5 && _StyleMode<3.5)
                {
                    float ink=smoothstep(0.32,0.62,input.waveInfo.y+abs(dot(normal,view)-0.45)*0.35);
                    baseColor=lerp(baseColor,_ShadowColor.rgb,ink*_OutlineStrength);
                }

                float fresnel=pow(1-saturate(dot(normal,view)),3);
                if (_StyleMode<6.5 || manga || retro) fresnel=floor(fresnel*max(_FresnelSteps,1)+0.5)/max(_FresnelSteps,1);
                float3 reflection=GlossyEnvironmentReflection(reflect(-view,normal),p,0.65,1,screenUV);
                if (_StyleMode<0.5) reflection=lerp(reflection,_AccentColor.rgb,0.55);
                else if (_StyleMode<1.5) reflection=_AccentColor.rgb*0.72;
                else if (_StyleMode<2.5) reflection=floor(saturate(reflection)*3+0.5)/3;
                else if (_StyleMode<3.5) reflection=_ShadowColor.rgb;
                else reflection=_AccentColor.rgb;
                if (_StyleMode>4.5) reflection=lerp(_DeepColor.rgb,_AccentColor.rgb,0.65);
                float3 background=SampleSceneColor(screenUV);
                float shallowVisibility=saturate(path/max(_ColorDepth*0.65,0.01));
                float3 color=lerp(background,baseColor,lerp(0.58,0.94,shallowVisibility));
                color=lerp(color,reflection,fresnel*_ReflectionStrength);
                float hardHighlight=smoothstep(0.86,0.9,dot(normal,SafeNormalize(view+sun.direction)))*_HighlightStrength;
                if (_StyleMode>4.5 && _StyleMode<5.5)
                    hardHighlight=smoothstep(0.92,0.94,dot(normal,SafeNormalize(view+sun.direction)))*_HighlightStrength;
                if (_StyleMode>6.5 && _StyleMode<8.5) hardHighlight*=0.15;
                if (_StyleMode>5.5 && _StyleMode<6.5) color=lerp(baseColor,reflection,fresnel*_ReflectionStrength);
                color+=hardHighlight*lerp(_FoamColor.rgb,sun.color,0.25)*sun.shadowAttenuation;

                if (watercolor)
                {
                    float2 washUV=p.xz*max(_BrushScale,0.001)+motion*0.025;
                    float wash=WashNoise(washUV)*0.65+WashNoise(washUV*2.13+11.7)*0.35;
                    float pooling=exp(-abs(wash-0.48)*22)*_PigmentStrength;
                    float grain=WashNoise(p.xz*max(_PaperScale,0.01))*2-1;
                    float grainFade=1-smoothstep(0.25,1.0,max(length(ddx(p.xz)),length(ddy(p.xz)))*max(_PaperScale,0.01));
                    float tint=saturate(path/max(_ColorDepth,0.01)+(wash-0.5)*_BrushStrength);
                    color=lerp(_ShallowColor.rgb,_DeepColor.rgb,tint);
                    color=lerp(color,_AccentColor.rgb,smoothstep(0.52,0.78,wash)*_BrushStrength*0.55);
                    color=lerp(color,color*_ShadowColor.rgb,pooling*0.45);
                    color*=1+grain*_PaperStrength*grainFade*0.35;
                    color=lerp(color,_FoamColor.rgb,pow(1-tint,3)*0.12);
                }
                if (manga)
                {
                    float shade=saturate((1-ndl)*1.8+depthBand*0.28);
                    color=lerp(_ShallowColor.rgb,_DeepColor.rgb,smoothstep(0.32,0.55,shade));
                    float phase=(p.x+p.z*0.7)*max(_HatchScale,0.01)*TWO_PI;
                    float hatch=InkHatch(phase)*smoothstep(0.23,0.48,shade);
                    float crossHatch=InkHatch((p.x-p.z)*max(_HatchScale,0.01)*TWO_PI)*smoothstep(0.58,0.78,shade);
                    float contour=1-smoothstep(0.025,0.025+max(fwidth(input.waveInfo.x)*1.2,0.01),abs(input.waveInfo.x-0.66));
                    color=lerp(color,_ShadowColor.rgb,saturate(max(hatch,crossHatch)*_HatchStrength+contour*_OutlineStrength));
                }
                if (retro)
                {
                    // World-space blocks retain their identity as the camera moves.
                    float2 pixel=artPosition*max(_MarkScale,0.001);
                    float wave=sin(pixel.x*1.7+styleTime*_MarkSpeed*3)+sin(pixel.y*2.3-styleTime*_MarkSpeed*2);
                    float dither=(Bayer4(floor(artPosition/max(_PixelSize,0.01)))-0.5)*_DitherStrength;
                    float band=saturate(0.48+wave*0.2+dither*0.23);
                    band=floor(band*3+0.5)/3;
                    color=lerp(_DeepColor.rgb,_ShallowColor.rgb,band);
                    float dash=step(0.88,WashNoise(float2(pixel.x*0.5,pixel.y*2)+float2(styleTime*_MarkSpeed,0)));
                    color=lerp(color,_AccentColor.rgb,dash*_MarkStrength);
                }

                if (paperCut)
                {
                    float along=p.x*0.94-p.z*0.34;
                    float across=p.x*0.34+p.z*0.94;
                    float bandCoord=across*max(_LayerScale,0.001)-_AW_Time*_MarkSpeed;
                    bandCoord+=sin(along*0.17+_AW_Time*0.13)*0.28+sin(along*0.071-across*0.09)*0.18;
                    bandCoord+=cos(along*max(_ScallopScale,0.01)*TWO_PI)*_ScallopSize;
                    float bandIndex=floor(bandCoord), bandFraction=frac(bandCoord);
                    float edgeAA=max(fwidth(bandCoord),0.001);
                    float currentTone=(sin(bandIndex*1.73)*0.5+0.5)*0.8+0.1;
                    float previousTone=(sin((bandIndex-1)*1.73)*0.5+0.5)*0.8+0.1;
                    float paletteTone=lerp(previousTone,currentTone,smoothstep(0,edgeAA,bandFraction));
                    color=lerp(_DeepColor.rgb,_ShallowColor.rgb,paletteTone);
                    float shadow=exp(-bandFraction/max(_LayerShadowWidth,0.005))*_LayerShadow;
                    color=lerp(color,_ShadowColor.rgb,shadow);
                    float grain=WashNoise(p.xz*max(_PaperScale,0.01))-0.5;
                    float grainFade=1-smoothstep(0.2,1.0,max(length(ddx(p.xz)),length(ddy(p.xz)))*max(_PaperScale,0.01));
                    color*=1+grain*_PaperStrength*grainFade*0.2;
                    #if defined(_FOAM)
                        float rim=smoothstep(1-_PaperEdgeWidth-edgeAA,1-_PaperEdgeWidth+edgeAA,bandFraction);
                        rim*=1-smoothstep(0.04,0.2,edgeAA);
                        color=lerp(color,_FoamColor.rgb,rim);
                    #endif
                }
                if (bioluminescent)
                {
                    float shade=floor(saturate(input.waveInfo.x)*3)/3;
                    color=lerp(_DeepColor.rgb,_ShallowColor.rgb,saturate((1-depthBand)*0.45+shade*0.18));
                    color+=_AccentColor.rgb*fresnel*_ReflectionStrength*0.1;
                }

                // Independent world-space graphic foam. It is deliberately unrelated to crest height.
                if (_SurfaceFoamStrength > 0.001)
                {
                    float2 warp=(float2(noise,SAMPLE_TEXTURE2D(_NoiseMap,sampler_NoiseMap,p.xz*_BrushScale*0.73-motion*0.02).r)-0.5)*_SurfaceFoamDistortion;
                    float2 surfaceFoamUV=p.xz*_SurfaceFoamScale+motion*0.018+warp;
                    float2 alternateUV=float2(p.x*0.819-p.z*0.574,p.x*0.574+p.z*0.819)*_SurfaceFoamScale*1.61-motion*0.011-warp*0.6;
                    float sampleA=SAMPLE_TEXTURE2D(_SurfaceFoamMap,sampler_SurfaceFoamMap,surfaceFoamUV).r;
                    float sampleB=SAMPLE_TEXTURE2D(_SurfaceFoamMap,sampler_SurfaceFoamMap,alternateUV).r;
                    float surfaceSample=lerp(sampleA,sampleB,_SurfaceFoamVariation*0.55);
                    float shadowA=SAMPLE_TEXTURE2D(_SurfaceFoamMap,sampler_SurfaceFoamMap,surfaceFoamUV+float2(0.014,-0.011)).r;
                    float shadowB=SAMPLE_TEXTURE2D(_SurfaceFoamMap,sampler_SurfaceFoamMap,alternateUV+float2(0.011,-0.008)).r;
                    float shadowSample=lerp(shadowA,shadowB,_SurfaceFoamVariation*0.55);
                    float edge=max(fwidth(surfaceSample)*1.5,0.012);
                    float surfaceLine=smoothstep(_SurfaceFoamCutoff-edge,_SurfaceFoamCutoff+edge,surfaceSample);
                    float shadowLine=smoothstep(_SurfaceFoamCutoff-edge,_SurfaceFoamCutoff+edge,shadowSample)*(1-surfaceLine);
                    color=lerp(color,_SurfaceFoamShadowColor.rgb,shadowLine*_SurfaceFoamStrength);
                    color=lerp(color,_SurfaceFoamColor.rgb,surfaceLine*_SurfaceFoamStrength);
                }

                float foam=0;
                #if defined(_FOAM)
                    float foamMask=SAMPLE_TEXTURE2D(_FoamMap,sampler_FoamMap,artPosition*_FoamScale-motion*0.16+noise*(retro?0:0.025)).r;
                    float shoreMask=SAMPLE_TEXTURE2D(_ShoreFoamMap,sampler_ShoreFoamMap,artPosition*_FoamScale*0.7+noise*(retro?0:0.04)).r;
                    float crestThreshold=lerp(0.88,0.45,_FoamCoverage);
                    float highCrest=smoothstep(crestThreshold,crestThreshold+0.1,input.waveInfo.x);
                    float breaking=smoothstep(0.28,0.58,input.waveInfo.y);
                    // Large-scale gating prevents a white stripe from appearing on every periodic wave ridge.
                    float crestRegion=SAMPLE_TEXTURE2D(_NoiseMap,sampler_NoiseMap,p.xz*0.027+float2(_AW_Time*0.006,-_AW_Time*0.004)).r;
                    float crest=highCrest*lerp(0.2,1,breaking)*smoothstep(0.48,0.68,crestRegion)*_CrestFoamStrength;
                    float graphicMask=smoothstep(_FoamCutoff,min(_FoamCutoff+0.16,1),foamMask);
                    float submerged=max(0,p.y-scene.y);
                    float groundOnly=smoothstep(0.015,0.08,submerged)*exp(-submerged*1.8);
                    float shore=saturate(1-submerged/max(_ShoreFoamWidth,0.01))*groundOnly*_ShoreFoamStrength;
                    float shoreRange=saturate(1-submerged/max(_ShoreFoamWidth*3,0.01))*groundOnly;
                    float shorePhase=submerged/max(_ShoreWaveSpacing,0.02)*TWO_PI-_AW_Time*_ShoreWaveSpeed*TWO_PI;
                    float shoreBreak=pow(saturate(sin(shorePhase)*0.5+0.5),7)*shoreRange*_ShoreWaveStrength;
                    float interaction=ripple.z*_InteractionFoamStrength*graphicMask;
                    foam=max(max(crest*graphicMask,smoothstep(0.18,0.7,shore*(0.35+shoreMask*0.8))),max(shoreBreak,interaction));
                    if ((_StyleMode>2.5 && _StyleMode<3.5) || manga || retro) foam=step(0.42,foam);
                    // Keep the wake's lifetime fade even in the hard-edged ink style.
                    foam=max(foam,wake.z*IS_FRONT_VFACE(face,1.0,0.18));
                    if (!bioluminescent) color=lerp(color,_FoamColor.rgb,foam);
                #endif

                if (_StyleMode>4.5 && (_StyleMode<5.5 || (_StyleMode>6.5 && _StyleMode<7.5)))
                {
                    float marks=Illustration(p.xz,_StyleMode>6.5)*_MarkStrength;
                    if (_StyleMode<5.5) marks*=smoothstep(0.4,0.78,input.waveInfo.x);
                    color=lerp(color,_FoamColor.rgb,marks);
                }
                if (_StyleMode>3.5 && _StyleMode<4.5)
                {
                    float glow=saturate(fresnel*0.7+foam+waveLine*0.45);
                    color+=_AccentColor.rgb*glow*_BrushStrength*1.8;
                }
                if (bioluminescent)
                {
                    // Reuse live ripple and boat-wake envelopes, including their lifetime fades.
                    // Emission is independent of the foam toggle and sun illumination.
                    float activity=saturate(max(wake.z,ripple.z));
                    float flecks=Plankton(p.xz);
                    float churn=0.65+0.35*WashNoise(p.xz*3+motion*0.2);
                    float emission=flecks*_BioGlowStrength*(1+activity*2)
                        +activity*churn*_BioTrailStrength;
                    color+=_BioGlowColor.rgb*emission*IS_FRONT_VFACE(face,1.0,0.4);
                }
                color=MixFog(color,input.waveInfo.z);
                return half4(color,saturate(gap/max(_EdgeFade,0.01)));
            }
            ENDHLSL
        }
    }
    CustomEditor "AdvancedWater.Editor.StylizedWaterShaderGUI"
    FallBack Off
}
