// Shared by model, terrain and basic shaders in the camera pass.
float4x4 LightViewProjection;
float2 ShadowTexelSize;
float ShadowBias;
float ShadowsEnabled;

texture ShadowMapTexture;
sampler2D ShadowSampler = sampler_state
{
    Texture = <ShadowMapTexture>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

float StoredShadowDepth(float2 uv)
{
    float4 packed = tex2D(ShadowSampler, uv);
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
