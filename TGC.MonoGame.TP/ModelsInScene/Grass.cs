using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.ModelsInScene;
using TGC.MonoGame.TP.Renderers;
using TGC.MonoGame.TP.Materials;
namespace TGC.MonoGame.TP.Ground
{
    public class Grass : ModelInScene
    {
        private static readonly Dictionary<Texture, Texture> _normalMaps =
        new Dictionary<Texture, Texture>();

        public override Dictionary<Texture, Texture> _normalMapsOfTexture
        => _normalMaps;
        public override Dictionary<string, string> _namesNormalMapsOfNameTexture { get; set; } = new Dictionary<string, string>
{
    { "Models\\Grass\\textures\\T_Grass_Base_D_0", "Models\\Grass\\textures\\T_Grass_Base_N" },
    { "Models\\Grass\\textures\\T_Grass_Reeds_D_0", "Models\\Grass\\textures\\T_Grass_Reeds_N" }
};

        public override Dictionary<string, IMaterial> _materialOfNameTexture { get; set; } = new Dictionary<string, IMaterial>
{
    { "Models\\Grass\\textures\\T_Grass_Base_D_0", Leaves },
    { "Models\\Grass\\textures\\T_Grass_Reeds_D_0", Leaves }
};
        public Grass(ContentManager Content, String modelPath, Vector3 position, Matrix rotation, Vector3 scale, IModelInSceneRenderer renderer)
            : base(Content, modelPath, position, rotation, scale, renderer, 1f)
        {
        }
    }
}