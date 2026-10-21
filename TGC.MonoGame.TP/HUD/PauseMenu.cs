using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;


namespace TGC.MonoGame.TP.HUD
{
    public class PauseMenu
    {
        private int _selectedOption = 0;
        private KeyboardState _previousKeyboard;
        public bool ResumeGame { get; set; }
        public bool StartMenu { get;  set; }
        private readonly Texture2D _pixel;
        private readonly SpriteFont _font;
        public string[] Options =
        {
            "JUGAR",
            "SALIR"
        };

        public PauseMenu(Texture2D pixel, SpriteFont font)
        {
            _pixel = pixel;
            _font = font;
        }

        public void Update()
        {
            KeyboardState keyboard = Keyboard.GetState();


            if (keyboard.IsKeyDown(Keys.Enter) && _previousKeyboard.IsKeyUp(Keys.Enter))
            {
                TGCGame.ClickSound.Play();
                if (_selectedOption == 0)
                {
                    ResumeGame = true;
                }
                if (_selectedOption == 1)
                    StartMenu = true;

            }

            if (keyboard.IsKeyDown(Keys.Down) && _previousKeyboard.IsKeyUp(Keys.Down))
            {
                TGCGame.ClickSound.Play();
                _selectedOption++;
            }

            if (keyboard.IsKeyDown(Keys.Up) && _previousKeyboard.IsKeyUp(Keys.Up))
            {
                TGCGame.ClickSound.Play();
                _selectedOption--;
            }

            _selectedOption = Math.Clamp(_selectedOption, 0, Options.Length - 1);
            _previousKeyboard = keyboard;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            //spriteBatch.GraphicsDevice.Clear(Color.White);
            int centerX = spriteBatch.GraphicsDevice.Viewport.Width / 2;
            int centerY = spriteBatch.GraphicsDevice.Viewport.Height / 2;
    
            spriteBatch.Begin();

            spriteBatch.Draw(_pixel,new Rectangle(0,0,spriteBatch.GraphicsDevice.Viewport.Width,spriteBatch.GraphicsDevice.Viewport.Height),Color.Black * 0.2f);

            spriteBatch.Draw(_pixel, new Rectangle(centerX - 205, centerY - 55, 410, 90), Color.Black);
            spriteBatch.Draw(_pixel, new Rectangle(centerX - 200, centerY - 50, 400, 80), _selectedOption == 0 ? Color.Green : Color.Gray);
            spriteBatch.DrawString(_font, "JUGAR", new Vector2(centerX - 50 , centerY - 30), Color.Black);

            spriteBatch.Draw(_pixel, new Rectangle(centerX - 205, centerY + 45, 410, 90), Color.Black);
            spriteBatch.Draw(_pixel, new Rectangle(centerX - 200, centerY + 50, 400, 80), _selectedOption == 1 ? Color.Red : Color.Gray);
            spriteBatch.DrawString(_font, "SALIR AL MENU", new Vector2(centerX - 120 , centerY + 70), Color.Black);

            


            spriteBatch.End();

            // esto protege al terreno de no renderizase mal
            spriteBatch.GraphicsDevice.BlendState = BlendState.Opaque;
            spriteBatch.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            spriteBatch.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        }
    }
}