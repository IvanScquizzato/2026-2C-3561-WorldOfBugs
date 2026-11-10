#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define SV_POSITION SV_POSITION
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// First pass of shadow mapping: store distance from the sun in a render target.
float4x4 WorldLightViewProjection;

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
    float Depth : TEXCOORD1;
};

VSOutput ShadowVS(VSInput input)
{
    VSOutput output;
    float4 clipPosition = mul(input.Position, WorldLightViewProjection);
    output.Position = clipPosition;
    output.TexCoord = input.TexCoord;
    output.Depth = clipPosition.z / clipPosition.w;
    return output;
}

float4 EncodeDepth(float depth)
{
    float value = saturate(depth);
    // Color is an 8-bit target. RGB shows the coarse depth in the debug view;
    // alpha stores the fractional byte for the final shadow comparison.
    float scaled = value * 255.0f;
    float highByte = floor(scaled) / 255.0f;
    float lowByte = frac(scaled);
    return float4(highByte, highByte, highByte, lowByte);
}

float4 OpaquePS(VSOutput input) : COLOR0
{
    return EncodeDepth(input.Depth);
}

float4 CutoutPS(VSOutput input) : COLOR0
{
    clip(tex2D(TextureSampler, input.TexCoord).a - 0.1f);
    return EncodeDepth(input.Depth);
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
