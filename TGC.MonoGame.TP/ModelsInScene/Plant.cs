using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.ModelsInScene;
using TGC.MonoGame.TP.Renderers;
using TGC.MonoGame.TP.Materials;
namespace TGC.MonoGame.TP.Plants
{
    public class Plant : ModelInScene
    {
        private static readonly Dictionary<Texture, Texture> _normalMaps =
        new Dictionary<Texture, Texture>();

        public override Dictionary<Texture, Texture> _normalMapsOfTexture
        => _normalMaps;
        public override Dictionary<string, string> _namesNormalMapsOfNameTexture { get; set; } = new Dictionary<string, string>
{
    { "Models\\Plant\\textures\\plant1Color_0", "Models\\Plant\\textures\\plant1Normal" }
};

        public override Dictionary<string, IMaterial> _materialOfNameTexture { get; set; } = new Dictionary<string, IMaterial>
{
    { "Models\\Plant\\textures\\plant1Color_0", Leaves }
};
        public Plant(ContentManager Content, String modelPath, Vector3 position, Matrix rotation, Vector3 scale, IModelInSceneRenderer renderer, float linearDrag, float angularDrag, float mass)
            : base(Content, modelPath, position, rotation, scale, renderer, mass)
        {
        }
    }
}