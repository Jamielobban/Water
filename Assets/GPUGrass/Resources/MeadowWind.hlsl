#ifndef GPU_MEADOW_WIND_INCLUDED
#define GPU_MEADOW_WIND_INCLUDED
TEXTURE2D(_WindFlowMap);
SAMPLER(sampler_WindFlowMap);
float4 _WindFlowSettings; // Map influence, field size, procedural variation, pattern size.
float4 _WindBaseDirection;
// Shared world-space flow keeps grass and flowers in the same local wind field.
float3 MeadowWind(float2 position, float seed, float time)
{
    float2 q=position/max(1,_WindFlowSettings.w);
    float rotation=(sin(q.x*2.1+sin(q.y*1.3))+sin(q.y*1.7-q.x*.6))*.9*_WindFlowSettings.z;
    float s=sin(rotation), c=cos(rotation);
    float2 baseDirection=_WindBaseDirection.xy;
    float2 direction=float2(baseDirection.x*c-baseDirection.y*s,baseDirection.x*s+baseDirection.y*c);
    float localStrength=1;
    if (_WindFlowSettings.x>0)
    {
        float2 uv=saturate(position/max(10,_WindFlowSettings.y)+.5);
        float3 flow=SAMPLE_TEXTURE2D_LOD(_WindFlowMap,sampler_WindFlowMap,uv,0).rgb;
        float2 flowDirection=flow.rg*2-1;
        float magnitude=length(flowDirection);
        float2 mapped=magnitude>.01 ? flowDirection/magnitude : direction;
        // Interpolate angles so opposite directions don't cancel into an abrupt flip.
        float turn=atan2(direction.x*mapped.y-direction.y*mapped.x,dot(direction,mapped));
        float a=turn*_WindFlowSettings.x;
        direction=float2(direction.x*cos(a)-direction.y*sin(a),direction.x*sin(a)+direction.y*cos(a));
        localStrength=lerp(1,flow.b*2*saturate(magnitude*100),_WindFlowSettings.x);
    }
    float wave=sin(dot(position,float2(.065,.042))-time*.72);
    float crossing=sin(dot(position,float2(-.032,.09))-time*.47);
    float gust=smoothstep(-.45,.85,wave*.7+crossing*.3);
    float flutter=sin(time*2.1+seed*6.283185+position.x*.6)*.025;
    float2 offset=direction*(.045+gust*.28+flutter)
        +float2(-direction.y,direction.x)*sin(time*.8+position.y*.08)*.035;
    return float3(offset.x,0,offset.y)*localStrength;
}
#endif
