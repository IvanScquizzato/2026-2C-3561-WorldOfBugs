using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TGC.MonoGame.TP.ModelsInScene;
namespace TGC.MonoGame.TP.Renderers
{
    public class BasicRenderer : IModelInSceneRenderer
    {
        public GraphicsDevice _graphicsDevice { get; set; }
        public Effect _effect { get; set; }

        public BasicRenderer(GraphicsDevice graphicsDevice, Effect effect)
        {
            _graphicsDevice = graphicsDevice;
            _effect = effect;
        }

        public void Draw(ModelInScene modelInScene, Matrix view, Matrix projection)
        {
            _effect.Parameters["View"].SetValue(view);
            _effect.Parameters["Projection"].SetValue(projection);


            var modelMeshesBaseTransforms = new Matrix[modelInScene._model.Bones.Count];
            modelInScene._model.CopyAbsoluteBoneTransformsTo(modelMeshesBaseTransforms);
            var index = 0;
            foreach (var mesh in modelInScene._model.Meshes)
            {
                var relativeTransform = modelMeshesBaseTransforms[mesh.ParentBone.Index];
                _effect.Parameters["World"].SetValue(relativeTransform * modelInScene._world);
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
        }
    }
}