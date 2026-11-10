namespace TGC.MonoGame.TP.ModelsInScene;

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed class Skybox : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Effect _skyEffect;

    private VertexBuffer _cubeVertexBuffer;
    private IndexBuffer _cubeIndexBuffer;

    private readonly DepthStencilState _skyDepthState;
    private readonly RasterizerState _skyRasterizerState;

    public Skybox(GraphicsDevice graphicsDevice, Effect skyAtmosphereEffect)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _skyEffect = skyAtmosphereEffect ?? throw new ArgumentNullException(nameof(skyAtmosphereEffect));

        // Configure depth testing: must succeed where Depth == 1.0 (LessEqual), but NEVER write depth
        _skyDepthState = new DepthStencilState
        {
            DepthBufferEnable = true,
            DepthBufferWriteEnable = false,
            DepthBufferFunction = CompareFunction.LessEqual
        };

        // Cull Counter-Clockwise inside the box
        _skyRasterizerState = new RasterizerState
        {
            CullMode = CullMode.None
        };
        
        _skyEffect.Parameters["SkyZenithColor"].SetValue(
            new Vector3(0.18f, 0.42f, 0.75f));
        _skyEffect.Parameters["SkyHorizonColor"].SetValue(
            new Vector3(0.65f, 0.78f, 0.92f));
        _skyEffect.Parameters["SunsetColor"].SetValue(
            new Vector3(0.95f, 0.45f, 0.15f));
        _skyEffect.Parameters["GroundColor"].SetValue(
            new Vector3(0.20f, 0.18f, 0.15f));

        BuildUnitCube();
    }

    private void BuildUnitCube()
    {
        VertexPosition[] vertices = new VertexPosition[]
        {
            new VertexPosition(new Vector3(-1,  1, -1)),
            new VertexPosition(new Vector3( 1,  1, -1)),
            new VertexPosition(new Vector3( 1, -1, -1)),
            new VertexPosition(new Vector3(-1, -1, -1)),
            new VertexPosition(new Vector3(-1,  1,  1)),
            new VertexPosition(new Vector3( 1,  1,  1)),
            new VertexPosition(new Vector3( 1, -1,  1)),
            new VertexPosition(new Vector3(-1, -1,  1))
        };

        short[] indices = new short[]
        {
            0, 1, 2, 2, 3, 0, // Front
            1, 5, 6, 6, 2, 1, // Right
            5, 4, 7, 7, 6, 5, // Back
            4, 0, 3, 3, 7, 4, // Left
            4, 5, 1, 1, 0, 4, // Top
            3, 2, 6, 6, 7, 3  // Bottom
        };

        _cubeVertexBuffer = new VertexBuffer(_graphicsDevice, typeof(VertexPosition), vertices.Length, BufferUsage.WriteOnly);
        _cubeVertexBuffer.SetData(vertices);

        _cubeIndexBuffer = new IndexBuffer(_graphicsDevice, IndexElementSize.SixteenBits, indices.Length, BufferUsage.WriteOnly);
        _cubeIndexBuffer.SetData(indices);
    }

    public void Draw(Matrix cameraView, Matrix cameraProjection, DayNightCycle cycle, TextureCube stars)
    {
        // 1. Strip translation from camera view matrix
        Matrix skyView = cameraView;
        skyView.M41 = 0.0f;
        skyView.M42 = 0.0f;
        skyView.M43 = 0.0f;

        // 2. Supply shader parameters
        _skyEffect.Parameters["SkyView"].SetValue(skyView);
        _skyEffect.Parameters["InverseSkyView"].SetValue(Matrix.Invert(skyView));
        _skyEffect.Parameters["Projection"].SetValue(cameraProjection);
        _skyEffect.Parameters["ProjectionScale"].SetValue(
            new Vector2(cameraProjection.M11, cameraProjection.M22));
        _skyEffect.Parameters["SunDirection"]?.SetValue(cycle.SunDirection);
        _skyEffect.Parameters["SunColor"]?.SetValue(cycle.SunColor);
        _skyEffect.Parameters["SunIntensity"]?.SetValue(cycle.SunIntensity);
        _skyEffect.Parameters["NightBlend"]?.SetValue(cycle.NightFactor);
        _skyEffect.Parameters["StarCubeMap"]?.SetValue(stars);

        // 3. Set device state for late-skybox Early-Z pass
        _graphicsDevice.DepthStencilState = _skyDepthState;
        _graphicsDevice.RasterizerState = _skyRasterizerState;
        _graphicsDevice.BlendState = BlendState.Opaque;

        _graphicsDevice.SetVertexBuffer(_cubeVertexBuffer);
        _graphicsDevice.Indices = _cubeIndexBuffer;

        _skyEffect.CurrentTechnique.Passes[0].Apply();
        _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 12);
    }

    public void Dispose()
    {
        _cubeVertexBuffer?.Dispose();
        _cubeIndexBuffer?.Dispose();
        _skyDepthState?.Dispose();
        _skyRasterizerState?.Dispose();
    }
}
