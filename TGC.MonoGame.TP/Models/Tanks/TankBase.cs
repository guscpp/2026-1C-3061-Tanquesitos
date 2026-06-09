using BepuPhysics;
using BepuPhysics.Collidables;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace TGC.MonoGame.TP.Models.Tanks;

public abstract class TankBase
{
    protected Effect _effect;
    protected Texture2D _texture;
    public Model Model { get; protected set; }

    public float MaxSpeed { get; set; }
    public float MotorForce { get; set; }
    public float TurnSpeed { get; set; }
    public float ForwardDrag { get; set; }
    public float LateralDrag { get; set; }
    public float HealthPoints { get; set; }
    public float AttackDamage { get; set; }

    public Microsoft.Xna.Framework.Vector3 Position { get; set; } = Microsoft.Xna.Framework.Vector3.Zero;
    public float RotationY { get; protected set; }
    public bool IsDead { get; protected set; }
    public BodyHandle TankHandler;

    protected System.Numerics.Quaternion _physicsOrientation = System.Numerics.Quaternion.Identity;
    protected float _turretRotation = 0f;
    protected float _cannonRotation = 0f;
    protected readonly float _minCannonPitch = MathHelper.ToRadians(-20f);
    protected readonly float _maxCannonPitch = MathHelper.ToRadians(10f);

    public float TurretRotationWorld => RotationY + _turretRotation;
    public float CannonRotation => _cannonRotation;

    public Microsoft.Xna.Framework.Vector3 CannonForward
    {
        get
        {
            var rot = Matrix.CreateRotationX(_cannonRotation) * Matrix.CreateRotationY(TurretRotationWorld);
            return Microsoft.Xna.Framework.Vector3.Transform(Microsoft.Xna.Framework.Vector3.Forward, rot);
        }
    }

    public Matrix WorldMatrix =>
        Matrix.CreateScale(GameConfig.Tank.TankScale) *
        Matrix.CreateRotationX(MathHelper.ToRadians(-90f)) *
        Matrix.CreateFromQuaternion(new Microsoft.Xna.Framework.Quaternion(_physicsOrientation.X, _physicsOrientation.Y, _physicsOrientation.Z, _physicsOrientation.W)) *
        Matrix.CreateTranslation(Position);

    public Matrix TurretWorld => Matrix.CreateRotationZ(_turretRotation) * WorldMatrix;

    public Matrix CannonWorld => Matrix.CreateTranslation(0f, 0f, -1.5f) * Matrix.CreateRotationX(_cannonRotation) * Matrix.CreateTranslation(0f, 0f, 1.5f) * TurretWorld;

    public Vector3 CannonMuzzlePosition => Vector3.Transform(new Vector3(0f, 1.5f, 2f), CannonWorld);

    public void Load(Model model, Texture2D texture, Effect effect, Simulation simulation)
    {
        Model = model; _effect = effect; _texture = texture;
        foreach (var mesh in Model.Meshes)
            foreach (var part in mesh.MeshParts) part.Effect = _effect;
        CreatePhysicsBody(simulation);
    }

    protected virtual void CreatePhysicsBody(Simulation simulation)
    {
        using var compoundBuilder = new CompoundBuilder(simulation.BufferPool, simulation.Shapes, 3);
        var chassisBox = new Box(GameConfig.Tank.PhysicsChassisWidth, GameConfig.Tank.PhysicsChassisHeight, GameConfig.Tank.PhysicsChassisLength);
        var turretBox = new Box(GameConfig.Tank.PhysicsTurretWidth, GameConfig.Tank.PhysicsTurretHeight, GameConfig.Tank.PhysicsTurretLength);

       // Reducimos de 2.6f a 2.2f de ancho y largo
      //veooooooooooooooooooooooooooooooooooooo si queda bien
        compoundBuilder.Add(new Box(2.2f, 0.3f, 2.2f), new RigidPose(new System.Numerics.Vector3(0, -0.9f, 0), System.Numerics.Quaternion.Identity), 6000f);
        compoundBuilder.Add(chassisBox, new RigidPose(new System.Numerics.Vector3(0, -0.4f, 0), System.Numerics.Quaternion.Identity), GameConfig.Tank.ChassisMass);
        compoundBuilder.Add(turretBox, new RigidPose(new System.Numerics.Vector3(0, GameConfig.Tank.PhysicsTurretOffsetY, 0), System.Numerics.Quaternion.Identity), GameConfig.Tank.TurretMass);

        compoundBuilder.BuildDynamicCompound(out var children, out var inertia, out var center);
        var shapeIdx = simulation.Shapes.Add(new Compound(children));

        TankHandler = simulation.Bodies.Add(BodyDescription.CreateDynamic(
            new RigidPose(new System.Numerics.Vector3(Position.X, Position.Y, Position.Z) + center, System.Numerics.Quaternion.Identity),
            inertia, new CollidableDescription(shapeIdx, 0.1f), new BodyActivityDescription(0.01f)));
    }

