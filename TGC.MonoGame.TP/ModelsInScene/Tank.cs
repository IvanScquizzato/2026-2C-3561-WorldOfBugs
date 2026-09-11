using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.ModelsInScene;
using TGC.MonoGame.TP.Renderers;
using Microsoft.Xna.Framework.Input;
using System.Security.Cryptography.X509Certificates;
namespace TGC.MonoGame.TP.Tanks
{
    public class Tank : ModelInScene
    {
        public float _accelerationPerSec { get; set; } = 60f;
        public float _currentAccelerationPerSec { get; set; } = 0f;
        public float _currentLeftAccelerationPerSec { get; set; } = 0f;
        public float _currentRightAccelerationPerSec { get; set; } = 0f;
        public float _currentSpeedPerSec { get; set; } = 0f;
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
                _currentLeftAccelerationPerSec -= 25f;
                _currentRightAccelerationPerSec += 25f;
            }
            if (keyboardState.IsKeyDown(Keys.D))
            {
                _currentLeftAccelerationPerSec += 25f;
                _currentRightAccelerationPerSec -= 25f;
            }

            if (MathF.Abs(_currentLeftSpeedPerSec) < _friction * elapsedSeconds)
            {
                _currentLeftSpeedPerSec = 0f; // Si la velocidad es casi 0, la clavamos en 0
            }
            else
            {
                // MathF.Sign devuelve 1 o -1, asegurando que la fricción siempre empuje en contra
                _currentLeftAccelerationPerSec -= MathF.Sign(_currentLeftSpeedPerSec) * _friction;
            }

            // Freno Oruga Derecha
            if (MathF.Abs(_currentRightSpeedPerSec) < _friction * elapsedSeconds)
            {
                _currentRightSpeedPerSec = 0f;
            }
            else
            {
                _currentRightAccelerationPerSec -= MathF.Sign(_currentRightSpeedPerSec) * _friction;
            }

            _currentLeftSpeedPerSec += _currentLeftAccelerationPerSec * elapsedSeconds;
            _currentRightSpeedPerSec += _currentRightAccelerationPerSec * elapsedSeconds;

            _angle += elapsedSeconds * (_currentLeftSpeedPerSec - _currentRightSpeedPerSec) / (-_width);

            _currentSpeedPerSec = (_currentLeftSpeedPerSec + _currentRightSpeedPerSec) * 0.5f;
            _position += new Vector3(MathF.Sin(_angle), 0, MathF.Cos(_angle)) * _currentSpeedPerSec * elapsedSeconds;

            _rotation = Matrix.CreateRotationY(_angle);
        }
    }
}