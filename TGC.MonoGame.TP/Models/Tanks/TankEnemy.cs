using BepuPhysics;
using Microsoft.Xna.Framework;
using System;
using System.Numerics;

namespace TGC.MonoGame.TP.Models;

public abstract class TankEnemy : TankBase
{
    protected Microsoft.Xna.Framework.Vector3 _targetPosition;
    private float attackRadius = GameConfig.Enemies.AttackRadius;
    private float attackRadiusSq => attackRadius * attackRadius;
    protected float _currentShootCooldown = 0f;
    public float ShootCooldown { get; protected set; }

    protected TankEnemy(float hp, float speed, float force, float turnSpeed, float damage, float cooldown)
    {
        HealthPoints = hp;
        MaxSpeed = speed;
        MotorForce = force;
        TurnSpeed = turnSpeed;
        ForwardDrag = GameConfig.Tank.ForwardDrag;
        LateralDrag = GameConfig.Tank.LateralDrag;
        AttackDamage = damage;
        ShootCooldown = cooldown;
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
        float worldAngleToPlayer = MathF.Atan2(-toTarget.X, -toTarget.Z);
        _turretRotation = worldAngleToPlayer - RotationY;
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
        var numericsToTarget = toTarget.ToNumerics();
        if (_currentShootCooldown <= 0f && dist > 15f &&
            (System.Numerics.Vector3.Dot(numericsToTarget, numericsToTarget) < attackRadiusSq)) // Distancia mínima para no dispararse a sí mismo
        {
            var dir = CannonForward;
            var spawnPos = Position + dir * 3f + Microsoft.Xna.Framework.Vector3.Up * 2f;

            // Agrega la bala a la lista global del juego
            TGCGame.Instance.Cannonballs.Add(TGCGame.Instance.CreateCannonball(spawnPos, dir));
            _currentShootCooldown = ShootCooldown;
        }
    }

    // Posicion inicial aleatoria (sin cambios)
    public Microsoft.Xna.Framework.Vector3 GetPosition(Terrain terrain, Random random)
    {
        var min = -terrain.WidthUnits;
        var max = terrain.WidthUnits;
        var x = random.NextSingle() * (max - min) + min;
        var z = random.NextSingle() * (max - min) + min;
        return new Microsoft.Xna.Framework.Vector3(x, terrain.GetHeight(x, z) + GameConfig.Tank.SpawnZMargin, z);
    }
}