    public void HandleHealth(float damage)
    {
        HealthPoints -= damage;
        if (HealthPoints <= 0) { HealthPoints = 0; IsDead = true; }
    }

    public virtual void Draw(Microsoft.Xna.Framework.Matrix view, Microsoft.Xna.Framework.Matrix projection)
    {
        if (Model == null || IsDead) return;

        foreach (var mesh in Model.Meshes)
        {
            var finalWorld = WorldMatrix;
            if (mesh.Name.Contains("Cabeza") || mesh.Name.Contains("Antena") || mesh.Name.Contains("Pistola")) finalWorld = TurretWorld;
            else if (mesh.Name.Contains("Cañon") || mesh.Name.Contains("Anillo")) finalWorld = CannonWorld;

            foreach (var eff in mesh.Effects)
            {
                eff.Parameters["World"].SetValue(finalWorld);
                eff.Parameters["View"].SetValue(view);
                eff.Parameters["Projection"].SetValue(projection);
                eff.Parameters["ModelTexture"].SetValue(_texture);
            }
            mesh.Draw();
        }
    }

    // Método protegido que aplica física Bepu. Lo llaman Player y Enemy.
    // Corregido para que los tanques no levanten vuelo como un avion cuando la trompa apunta hacia arriba
    protected void ApplyPhysics(Simulation simulation, float dt, float forwardInput, float turnInput)
    {
        if (IsDead) return;

        var body = simulation.Bodies.GetBodyReference(TankHandler);
        var orientation = body.Pose.Orientation;

        // Obtener el vector forward 3D real del tanque segun su rotacion actual
        var forward3D = System.Numerics.Vector3.Transform(new System.Numerics.Vector3(0, 0, -1), orientation);

        // Aplanar el vector al plano horizontal (X, Z) para evitar que el motor lo levante
        var forward = new System.Numerics.Vector3(forward3D.X, 0f, forward3D.Z);
        if (forward.LengthSquared() > 0.001f)
        {
            forward = System.Numerics.Vector3.Normalize(forward);
        }
        else
        {
            forward = new System.Numerics.Vector3(0f, 0f, -1f); // Fallback por si mira exactamente al cielo/suelo
        }

        // Hacer lo mismo con el vector lateral (right) para que el arrastre lateral tampoco lo levante
        var right3D = System.Numerics.Vector3.Cross(new System.Numerics.Vector3(0f, 1f, 0f), forward3D);
        var right = new System.Numerics.Vector3(right3D.X, 0f, right3D.Z);
        if (right.LengthSquared() > 0.001f)
        {
            right = System.Numerics.Vector3.Normalize(right);
        }
        else
        {
            right = new System.Numerics.Vector3(1f, 0f, 0f); // Fallback
        }

        var vel = body.Velocity.Linear;
        float fwdSpeed = System.Numerics.Vector3.Dot(vel, forward);
        float rightSpeed = System.Numerics.Vector3.Dot(vel, right);

        // Las fuerzas ahora se calculan exclusivamente sobre el plano horizontal
        var motorForce = forward * MotorForce * forwardInput;
        var dragForce = -forward * (ForwardDrag * fwdSpeed) - right * (LateralDrag * rightSpeed);

        body.ApplyLinearImpulse((motorForce + dragForce) * dt);

        // Control de giro (solo modificar el eje Y, dejando que la fisica maneje X y Z por los choques)
        ref var angVel = ref body.Velocity.Angular;
        angVel.Y = turnInput * TurnSpeed;

        // Amortiguador para evitar que el tanque quede girando como loco en X y Z tras un choque
        angVel.X = MathHelper.Clamp(angVel.X, -0.5f, 0.5f);
        angVel.Z = MathHelper.Clamp(angVel.Z, -0.3f, 0.3f);
        angVel.X *= 0.88f;
        angVel.Y *= 0.98f;
        angVel.Z *= 0.88f;

        body.Awake = true;

        // Actualizar variables de estado para el render y la logica de juego
        var pose = body.Pose;
        Position = new Microsoft.Xna.Framework.Vector3(pose.Position.X, pose.Position.Y, pose.Position.Z);
        _physicsOrientation = pose.Orientation;

        // Extraer el angulo de yaw para la rotacion de la torreta y camara
        float sinYaw = 2f * (_physicsOrientation.W * _physicsOrientation.Y + _physicsOrientation.X * _physicsOrientation.Z);
        float cosYaw = 1f - 2f * (_physicsOrientation.X * _physicsOrientation.X + _physicsOrientation.Y * _physicsOrientation.Y);
        RotationY = MathF.Atan2(sinYaw, cosYaw);
    }
}