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
                Console.WriteLine(
    $"Meshes: {modelInScene._model.Meshes.Count} | Textures: {modelInScene._textures.Count}"
);
                if (modelInScene._textures[index] != null)
                {
                    _effect.Parameters["ModelTexture"].SetValue(modelInScene._textures[index]);
                }

                var relativeTransform = modelMeshesBaseTransforms[mesh.ParentBone.Index];
                _effect.Parameters["World"].SetValue(relativeTransform * modelInScene._world);
                mesh.Draw();
                index++;
            }
        }
    }
}