// Shared by model, terrain and basic shaders in the camera pass.
float4x4 CloseLightViewProjection;
float4x4 FarLightViewProjection;
float2 ShadowCloseTexelSize;
float2 ShadowFarTexelSize;
float CloseShadowAreaSize;
float ShadowBias;
float ShadowsEnabled;
float3 LightDirection;
float3 TankPosition;
static bool IsCloseShadow;

texture CloseShadowMapTexture;
sampler2D CloseShadowSampler = sampler_state
{
    Texture = <CloseShadowMapTexture>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};
texture FarShadowMapTexture;

sampler2D FarShadowSampler = sampler_state
{
    Texture = <FarShadowMapTexture>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};
float4 SampleShadowMap(float2 uv)
{

    if(IsCloseShadow)
        return tex2D(CloseShadowSampler, uv);
    else
        return tex2D(FarShadowSampler, uv);
}
float StoredShadowDepth(float2 uv)
{
    float4 packed = SampleShadowMap(uv);
    return packed.r + packed.a / 255.0f;
}

float CompareShadowDepth(float2 uv, float receiverDepth)
{
    return receiverDepth <= StoredShadowDepth(uv) ? 1.0f : 0.0f;
}

float ShadowVisibility(float3 worldPosition, float normalDotLight)
{
    if (ShadowsEnabled < 0.5f)
        return 1.0f;
    float4 clipPos = mul(float4(worldPosition, 1.0f), CloseLightViewProjection);
    float3 ndc = clipPos.xyz / clipPos.w;
    IsCloseShadow = all(abs(ndc.xy) <= 1.0f) && (ndc.z >= 0.0f && ndc.z <= 1.0f);
    float4x4 LightViewProjection = IsCloseShadow ? CloseLightViewProjection : FarLightViewProjection;
    float ShadowTexelSize = IsCloseShadow ? ShadowCloseTexelSize : ShadowFarTexelSize;
    
    float4 lightClip = mul(float4(worldPosition, 1.0f), LightViewProjection);
    float3 lightNdc = lightClip.xyz / lightClip.w;
    float2 uv = float2(
        lightNdc.x * 0.5f + 0.5f,
        0.5f - lightNdc.y * 0.5f);

    // The shadow map only covers the area around the player.
    if (uv.x <= 0.0f || uv.x >= 1.0f ||
        uv.y <= 0.0f || uv.y >= 1.0f ||
        lightNdc.z <= 0.0f || lightNdc.z >= 1.0f)
        return 1.0f;

    float bias = ShadowBias * (1.25f - saturate(normalDotLight));
    float receiverDepth = lightNdc.z - bias;
    float2 halfTexel = ShadowTexelSize * 0.5f;

    float visibility = 0.0f;
    visibility += CompareShadowDepth(uv + float2(-halfTexel.x, -halfTexel.y), receiverDepth);
    visibility += CompareShadowDepth(uv + float2( halfTexel.x, -halfTexel.y), receiverDepth);
    visibility += CompareShadowDepth(uv + float2(-halfTexel.x,  halfTexel.y), receiverDepth);
    visibility += CompareShadowDepth(uv + float2( halfTexel.x,  halfTexel.y), receiverDepth);
    return visibility * 0.25f;
}
