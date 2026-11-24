using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Missile;
using TGC.MonoGame.TP.Renderers;

namespace TGC.MonoGame.TP.ModelsInScene;

public class Lamp : ModelInScene
{
    private const float InitialHealth = 150f;
    private const float MissileDamage = 50f;

    private static readonly Dictionary<Texture, Texture> NormalMaps = new();

    public override Dictionary<Texture, Texture> _normalMapsOfTexture => NormalMaps;

    public Vector3 BulbLocalPosition { get; set; } = new(0f, 440f, 0f);
    public Vector3 LightPosition => Vector3.Transform(BulbLocalPosition, _world);

    public Vector3 LightColor { get; set; } = new(1f, 0.65f, 0.35f);
    public float LightIntensity { get; set; } = 3f;
    public float LightRadius { get; set; } = 1200f;

    public float Health { get; private set; } = InitialHealth;
    public bool IsOn { get; set; } = true;

    public float EffectiveLightIntensity => IsOn && !_isDisabled ? LightIntensity : 0f;

    public Lamp(
        ContentManager content,
        string modelPath,
        Vector3 position,
        Matrix rotation,
        Vector3 scale,
        IModelInSceneRenderer renderer)
        : base(content, modelPath, position, rotation, scale, renderer, 1f)
    {
    }

    public override void CreateHitbox(Effect effect, GraphicsDevice graphicsDevice)
    {
        Vector3 halfSize = new Vector3(_widthLocal, _heightLocal, _depthLocal) * 0.5f;
        _hitbox = new BoxHitbox(_midpoint - halfSize, _midpoint + halfSize, effect, graphicsDevice);
    }

    public override bool ShouldCollideWithMissile(Missil missil)
    {
        if (_isDisabled || missil._isDisabled || _hitbox is not BoxHitbox hitbox)
            return false;

        Matrix toLocal = Matrix.Invert(_world);
        Vector3 start = Vector3.Transform(missil.PreviousPosition, toLocal);
        Vector3 end = Vector3.Transform(missil.Position, toLocal);
        var bounds = new BoundingBox(hitbox.Min, hitbox.Max);
        Vector3 travel = end - start;
        float distance = travel.Length();

        if (distance == 0f)
            return bounds.Contains(start) != ContainmentType.Disjoint;

        float? hitDistance = new Ray(start, travel / distance).Intersects(bounds);
        return hitDistance is float hit && hit <= distance;
    }

    public override void CollidedWithMissil(Missil missil)
    {
        if (_isDisabled || missil._isDisabled)
            return;

        Health = MathF.Max(0f, Health - MissileDamage);
        if (Health == 0f)
            Destroy();
    }

    public override void Destroy()
    {
        if (_isDisabled)
            return;

        Health = 0f;
        IsOn = false;
        base.Destroy();
    }
}
