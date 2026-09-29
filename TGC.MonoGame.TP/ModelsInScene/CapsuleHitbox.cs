using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP.ModelsInScene;

public class CapsuleHitbox : Hitbox
{
    private Vector3 _start;
    private Vector3 _end;
    private float _radius;

    public CapsuleHitbox(
        Vector3 start,
        Vector3 end,
        
        float radius
    )
    {
        _start = start;
        _end = end;
        _radius = radius;
    }

    public override bool ContainsPoint(
        Vector3 worldPoint,
        Matrix world
    )
    {
        Matrix inverseWorld = Matrix.Invert(world);

        Vector3 point =
            Vector3.Transform(worldPoint, inverseWorld);

        Vector3 segment = _end - _start;

        float lengthSquared =
            segment.LengthSquared();

        if (lengthSquared == 0)
            return Vector3.DistanceSquared(point, _start)
                   <= _radius * _radius;

        float t =
            Vector3.Dot(point - _start, segment)
            / lengthSquared;

        t = MathHelper.Clamp(t, 0f, 1f);

        Vector3 closest =
            _start + segment * t;

        return Vector3.DistanceSquared(point, closest)
               <= _radius * _radius;
    }
}