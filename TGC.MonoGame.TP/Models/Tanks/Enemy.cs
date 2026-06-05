using System;
using System.Numerics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using BepuPhysics.Collidables;
using BepuUtilities;
using TGC.MonoGame.TP.Models;
using BepuPhysics;
using MathHelper = BepuUtilities.MathHelper;
using Vector3 = System.Numerics.Vector3;

namespace TGC.MonoGame.TP.Models
{
    public class Enemy : Tank //hereda el comportamiento de un tanque (movimiento, disparo, configuracion)
    {
        public float HealthPoints = GameConfig.Tank.EnemyHealthPoints; // 10 puntos de salud, cada bala quita 2
        public const float AttackRadius = GameConfig.Tank.EnemyAttackRadius;
        public const float ShootCooldown = GameConfig.Tank.EnemyCooldown;
        private float _currentShootCooldown = 0f;

        public Vector3 GetPosition(Terrain terrain, Random random)
        {
            var minHorizontal = -terrain.WidthUnits;
            var maxHorizontal = terrain.WidthUnits;
            var horizontalRange = maxHorizontal - minHorizontal;

            var x = random.NextSingle() * horizontalRange + minHorizontal;
            var z = random.NextSingle() * horizontalRange + minHorizontal;
            return new Vector3(x, terrain.GetHeight(x, z), z);
        }

        private void ShootTarget(Vector3 targetPosition, GameTime gametime, Simulation simulation)
        {
            float dt = (float)gametime.ElapsedGameTime.TotalSeconds;
            var cannonballs = TGCGame.Instance.Cannonballs;
            Microsoft.Xna.Framework.Vector3 targetDistanceToSelf = targetPosition - Position;

            // no se dispara a objetivos sobre los que estamos o que estan demasiado lejos
            if(targetDistanceToSelf.X == 0 || targetDistanceToSelf.Y == 0 || targetDistanceToSelf.Length() > 30f)  
                return;

            targetDistanceToSelf.Normalize();
            Microsoft.Xna.Framework.Vector3 direction = CannonForward;      // dirreccion actual del cañon
            direction.Normalize();
            // float alineamiento, mientras mas cerca de 1 el valor, mayor alineamiento con el objetivo
            var alignmentToTarget = Vector3.Dot(direction.ToNumerics(), targetDistanceToSelf.ToNumerics()); 

            // Solo se dispara si el cooldown lo permite y si se apunta correctamente al objetivo
            // if (alignmentToTarget > 0.95f && _currentShootCooldown <= 0f)
            if (_currentShootCooldown <= 0f)
            {
                // Posición desde donde sale la bala
                Microsoft.Xna.Framework.Vector3 spawnPosition = Position + direction * 3f + new Microsoft.Xna.Framework.Vector3(0, 1, 0) * 2f;

                Cannonball cannonball = TGCGame.Instance.CreateCannonball(spawnPosition, direction);

                cannonballs.Add(cannonball);
                _currentShootCooldown = ShootCooldown;
            }
        }

