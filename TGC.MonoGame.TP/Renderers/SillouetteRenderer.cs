using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.ModelsInScene;
using TGC.MonoGame.TP.Tanks;
using TGC.MonoGame.TP.Terrain;
using System.Diagnostics;
using BepuPhysics.Trees;
using TGC.MonoGame.TP.Cameras;
using System.IO.Pipes;
namespace TGC.MonoGame.TP.Renderers;

class SillouetteRenderer
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Effect _effect;
    private DepthStencilState _compositeState;
    public RenderTarget2D WhiteTarget { get; private set; }
    private Effect _outlineEffect;
    public SillouetteRenderer(GraphicsDevice graphicsDevice, Effect solidEffect, Effect outlineEffect)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _effect = solidEffect ?? throw new ArgumentNullException(nameof(solidEffect));
        _outlineEffect = outlineEffect;
        WhiteTarget = new RenderTarget2D(
            graphicsDevice, _graphicsDevice.PresentationParameters.BackBufferWidth,
           _graphicsDevice.PresentationParameters.BackBufferHeight, false,
            SurfaceFormat.Color, DepthFormat.Depth24, 0,
            RenderTargetUsage.DiscardContents);


        _compositeState = new DepthStencilState
        {
            DepthBufferEnable = true,
            DepthBufferWriteEnable = false,
            DepthBufferFunction = CompareFunction.Greater,

            StencilEnable = false,
        };
    }
    private void EnsureWhiteTargetSize()
    {
        int width = _graphicsDevice.PresentationParameters.BackBufferWidth;
        int height = _graphicsDevice.PresentationParameters.BackBufferHeight;

        if (WhiteTarget != null &&
            WhiteTarget.Width == width &&
            WhiteTarget.Height == height)
        {
            return;
        }

        WhiteTarget?.Dispose();

        WhiteTarget = new RenderTarget2D(
            _graphicsDevice,
            width,
            height,
            false,
            SurfaceFormat.Color,
            DepthFormat.Depth24,
            0,
            RenderTargetUsage.DiscardContents
        );
    }
    public void Render(IReadOnlyList<ModelInScene> modelsInScene, Matrix view, Matrix projection, Camera camera)
    {
        EnsureWhiteTargetSize();
        Vector3 realCameraPosition = Matrix.Invert(view).Translation;
        var previousDepthState = _graphicsDevice.DepthStencilState;
        var previousRasterizer = _graphicsDevice.RasterizerState;
        try
        {
            _graphicsDevice.SetRenderTarget(WhiteTarget);
            _graphicsDevice.Clear(
                ClearOptions.Target | ClearOptions.DepthBuffer,
                Color.Transparent,
                1f,
                0
            );
            _graphicsDevice.BlendState = BlendState.Opaque;
            _graphicsDevice.DepthStencilState = DepthStencilState.Default;
            _graphicsDevice.RasterizerState = RasterizerState.CullNone;
            _effect.CurrentTechnique = _effect.Techniques["ZBufferDrawing"];
            _effect.Parameters["DiffuseColor"].SetValue(Color.White.ToVector3());
            _effect.Parameters["FarPlane"].SetValue(camera.FarPlane);

            foreach (ModelInScene model in modelsInScene)
            {

                Model modelo = model.GetModelForRendering((realCameraPosition - model.Position).Length());


                Matrix[] bones = new Matrix[modelo.Bones.Count];
                modelo.CopyAbsoluteBoneTransformsTo(bones);
                int index = 0;

                foreach (ModelMesh mesh in modelo.Meshes)
                {
                    Matrix bone = bones[mesh.ParentBone.Index];
                    Matrix world = GetMeshWorld(model, mesh, bone);
                    _effect.Parameters["World"].SetValue(world);
                    _effect.Parameters["WorldInverseTranspose"].SetValue(
                        Matrix.Transpose(Matrix.Invert(world))
                    );
                    _effect.Parameters["View"].SetValue(view);
                    _effect.Parameters["Projection"].SetValue(projection);
                    foreach (ModelMeshPart part in mesh.MeshParts)
                    {
                        if (model.GetTextures()[index] != null)
                        {
                            _effect.Parameters["ModelTexture"].SetValue(
                                model.GetTextures()[index]
                            );
                        }
                        _effect.CurrentTechnique.Passes[0].Apply();
                        if (model is Tank tank)
                        {
                            _graphicsDevice.SetVertexBuffer(tank._dynamicVertexBuffers[part]);
                            _graphicsDevice.Indices = part.IndexBuffer;
                            _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, part.StartIndex, part.PrimitiveCount);
                        }
                        else
                        {
                            _graphicsDevice.SetVertexBuffer(part.VertexBuffer);
                            _graphicsDevice.Indices = part.IndexBuffer;
                            _graphicsDevice.DrawIndexedPrimitives(
                                PrimitiveType.TriangleList, part.VertexOffset,
                                part.StartIndex, part.PrimitiveCount);
                        }
                        index++;
                    }
                }
            }
        }
        finally
        {
            _graphicsDevice.SetRenderTarget(null);

            _graphicsDevice.DepthStencilState = previousDepthState;
            _graphicsDevice.RasterizerState = previousRasterizer;
        }
    }
    private static Matrix GetMeshWorld(ModelInScene model, ModelMesh mesh, Matrix bone)
    {
        if (model is Tank tank)
        {
            if (mesh.Name == "Turret")
                return bone * Matrix.CreateRotationY(tank.TurretYaw) * tank._world;
            if (mesh.Name == "Cannon")
                return bone * Matrix.CreateRotationX(tank.CannonPitch) *
                    Matrix.CreateRotationY(tank.TurretYaw) * tank._world;
        }

        return bone * model._world;
    }
    public void Show(SpriteBatch spriteBatch, Camera camera, Matrix projection)
    {

        _graphicsDevice.DepthStencilState = _compositeState;
        _graphicsDevice.BlendState = BlendState.Opaque;
        _graphicsDevice.RasterizerState = RasterizerState.CullNone;

        _outlineEffect.Parameters["TexelSize"]?.SetValue(
            new Vector2(
                1f / WhiteTarget.Width,
                1f / WhiteTarget.Height
            )
        );
        _outlineEffect.CurrentTechnique = _outlineEffect.Techniques["SillouetteDrawing"];
        Viewport viewport = _graphicsDevice.Viewport;

        Matrix spriteTransform;

        Matrix.CreateOrthographicOffCenter(
            0,
            viewport.Width,
            viewport.Height,
            0,
            0,
            -1,
            out spriteTransform
        );

        _outlineEffect.Parameters["MatrixTransform"]
            .SetValue(spriteTransform);
        _outlineEffect.Parameters["FarPlane"].SetValue(camera.FarPlane);
        _outlineEffect.Parameters["ProjM33"].SetValue(projection.M33);
        _outlineEffect.Parameters["ProjM43"].SetValue(projection.M43);

        spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.Opaque,
                SamplerState.PointClamp,
                _compositeState,
                RasterizerState.CullNone,
                _outlineEffect);

        spriteBatch.Draw(
            WhiteTarget,
            _graphicsDevice.Viewport.Bounds,
            Color.White);
        spriteBatch.End();
        _graphicsDevice.DepthStencilState = DepthStencilState.Default;

    }

}
