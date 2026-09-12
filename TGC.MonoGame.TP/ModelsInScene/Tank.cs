using System;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TGC.MonoGame.TP.ModelsInScene;
using TGC.MonoGame.TP.Renderers;
namespace TGC.MonoGame.TP.Tanks
{
    public class Tank : ModelInScene
    {
        public enum EstadoDeMovimiento
        {
            AVANZANDO_IZQUIERDA,
            AVANZANDO_DERECHA,
            RETROCEDIENDO_IZQUIERDA,
            RETROCEDIENDO_DERECHA,
            AVANZANDO,
            RETROCEDIENDO,
            QUIETO
        }
        public EstadoDeMovimiento _estado = EstadoDeMovimiento.QUIETO;
        public float _accelerationPerSec { get; set; } = 200f;
        public float _maxSpeed { get; set; } = 5000f;
        public float _maxSpeedDifference { get; set; } = 500f;
        public float _currentAccelerationPerSec { get; set; } = 0f;
        public float _currentLeftAccelerationPerSec { get; set; } = 0f;
        public float _currentRightAccelerationPerSec { get; set; } = 0f;
        public float _currentSpeedPerSec { get; set; } = 0f;
        public float _accelerationToRotateMultiplier { get; set; } = 0.2f;
        public float _stabilizerAccelerationMultiplier { get; set; } = 0.1f;
        public float _friction { get; set; } = 50f;
        public float _angle { get; set; } = 0f;
        public float _currentLeftSpeedPerSec { get; set; } = 0f;
        public float _currentRightSpeedPerSec { get; set; } = 0f;
        public Tank(ContentManager Content, String modelPath, Vector3 position, Matrix rotation, Vector3 scale, IModelInSceneRenderer renderer)
            : base(Content, modelPath, position, rotation, scale, renderer)
        {
        }
        public void Update(GameTime gameTime)
        {
            float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            this.setUserAccelerations();
            this.stabilize();
            this.addFriction(elapsedSeconds);

            _currentLeftSpeedPerSec += _currentLeftAccelerationPerSec * elapsedSeconds;
            _currentRightSpeedPerSec += _currentRightAccelerationPerSec * elapsedSeconds;

            this.limitSpeeds();

            _angle += elapsedSeconds * (_currentLeftSpeedPerSec - _currentRightSpeedPerSec) / (-Width);

            _currentSpeedPerSec = (_currentLeftSpeedPerSec + _currentRightSpeedPerSec) * 0.5f;

            _position += new Vector3(MathF.Sin(_angle), 0, MathF.Cos(_angle)) * _currentSpeedPerSec * elapsedSeconds;

            _rotation = Matrix.CreateRotationY(_angle);
        }
        private void setUserAccelerations()
        {
            float accelerationToRotate = MathF.Abs(_currentSpeedPerSec) * _accelerationToRotateMultiplier;

            KeyboardState keyboardState = Keyboard.GetState();

            _currentLeftAccelerationPerSec = 0;
            _currentRightAccelerationPerSec = 0;
            if (keyboardState.IsKeyDown(Keys.W))
            {
                _currentLeftAccelerationPerSec = _accelerationPerSec;
                _currentRightAccelerationPerSec = _accelerationPerSec;
            }

            if (keyboardState.IsKeyDown(Keys.S))
            {
                _currentLeftAccelerationPerSec = -_accelerationPerSec;
                _currentRightAccelerationPerSec = -_accelerationPerSec;
            }
            if (keyboardState.IsKeyDown(Keys.A))
            {
                _currentRightAccelerationPerSec += accelerationToRotate;
                _currentLeftAccelerationPerSec -= accelerationToRotate;
            }
            if (keyboardState.IsKeyDown(Keys.D))
            {
                _currentLeftAccelerationPerSec += accelerationToRotate;
                _currentRightAccelerationPerSec -= accelerationToRotate;
            }

        }
        private void stabilize()
        {
            float stabilizerAcceleration = MathF.Abs(_currentSpeedPerSec) * _stabilizerAccelerationMultiplier;

            KeyboardState keyboardState = Keyboard.GetState();
            if (!keyboardState.IsKeyDown(Keys.A) && !keyboardState.IsKeyDown(Keys.D))
            {
                float speedDifference = _currentLeftSpeedPerSec - _currentRightSpeedPerSec;
                if (MathF.Abs(speedDifference) > 0.1f)
                {
                    float correction = MathF.Sign(speedDifference) * stabilizerAcceleration;
                    _currentLeftAccelerationPerSec -= correction;
                    _currentRightAccelerationPerSec += correction;
                }

            }
        }
        private void addFriction(float elapsedSeconds)
        {
            float frictionDrop = _friction * elapsedSeconds;

            if (MathF.Abs(_currentLeftSpeedPerSec) <= frictionDrop)
                _currentLeftSpeedPerSec = 0f;
            else
                _currentLeftSpeedPerSec -= MathF.Sign(_currentLeftSpeedPerSec) * frictionDrop;

            if (MathF.Abs(_currentRightSpeedPerSec) <= frictionDrop)
                _currentRightSpeedPerSec = 0f;
            else
                _currentRightSpeedPerSec -= MathF.Sign(_currentRightSpeedPerSec) * frictionDrop;
        }
        private void limitSpeeds()
        {
            _currentLeftSpeedPerSec = Math.Clamp(_currentLeftSpeedPerSec, -_maxSpeed, _maxSpeed);
            _currentRightSpeedPerSec = Math.Clamp(_currentRightSpeedPerSec, -_maxSpeed, _maxSpeed);

            var speedDiference = MathF.Abs(_currentLeftSpeedPerSec - _currentRightSpeedPerSec);
            if (speedDiference > _maxSpeedDifference)
            {
                if (Math.Abs(_currentLeftSpeedPerSec) > Math.Abs(_currentRightSpeedPerSec))
                {
                    _currentLeftSpeedPerSec -= Math.Sign(_currentLeftSpeedPerSec) * (speedDiference - _maxSpeedDifference);
                }
                else
                {
                    _currentRightSpeedPerSec -= Math.Sign(_currentRightSpeedPerSec) * (speedDiference - _maxSpeedDifference);
                }
            }
        }
    }
}