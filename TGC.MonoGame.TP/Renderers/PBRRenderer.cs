using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TGC.MonoGame.TP.ModelsInScene;
using TGC.MonoGame.TP.Tanks;
using System.Diagnostics;
using TGC.MonoGame.TP.Materials;
namespace TGC.MonoGame.TP.Renderers
{
    public class PBRRenderer : IModelInSceneRenderer
    {
        public GraphicsDevice _graphicsDevice { get; set; }
        public Effect _effect { get; set; }
        public Effect _hitBoxEffect { get; set; }

        public PBRRenderer(GraphicsDevice graphicsDevice, Effect effect, Effect hitboxEffect)
        {
            _graphicsDevice = graphicsDevice;
            _effect = effect;
            _hitBoxEffect = hitboxEffect;
        }

        public void Draw(ModelInScene modelInScene, Matrix view, Matrix projection)
        {
            _effect.Parameters["View"].SetValue(view);
            _effect.Parameters["Projection"].SetValue(projection);
            _effect.Parameters["Roughness"].SetValue(0.7f);
            _effect.Parameters["K"].SetValue((0.7f + 1f) * (0.7f + 1f) / 8f);
            _effect.Parameters["F0"].SetValue(new Vector3(0.04f, 0.04f, 0.04f));
            Vector3 realCameraPosition = Matrix.Invert(view).Translation;
            _effect.Parameters["CameraPosition"].SetValue(realCameraPosition);


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
                        if (modelInScene._normalMapsOfTexture.TryGetValue(modelInScene.GetTextures()[index], out Texture normalMap))
                        {
                            _effect.Parameters["NormalMap"].SetValue(normalMap);
                            _effect.CurrentTechnique = _effect.Techniques["WithNormalMap"];
                        }
                        else
                        {
                            _effect.CurrentTechnique = _effect.Techniques["WithoutNormalMap"];
                        }
                        if (modelInScene._materialOfNameTexture.TryGetValue(modelInScene.GetTextures()[index].Name, out IMaterial material))
                        {
                            _effect.Parameters["Roughness"].SetValue(material.Roughness);
                            _effect.Parameters["K"].SetValue(material.K);
                            _effect.Parameters["F0"].SetValue(material.F0);
                        }
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
