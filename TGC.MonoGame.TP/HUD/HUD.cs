using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Tanks;
using TGC.MonoGame.TP.Trees;
using System.Collections.Generic;
using System;
using System.Linq;

namespace TGC.MonoGame.TP.HUD
{
    public class HUD
    {
        private readonly Texture2D _pixel;
        private int mapSize = 200;
        private Tank _tank;
        private readonly SpriteFont _font;

        public HUD(Texture2D pixel, Tank tank, SpriteFont font)
        {
            _pixel = pixel;
            _tank = tank;
            _font = font;
        }

        public void Draw(SpriteBatch spriteBatch, Tank player, List<Tank> tanks , float fps, float remainingTime)
        {
                
            int screenWidth =
        spriteBatch.GraphicsDevice.Viewport.Width;

    int screenHeight =
        spriteBatch.GraphicsDevice.Viewport.Height;

    float scaleX = screenWidth / 1920f;
    float scaleY = screenHeight / 1080f;

    float vidaActual = player.health;
    float vidaMaxima = player.maxHealth;

    int centerX = screenWidth / 2;
    int centerY = screenHeight / 2;

    spriteBatch.Begin();

    // ---------------- MIRA ----------------

    int crossSize = (int)(20 * scaleY);

    spriteBatch.Draw(
        _pixel,
        new Rectangle(centerX - crossSize, centerY, crossSize / 2, 2),
        Color.Red);

    spriteBatch.Draw(
        _pixel,
        new Rectangle(centerX + crossSize / 2, centerY, crossSize / 2, 2),
        Color.Red);

    spriteBatch.Draw(
        _pixel,
        new Rectangle(centerX, centerY - crossSize, 2, crossSize / 2),
        Color.Red);

    spriteBatch.Draw(
        _pixel,
        new Rectangle(centerX, centerY + crossSize / 2, 2, crossSize / 2),
        Color.Red);

    DrawCircle(
        spriteBatch,
        _pixel,
        new Vector2(centerX, centerY),
        1,
        Color.Red);

    DrawCircle(
        spriteBatch,
        _pixel,
        new Vector2(centerX, centerY),
        (int)(20 * scaleY),
        Color.Red);

    // ---------------- VIDA ----------------

    int marginX = (int)(15 * scaleX);
    int marginY = (int)(40 * scaleY);

    int healthWidth = (int)(200 * scaleX);
    int healthHeight = (int)(20 * scaleY);

    spriteBatch.Draw(
        _pixel,
        new Rectangle(
            marginX,
            marginY,
            healthWidth + 10,
            healthHeight + 10),
        Color.Black);

    spriteBatch.Draw(
        _pixel,
        new Rectangle(
            marginX + 5,
            marginY + 5,
            healthWidth,
            healthHeight),
        Color.DarkRed);

    spriteBatch.Draw(
        _pixel,
        new Rectangle(
            marginX + 5,
            marginY + 5,
            (int)((vidaActual / vidaMaxima) * healthWidth),
            healthHeight),
        Color.Red);

    spriteBatch.DrawString(
        _font,
        $"{vidaActual}/{vidaMaxima}",
        new Vector2(
            marginX,
            marginY - 35 * scaleY),
        Color.White);

    // ---------------- MINIMAPA ----------------

    mapSize = (int)(screenHeight * 0.18f);

    int minimapX =
        screenWidth - mapSize - (int)(20 * scaleX);

    int minimapY =
        (int)(20 * scaleY);

    spriteBatch.Draw(
        _pixel,
        new Rectangle(
            minimapX,
            minimapY,
            mapSize,
            mapSize),
        Color.Black * 0.5f);

    const float worldMinX = -17000f;
const float worldMaxX = 17000f;

const float worldMinZ = -17000f;
const float worldMaxZ = 17000f;

    int mapX = (int)(
    ((player.Position.X - worldMinX) /
    (worldMaxX - worldMinX))
    * mapSize);

int mapY = (int)(
    ((player.Position.Z - worldMinZ) /
    (worldMaxZ - worldMinZ))
    * mapSize);

    Vector2 dir =
        new Vector2(
            player._forward.X,
            player._forward.Z);

    dir.Normalize();

    int playerMapX = minimapX + mapX;
    int playerMapY = minimapY + mapY;

    Vector2 center =
        new Vector2(
            playerMapX + 5,
            playerMapY + 5);

    Vector2 right =
        new Vector2(-dir.Y, dir.X);

    Vector2 front =
        center + dir * (12f * scaleX);

    Vector2 leftCorner =
        center - dir * (3f * scaleX)
        - right * (5f * scaleX);

    Vector2 rightCorner =
        center - dir * (4f * scaleX)
        + right * (5f * scaleX);

    DrawLine(spriteBatch, leftCorner, front, Color.Lime);
    DrawLine(spriteBatch, rightCorner, front, Color.Lime);
    DrawLine(spriteBatch, leftCorner, rightCorner, Color.Lime);

    for (float t = 0; t <= 1f; t += 0.05f)
    {
        Vector2 p1 =
            Vector2.Lerp(front, leftCorner, t);

        Vector2 p2 =
            Vector2.Lerp(front, rightCorner, t);

        DrawLine(spriteBatch, p1, p2, Color.Lime);
    }

    foreach (var tank in tanks)
    {
        if (tank == player)
            continue;

        int enemyMapX =
            (int)(((tank.Position.X + 6000f)
                / (worldMaxX - worldMinX)) * mapSize);

        int enemyMapY =
            (int)(((tank.Position.Z + 6000f)
                / (worldMaxZ - worldMinZ)) * mapSize);

        spriteBatch.Draw(
            _pixel,
            new Rectangle(
                minimapX + enemyMapX,
                minimapY + enemyMapY,
                (int)(8 * scaleX),
                (int)(8 * scaleY)),
            Color.Red);
    }

    // ---------------- RECARGA ----------------

    int reloadY = (int)(115 * scaleY);

    spriteBatch.Draw(
        _pixel,
        new Rectangle(
            marginX,
            reloadY,
            healthWidth + 10,
            healthHeight + 10),
        Color.Black);

    spriteBatch.Draw(
        _pixel,
        new Rectangle(
            marginX + 5,
            reloadY + 5,
            healthWidth,
            healthHeight),
        Color.DarkSlateGray);

    int width =
        (int)(healthWidth * player.CooldownProgress);

    Color reloadColor =
        player.CooldownProgress >= 1f
            ? Color.DarkGreen
            : Color.SeaGreen;

    spriteBatch.Draw(
        _pixel,
        new Rectangle(
            marginX + 5,
            reloadY + 5,
            width,
            healthHeight),
        reloadColor);

    spriteBatch.DrawString(
        _font,
        "RECARGA",
        new Vector2(
            marginX,
            75 * scaleY),
        Color.White);

    if (player.CooldownProgress >= 1f)
    {
        spriteBatch.DrawString(
            _font,
            "Presione X para disparar",
            new Vector2(
                marginX,
                150 * scaleY),
            Color.GreenYellow);
    }

    spriteBatch.DrawString(
        _font,
        $"FPS: {(int)fps}",
        new Vector2(
            marginX,
            screenHeight - 50 * scaleY),
        Color.White);

        //---------------------------Enemigos Vivos -----------------------------------
        int enemigosVivos = tanks.Count(t => t.esEnemigo);
        spriteBatch.DrawString(_font,$"ENEMIGOS: {enemigosVivos}",new Vector2(marginX,190 * scaleY),Color.IndianRed);
        //---------------------------Contra Reloj -------------------------------------
        Vector2 timePos =
    new Vector2(
        screenWidth / 2 - 60,
        20);
        
        int minutes = (int)(remainingTime / 60);

        int seconds = (int)(remainingTime % 60);
        Color timeColor = remainingTime > 60f? Color.White: remainingTime > 30f? Color.Orange: Color.Red;
        spriteBatch.DrawString(
    _font,
    $"TIEMPO: {minutes:00}:{seconds:00}",
    timePos,
    timeColor);

    spriteBatch.End();

    spriteBatch.GraphicsDevice.BlendState =
        BlendState.Opaque;

    spriteBatch.GraphicsDevice.DepthStencilState =
        DepthStencilState.Default;

    spriteBatch.GraphicsDevice.RasterizerState =
        RasterizerState.CullNone;
        }

        public void DrawCircle(SpriteBatch spriteBatch, Texture2D pixel, Vector2 center, float radius, Color color)
        {
            int segments = 64;

            for (int i = 0; i < segments; i++)
            {
                float angle = MathHelper.TwoPi * i / segments;
                float x = center.X + (float)Math.Cos(angle) * radius;
                float y = center.Y + (float)Math.Sin(angle) * radius;
                spriteBatch.Draw(pixel, new Rectangle((int)x, (int)y, 2, 2), color);
            }
        }

        private void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color)
        {
            Vector2 edge = end - start;

            float angle =
                (float)Math.Atan2(edge.Y, edge.X);

            spriteBatch.Draw(_pixel, start, null, color, angle, Vector2.Zero, new Vector2(edge.Length(), 2), SpriteEffects.None, 0);
        }
    }
}
