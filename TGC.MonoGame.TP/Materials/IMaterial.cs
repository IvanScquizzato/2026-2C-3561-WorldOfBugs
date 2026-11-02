using System.Runtime.InteropServices.Marshalling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.Materials
{
    public interface IMaterial
    {
        Vector3 F0 { get; set; }
        float Roughness { get; set; }
        float K { get; set; }
    }
}