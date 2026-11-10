namespace TGC.MonoGame.TP.ModelsInScene;

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed class EnvironmentProbe : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    public RenderTargetCube ReflectionTarget { get; private set; }
    public int Resolution { get; }

    private readonly Matrix _probeProjection;
    private readonly Vector3[] _faceForwardVectors = new Vector3[6];
    private readonly Vector3[] _faceUpVectors = new Vector3[6];

    public EnvironmentProbe(GraphicsDevice graphicsDevice, int resolution = 256)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        Resolution = resolution;

        ReflectionTarget = new RenderTargetCube(
            _graphicsDevice,
            Resolution,
            false,
            SurfaceFormat.Color,
            DepthFormat.Depth24Stencil8
        );

        _probeProjection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver2, 1.0f, 0.1f, 500.0f);

        // CubeMap Face 0: Positive X (Right)
        _faceForwardVectors[0] = Vector3.Right;
        _faceUpVectors[0] = Vector3.Up;

        // CubeMap Face 1: Negative X (Left)
        _faceForwardVectors[1] = Vector3.Left;
        _faceUpVectors[1] = Vector3.Up;

        // CubeMap Face 2: Positive Y (Up)
        _faceForwardVectors[2] = Vector3.Up;
        _faceUpVectors[2] = Vector3.Forward;

        // CubeMap Face 3: Negative Y (Down)
        _faceForwardVectors[3] = Vector3.Down;
        _faceUpVectors[3] = Vector3.Backward;

        // CubeMap Face 4: Positive Z (Forward)
        _faceForwardVectors[4] = Vector3.Forward;
        _faceUpVectors[4] = Vector3.Up;

        // CubeMap Face 5: Negative Z (Backward)
        _faceForwardVectors[5] = Vector3.Backward;
        _faceUpVectors[5] = Vector3.Up;
    }

    public void RenderProbeFaces(Vector3 probePosition, Action<Matrix, Matrix> renderSceneCallback)
    {
        for (int faceIdx = 0; faceIdx < 6; faceIdx++)
        {
            CubeMapFace face = (CubeMapFace)faceIdx;

            // Bind the specific face of the RenderTargetCube
            _graphicsDevice.SetRenderTarget(ReflectionTarget, face);
            _graphicsDevice.Clear(Color.CornflowerBlue);

            Vector3 target = probePosition + _faceForwardVectors[faceIdx];
            Matrix view = Matrix.CreateLookAt(probePosition, target, _faceUpVectors[faceIdx]);

            // Draw entire scene from probe camera
            renderSceneCallback.Invoke(view, _probeProjection);
        }

        // Unbind cube target
        _graphicsDevice.SetRenderTarget(null);
    }

    public void Dispose()
    {
        ReflectionTarget?.Dispose();
        ReflectionTarget = null;
    }
}
