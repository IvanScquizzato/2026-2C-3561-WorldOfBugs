using System;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TGC.MonoGame.TP.ModelsInScene;
using TGC.MonoGame.TP.Renderers;
using TGC.MonoGame.TP.Forces;
using TGC.MonoGame.TP.Contact;
using System.Collections.Generic;
using NumericVector = System.Numerics.Vector3;
namespace TGC.MonoGame.TP.Tanks
{
    public class Tank : ModelInScene
    {

        public List<ContactPoint> leftContactPoints { get; } = new List<ContactPoint>();
        public List<ContactPoint> rightContactPoints { get; } = new List<ContactPoint>();

        public float _engineThrust { get; set; } = 5000000f;

        public float _lateralFriction { get; set; } = 15f;
        public Tank(ContentManager Content, String modelPath, Vector3 position, Matrix rotation, Vector3 scale, IModelInSceneRenderer renderer, float linearDrag, float angularDrag, float mass)
            : base(Content, modelPath, position, rotation, scale, renderer, linearDrag, angularDrag, mass)
        {
        }
        public void Update(GameTime gameTime)
        {
            float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (elapsedSeconds <= 0.0001f)
                return;
            float leftThrust = 0f;
            float rightThrust = 0f;
            KeyboardState keyboardState = Keyboard.GetState();

            if (keyboardState.IsKeyDown(Keys.W)) { leftThrust += 1; rightThrust += 1; }
            if (keyboardState.IsKeyDown(Keys.S)) { leftThrust -= 1; rightThrust -= 1; }

            if (keyboardState.IsKeyDown(Keys.D)) { leftThrust += 1; rightThrust -= 1; }
            if (keyboardState.IsKeyDown(Keys.A)) { leftThrust -= 1; rightThrust += 1; }

            Vector3 leftTrackLocalPos = -_right * Width / 2f;
            Vector3 rightTrackLocalPos = _right * Width / 2f;

            var leftContactPointsSize = leftContactPoints.Count;
            var rightContactPointsSize = rightContactPoints.Count;

            foreach (ContactPoint contact in leftContactPoints)
            {
                Vector3 forceDirection = Vector3.Normalize(_forward - contact._normal * Vector3.Dot(_forward, contact._normal) / MathF.Pow(contact._normal.Length(), 2));
                Vector3 forceInThatPoint = forceDirection * (leftThrust * _engineThrust / leftContactPointsSize);
                this.agregarFuerzaAAplicar(new Force(forceInThatPoint, contact._point), elapsedSeconds);
            }
            foreach (ContactPoint contact in rightContactPoints)
            {
                Vector3 forceDirection = Vector3.Normalize(_forward - contact._normal * Vector3.Dot(_forward, contact._normal) / MathF.Pow(contact._normal.Length(), 2));
                Vector3 forceInThatPoint = forceDirection * (rightThrust * _engineThrust / rightContactPointsSize);
                this.agregarFuerzaAAplicar(new Force(forceInThatPoint, contact._point), elapsedSeconds);
            }


            leftContactPoints.Clear();
            rightContactPoints.Clear();

            float sidewaysSpeed = Vector3.Dot(_velocity, _right);
            float exactForceToStop = (_mass * sidewaysSpeed) / elapsedSeconds;

            float frictionMagnitude = sidewaysSpeed * _mass * _lateralFriction;

            float appliedFrictionMag = MathF.Min(MathF.Abs(frictionMagnitude), MathF.Abs(exactForceToStop));

            appliedFrictionMag *= MathF.Sign(frictionMagnitude);

            Vector3 lateralForce = -_right * appliedFrictionMag;
            this.agregarFuerzaAAplicar(new Force(lateralForce, Vector3.Zero), elapsedSeconds);

            //this.aplicarFuerzas(elapsedSeconds);
        }
        public void AddLeftContact(ContactPoint contact)
        {
            leftContactPoints.Add(contact);
        }
        public void AddRightContact(ContactPoint contact)
        {
            rightContactPoints.Add(contact);
        }
        public override void Draw(Matrix view, Matrix projection)
        {
            _renderer.Draw(this, view, projection);
        }
        public override Vector3 Correction()
        {
            return -_up * Height * 0.5f;
        }
    }

}