using System;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TGC.MonoGame.TP.ModelsInScene;
using TGC.MonoGame.TP.Renderers;
using TGC.MonoGame.TP.Forces;
namespace TGC.MonoGame.TP.Tanks
{
    public class Tank : ModelInScene
    {

        public float _engineThrust { get; set; } = 500000f;

        public float _linearDrag { get; set; } = 2.5f;

        public float _lateralFriction { get; set; } = 15f;
        public Tank(ContentManager Content, String modelPath, Vector3 position, Matrix rotation, Vector3 scale, IModelInSceneRenderer renderer, float angularDrag, float mass)
            : base(Content, modelPath, position, rotation, scale, renderer, angularDrag, mass)
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

            this.agregarFuerzaAAplicar(new Force(_forward * leftThrust * _engineThrust, leftTrackLocalPos));
            this.agregarFuerzaAAplicar(new Force(_forward * rightThrust * _engineThrust, rightTrackLocalPos));

            float sidewaysSpeed = Vector3.Dot(_velocity, _right);
            float exactForceToStop = (_mass * sidewaysSpeed) / elapsedSeconds;

            float frictionMagnitude = sidewaysSpeed * _mass * _lateralFriction;

            float appliedFrictionMag = MathF.Min(MathF.Abs(frictionMagnitude), MathF.Abs(exactForceToStop));

            appliedFrictionMag *= MathF.Sign(frictionMagnitude);

            Vector3 lateralForce = -_right * appliedFrictionMag;
            this.agregarFuerzaAAplicar(new Force(lateralForce, Vector3.Zero));

            Vector3 linearDragForce = -_velocity * _mass * _linearDrag;
            this.agregarFuerzaAAplicar(new Force(linearDragForce, Vector3.Zero));

            this.aplicarFuerzas(elapsedSeconds);
        }

    }
}