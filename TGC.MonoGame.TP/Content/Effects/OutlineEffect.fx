#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float4x4 MatrixTransform;
float FarPlane;
float ProjM33;      
float ProjM43;
texture Texture;

sampler2D TextureSampler = sampler_state
{
    Texture = <Texture>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = None;
    AddressU = Clamp;
    AddressV = Clamp;
};

float2 TexelSize;
struct SpriteVertexInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

struct SpriteVertexOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};
struct OutlineOutput
{
    float4 Color : COLOR0;
    float Depth : DEPTH0;
};

SpriteVertexOutput SpriteVS(SpriteVertexInput input)
{
    SpriteVertexOutput output = (SpriteVertexOutput)0;

    output.Position = mul(input.Position, MatrixTransform);
    output.Color = input.Color;
    output.TexCoord = input.TexCoord;

    return output;
}


OutlineOutput OutlinePS(SpriteVertexOutput input)
{
    float2 uv = input.TexCoord;

    float4 center = tex2D(TextureSampler, uv);

    float left = tex2D(
                     TextureSampler,
                     uv + float2(-TexelSize.x, 0))
                     .a;

    float right = tex2D(
                      TextureSampler,
                      uv + float2(TexelSize.x, 0))
                      .a;

    float up = tex2D(
                   TextureSampler,
                   uv + float2(0, -TexelSize.y))
                   .a;

    float down = tex2D(
                     TextureSampler,
                     uv + float2(0, TexelSize.y))
                     .a;

    bool isEdge =
        center.a > 0.5 &&
        (left < 0.5 ||
         right < 0.5 ||
         up < 0.5 ||
         down < 0.5);

    clip(isEdge ? 1 : -1);
    float DepthBias = 8.0;
    float d = (center.r + center.g / 255.0) * FarPlane - DepthBias;
    d = max(d, 0.01);
    float hardwareDepth = ProjM43 / d - ProjM33;

    OutlineOutput o;
    o.Color = float4(1, 1, 1, 1);
    o.Depth = hardwareDepth;
    return o;

}
technique SillouetteDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL SpriteVS();
		PixelShader = compile PS_SHADERMODEL OutlinePS();
	}
};