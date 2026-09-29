float4x4 World;
float4x4 View;
float4x4 Projection;

float4 DebugColor;

struct VertexShaderInput
{
    float4 Position : POSITION0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
};

VertexShaderOutput VSMain(VertexShaderInput input)
{
    VertexShaderOutput output;

    float4 worldPosition =
        mul(input.Position, World);

    float4 viewPosition =
        mul(worldPosition, View);

    output.Position =
        mul(viewPosition, Projection);

    return output;
}

float4 PSMain(VertexShaderOutput input) : SV_TARGET
{
    return DebugColor;
}

technique Debug
{
    pass Pass0
    {
        VertexShader = compile vs_3_0 VSMain();
        PixelShader = compile ps_3_0 PSMain();
    }
}