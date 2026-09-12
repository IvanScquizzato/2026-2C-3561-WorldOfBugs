using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.ModelsInScene;
using TGC.MonoGame.TP.Renderers;
namespace TGC.MonoGame.TP.Plants
{
    public class Plant : ModelInScene
    {
        public Plant(ContentManager Content, String modelPath, Vector3 position, Matrix rotation, Vector3 scale, IModelInSceneRenderer renderer, float angularDrag, float mass)
            : base(Content, modelPath, position, rotation, scale, renderer, angularDrag, mass)
        {
        }
    }
}