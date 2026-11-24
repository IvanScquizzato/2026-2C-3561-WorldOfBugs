using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;


namespace TGC.MonoGame.TP.HUD
{
    public class MainMenu
    {
        private int _selectedOption = 0;
        private KeyboardState _previousKeyboard;
        public bool StartGame { get; set; }

        public bool HelpMenu { get; set; }
        public bool LeaveGame { get; private set; }
        private readonly Texture2D _pixel;
        private readonly SpriteFont _font;
        public string[] Options =
        {
            "JUGAR",
            "SALIR",
            "AYUDA"
        };

        public MainMenu(Texture2D pixel, SpriteFont font)
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
                    StartGame = true;
                }
                if (_selectedOption == 2)
                    LeaveGame = true;
                if(_selectedOption == 1)
                {
                    
                    HelpMenu = true;
                }

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
            
            int centerX = spriteBatch.GraphicsDevice.Viewport.Width / 2;
int centerY = spriteBatch.GraphicsDevice.Viewport.Height / 2;

const int buttonWidth = 400;
const int buttonHeight = 80;
const int buttonBorder = 5;
const int separation = 100;

spriteBatch.Begin();

// Título
spriteBatch.DrawString(
    _font,
    "WORLD OF BUGS",
    new Vector2(centerX - 170, centerY - 220),
    Color.White);

// JUGAR
spriteBatch.Draw(
    _pixel,
    new Rectangle(
        centerX - buttonWidth / 2 - buttonBorder,
        centerY - separation - buttonBorder,
        buttonWidth + buttonBorder * 2,
        buttonHeight + buttonBorder * 2),
    Color.Black);

spriteBatch.Draw(
    _pixel,
    new Rectangle(
        centerX - buttonWidth / 2,
        centerY - separation,
        buttonWidth,
        buttonHeight),
    _selectedOption == 0 ? Color.Green : Color.Gray);

spriteBatch.DrawString(
    _font,
    "JUGAR",
    new Vector2(centerX - 45, centerY - separation + 25),
    Color.Black);

// AYUDA
spriteBatch.Draw(
    _pixel,
    new Rectangle(
        centerX - buttonWidth / 2 - buttonBorder,
        centerY - buttonBorder,
        buttonWidth + buttonBorder * 2,
        buttonHeight + buttonBorder * 2),
    Color.Black);

spriteBatch.Draw(
    _pixel,
    new Rectangle(
        centerX - buttonWidth / 2,
        centerY,
        buttonWidth,
        buttonHeight),
    _selectedOption == 1 ? Color.CornflowerBlue : Color.Gray);

spriteBatch.DrawString(
    _font,
    "AYUDA",
    new Vector2(centerX - 50, centerY + 25),
    Color.Black);

// SALIR
spriteBatch.Draw(
    _pixel,
    new Rectangle(
        centerX - buttonWidth / 2 - buttonBorder,
        centerY + separation - buttonBorder,
        buttonWidth + buttonBorder * 2,
        buttonHeight + buttonBorder * 2),
    Color.Black);

spriteBatch.Draw(
    _pixel,
    new Rectangle(
        centerX - buttonWidth / 2,
        centerY + separation,
        buttonWidth,
        buttonHeight),
    _selectedOption == 2 ? Color.Red : Color.Gray);

spriteBatch.DrawString(
    _font,
    "SALIR",
    new Vector2(centerX - 40, centerY + separation + 25),
    Color.Black);

spriteBatch.End();
        }
    }
}