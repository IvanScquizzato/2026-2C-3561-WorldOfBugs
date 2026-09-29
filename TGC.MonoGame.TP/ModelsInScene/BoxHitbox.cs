using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP.ModelsInScene;

public class BoxHitbox : Hitbox
{
    private Vector3 _min;
    
    private Vector3 _max;

    public BoxHitbox(Vector3 min, Vector3 max)
    {
        _min = min;
        _max = max;
    }

    public override bool ContainsPoint(
        Vector3 worldPoint,
        Matrix world
    )
    {
        Matrix inverseWorld = Matrix.Invert(world);

        Vector3 localPoint =
            Vector3.Transform(worldPoint, inverseWorld);

        return
            localPoint.X >= _min.X &&
            localPoint.X <= _max.X &&
            localPoint.Y >= _min.Y &&
            localPoint.Y <= _max.Y &&
            localPoint.Z >= _min.Z &&
            localPoint.Z <= _max.Z;
    }
}