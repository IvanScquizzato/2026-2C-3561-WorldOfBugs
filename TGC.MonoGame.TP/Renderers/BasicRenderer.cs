using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TGC.MonoGame.TP.ModelsInScene;
using TGC.MonoGame.TP.Tanks;
using System.Diagnostics;
namespace TGC.MonoGame.TP.Renderers
{
    public class BasicRenderer : IModelInSceneRenderer
    {
        public GraphicsDevice _graphicsDevice { get; set; }
        public Effect _effect { get; set; }
        public Effect _hitBoxEffect { get; set; }
        private DepthStencilState _occluderState;

        public BasicRenderer(GraphicsDevice graphicsDevice, Effect effect, Effect hitboxEffect)
        {
            _graphicsDevice = graphicsDevice;
            _effect = effect;
            _hitBoxEffect = hitboxEffect;
            _occluderState =
                new DepthStencilState
                {
                    DepthBufferEnable = true,
                    DepthBufferWriteEnable = true,
                    DepthBufferFunction = CompareFunction.Less,

                    StencilEnable = true,
                    StencilFunction = CompareFunction.Always,

                    StencilPass = StencilOperation.Replace,
                    StencilFail = StencilOperation.Keep,
                    StencilDepthBufferFail = StencilOperation.Keep,

                    ReferenceStencil = 2
                };
        }

        public void Draw(ModelInScene modelInScene, Matrix view, Matrix projection)
        {
            var previousStencil = _graphicsDevice.DepthStencilState;
            if (modelInScene.ShouldOcclude)
            {
                //_graphicsDevice.DepthStencilState = _occluderState;
                _graphicsDevice.DepthStencilState = DepthStencilState.Default;

            }
            else
            {
                _graphicsDevice.DepthStencilState = DepthStencilState.Default;

            }
            _effect.Parameters["View"].SetValue(view);
            _effect.Parameters["Projection"].SetValue(projection);
            _effect.CurrentTechnique = _effect.Techniques["BasicColorDrawing"];

            var modelMeshesBaseTransforms = new Matrix[modelInScene._model.Bones.Count];
            modelInScene._model.CopyAbsoluteBoneTransformsTo(modelMeshesBaseTransforms);
            var index = 0;
            foreach (var mesh in modelInScene._model.Meshes)
            {
                var relativeTransform = modelMeshesBaseTransforms[mesh.ParentBone.Index];

                Matrix world = relativeTransform * modelInScene._world;

                _effect.Parameters["World"].SetValue(world);
                foreach (var meshPart in mesh.MeshParts)
                {
                    if (modelInScene.GetTextures()[index] != null)
                    {
                        _effect.Parameters["ModelTexture"].SetValue(modelInScene.GetTextures()[index]);
                    }
                    _effect.CurrentTechnique.Passes[0].Apply();
                    _graphicsDevice.SetVertexBuffer(meshPart.VertexBuffer);
                    _graphicsDevice.Indices = meshPart.IndexBuffer;
                    _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, meshPart.VertexOffset, meshPart.StartIndex, meshPart.PrimitiveCount);
                    index++;
                }
            }
            _graphicsDevice.DepthStencilState = previousStencil;
        }
    }
}