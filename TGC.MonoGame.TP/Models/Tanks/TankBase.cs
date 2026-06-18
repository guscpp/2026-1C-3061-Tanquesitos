using BepuPhysics;
using BepuPhysics.Collidables;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TGC.MonoGame.TP.Gizmos;

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
    public GameConfig.TankClass TankClass { get; protected set; }

    public Vector3 Position { get; set; } = Vector3.Zero;
    public float RotationY { get; protected set; }
    public bool IsDead { get; protected set; }
    public BodyHandle TankHandler;

    protected GraphicsDevice _graphicsDevice;

    protected System.Numerics.Quaternion _physicsOrientation = System.Numerics.Quaternion.Identity;
    protected float _turretRotation = 0f;
    protected float _cannonRotation = 0f;
    protected readonly float _minCannonPitch = MathHelper.ToRadians(GameConfig.Tank.MinCannonPitch);
    protected readonly float _maxCannonPitch = MathHelper.ToRadians(GameConfig.Tank.MaxCannonPitch);

    public float TurretRotationWorld => RotationY + _turretRotation;
    public float CannonRotation => _cannonRotation;

    public Vector3 CannonForward
    {
        get
        {
            var rot = Matrix.CreateRotationX(_cannonRotation) * Matrix.CreateRotationY(TurretRotationWorld);
            return Vector3.Transform(Vector3.Forward, rot);
        }
    }

    public Matrix WorldMatrix =>
        Matrix.CreateScale(GameConfig.Tank.TankScale) *
        Matrix.CreateRotationX(MathHelper.ToRadians(-90f)) *
        Matrix.CreateFromQuaternion(new Quaternion(
            _physicsOrientation.X, 
            _physicsOrientation.Y, 
            _physicsOrientation.Z, 
            _physicsOrientation.W)) *
        Matrix.CreateTranslation(Position + new Vector3(0, GameConfig.Tank.VisualOffsetY, 0));

    public Matrix TurretWorld => Matrix.CreateRotationZ(_turretRotation) * WorldMatrix;

    public Matrix CannonWorld => Matrix.CreateTranslation(0f, 0f, -1.5f) * Matrix.CreateRotationX(_cannonRotation) * Matrix.CreateTranslation(0f, 0f, 1.5f) * TurretWorld;

    public Vector3 CannonMuzzlePosition => Vector3.Transform(
        new Vector3(0f, GameConfig.Tank.CannonMuzzleOffsetY, GameConfig.Tank.CannonMuzzleOffsetZ), CannonWorld);

    protected Color GetTankColor()
    {
        switch (TankClass)
        {
            case GameConfig.TankClass.Scout:
                return new Color(50, 205, 50);
            case GameConfig.TankClass.Medium:
                return new Color(255, 215, 0);
            case GameConfig.TankClass.Heavy:
                return new Color(178, 34, 34);
            default:
                return new Color(255, 255, 255);
        }
    }

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
        compoundBuilder.Add(
            new Box(GameConfig.Tank.Stabilizer.Width, GameConfig.Tank.Stabilizer.Height, GameConfig.Tank.Stabilizer.Length), 
            new RigidPose(new System.Numerics.Vector3(0, GameConfig.Tank.Stabilizer.YOffset, 0), System.Numerics.Quaternion.Identity), 
            GameConfig.Tank.Stabilizer.Mass);
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
        if (HealthPoints <= 0) { 
            HealthPoints = 0;
            if (!(this is TankPlayer)) TGCGame.Instance.EnemiesKilled++;
            IsDead = true; }
    }

    public virtual void Draw(Matrix view, Matrix projection)
    {
        if (Model == null || IsDead) return;

        _effect.CurrentTechnique = _effect.Techniques["DrawShadowedHibrido"];

        var smm = TGCGame.Instance.ShadowMapManager;
        _effect.Parameters["View"].SetValue(view);
        _effect.Parameters["Projection"].SetValue(projection);
        _effect.Parameters["ModelTexture"].SetValue(_texture);
        _effect.Parameters["LightViewProjection"].SetValue(smm.LightViewProjection);
        _effect.Parameters["lightPosition"].SetValue(smm.LightPosition);

        Vector3 colorVector = GetTankColor().ToVector3();
        Vector3 whiteColor = Vector3.One;

        foreach (var mesh in Model.Meshes)
        {
            var finalWorld = WorldMatrix;

            if (mesh.Name.Contains("Cabeza") || mesh.Name.Contains("Antena") || mesh.Name.Contains("Pistola"))
                finalWorld = TurretWorld;

            else if (mesh.Name.Contains("Cañon") || mesh.Name.Contains("Anillo"))
                finalWorld = CannonWorld;

            if (mesh.Name.Contains("Cabeza") || mesh.Name.Contains("Anillo") || 
                mesh.Name.Contains("Proteccion_d") || mesh.Name.Contains("Proteccion_i") || 
                mesh.Name.Contains("Cuerpo") || mesh.Name.Contains("Cubre"))
                _effect.Parameters["DiffuseColor"].SetValue(colorVector);
            else
                _effect.Parameters["DiffuseColor"].SetValue(whiteColor);

            _effect.Parameters["World"].SetValue(finalWorld);
            _effect.Parameters["InverseTransposeWorld"].SetValue(
            Matrix.Transpose(Matrix.Invert(finalWorld)));

            mesh.Draw();
        }
    }

    public virtual void DrawDepth(Matrix lightViewProjection)
    {
        if (Model == null || IsDead) return;

        _effect.CurrentTechnique = _effect.Techniques["DepthPass"];

        foreach (var mesh in Model.Meshes)
        {
            var world = WorldMatrix;
            if (mesh.Name.Contains("Cabeza") || mesh.Name.Contains("Antena") || mesh.Name.Contains("Pistola"))
                world = TurretWorld;
            else if (mesh.Name.Contains("Cañon") || mesh.Name.Contains("Anillo"))
                world = CannonWorld;

            _effect.Parameters["WorldViewProjection"].SetValue(world * lightViewProjection);

            foreach (var meshPart in mesh.MeshParts)
            {
                _graphicsDevice.SetVertexBuffer(meshPart.VertexBuffer);
                _graphicsDevice.Indices = meshPart.IndexBuffer;

                foreach (var pass in _effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    _graphicsDevice.DrawIndexedPrimitives(
                        PrimitiveType.TriangleList,
                        meshPart.VertexOffset,
                        meshPart.StartIndex,
                        meshPart.PrimitiveCount
                    );
                }
            }
        }
    }

    public virtual void DrawCollisionChamber(Gizmo gizmos, Simulation simulation, Color color)
    {
        if (IsDead || Model == null) return;

        //obtengo la referencia del cuerpo y su pose actual en el mundo
        var body = simulation.Bodies.GetBodyReference(TankHandler);
        var pose = body.Pose;

        //Creo la matriz de mundo del cuerpo físico completo (centrada en su Centro de Masa)
        Matrix bodyWorld = Matrix.CreateFromQuaternion(new Quaternion(
                            pose.Orientation.X, pose.Orientation.Y, pose.Orientation.Z, pose.Orientation.W)) 
                        * Matrix.CreateTranslation(new Vector3(pose.Position.X, pose.Position.Y, pose.Position.Z));

        //Vuelvo a simular las dimensiones de las cajas
        Vector3 chassisScale = new Vector3(GameConfig.Tank.PhysicsChassisWidth, GameConfig.Tank.PhysicsChassisHeight, GameConfig.Tank.PhysicsChassisLength);
        Vector3 turretScale = new Vector3(GameConfig.Tank.PhysicsTurretWidth, GameConfig.Tank.PhysicsTurretHeight, GameConfig.Tank.PhysicsTurretLength);

        //Debo compensar el centro de masa restandolo de los offsets originales
        using var compoundBuilder = new CompoundBuilder(simulation.BufferPool, simulation.Shapes, 3);
        var chassisBox = new Box(GameConfig.Tank.PhysicsChassisWidth, GameConfig.Tank.PhysicsChassisHeight, GameConfig.Tank.PhysicsChassisLength);
        var turretBox = new Box(GameConfig.Tank.PhysicsTurretWidth, GameConfig.Tank.PhysicsTurretHeight, GameConfig.Tank.PhysicsTurretLength);

        compoundBuilder.Add(
            new Box(GameConfig.Tank.Stabilizer.Width, GameConfig.Tank.Stabilizer.Height, GameConfig.Tank.Stabilizer.Length), 
            new RigidPose(new System.Numerics.Vector3(0, -0.9f, 0), System.Numerics.Quaternion.Identity),
            GameConfig.Tank.Stabilizer.Mass);
        compoundBuilder.Add(chassisBox, new RigidPose(new System.Numerics.Vector3(0, -0.4f, 0), System.Numerics.Quaternion.Identity), GameConfig.Tank.ChassisMass);
        compoundBuilder.Add(turretBox, new RigidPose(new System.Numerics.Vector3(0, GameConfig.Tank.PhysicsTurretOffsetY, 0), System.Numerics.Quaternion.Identity), GameConfig.Tank.TurretMass);

        compoundBuilder.BuildDynamicCompound(out _, out _, out var center);
        Vector3 cpmCenter = new Vector3(center.X, center.Y, center.Z);

        //Calculo los offsets reales corregidos respecto al centro de masa
        Vector3 chassisOffset = new Vector3(0f, -0.4f, 0f) - cpmCenter*1.5f;
        Vector3 turretOffset = new Vector3(0f, GameConfig.Tank.PhysicsTurretOffsetY, 0f) - cpmCenter;

        //Dibujo el Chasis con la matriz final combinada
        Matrix chassisWorld = Matrix.CreateScale(chassisScale) 
                            * Matrix.CreateTranslation(chassisOffset) 
                            * bodyWorld;
        gizmos.DrawCube(chassisWorld, color);

        //Dibujo la Torreta con la matriz final combinada
        Matrix turretWorld = Matrix.CreateScale(turretScale) 
                            * Matrix.CreateTranslation(turretOffset) 
                            * bodyWorld;
        gizmos.DrawCube(turretWorld, color);
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
        angVel.X = MathHelper.Clamp(angVel.X, -GameConfig.Tank.AngularVelocityClampX, GameConfig.Tank.AngularVelocityClampX);
        angVel.Z = MathHelper.Clamp(angVel.Z, -GameConfig.Tank.AngularVelocityClampZ, GameConfig.Tank.AngularVelocityClampZ);
        angVel.X *= GameConfig.Tank.AngularDampingXZ;
        angVel.Y *= GameConfig.Tank.AngularDampingY;
        angVel.Z *= GameConfig.Tank.AngularDampingXZ;

        body.Awake = true;

        // Actualizar variables de estado para el render y la logica de juego
        var pose = body.Pose;
        Position = new Vector3(pose.Position.X, pose.Position.Y, pose.Position.Z);
        _physicsOrientation = pose.Orientation;

        // Extraer el angulo de yaw para la rotacion de la torreta y camara
        float sinYaw = 2f * (_physicsOrientation.W * _physicsOrientation.Y + _physicsOrientation.X * _physicsOrientation.Z);
        float cosYaw = 1f - 2f * (_physicsOrientation.X * _physicsOrientation.X + _physicsOrientation.Y * _physicsOrientation.Y);
        RotationY = MathF.Atan2(sinYaw, cosYaw);
    }
}