        public void UpdateEnemy(GameTime gameTime, Simulation simulation, Vector3 targetPosition, Terrain terrain)
        {
            if (IsDead) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _currentShootCooldown -= dt;
            if (_currentShootCooldown < 0f)
                _currentShootCooldown = 0f;

            var body = simulation.Bodies.GetBodyReference(TankHandler);
            // vector de enemigo a su target, importante para la direccion
            var targetDistanceToSelf = targetPosition - Position;
            var distanceToSelf = targetDistanceToSelf.Length();
            var targetDirection = Vector3.Normalize(
                new Vector3(targetDistanceToSelf.X, targetDistanceToSelf.Y, targetDistanceToSelf.Z)
            );
            // 1. Mover la torreta a la direccion del objetivo

            // si la distancia entre el enemigo y el target es mayor al radio de ataque el tanque no se moviliza
            // la torreta sigue la posicion del target
            float sensitivity = 0.0015f; //Ajusto la velocidad (sensibilidad)
                                         //mov izquiera derecha (eje y)
            var targetYaw = MathF.Atan2(-targetDirection.X, -targetDirection.Z);
            _turretRotation = targetYaw;

            var deltaY = targetDistanceToSelf.Y;
            // Determino la rotacion segun el desplazamiento del mouse y la velocidad
            _cannonRotation -= deltaY * sensitivity;

            // Le aplico una correccion al cañon en funcion de los limites que puse arriba
            _cannonRotation = MathHelper.Clamp(_cannonRotation, _minCannonPitch, _maxCannonPitch);

            // 2. Comprobar si esta dentro del alcance de ataque
            if (distanceToSelf > AttackRadius)
                return;

            // 3. Si lo esta, el tanque persigue 
            // Movimiento lineal mediante fuerzas (convertidas a impulso)
            var orientation = body.Pose.Orientation;
            // Dirección "forward" del tanque en coordenadas de mundo (sistema diestro: -Z es adelante)
            Vector3 forward = Vector3.Transform(
                new Vector3(0, 0, -1), orientation);

            // Rotacion hacia el objetivo
            // Calculo hacia donde girar: si > 0 gira hacia a la izquierda y viceversa
            float turnAmount = Vector3.Cross(forward, targetDirection).Y;
            // Aplico
            body.Velocity.Angular = new Vector3(0, turnAmount * TurnSpeed, 0);

            // Esta mirando al objetivo?
            var alignmentToTarget = Vector3.Dot(forward, targetDirection);
            if (alignmentToTarget > 0.8f) //mayor a cero, cercano a 1 (lo esta mirando)
            {
                System.Numerics.Vector3 motorForce = forward * GameConfig.Tank.EnemyMotorForce; // mas lento que el target por configuracion

                // Fuerza de arrastre (drag) proporcional a la velocidad horizontal actual
                // Obtener vectores locales del tanque (en coordenadas mundo)
                var right = System.Numerics.Vector3.Normalize(
                    System.Numerics.Vector3.Cross(new System.Numerics.Vector3(0, 1, 0), forward));

                // Descomponer la velocidad actual en componentes local
                var velocity = body.Velocity.Linear;
                float forwardSpeed = System.Numerics.Vector3.Dot(velocity, forward);
                float rightSpeed = System.Numerics.Vector3.Dot(velocity, right);

                // Calcular fuerza de arrastre en espacio local
                var dragForce = -forward * (GameConfig.Tank.ForwardDrag * forwardSpeed)
                                - right * (GameConfig.Tank.LateralDrag * rightSpeed);

                // Aplicamos el impulso equivalente a la fuerza durante este frame
                System.Numerics.Vector3 impulse = (motorForce + dragForce) * dt;
                body.ApplyLinearImpulse(impulse);
            }
            // 4. Estabilización de la carrocería
            ref var vel = ref body.Velocity;
            vel.Angular.X = MathHelper.Clamp(vel.Angular.X, -0.5f, 0.5f);
            vel.Angular.Z = MathHelper.Clamp(vel.Angular.Z, -0.3f, 0.3f);
            vel.Angular.X *= 0.88f;
            vel.Angular.Y *= 0.98f;
            vel.Angular.Z *= 0.88f;

            body.Awake = true;
            // 5. Sincronizar estado visual
            var pose = body.Pose;
            Position = new Vector3(pose.Position.X, terrain.GetHeight(pose.Position.X, pose.Position.Z), pose.Position.Z);
            _physicsOrientation = pose.Orientation;

            ShootTarget(targetPosition, gameTime, simulation);

            // Rotación en Y para la cámara
            float sinYaw = 2f * (_physicsOrientation.W * _physicsOrientation.Z + _physicsOrientation.X * _physicsOrientation.Y);
            float cosYaw = 1f - 2f * (_physicsOrientation.Y * _physicsOrientation.Y + _physicsOrientation.Z * _physicsOrientation.Z);
            RotationY = MathF.Atan2(sinYaw, cosYaw);
        }

    }
}