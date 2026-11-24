TextureCube PointLightShadowMap0;
TextureCube PointLightShadowMap1;
TextureCube PointLightShadowMap2;
TextureCube PointLightShadowMap3;
float PointShadowTexelSize;

#define POINT_SHADOW_SAMPLER(name, source) \
samplerCUBE name = sampler_state \
{ \
    Texture = <source>; \
    MinFilter = Point; \
    MagFilter = Point; \
    MipFilter = None; \
    AddressU = Clamp; \
    AddressV = Clamp; \
    AddressW = Clamp; \
};

POINT_SHADOW_SAMPLER(PointShadowSampler0, PointLightShadowMap0)
POINT_SHADOW_SAMPLER(PointShadowSampler1, PointLightShadowMap1)
POINT_SHADOW_SAMPLER(PointShadowSampler2, PointLightShadowMap2)
POINT_SHADOW_SAMPLER(PointShadowSampler3, PointLightShadowMap3)

float ReadPointShadowDepth(int lightIndex, float3 direction)
{
    float2 packed;
    if (lightIndex == 0)
        packed = texCUBE(PointShadowSampler0, direction).rg;
    else if (lightIndex == 1)
        packed = texCUBE(PointShadowSampler1, direction).rg;
    else if (lightIndex == 2)
        packed = texCUBE(PointShadowSampler2, direction).rg;
    else
        packed = texCUBE(PointShadowSampler3, direction).rg;

    return saturate(packed.r + packed.g / 255.0f);
}

float PointShadowVisibility(int lightIndex, float3 fromLight, float distanceToLight, float radius, float facing)
{
    float slope = 0.5f + 1.5f * (1.0f - facing);
    float bias = 1.5f + 2.0f * distanceToLight * PointShadowTexelSize * slope;
    float blockerDistance = ReadPointShadowDepth(lightIndex, fromLight) * radius;
    return step(distanceToLight - bias, blockerDistance);
}
