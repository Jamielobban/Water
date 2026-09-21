#ifndef ADVANCED_WATER_WAVES_INCLUDED
#define ADVANCED_WATER_WAVES_INCLUDED
float4 _AW_Waves[12];
float _AW_Time, _AW_Steepness, _AW_WaveSpeed, _AW_Level, _AW_Quality;
float _AW_ShapeVariation, _AW_ShapeScale, _AW_CrestSharpness;
float4 _AW_Ripples[32];
float4 _AW_RippleData[32];
int _AW_RippleCount;
float _AW_RippleStrength;
float4 _AW_WakeStarts[48], _AW_WakeEnds[48], _AW_WakeData[48];
int _AW_WakeCount;

float AWWakeHash(float2 cell)
{
    float3 h=frac(float3(cell.x,cell.y,cell.x)*0.1031);
    h+=dot(h,h.yzx+33.33);
    return frac((h.x+h.y)*h.z);
}
float AWWakeNoise(float2 p)
{
    float2 cell=floor(p), f=frac(p);
    f=f*f*(3-2*f);
    return lerp(lerp(AWWakeHash(cell),AWWakeHash(cell+float2(1,0)),f.x),
        lerp(AWWakeHash(cell+float2(0,1)),AWWakeHash(cell+float2(1,1)),f.x),f.y);
}

// XY is a normal slope; Z is independently controlled boat foam coverage.
float3 AWWake(float2 p)
{
    float3 result=0;
    [loop] for (int i=0;i<_AW_WakeCount;i++)
    {
        float4 start=_AW_WakeStarts[i], end=_AW_WakeEnds[i], data=_AW_WakeData[i];
        if (data.y<=0 || _AW_Time-end.z>=data.w) continue;
        float2 travel=end.xy-start.xy;
        float segmentLength=max(length(travel),0.001);
        float2 forward=travel/segmentLength;
        float2 side=float2(-forward.y,forward.x);
        float along=dot(p-start.xy,forward);
        float t=saturate(along/segmentLength);
        float age=max(0,_AW_Time-lerp(start.z,end.z,t));
        float life=1-smoothstep(0,data.w,age);
        float endDistance=max(-along,along-segmentLength);
        float join=1-smoothstep(0,0.7,max(0,endDistance));
        float lateral=dot(p-lerp(start.xy,end.xy,t),side);
        // A short-lived bow shoulder ahead of each fresh stern segment. It
        // disappears smoothly as the hull moves on, leaving the stern trail.
        float bowAlong=along-segmentLength-data.x*3;
        float bowAge=max(0,_AW_Time-end.z);
        float bowLife=(1-smoothstep(0.15,0.75,bowAge))*data.y;
        float bowSide=(abs(lateral)-data.x)/0.25;
        float bowShape=exp(-bowAlong*bowAlong/0.5-bowSide*bowSide);
        result.z=max(result.z,bowShape*bowLife*0.65);
        float2 bowSlope=side*sign(lateral)*bowShape*bowLife*0.35;
        if (dot(bowSlope,bowSlope)>dot(result.xy,result.xy)) result.xy=bowSlope;
        if (join<=0 || life<=0) continue;
        float armDistance=data.x+age*data.z;
        float armWidth=0.18+age*0.045;
        float arm=(abs(lateral)-armDistance)/armWidth;
        float armEnvelope=exp(-arm*arm);
        float strength=data.y*life*join*smoothstep(0,0.18,age);
        float slope=-2*arm*armEnvelope*strength*0.65;
        float2 wakeSlope=side*sign(lateral)*slope;
        // Max blending avoids bright seams where adjacent path segments overlap.
        if (dot(wakeSlope,wakeSlope)>dot(result.xy,result.xy)) result.xy=wakeSlope;
        float churnWidth=data.x+age*data.z*0.28;
        float sternDistance=lateral/max(churnWidth,0.1);
        float stern=exp(-sternDistance*sternDistance*1.5);
        float churnFade=1-smoothstep(data.w*0.25,data.w,age);
        result.z=max(result.z,(stern*churnFade*0.85+armEnvelope*0.55)*strength);

    }
    // Slowly drifting, non-tiled breakup is shared across segment boundaries.
    if (result.z>0)
    {
        float coarse=AWWakeNoise(p*1.7+float2(_AW_Time*0.08,-_AW_Time*0.05));
        float fine=AWWakeNoise(p*5.3-float2(_AW_Time*0.11,0));
        result.z*=smoothstep(0.22,0.72,coarse*0.65+fine*0.35);
    }
    return result;
}

