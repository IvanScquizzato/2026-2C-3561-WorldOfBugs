#ifdef OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float4x4 MatrixTransform;
float2 BlurStep;
float UseEmissionMask;
float BloomStrength;

texture SourceTexture : register(t0);
sampler2D SourceSampler : register(s0) = sampler_state
{
    Texture = (SourceTexture);
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = None;
    AddressU = Clamp;
    AddressV = Clamp;
};

texture BloomTexture : register(t1);
sampler2D BloomSampler : register(s1) = sampler_state
{
    Texture = (BloomTexture);
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = None;
    AddressU = Clamp;
    AddressV = Clamp;
};

struct SpriteVertex
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

struct SpritePixel
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

SpritePixel SpriteVS(SpriteVertex input)
{
    SpritePixel output;
    output.Position = mul(input.Position, MatrixTransform);
    output.TexCoord = input.TexCoord;
    return output;
}

float3 SampleGlow(float2 uv)
{
    float4 color = tex2D(SourceSampler, uv);
    return color.rgb * lerp(1.0f, color.a, UseEmissionMask);
}

float4 BlurPS(SpritePixel input) : COLOR0
{
    float2 uv = input.TexCoord;
    float2 nearOffset = BlurStep * 1.384615f;
    float2 farOffset = BlurStep * 3.230769f;

    float3 glow = SampleGlow(uv) * 0.227027f;
    glow += (SampleGlow(uv - nearOffset) + SampleGlow(uv + nearOffset)) * 0.316216f;
    glow += (SampleGlow(uv - farOffset) + SampleGlow(uv + farOffset)) * 0.070270f;
    return float4(glow, 1.0f);
}

float4 CompositePS(SpritePixel input) : COLOR0
{
    float3 scene = tex2D(SourceSampler, input.TexCoord).rgb;
    float3 bloom = tex2D(BloomSampler, input.TexCoord).rgb;
    return float4(scene + bloom * BloomStrength, 1.0f);
}

technique BlurGlow
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL SpriteVS();
        PixelShader = compile PS_SHADERMODEL BlurPS();
    }
}

technique CompositeGlow
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL SpriteVS();
        PixelShader = compile PS_SHADERMODEL CompositePS();
    }
}
