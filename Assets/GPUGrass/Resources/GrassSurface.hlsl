#ifndef GRASS_SURFACE_INCLUDED
#define GRASS_SURFACE_INCLUDED
float3 SurfaceRotate(float3 value, float3 up)
{
    float3 axis=cross(float3(0,1,0),up);
    return value+cross(axis,value)+cross(axis,cross(axis,value))/max(.001,1+up.y);
}
#endif