void AWDisplace(float2 p, out float3 displacement, out float3 tangent, out float3 binormal)
{
    displacement = 0; tangent = float3(1,0,0); binormal = float3(0,0,1);
    [unroll] for (int i = 0; i < 12; i++)
    {
        float4 w = _AW_Waves[i];
        if (w.z <= 0) continue;
        float k = TWO_PI / max(w.w, 0.1);
        float2 direction=w.xy;
        float2 perpendicular=float2(-direction.y,direction.x);
        float2 diagonal=normalize(float2(direction.x-direction.y,direction.x+direction.y));
        float2 modulationDirection=normalize(float2(direction.x+direction.y,direction.y-direction.x));
        float frequency=max(0.01,_AW_ShapeScale);
        float seed=i*2.173;
        float groupTime=_AW_Time*_AW_WaveSpeed;
        float phaseA=dot(perpendicular,p)*frequency+seed+groupTime*(0.11+i*0.017);
        float phaseB=dot(diagonal,p)*frequency*0.43+seed*1.91+1.7-groupTime*(0.073+i*0.013);
        float amplitudePhase=dot(modulationDirection,p)*frequency*0.29+seed*2.37+0.8-groupTime*(0.16+i*0.021);
        float phase=k*dot(direction,p)-sqrt(9.81*k)*_AW_Time*_AW_WaveSpeed
            +(i >= 4 ? seed : 0)+_AW_ShapeVariation*(sin(phaseA)*0.85+sin(phaseB)*0.5);
        float2 phaseGradient=k*direction+_AW_ShapeVariation*(cos(phaseA)*0.85*frequency*perpendicular
            +cos(phaseB)*0.5*frequency*0.43*diagonal);
        float amplitudeFactor=1+_AW_ShapeVariation*(0.35*sin(amplitudePhase)+0.15*sin(phaseB));
        float2 amplitudeGradient=_AW_ShapeVariation*(0.35*cos(amplitudePhase)*frequency*0.29*modulationDirection
            +0.15*cos(phaseB)*frequency*0.43*diagonal);
        float hBase=min(_AW_Steepness*w.z,0.16/k);
        float h=hBase*amplitudeFactor;
        float s, c; sincos(phase, s, c);
        float sharpness=_AW_CrestSharpness*0.18;
        float shape=(s-sharpness*cos(phase*2))/(1+sharpness);
        float shapeDerivative=(c+sharpness*2*sin(phase*2))/(1+sharpness);
        float verticalAmplitude=w.z*amplitudeFactor;
        displacement+=float3(h*direction.x*c,verticalAmplitude*shape,h*direction.y*c);
        float2 horizontalX=direction*(hBase*amplitudeGradient.x*c-h*s*phaseGradient.x);
        float2 horizontalZ=direction*(hBase*amplitudeGradient.y*c-h*s*phaseGradient.y);
        float verticalX=w.z*(amplitudeGradient.x*shape+amplitudeFactor*shapeDerivative*phaseGradient.x);
        float verticalZ=w.z*(amplitudeGradient.y*shape+amplitudeFactor*shapeDerivative*phaseGradient.y);
        tangent+=float3(horizontalX.x,verticalX,horizontalX.y);
        binormal+=float3(horizontalZ.x,verticalZ,horizontalZ.y);
    }
}

float AWSurfaceHeight(float2 xz)
{
    float2 p = xz;
    float3 d, t, b;
    [unroll] for (int i=0; i<4; i++) { AWDisplace(p,d,t,b); p = xz - d.xz; }
    AWDisplace(p,d,t,b);
    return _AW_Level + d.y;
}

float3 AWRipple(float2 p)
{
    float3 result = 0; // XY = normal slope, Z = optional interaction-foam envelope.
    [loop] for (int i=0; i<_AW_RippleCount; i++)
    {
        float4 r = _AW_Ripples[i], data = _AW_RippleData[i];
        float age = _AW_Time - r.z;
        if (r.w <= 0 || age < 0 || age > data.z) continue;
        float2 delta = p - r.xy;
        float distanceToCenter = length(delta);
        // Smoothly reach zero with a flat endpoint before the CPU frees the slot.
        float life = 1-smoothstep(0,1,saturate(age/data.z));
        float width = max(data.y*0.7,0.08);
        float u = (distanceToCenter - age * data.x) / width;
        // A crisp leading wave and a weaker trailing wave, with a smooth birth.
        // Differentiate the height packet to keep the normal rings coherent.
        float birth=smoothstep(0,0.08,age);
        float envelope = exp(-u*u*0.85);
        float carrier=4.5*u;
        float slope=(4.5*cos(carrier)-1.7*u*sin(carrier))*envelope;
        float tail=u+2.6;
        slope+=0.3*(4.5*cos(tail*4.5)-1.7*tail*sin(tail*4.5))*exp(-tail*tail*0.85);
        slope*=life*birth*r.w/width*0.38;
        // Suppress the undefined radial direction at the exact impact center.
        slope*=smoothstep(0,width*0.25,distanceToCenter);
        result.xy += delta / max(distanceToCenter,0.001) * slope;
        result.z += envelope*life*birth*saturate(abs(cos(u*1.8))-0.35)*abs(r.w);
    }
    result *= _AW_RippleStrength;
    // Dense wakes can overlap many packets; limit the accumulated tilt smoothly.
    result.xy /= sqrt(1+dot(result.xy,result.xy)/9);
    return result;
}
#endif
