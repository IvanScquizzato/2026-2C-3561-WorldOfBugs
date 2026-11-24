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
        private HashSet<ModelInScene> _loggedModels = new();

        // Si algo se ve mal, poner en false vuelve al camino anterior
        // (un draw por ModelMeshPart).
        private const bool UseMergedMeshes = true;

        // Una malla unificada por Model, compartida entre instancias. null =
        // el modelo no se pudo unificar y se dibuja por el camino de siempre.
        private readonly Dictionary<Model, MergedModelMesh> _mergedMeshes = new();
        public PBRRenderer(GraphicsDevice graphicsDevice, Effect effect, Effect hitboxEffect)
        {
            _graphicsDevice = graphicsDevice;
            _effect = effect;
            _hitBoxEffect = hitboxEffect;

        }

        public void Draw(ModelInScene modelInScene, Matrix view, Matrix projection)
        {

            if (!new BoundingFrustum(view * projection).Intersects(modelInScene.AABB))
            {
                return;
            }

            _effect.Parameters["View"].SetValue(view);
            _effect.Parameters["Projection"].SetValue(projection);
            _effect.Parameters["Roughness"]?.SetValue(0.7f);
            _effect.Parameters["K"]?.SetValue((0.7f + 1f) * (0.7f + 1f) / 8f);
            _effect.Parameters["F0"]?.SetValue(new Vector3(0.04f, 0.04f, 0.04f));
            Vector3 realCameraPosition = Matrix.Invert(view).Translation;
            _effect.Parameters["CameraPosition"]?.SetValue(realCameraPosition);
            _effect.Parameters["TrackOffset"]?.SetValue(0f);
            Model model = modelInScene.GetModelForRendering((realCameraPosition - modelInScene.Position).Length());

            if (UseMergedMeshes && TryDrawMerged(modelInScene, model))
            {
                _graphicsDevice.DepthStencilState = DepthStencilState.Default;
                return;
            }

            var modelMeshesBaseTransforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(modelMeshesBaseTransforms);
            var index = 0;
            foreach (var mesh in model.Meshes)
            {
                var relativeTransform = modelMeshesBaseTransforms[mesh.ParentBone.Index];

                Matrix world = relativeTransform * modelInScene._world;

                _effect.Parameters["World"].SetValue(world);
                foreach (var meshPart in mesh.MeshParts)
                {
                    if (modelInScene.GetTextures()[index] != null)
                    {
                        _effect.Parameters["ModelTexture"]?.SetValue(modelInScene.GetTextures()[index]);
                        if (modelInScene._normalMapsOfTexture.TryGetValue(modelInScene.GetTextures()[index], out Texture normalMap))
                        {
                            _effect.Parameters["NormalMap"]?.SetValue(normalMap);
                            _effect.CurrentTechnique = _effect.Techniques["WithNormalMap"];
                        }
                        else
                        {
                            _effect.CurrentTechnique = _effect.Techniques["WithoutNormalMap"];
                        }
                        if (modelInScene._materialOfNameTexture.TryGetValue(modelInScene.GetTextures()[index].Name, out IMaterial material))
                        {
                            _effect.Parameters["Roughness"]?.SetValue(material.Roughness);
                            _effect.Parameters["K"]?.SetValue(material.K);
                            _effect.Parameters["F0"]?.SetValue(material.F0);
                        }
                    }

                    _effect.CurrentTechnique.Passes[0].Apply();
                    _graphicsDevice.SetVertexBuffer(meshPart.VertexBuffer);
                    _graphicsDevice.Indices = meshPart.IndexBuffer;
                    _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, meshPart.VertexOffset, meshPart.StartIndex, meshPart.PrimitiveCount);
                    index++;
                }
            }
            _graphicsDevice.DepthStencilState = DepthStencilState.Default;

        }

        private bool TryDrawMerged(ModelInScene modelInScene, Model model)
        {
            if (!_mergedMeshes.TryGetValue(model, out MergedModelMesh merged))
            {
                // Se construye la primera vez que el modelo se dibuja.
                merged = MergedModelMesh.TryBuild(_graphicsDevice, model, modelInScene.GetTextures());
                _mergedMeshes[model] = merged;
            }
            if (merged == null)
                return false;

            // Las transformaciones de los huesos ya están aplicadas en los vértices.
            _effect.Parameters["World"].SetValue(modelInScene._world);

            foreach (MergedModelMesh.Batch batch in merged.Batches)
            {
                // Mismo seteo de estado que hace el camino por partes.
                Texture2D texture = batch.Texture;
                if (texture != null)
                {
                    _effect.Parameters["ModelTexture"]?.SetValue(texture);
                    if (modelInScene._normalMapsOfTexture.TryGetValue(texture, out Texture normalMap))
                    {
                        _effect.Parameters["NormalMap"]?.SetValue(normalMap);
                        _effect.CurrentTechnique = _effect.Techniques["WithNormalMap"];
                    }
                    else
                    {
                        _effect.CurrentTechnique = _effect.Techniques["WithoutNormalMap"];
                    }
                    if (modelInScene._materialOfNameTexture.TryGetValue(texture.Name, out IMaterial material))
                    {
                        _effect.Parameters["Roughness"]?.SetValue(material.Roughness);
                        _effect.Parameters["K"]?.SetValue(material.K);
                        _effect.Parameters["F0"]?.SetValue(material.F0);
                    }
                }

                _effect.CurrentTechnique.Passes[0].Apply();
                _graphicsDevice.SetVertexBuffer(batch.Vertices);
                _graphicsDevice.Indices = batch.Indices;
                _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, batch.PrimitiveCount);
            }
            return true;
        }
    }
}
