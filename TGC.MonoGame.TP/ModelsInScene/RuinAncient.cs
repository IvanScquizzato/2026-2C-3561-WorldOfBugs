using System;
using System.Collections.Generic;
using System.Linq;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Colliders;
using TGC.MonoGame.TP.Editor;
using TGC.MonoGame.TP.Renderers;
using NumericQuaternion = System.Numerics.Quaternion;
using NumericVector3 = System.Numerics.Vector3;

namespace TGC.MonoGame.TP.ModelsInScene;

public class RuinAncient : ModelInScene
{
    private static readonly Dictionary<Texture, Texture> NormalMaps = new();
    private readonly Matrix[] _boxTransforms;

    public override Dictionary<Texture, Texture> _normalMapsOfTexture => NormalMaps;
    internal SceneRepository.SceneHitbox[] Hitboxes { get; }

    public RuinAncient(ContentManager content, string modelPath, Vector3 position,
        Matrix rotation, Vector3 scale, IModelInSceneRenderer renderer, SceneRepository.SceneHitbox[] hitboxes)
        : base(content, modelPath, position, rotation, scale, renderer, 1f)
    {
        Hitboxes = hitboxes;
        _boxTransforms = hitboxes.Select(box => box.ToModelTransform()).ToArray();
    }

    public override void CreateHitbox(Effect effect, GraphicsDevice graphicsDevice)
    {
        var hitbox = new CompoundHitbox();
        foreach (Matrix boxTransform in _boxTransforms)
        {
            hitbox.hitboxes.Add(new BoxHitbox(-Vector3.One, Vector3.One, boxTransform, effect, graphicsDevice));
        }
        _hitbox = hitbox;
    }

    public override void CreateCollider(Simulation simulation, BufferPool bufferPool, Effect effect,
        GraphicsDevice graphicsDevice)
    {
        _collider = new RuinCollider(this, simulation, bufferPool, effect, graphicsDevice);
    }

    private sealed class RuinCollider : Collider
    {
        private TypedIndex _shape;

        public RuinCollider(RuinAncient ruin, Simulation simulation, BufferPool pool, Effect effect,
            GraphicsDevice graphicsDevice) : base(simulation, pool, effect, graphicsDevice)
        {
            Matrix scale = Matrix.CreateScale(ruin.Scale);
            pool.Take(ruin._boxTransforms.Length, out Buffer<CompoundChild> children);
            int count = 0;
            StaticHandle? handle = null;
            try
            {
                foreach (Matrix box in ruin._boxTransforms)
                {
                    children[count] = CreateChild(box * scale);
                    count++;
                }

                _shape = simulation.Shapes.Add(new Compound(children));
                var pose = new RigidPose(ToNumerics(ruin.Position), ToNumerics(ruin.Rotation));
                handle = simulation.Statics.Add(new StaticDescription(pose, _shape));
                _staticReference = simulation.Statics.GetStaticReference(handle.Value);
            }
            catch
            {
                if (handle.HasValue)
                    simulation.Statics.Remove(handle.Value);
                if (_shape.Exists)
                    simulation.Shapes.RecursivelyRemoveAndDispose(_shape, pool);
                else
                {
                    for (int i = 0; i < count; i++)
                        simulation.Shapes.RemoveAndDispose(children[i].ShapeIndex, pool);
                    pool.Return(ref children);
                }
                throw;
            }
        }

        public override void Remove()
        {
            if (!_shape.Exists)
                return;

            base.Remove();
            _simulation.Shapes.RecursivelyRemoveAndDispose(_shape, _bufferPool);
            _childColliders.Clear();
            _shape = default;
        }

        private CompoundChild CreateChild(Matrix frame)
        {
            Vector3 halfSize = new(frame.Right.Length(), frame.Up.Length(), frame.Backward.Length());
            Vector3 right = frame.Right / halfSize.X;
            Vector3 up = frame.Up / halfSize.Y;
            Vector3 backward = frame.Backward / halfSize.Z;
            const float tolerance = 1e-5f;
            if (MathF.Abs(Vector3.Dot(right, up)) < tolerance &&
                MathF.Abs(Vector3.Dot(right, backward)) < tolerance &&
                MathF.Abs(Vector3.Dot(up, backward)) < tolerance)
            {
                Matrix rotation = Matrix.Identity;
                rotation.Right = right;
                rotation.Up = up;
                rotation.Backward = Vector3.Normalize(Vector3.Cross(right, up));
                return new CompoundChild
                {
                    ShapeIndex = _simulation.Shapes.Add(new Box(2 * halfSize.X, 2 * halfSize.Y, 2 * halfSize.Z)),
                    LocalPose = new RigidPose(ToNumerics(frame.Translation), ToNumerics(rotation))
                };
            }

            // A rotated box can become skewed under nonuniform scale.
            // Its eight corners preserve the volume used by the hitbox.
            Span<NumericVector3> corners = stackalloc NumericVector3[8];
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 corner = ((i & 1) == 0 ? -frame.Right : frame.Right)
                    + ((i & 2) == 0 ? -frame.Up : frame.Up)
                    + ((i & 4) == 0 ? -frame.Backward : frame.Backward);
                corners[i] = ToNumerics(corner);
            }

            var hull = new ConvexHull(corners, _bufferPool, out NumericVector3 center);
            try
            {
                return new CompoundChild
                {
                    ShapeIndex = _simulation.Shapes.Add(hull),
                    LocalPose = new RigidPose(ToNumerics(frame.Translation) + center)
                };
            }
            catch
            {
                hull.Dispose(_bufferPool);
                throw;
            }
        }
    }

    private static NumericVector3 ToNumerics(Vector3 vector) => new(vector.X, vector.Y, vector.Z);

    private static NumericQuaternion ToNumerics(Matrix rotation)
    {
        Quaternion quaternion = Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(rotation));
        return new NumericQuaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
    }
}
