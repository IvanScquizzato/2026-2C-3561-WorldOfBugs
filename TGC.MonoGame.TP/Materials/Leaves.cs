using System.Runtime.InteropServices.Marshalling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.Materials
{
    public class Leaves : IMaterial
    {
        public Vector3 F0 { get; set; } = new Vector3(0.04f, 0.04f, 0.04f);
        public float Roughness { get; set; } = 0.3f;
        public float K { get; set; } = 0;

    }
}