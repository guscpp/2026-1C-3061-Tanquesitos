using BepuPhysics;
using Microsoft.Xna.Framework;
using System;

namespace TGC.MonoGame.TP.Models;

public abstract class TankEnemy : TankBase
{
    protected Microsoft.Xna.Framework.Vector3 _targetPosition;
    protected float _currentShootCooldown = 0f;
    public float ShootCooldown { get; protected set; }

    protected TankEnemy(float hp, float speed, float force, float damage, float cooldown)
    {
        HealthPoints = hp; MaxSpeed = speed; MotorForce = force;
        ForwardDrag = GameConfig.Tank.ForwardDrag; LateralDrag = GameConfig.Tank.LateralDrag;
        TurnSpeed = GameConfig.Tank.TurnSpeed; AttackDamage = damage; ShootCooldown = cooldown;
    }

    // Mantengo la firma original para no tocar TGCGame.cs
    public void UpdateEnemy(GameTime gameTime, Simulation simulation, Microsoft.Xna.Framework.Vector3 targetPos, Terrain terrain)
    {
        if (IsDead) return;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _targetPosition = targetPos;
        _currentShootCooldown -= dt;

        var body = simulation.Bodies.GetBodyReference(TankHandler);
        var currentPos = new Microsoft.Xna.Framework.Vector3(body.Pose.Position.X, body.Pose.Position.Y, body.Pose.Position.Z);
        var toTarget = _targetPosition - currentPos;
        float dist = toTarget.Length();

        // 1. APUNTAR TORRETA Y CAÑÓN DIRECTAMENTE AL JUGADOR
        _turretRotation = MathF.Atan2(-toTarget.X, -toTarget.Z);
        _cannonRotation = MathHelper.Clamp(-toTarget.Y * 0.015f, _minCannonPitch, _maxCannonPitch);

        // 2. MOVER EN LÍNEA RECTA HACIA EL JUGADOR
        var orientation = body.Pose.Orientation;
        var forward = System.Numerics.Vector3.Transform(new System.Numerics.Vector3(0, 0, -1), orientation);
        var forwardXna = new Microsoft.Xna.Framework.Vector3(forward.X, forward.Y, forward.Z);

        // Calcular ángulo entre la dirección actual del chasis y el jugador
        float angleToTarget = MathF.Atan2(
            Microsoft.Xna.Framework.Vector3.Cross(forwardXna, toTarget).Y,
            Microsoft.Xna.Framework.Vector3.Dot(forwardXna, toTarget)
        );

        // Gira hacia el objetivo (max 1.0 para no girar a velocidad infinita)
        float turnInput = Math.Sign(angleToTarget) * Math.Min(MathF.Abs(angleToTarget), 1f);
        // Avanza constante si no está pegado al jugador
        float forwardInput = (dist > 5f) ? 1f : 0f;

        ApplyPhysics(simulation, dt, forwardInput, turnInput);

        // 3. DISPARAR CONSTANTEMENTE CUANDO EL COOLDOWN LO PERMITA
        if (_currentShootCooldown <= 0f && dist > 15f) // Distancia mínima para no dispararse a sí mismo
        {
            var dir = CannonForward;
            var spawnPos = Position + dir * 3f + Microsoft.Xna.Framework.Vector3.Up * 2f;

            // Agrega la bala a la lista global del juego
            TGCGame.Instance.Cannonballs.Add(TGCGame.Instance.CreateCannonball(spawnPos, dir));
            _currentShootCooldown = ShootCooldown;
        }
    }

    // Posición inicial aleatoria (sin cambios)
    public Microsoft.Xna.Framework.Vector3 GetPosition(Terrain terrain, Random random)
    {
        var min = -terrain.WidthUnits;
        var max = terrain.WidthUnits;
        var x = random.NextSingle() * (max - min) + min;
        var z = random.NextSingle() * (max - min) + min;
        return new Microsoft.Xna.Framework.Vector3(x, terrain.GetHeight(x, z) + GameConfig.Tank.SpawnZMargin, z);
    }
}