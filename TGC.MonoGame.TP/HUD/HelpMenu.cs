using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
namespace TGC.MonoGame.TP.HUD
{
    
    public class HelpMenu
    {
        private KeyboardState _previousKeyboard;

        private readonly Texture2D _pixel;
        private readonly SpriteFont _font;

        public bool BackToMenu { get; set; }

         
        public HelpMenu(Texture2D pixel, SpriteFont font)
        {
            _pixel = pixel;
            _font = font;
        }

        public void Draw(SpriteBatch spriteBatch)
{
    int screenWidth =
        spriteBatch.GraphicsDevice.Viewport.Width;

    int screenHeight =
        spriteBatch.GraphicsDevice.Viewport.Height;

    spriteBatch.Begin();

    // Fondo
    spriteBatch.Draw(
        _pixel,
        new Rectangle(
            0,
            0,
            screenWidth,
            screenHeight),
        Color.Black * 0.9f);

    // Título
    string titulo = "AYUDA";

    Vector2 titleSize =
        _font.MeasureString(titulo);

    spriteBatch.DrawString(
        _font,
        titulo,
        new Vector2(
            (screenWidth - titleSize.X) / 2,
            30),
        Color.Gold);

    // ---------------- OBJETIVO ----------------

    string objetivo =
@"      OBJETIVO

Destrui todos los tanques
enemigos antes de que el
tiempo llegue a cero.";

    Vector2 objetivoSize =
        _font.MeasureString(objetivo);

    spriteBatch.DrawString(
        _font,
        objetivo,
        new Vector2(
            (screenWidth - objetivoSize.X) / 2,
            100),
        Color.White);

    // ---------------- CONTROLES ----------------

    string controles =
@"CONTROLES

W  - Avanzar
S  - Retroceder
A  - Girar izquierda
D  - Girar derecha

Mouse - Apuntar
X     - Disparar
C     - Cambiar camara
ESC   - Pausa";

    spriteBatch.DrawString(
        _font,
        controles,
        new Vector2(
            80,
            350),
        Color.Cyan);

    // ---------------- HUD Y REGLAS ----------------

    string reglas =
@"HUD

Barra roja     -> Vida
Barra verde    -> Recarga
Minimapa       -> Enemigos
Temporizador   -> Tiempo

VICTORIA

Eliminar todos los enemigos

DERROTA

Perder toda la vida
o quedarse sin tiempo";

    spriteBatch.DrawString(
        _font,
        reglas,
        new Vector2(
            screenWidth / 2 + 500,
            260),
        Color.LightGreen);

    // ---------------- VOLVER ----------------

    string volver =
        "ENTER PARA VOLVER";

    Vector2 volverSize =
        _font.MeasureString(volver);

    spriteBatch.DrawString(
        _font,
        volver,
        new Vector2(
            (screenWidth - volverSize.X) / 2,
            screenHeight - 80),
        Color.Yellow);

    spriteBatch.End();
}
        public void Update()
        {
            KeyboardState keyboard = Keyboard.GetState();


            if (keyboard.IsKeyDown(Keys.Enter) &&
        _previousKeyboard.IsKeyUp(Keys.Enter))
    {
        BackToMenu = true;
    }

    _previousKeyboard = keyboard;

            

        }
    }
}