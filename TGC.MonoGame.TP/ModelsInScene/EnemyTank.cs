using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TGC.MonoGame.TP.Tanks;
using System.Diagnostics;

public class EnemyTank
{
    private readonly Tank _tank;
    public Tank Tank => _tank;
    private float _shootCooldown = 10f;
    private float _currentCooldown = 5f;
    private float presicion = 0.55f;
    public Action<Tank>? FireMissile;
    private static Random _random = new();
    private Vector3 _patrolTarget;
    private bool _hasPatrolTarget;
    private float speed = 1f;
    private float _waitTime = 0f;
    private float _avoidTime = 0f;
    public EnemyTank(Tank tank)
    {
        _tank = tank;
    }

    public void Update(Tank player,GameTime gameTime){


     
        _tank.IsColliding = false;
        Vector3 direction = player._position - _tank._position;

        float distance = direction.Length();
        
        direction.Y = 0;

        if (direction != Vector3.Zero)
        {
            direction.Normalize();
        }

        //vector al jugador
        float targetYaw =(float)Math.Atan2(direction.X,direction.Z);

        //vector donde apunta la torreta
        float hullYaw =(float)Math.Atan2(_tank._forward.X,_tank._forward.Z);

        //calculo la distancia hasta llegar a la direccion deseada
        float turretYaw = targetYaw - hullYaw;

        //lo normalizo para que este entre 180 y -180
        turretYaw = MathF.Atan2(MathF.Sin(turretYaw),MathF.Cos(turretYaw));

        //le pido a la torreta que se vaya moviendo con la diferencia, asi se mueve mas fluido
        _tank.SetTurretRotation(turretYaw,0);

        float deltaTime =(float)gameTime.ElapsedGameTime.TotalSeconds;
        
        //recargo el cooldown
        _currentCooldown -= deltaTime;

        
        Vector3 cannonDir =_tank.GetCannonDirection();

        
        
        float aimError =Vector3.Dot(cannonDir,direction);

        //calculo hacia donde esta mirando el cuerpo del enemigo
        float currentYaw =(float)Math.Atan2(-_tank._forward.X,-_tank._forward.Z);

        //calculo el error entre donde apunta el enemigo y donde se encuentra el jugador
        float error =MathF.Atan2(MathF.Sin(targetYaw - currentYaw),MathF.Cos(targetYaw - currentYaw));

        //lo paso a una variable que va de -1 a 1
        float turnInput = MathHelper.Clamp(error * 2f, -1f, 1f);
        //distancia 
        
        if (distance > 6000f)
        {
            
            Patrol(deltaTime);
        }
        else
        {
            if (distance > 2000f)
            {
                //segun donde se encuentre el jugador va a manejar siempre para adelante y doblar cuando el cuerpo no coincida con el mismo
                //persigue al jugador
                _tank.Drive(speed, turnInput, deltaTime);
            }
            else if (distance <= 1500f)
            {
                // se aleja del jugador
                _tank.Drive(-speed, turnInput, deltaTime);
            }else
            {
                _tank.Drive(0, 0, deltaTime);
            }
        }
        if (_tank.IsColliding)
        {
            _avoidTime = 1.5f;
            if(_avoidTime > 0)
            {
                _avoidTime -= deltaTime;
                _tank.Drive(-1f, 1f, deltaTime);
                return;
            }
        }

        if (distance < 4000f && aimError > presicion  && _currentCooldown <= 0f)
        {
           //dispara
            FireMissile?.Invoke(_tank);
           _currentCooldown = _shootCooldown;
        }
    }

    private void GeneratePatrolPoint()
    {
        float x = _tank._position.X +
                  _random.Next(-5000, 5000);

        float z = _tank._position.Z +
                  _random.Next(-5000, 5000);

        _patrolTarget = new Vector3(x,_tank._position.Y,z);

        _hasPatrolTarget = true;
    }

    private void Patrol(float deltaTime)
    {
        if (!_hasPatrolTarget)
        {
            GeneratePatrolPoint();
        }

        Vector3 direction = _patrolTarget - _tank._position;

        direction.Y = 0;

        float distanceToTarget = direction.Length();

        if (distanceToTarget < 500f)
        {
            _waitTime = 3;
            if (_waitTime > 0)
            {
                _waitTime -= deltaTime;
                return;
            }
            GeneratePatrolPoint();
            return;
        }

        direction.Normalize();

        float targetYaw =(float)Math.Atan2(direction.X,direction.Z);

        float currentYaw =(float)Math.Atan2(-_tank._forward.X,-_tank._forward.Z);

        float error =MathF.Atan2(MathF.Sin(targetYaw - currentYaw),MathF.Cos(targetYaw - currentYaw));

        float turnInput =MathHelper.Clamp(error * 2f,-1f,1f);

        _tank.Drive(0.5f,turnInput,deltaTime);
    }

}