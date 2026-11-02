using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TGC.MonoGame.TP.Tanks;
using System.Diagnostics;

public class EnemyTank
{
    private readonly Tank _tank;

    public EnemyTank(Tank tank)
    {
        _tank = tank;
    }

    public void Update(Tank player,GameTime gameTime){
        Debug.WriteLine(_tank._velocity.Length());

        Vector3 direction = player._position - _tank._position;

        float distance = direction.Length();

        direction.Y = 0;

        if (direction != Vector3.Zero)
        {
            direction.Normalize();
        }

        //guardo la ubicacion del jugador a partir de sus vectores respecto a los enemigos
        float yaw =(float)Math.Atan2(direction.X,direction.Z);

        //apunto las torretas enemigas hacia el jugador
        _tank.SetTurretRotation(yaw,0);

        float deltaTime =(float)gameTime.ElapsedGameTime.TotalSeconds;

        //distancia 
        float speed = 200f;

        if (distance > 1500f)
        {
            _tank.Drive(1f, 0f, deltaTime );

            _tank._position += direction * 100f * deltaTime;
            _tank.MoveForward(_tank._engineThrust * 0.5f,deltaTime);
        }


    }

}