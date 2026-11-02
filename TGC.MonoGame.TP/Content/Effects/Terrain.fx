#include "PBR.fx"
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

    return output;
}

struct PS_INPUT
{
    float2 Texcoord : TEXCOORD0;
    float3 WorldPos : TEXCOORD1;
    float3 WorldNormal : TEXCOORD2;
};

//Pixel Shader
float4 ps_RenderTerrain(PS_INPUT input) : COLOR0
{
    float3 blendMap = tex2D(colorMap, input.Texcoord).rgb;
    float3 tex1 = tex2D(diffuseMap, input.Texcoord * 31).rgb;
    float3 tex2 = tex2D(diffuseMap2, input.Texcoord * 27).rgb;
    float3 albedo = lerp(lerp(tex1, tex2, blendMap.r), blendMap, 0.3);

    float3 N = normalize(input.WorldNormal);
    float3 omega0 = normalize(CameraPosition - input.WorldPos);
    float3 omegai = normalize(-SunlightDirection);

    float3 hdrColor = BDRF(albedo, omega0, omegai, N);

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
