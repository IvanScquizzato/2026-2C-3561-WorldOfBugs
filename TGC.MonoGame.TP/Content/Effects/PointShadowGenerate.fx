#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float4x4 World;
float4x4 LightViewProjection;
float3 LightOrigin;
float InverseLightRadius;

texture ModelTexture;
sampler2D TextureSampler = sampler_state
{
    Texture = <ModelTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

struct VSInput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
    float3 FromLight : TEXCOORD1;
};

VSOutput ShadowVS(VSInput input)
{
    VSOutput output;
    float4 worldPosition = mul(input.Position, World);
    output.Position = mul(worldPosition, LightViewProjection);
    #if OPENGL
    // Undo MonoGame's render-target Y flip to match the cube sampler's axes.
    output.Position.y = -output.Position.y;
    #endif
    output.TexCoord = input.TexCoord;
    output.FromLight = worldPosition.xyz - LightOrigin;
    return output;
}

float4 EncodePointDepth(float3 fromLight)
{
    // Two bytes keep the distance precise on a Reach Color render target.
    float scaledDepth = saturate(length(fromLight) * InverseLightRadius) * 255.0f;
    return float4(floor(scaledDepth) / 255.0f, frac(scaledDepth), 0.0f, 1.0f);
}

float4 OpaquePS(VSOutput input) : COLOR0
{
    return EncodePointDepth(input.FromLight);
}

float4 CutoutPS(VSOutput input) : COLOR0
{
    clip(tex2D(TextureSampler, input.TexCoord).a - 0.1f);
    return EncodePointDepth(input.FromLight);
}

technique Opaque
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL ShadowVS();
        PixelShader = compile PS_SHADERMODEL OpaquePS();
    }
}

technique Cutout
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL ShadowVS();
        PixelShader = compile PS_SHADERMODEL CutoutPS();
    }
}
