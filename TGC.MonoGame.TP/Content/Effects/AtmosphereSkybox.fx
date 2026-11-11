#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_3
#define PS_SHADERMODEL ps_4_0_level_9_3
#endif

matrix SkyView;
matrix InverseSkyView;
matrix Projection;
float2 ProjectionScale;

float3 SunDirection;
float3 SunColor;
float SunIntensity;

float3 SkyZenithColor;
float3 SkyHorizonColor;
float3 SunsetColor;
float3 GroundColor;

float NightBlend;

TextureCube StarCubeMap;
samplerCUBE StarSampler = sampler_state
{
    Texture = <StarCubeMap>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
    AddressW = Clamp;
};

struct VSInput
{
    float4 Position : POSITION0;
};

struct VSOutput
{
    float4 Position : SV_POSITION;
    float3 ClipXYW : TEXCOORD0;
};

VSOutput SkyVS(VSInput input)
{
    VSOutput output;
    float4 clipPos = mul(mul(input.Position, SkyView), Projection);
    output.ClipXYW = clipPos.xyw;
    output.Position = clipPos.xyww;
    return output;
}

float HenyeyGreenstein(float cosTheta, float g)
{
    float g2 = g * g;
    return (1.0f - g2) /
        (4.0f * 3.14159265f * pow(max(1.0f + g2 - 2.0f * g * cosTheta, 0.001f), 1.5f));
}

float4 SkyPS(VSOutput input) : COLOR0
{
    float2 pixelNdc = input.ClipXYW.xy / input.ClipXYW.z;
    float3 viewRay = normalize(float3(
        pixelNdc.x / ProjectionScale.x,
        pixelNdc.y / ProjectionScale.y,
        -1.0f));
    float3 V = normalize(mul(float4(viewRay, 0.0f), InverseSkyView).xyz);
    float3 S = normalize(SunDirection);

    float height = V.y;
    float sunAltitude = S.y;
    float sunsetFactor = saturate(1.0f - abs(sunAltitude) * 3.0f);

    float3 dayHorizon = lerp(SkyHorizonColor, SunsetColor, sunsetFactor);
    float zenithBlend = smoothstep(0.0f, 1.0f, saturate(height));
    float3 daySky = lerp(dayHorizon, SkyZenithColor, zenithBlend);
    if (height < 0.0f)
    {
        float groundBlend = smoothstep(0.0f, 1.0f, saturate(-height));
        daySky = lerp(dayHorizon, GroundColor, groundBlend);
    }

    float cosTheta = dot(V, S);
    float sunDisc = smoothstep(0.9990f, 0.9995f, cosTheta) * 15.0f;
    float mieHalo = HenyeyGreenstein(cosTheta, 0.85f) * 0.15f;
    float sunVisibility = max(sunAltitude + 0.1f, 0.0f);
    float3 sunLight = (sunDisc + mieHalo) * SunColor * SunIntensity * sunVisibility;

    float3 stars = texCUBE(StarSampler, V).rgb;
    float3 nightSky = float3(0.01f, 0.015f, 0.03f) + stars * 1.5f;
    float3 finalColor = lerp(daySky + sunLight, nightSky, saturate(NightBlend));

    return float4(finalColor, 1.0f);
}

technique AtmosphericSky
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL SkyVS();
        PixelShader = compile PS_SHADERMODEL SkyPS();
    }
}
