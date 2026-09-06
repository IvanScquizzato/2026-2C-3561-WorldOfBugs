using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TGC.MonoGame.TP.ModelsInScene;
namespace TGC.MonoGame.TP.Renderers
{
    public interface IModelInSceneRenderer
    {
        public Effect _effect { get; set; }
        void Draw(ModelInScene modelInScene, Matrix view, Matrix projection);
    }
}