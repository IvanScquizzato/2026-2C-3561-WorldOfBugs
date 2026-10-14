using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.ModelsInScene;

public class CapsuleHitbox : Hitbox
{
    public Vector3 _start;
    public Vector3 _end;
    public float _radius;
    public CapsuleHitboxRenderer capsuleHitboxRenderer { get; set; }

    public CapsuleHitbox(
        Vector3 start,
        Vector3 end,
        float radius,
        Effect hitboxEffect,
        GraphicsDevice graphicsDevice
    )
    {
        _start = start;
        _end = end;
        _radius = radius;
        capsuleHitboxRenderer = new CapsuleHitboxRenderer(hitboxEffect, graphicsDevice);
    }

    public override bool CheckContainsPoint(
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
    public override void Draw(ModelInScene model, Matrix world, Matrix view, Matrix projection)
    {
        capsuleHitboxRenderer.Draw(this, world, view, projection);
    }
}