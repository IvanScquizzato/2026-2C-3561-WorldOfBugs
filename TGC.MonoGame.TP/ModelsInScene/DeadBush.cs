using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.ModelsInScene;
using TGC.MonoGame.TP.Renderers;
using TGC.MonoGame.TP.Materials;
namespace TGC.MonoGame.TP.Bush
{
    public class DeadBush : ModelInScene
    {
        private static readonly Dictionary<Texture, Texture> _normalMaps =
        new Dictionary<Texture, Texture>();

        public override Dictionary<Texture, Texture> _normalMapsOfTexture
        => _normalMaps;
        public override Dictionary<string, IMaterial> _materialOfNameTexture { get; set; } = new Dictionary<string, IMaterial>
{
    { "Models\\DeadBush\\Salix elaeagnos HD_Dead mat 50_LOD0.fbm\\DeadBush_Base_color_ambient_occlusion_0", Wood }
};
        public DeadBush(ContentManager Content, String modelPath, Vector3 position, Matrix rotation, Vector3 scale, IModelInSceneRenderer renderer)
            : base(Content, modelPath, position, rotation, scale, renderer, 1f)
        {
        }
    }
}