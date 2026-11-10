#include "PBR.fx"
#include "ShadowSampling.fx"
#ifdef OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// Custom Effects - https://docs.monogame.net/articles/content/custom_effects.html
// High-level shader language (HLSL) - https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl
// Programming guide for HLSL - https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-pguide
// Reference for HLSL - https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-reference
// HLSL Semantics - https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-semantics

float4x4 World;
float4x4 View;
float4x4 Projection;
float TrackOffset = 0;

float Time = 0;

Texture2D NormalMap:  register(t1);
sampler2D NormalSampler: register(s1) = sampler_state
{
    Texture = (NormalMap);
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};



texture ModelTexture:register(t0);
sampler2D TextureSampler: register(s0) = sampler_state
{
	Texture = (ModelTexture);
	MagFilter = Linear;
	MinFilter = Linear;
	MipFilter = Linear;
	AddressU = Wrap;
	AddressV = Wrap;
};

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float2 TexCoord : TEXCOORD0;
    float3 Normal : NORMAL;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float2 TexCoord : TEXCOORD0;
    float3 Normal : TEXCOORD1;
    float3 WorldPosition : TEXCOORD2;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
    // Clear the output
	VertexShaderOutput output = (VertexShaderOutput)0;
    // Model space to World space
    float4 worldPosition = mul(input.Position, World);
    // World space to View space
    float4 viewPosition = mul(worldPosition, View);	
	// View space to Projection space
    output.Position = mul(viewPosition, Projection);
	output.TexCoord = input.TexCoord;
    output.TexCoord.y += TrackOffset;

    output.Normal = normalize(mul(input.Normal, (float3x3)World));
    output.WorldPosition = worldPosition.xyz;
    return output;
}

float4 NormalMapPS(VertexShaderOutput input) : COLOR
{
    float4 albedo = tex2D(TextureSampler, input.TexCoord);
    clip(albedo.a - 0.1f);

    float3 omega0 = normalize(CameraPosition - input.WorldPosition);
    float3 omegai = normalize(-SunlightDirection);

    float3 tangentNormal = tex2D(NormalSampler, input.TexCoord).xyz * 2.0f - 1.0f;

    float3 N = normalize(input.Normal);

    float3 Q1  = ddx(input.WorldPosition);
    float3 Q2  = ddy(input.WorldPosition);
    float2 st1 = ddx(input.TexCoord);
    float2 st2 = ddy(input.TexCoord);

    float3 T  = normalize(Q1 * st2.y - Q2 * st1.y);
    float3 B  = normalize(cross(N, T));
    float3x3 TBN = float3x3(T, B, N);

    float3 finalNormal = normalize(mul(tangentNormal, TBN));

    float shadow = ShadowVisibility(input.WorldPosition, saturate(dot(finalNormal, omegai)));
    float3 hdrColor = BDRF(albedo.xyz, omega0, omegai, finalNormal, shadow);

    float a = 2.51f, b = 0.03f, c = 2.43f, d = 0.59f, e = 0.14f;
    float3 mappedColor = saturate((hdrColor*(a*hdrColor+b))/(hdrColor*(c*hdrColor+d)+e));
    
    return float4(mappedColor, 1.0f);
}
float4 InterpolatedNormalPS(VertexShaderOutput input) : COLOR{
    float4 albedo = tex2D(TextureSampler, input.TexCoord);
    clip(albedo.a - 0.1f);

    
    float3 omega0 = normalize(CameraPosition - input.WorldPosition + float3(0.0001f, 0.0f, 0.0f));
    float3 omegai = normalize(-SunlightDirection + float3(0.0001f, 0.0f, 0.0f));

    float3 normal = normalize(input.Normal + float3(0.0001f, 0.0f, 0.0f));

    float shadow = ShadowVisibility(input.WorldPosition, saturate(dot(normal, omegai)));
    float3 hdrColor = BDRF(albedo.xyz, omega0, omegai, normal, shadow);

    float a = 2.51f, b = 0.03f, c = 2.43f, d = 0.59f, e = 0.14f;
    float3 mappedColor = saturate((hdrColor*(a*hdrColor+b))/(hdrColor*(c*hdrColor+d)+e));
    return float4(mappedColor, 1.0f);
    //return float4(0.0f,0.0f,0.0f, 1.0f);
    /*float3 normal = normalize(input.Normal);

    return float4(normal * 0.5f + 0.5f, 1.0f);*/
}

technique WithNormalMap{
    pass P0{
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL NormalMapPS();
    }
}
technique WithoutNormalMap{
    pass P0{
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL InterpolatedNormalPS();
    }
}
