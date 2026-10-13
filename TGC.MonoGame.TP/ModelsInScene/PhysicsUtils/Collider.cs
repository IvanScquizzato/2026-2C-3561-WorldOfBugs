using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Renderers;
using System.Collections.Concurrent;
using BepuPhysics;
using TGC.MonoGame.TP.Tanks;
using TGC.MonoGame.TP.Physics.Bepu;
using NumericVector3 = System.Numerics.Vector3;
using TGC.MonoGame.Utils;
using System.Diagnostics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using TGC.MonoGame.TP.ColliderRenderers;
using System.Linq;

namespace TGC.MonoGame.TP.Colliders
{
    public class Collider
    {
        private BodyReference _internalBodyRef;

        protected BodyReference _bodyReference
        {
            get
            {
                return _internalBodyRef;
            }
            set
            {
                _internalBodyRef = value;
                createChildColliders();
            }
        }
        private StaticReference _internalStaticRef;

        protected StaticReference _staticReference
        {
            set
            {
                _internalStaticRef = value;
                createChildColliders();
            }
            get
            {
                return _internalStaticRef;
            }
        }
        protected bool _isStatic = false;
        public Vector3 _centerOfMass;
        public Simulation _simulation;
        protected Dictionary<CollidableReference, object> _physicsToGameObjects;
        protected BufferPool _bufferPool;
        protected Effect _effect;
        protected GraphicsDevice _graphicsDevice;
        protected ColliderRenderer _colliderRenderer;
        public List<Collider> _childColliders { get; set; } = new List<Collider>();
        public Collider parentCollider { get; set; }
        private bool _isChildCollider = false;
        private CompoundChild childObjectOfThis;
        public Collider(Simulation simulation, BufferPool bufferPool, Effect effect, GraphicsDevice graphicsDevice)
        {
            _simulation = simulation;
            _bufferPool = bufferPool;
            _effect = effect;
            _graphicsDevice = graphicsDevice;
            _colliderRenderer = new ColliderRenderer(graphicsDevice, effect);
        }
        public void Draw(Matrix view, Matrix projection)
        {
            _colliderRenderer.Draw(this, view, projection);
        }
        private void createChildColliders()
        {
            if (!_childColliders.Any())
            {
                TypedIndex shapeIndex = GetShape();

                if (shapeIndex.Type == Compound.Id)
                {
                    ref Compound compoundShape = ref _simulation.Shapes.GetShape<Compound>(shapeIndex.Index);
                    for (var i = 0; i < compoundShape.Children.Length; i++)
                    {
                        CompoundChild child = compoundShape.Children[i];

                        Collider childCollider = new Collider(_simulation, _bufferPool, _effect, _graphicsDevice)
                        {
                            parentCollider = this,
                            _isChildCollider = true,
                            childObjectOfThis = child
                        };
                        _childColliders.Add(childCollider);
                    }
                }
            }
        }
        public Matrix GetWorldMatrixNotScaled()
        {

            if (_isStatic)
            {
                if (_isChildCollider)
                {
                    StaticReference parentStatic = this.parentCollider._staticReference;

                    Vector3 parentPos = new Vector3(parentStatic.Pose.Position.X, parentStatic.Pose.Position.Y, parentStatic.Pose.Position.Z);
                    Quaternion parentRot = new Quaternion(parentStatic.Pose.Orientation.X, parentStatic.Pose.Orientation.Y, parentStatic.Pose.Orientation.Z, parentStatic.Pose.Orientation.W);
                    Vector3 localPos = new Vector3(childObjectOfThis.LocalPose.Position.X, childObjectOfThis.LocalPose.Position.Y, childObjectOfThis.LocalPose.Position.Z);
                    Quaternion localRot = new Quaternion(childObjectOfThis.LocalPose.Orientation.X, childObjectOfThis.LocalPose.Orientation.Y, childObjectOfThis.LocalPose.Orientation.Z, childObjectOfThis.LocalPose.Orientation.W);

                    return Matrix.CreateFromQuaternion(localRot) * Matrix.CreateTranslation(localPos) *
                                       Matrix.CreateFromQuaternion(parentRot) * Matrix.CreateTranslation(parentPos);
                }
                else
                {
                    StaticReference thisBody = this._staticReference;
                    Vector3 thisPos = new Vector3(thisBody.Pose.Position.X, thisBody.Pose.Position.Y, thisBody.Pose.Position.Z);
                    Quaternion thisRot = new Quaternion(thisBody.Pose.Orientation.X, thisBody.Pose.Orientation.Y, thisBody.Pose.Orientation.Z, thisBody.Pose.Orientation.W);
                    return Matrix.CreateFromQuaternion(thisRot) * Matrix.CreateTranslation(thisPos);
                }
            }
            else
            {
                if (_isChildCollider)
                {
                    BodyReference parentBody = this.parentCollider._bodyReference;

                    Vector3 parentPos = new Vector3(parentBody.Pose.Position.X, parentBody.Pose.Position.Y, parentBody.Pose.Position.Z);
                    Quaternion parentRot = new Quaternion(parentBody.Pose.Orientation.X, parentBody.Pose.Orientation.Y, parentBody.Pose.Orientation.Z, parentBody.Pose.Orientation.W);
                    Vector3 localPos = new Vector3(childObjectOfThis.LocalPose.Position.X, childObjectOfThis.LocalPose.Position.Y, childObjectOfThis.LocalPose.Position.Z);
                    Quaternion localRot = new Quaternion(childObjectOfThis.LocalPose.Orientation.X, childObjectOfThis.LocalPose.Orientation.Y, childObjectOfThis.LocalPose.Orientation.Z, childObjectOfThis.LocalPose.Orientation.W);

                    return Matrix.CreateFromQuaternion(localRot) * Matrix.CreateTranslation(localPos) *
                   Matrix.CreateFromQuaternion(parentRot) * Matrix.CreateTranslation(parentPos);
                }
                else
                {
                    Vector3 thisPos = new Vector3(_bodyReference.Pose.Position.X, _bodyReference.Pose.Position.Y, _bodyReference.Pose.Position.Z);
                    Quaternion thisRot = new Quaternion(_bodyReference.Pose.Orientation.X, _bodyReference.Pose.Orientation.Y, _bodyReference.Pose.Orientation.Z, _bodyReference.Pose.Orientation.W);
                    return Matrix.CreateFromQuaternion(thisRot) * Matrix.CreateTranslation(thisPos);
                }
            }
        }
        public TypedIndex GetShape()
        {
            if (_isChildCollider)
            {
                return childObjectOfThis.ShapeIndex;
            }
            if (_isStatic)
            {
                return _staticReference.Static.Shape;
            }
            else
            {
                return _bodyReference.Collidable.Shape;
            }
        }
        public void ApplyImpulse(NumericVector3 impulse, NumericVector3 impulseOffset)
        {
            if (!_isStatic)
            {
                BodyReference bodyReference = _bodyReference;
                bodyReference.Awake = true;
                bodyReference.ApplyImpulse(impulse, impulseOffset);
            }
        }
        public void ApplyAngularImpulse(NumericVector3 impulse)
        {
            if (!_isStatic)
            {
                BodyReference bodyReference = _bodyReference;
                bodyReference.Awake = true;
                bodyReference.ApplyAngularImpulse(impulse);
            }
        }
        public Vector3 GetLinearVelocity()
        {
            return this._bodyReference.Velocity.Linear;
        }
        public Vector3 GetAngularVelocity()
        {
            return this._bodyReference.Velocity.Angular;
        }
        public BepuUtilities.Symmetric3x3 GetInverseInertiaTensor()
        {
            return _bodyReference.LocalInertia.InverseInertiaTensor;
        }
        public BodyHandle GetBodyHandle()
        {
            return _bodyReference.Handle;
        }
    }
}