#include "PBR.fx"
#include "ShadowSampling.fx"
#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float4x4 World;
float4x4 View;
float4x4 Projection;
float4x4 WorldInverseTranspose;

float alphaValue = 1;
float time = 0;

//Textura para DiffuseMap
texture texDiffuseMap;
sampler2D diffuseMap = sampler_state
{
    Texture = (texDiffuseMap);
    ADDRESSU = WRAP;
    ADDRESSV = WRAP;
    MINFILTER = LINEAR;
    MAGFILTER = LINEAR;
    MIPFILTER = LINEAR;
};

texture texDiffuseMap2;
sampler2D diffuseMap2 = sampler_state
{
    Texture = (texDiffuseMap2);
    ADDRESSU = MIRROR;
    ADDRESSV = MIRROR;
    MINFILTER = LINEAR;
    MAGFILTER = LINEAR;
    MIPFILTER = LINEAR;
};

texture texGrassNormalMap;
sampler2D grassNormalMap = sampler_state
{
    Texture = (texGrassNormalMap);
    ADDRESSU = WRAP;
    ADDRESSV = WRAP;
    MINFILTER = LINEAR;
    MAGFILTER = LINEAR;
    MIPFILTER = LINEAR;
};

texture texGroundNormalMap;
sampler2D groundNormalMap = sampler_state
{
    Texture = (texGroundNormalMap);
    ADDRESSU = MIRROR;
    ADDRESSV = MIRROR;
    MINFILTER = LINEAR;
    MAGFILTER = LINEAR;
    MIPFILTER = LINEAR;
};

texture texColorMap;
sampler2D colorMap = sampler_state
{
    Texture = (texColorMap);
    ADDRESSU = WRAP;
    ADDRESSV = WRAP;
    MINFILTER = LINEAR;
    MAGFILTER = LINEAR;
    MIPFILTER = LINEAR;
};

struct VS_INPUT
{
    float4 Position : POSITION0;
    float2 Texcoord : TEXCOORD0;
    float3 Normal : NORMAL0;
};

struct VS_OUTPUT
{
    float4 Position : POSITION0;
    float2 Texcoord : TEXCOORD0;
    float3 WorldPos : TEXCOORD1;
    float3 WorldNormal : TEXCOORD2;
    float3 WorldTangent : TEXCOORD3;
};

VS_OUTPUT vs_RenderTerrain(VS_INPUT input)
{
    VS_OUTPUT output;

    //Proyectar posicion
    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

    //Enviar Texcoord directamente
    output.Texcoord = input.Texcoord;


    output.WorldPos = worldPosition.xyz;
    output.WorldNormal = mul(float4(input.Normal ,0.0), WorldInverseTranspose).xyz;
    output.WorldTangent = mul(float4(1.0f, 0.0f, 0.0f, 0.0f), World).xyz;

    return output;
}

struct PS_INPUT
{
    float2 Texcoord : TEXCOORD0;
    float3 WorldPos : TEXCOORD1;
    float3 WorldNormal : TEXCOORD2;
    float3 WorldTangent : TEXCOORD3;
};

//Pixel Shader
float4 ps_RenderTerrain(PS_INPUT input) : COLOR0
{
    float3 blendMap = tex2D(colorMap, input.Texcoord).rgb;
    float2 grassBaseUv = input.Texcoord * 31.0f;
    float2 groundBaseUv = input.Texcoord * 27.0f;
    float2 grassDetailUv = grassBaseUv * 4.0f;
    float2 groundDetailUv = groundBaseUv * 4.0f;

    // Cerca de la cámara aparece el detalle fino; lejos queda el color de escala grande.
    float cameraDistance = length(CameraPosition - input.WorldPos);
    float detailFade = 1.0f - smoothstep(600.0f, 3000.0f, cameraDistance);
    float3 grassColor = lerp(
        tex2D(diffuseMap, grassBaseUv).rgb,
        tex2D(diffuseMap, grassDetailUv).rgb,
        detailFade * 0.55f);
    float3 groundColor = lerp(
        tex2D(diffuseMap2, groundBaseUv).rgb,
        tex2D(diffuseMap2, groundDetailUv).rgb,
        detailFade * 0.55f);
    float3 albedo = lerp(lerp(grassColor, groundColor, blendMap.r), blendMap, 0.12f);

    float3 baseNormal = normalize(input.WorldNormal);
    float3 grassNormal = tex2D(grassNormalMap, grassDetailUv).xyz * 2.0f - 1.0f;
    float3 groundNormal = tex2D(groundNormalMap, groundDetailUv).xyz * 2.0f - 1.0f;

    // La segunda textura usa MIRROR: invertir X/Y en los mosaicos reflejados.
    float2 mirrorParity = step(0.5f, frac(groundDetailUv * 0.5f));
    groundNormal.xy *= 1.0f - 2.0f * mirrorParity;

    float3 detailNormal = normalize(lerp(grassNormal, groundNormal, blendMap.r));
    detailNormal.xy *= 0.7f * detailFade;
    detailNormal = normalize(detailNormal);

    // El terreno está parametrizado en X/Z; convertir la normal de textura al mundo.
    float3 tangent = normalize(input.WorldTangent);
    tangent = normalize(tangent - baseNormal * dot(tangent, baseNormal));
    float3 bitangent = normalize(cross(tangent, baseNormal));
    float3 litNormal = normalize(
        tangent * detailNormal.x + bitangent * detailNormal.y + baseNormal * detailNormal.z);

    float3 omega0 = normalize(CameraPosition - input.WorldPos);
    float3 omegai = normalize(-SunlightDirection);

    float shadow = ShadowVisibility(input.WorldPos, saturate(dot(baseNormal, omegai)));
    float3 hdrColor = BDRF(albedo, omega0, omegai, litNormal, shadow);

    float a = 2.51f, b = 0.03f, c = 2.43f, d = 0.59f, e = 0.14f;
    float3 mappedColor = saturate((hdrColor*(a*hdrColor+b))/(hdrColor*(c*hdrColor+d)+e));

    return float4(mappedColor, 1.0f);
}

technique RenderTerrain
{
    pass Pass_0
    {
        VertexShader = compile VS_SHADERMODEL vs_RenderTerrain();
        PixelShader = compile PS_SHADERMODEL ps_RenderTerrain();
    }
}
