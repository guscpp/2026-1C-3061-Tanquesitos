using BepuPhysics;
using BepuPhysics.Collidables;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TGC.MonoGame.TP.Collisions;
using TGC.MonoGame.TP.Gizmos;

using System.Collections.Generic; // Necesario para List<T>

namespace TGC.MonoGame.TP.Models.Tanks;

public abstract class TankBase
{
    protected Effect _effect;
    protected Texture2D _texture;
    public Model Model { get; protected set; }

        // para las orugas
    protected float _trackOffsetAccumulator = 0f;
    private List<ModelMesh> _trackMeshes = new List<ModelMesh>();
    public float MaxSpeed { get; set; }
    public float MotorForce { get; set; }
    public float TurnSpeed { get; set; }
    public float ForwardDrag { get; set; }
    public float LateralDrag { get; set; }
    public float HealthPoints { get; set; }
    public float AttackDamage { get; set; }
    public GameConfig.TankClass TankClass { get; protected set; }

    public Microsoft.Xna.Framework.Vector3 Position { get; set; } = Microsoft.Xna.Framework.Vector3.Zero;
    public float RotationY { get; protected set; }
    public bool IsDead { get; protected set; }
    public BodyHandle TankHandler;

    protected System.Numerics.Quaternion _physicsOrientation = System.Numerics.Quaternion.Identity;
    protected float _turretRotation = 0f;
    protected float _cannonRotation = 0f;
    protected readonly float _minCannonPitch = MathHelper.ToRadians(GameConfig.Tank.MinCannonPitch);
    protected readonly float _maxCannonPitch = MathHelper.ToRadians(GameConfig.Tank.MaxCannonPitch);

    public float TurretRotationWorld => RotationY + _turretRotation;
    public float CannonRotation => _cannonRotation;

    public Vector3 ImpactPointLocal { get; private set; } = Vector3.Zero;
    public Vector3 ImpactPointWorld { get; private set; } = Vector3.Zero;
    public bool HasImpact { get; private set; } = false;
    public float ImpactRadius { get; set; } = 1.5f;  //radio de deformacion
    public float ImpactDepth { get; set; } = 0.4f;   //profundidad de deformacion

    
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
        Matrix.CreateFromQuaternion(new Microsoft.Xna.Framework.Quaternion(
            _physicsOrientation.X, 
            _physicsOrientation.Y, 
            _physicsOrientation.Z, 
            _physicsOrientation.W)) *
        Matrix.CreateTranslation(Position + new Vector3(0, GameConfig.Tank.VisualOffsetY, 0));

    public Matrix TurretWorld => Matrix.CreateRotationZ(_turretRotation) * WorldMatrix;

    public Matrix CannonWorld => Matrix.CreateTranslation(0f, 0f, -1.5f) * Matrix.CreateRotationX(_cannonRotation) * Matrix.CreateTranslation(0f, 0f, 1.5f) * TurretWorld;

    public Vector3 CannonMuzzlePosition => Vector3.Transform(
        new Vector3(0f, GameConfig.Tank.CannonMuzzleOffsetY, GameConfig.Tank.CannonMuzzleOffsetZ), CannonWorld);

    protected Microsoft.Xna.Framework.Color GetTankColor()
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

  // Agrega este campo arriba, junto a _texture
protected Texture2D _trackTexture;


// Modifica el método Load
public void Load(Model model, Texture2D texture, Texture2D trackTexture, Effect effect, Simulation simulation)
{
    Model = model; 
    _effect = effect; 
    _texture = texture;
    _trackTexture = trackTexture; // Guardamos la nueva textura
    
    
    _trackMeshes.Clear();
    foreach (var mesh in Model.Meshes)
    {
        if (mesh.Name.Contains("Cadena_i") || mesh.Name.Contains("Cadena_d")) 
        {
            _trackMeshes.Add(mesh);
        }
        
        // Asignamos el efecto a todas las partes
        foreach (var part in mesh.MeshParts) 
        {
            part.Effect = _effect;
        }
    }
    CreatePhysicsBody(simulation);
}


public void UpdateTrackAnimation(float deltaTime, float speed)
{
    // speed ya viene de la magnitud de la velocidad física (en metros/segundo).
    // Dividimos por el radio de la rueda (ej. 0.5f) para que sea proporcional
    float wheelRadius = 0.5f; 
    float angularVelocity = speed / wheelRadius;
    
    // Ajusta este multiplicador (0.05f)para la velocidad
    _trackOffsetAccumulator += angularVelocity * deltaTime * 0.02f;
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

    public void HandleHealth(float damage, Vector3 impactPointWorld)
    {
        HealthPoints -= damage;
        if (HealthPoints <= 0) { 
            HealthPoints = 0;
            if (!(this is TankPlayer)) TGCGame.Instance.EnemiesKilled++;
            IsDead = true;
        }

        ImpactPointLocal = Vector3.Transform(impactPointWorld, Matrix.Invert(WorldMatrix));
        HasImpact = true;
    }

    //overload sin punto de impacto
    public void HandleHealth(float damage) => HandleHealth(damage, Position);

 public virtual void Draw(Microsoft.Xna.Framework.Matrix view, Microsoft.Xna.Framework.Matrix projection, Vector3 cameraPosition)
{
    if (Model == null || IsDead) return;

    Microsoft.Xna.Framework.Vector3 colorVector = GetTankColor().ToVector3();
    Microsoft.Xna.Framework.Vector3 whiteColor = Microsoft.Xna.Framework.Vector3.One;

    // Parametros generales que no cambian por malla
    _effect.Parameters["View"].SetValue(view);
    _effect.Parameters["Projection"].SetValue(projection);
    _effect.Parameters["LightDirection"].SetValue(new Vector3(0.5f, 1f, 0.3f));
    _effect.Parameters["LightColor"].SetValue(Vector3.One);
    _effect.Parameters["AmbientColor"].SetValue(new Vector3(0.2f, 0.2f, 0.2f));
    _effect.Parameters["EyePosition"].SetValue(cameraPosition);
    _effect.Parameters["Shininess"].SetValue(32f);

    // Parametros de deformacion
    _effect.Parameters["ImpactPointWorld"].SetValue(ImpactPointWorld);
    _effect.Parameters["ImpactRadius"].SetValue(ImpactRadius);
    _effect.Parameters["ImpactDepth"].SetValue(ImpactDepth);
    _effect.Parameters["HasImpact"].SetValue(HasImpact ? 1 : 0);

    foreach (var mesh in Model.Meshes)
    {
        // 1. Determinar si es oruga
        bool isTrack = mesh.Name.Contains("Cadena_i") || mesh.Name.Contains("Cadena_d");

        // 2. Asignar la textura correcta
        // Debes tener el campo _trackTexture ya cargado en el Load
        Texture2D activeTexture = isTrack ? _trackTexture : _texture;
        _effect.Parameters["ModelTexture"].SetValue(isTrack ? _trackTexture : _texture);

        // 3. Configurar Shader para la malla
        bool isDeformable = mesh.Name.Contains("Cabeza") || mesh.Name.Contains("Cano_") ||
            mesh.Name.Contains("Cubre") || mesh.Name.Contains("Cuerpo") ||
            mesh.Name.Contains("Pistola") || mesh.Name.Contains("Proteccion");

        _effect.Parameters["IsDeformable"].SetValue(isDeformable ? 1 : 0);

        // 4. Calcular matriz de mundo segun la pieza
        var finalWorld = WorldMatrix;
        if (mesh.Name.Contains("Cabeza") || mesh.Name.Contains("Antena") || mesh.Name.Contains("Pistola"))
            finalWorld = TurretWorld;
        else if (mesh.Name.Contains("Canon") || mesh.Name.Contains("Anillo"))
            finalWorld = CannonWorld;

        // 5. Aplicar offset solo si es oruga
        _effect.Parameters["TextureOffset"]?.SetValue(isTrack ? new Vector2(0f, _trackOffsetAccumulator) : Vector2.Zero);

        // 6. Aplicar color difuso
        var diffuseParam = _effect.Parameters["DiffuseColor"];
        if (diffuseParam != null)
        {
            if (mesh.Name.Contains("Cabeza") || mesh.Name.Contains("Anillo") ||
                mesh.Name.Contains("Proteccion_d") || mesh.Name.Contains("Proteccion_i") ||
                mesh.Name.Contains("Cuerpo") || mesh.Name.Contains("Cubre"))
                diffuseParam.SetValue(colorVector);
            else
                diffuseParam.SetValue(whiteColor);
        }

        _effect.Parameters["World"].SetValue(finalWorld);

        mesh.Draw();
    }
}

    public virtual void DrawCollisionChamber(Gizmo gizmos, Simulation simulation, Color color)
    {
        if (IsDead || Model == null) return;

        //obtengo la referencia del cuerpo y su pose actual en el mundo
        var body = simulation.Bodies.GetBodyReference(TankHandler);
        var pose = body.Pose;

        //Creo la matriz de mundo del cuerpo físico completo (centrada en su Centro de Masa)
        Matrix bodyWorld = Matrix.CreateFromQuaternion(new Microsoft.Xna.Framework.Quaternion(
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
        forward = new System.Numerics.Vector3(0f, 0f, -1f); // Fallback
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

    

    // --- NUEVA LÓGICA DE ANIMACIÓN DE ORUGAS ---
    // Calculamos la magnitud de la velocidad para animar la textura
    float currentSpeed = vel.Length();
    UpdateTrackAnimation(dt, currentSpeed);
    // ------------------------------------------

    // Las fuerzas ahora se calculan exclusivamente sobre el plano horizontal
    var motorForce = forward * MotorForce * forwardInput;
    var dragForce = -forward * (ForwardDrag * fwdSpeed) - right * (LateralDrag * rightSpeed);

    body.ApplyLinearImpulse((motorForce + dragForce) * dt);

    // Control de giro
    ref var angVel = ref body.Velocity.Angular;
    angVel.Y = turnInput * TurnSpeed;

    // Amortiguador para evitar que el tanque quede girando como loco
    angVel.X = MathHelper.Clamp(angVel.X, -GameConfig.Tank.AngularVelocityClampX, GameConfig.Tank.AngularVelocityClampX);
    angVel.Z = MathHelper.Clamp(angVel.Z, -GameConfig.Tank.AngularVelocityClampZ, GameConfig.Tank.AngularVelocityClampZ);
    angVel.X *= GameConfig.Tank.AngularDampingXZ;
    angVel.Y *= GameConfig.Tank.AngularDampingY;
    angVel.Z *= GameConfig.Tank.AngularDampingXZ;

    body.Awake = true;

    // Actualizar variables de estado
    var pose = body.Pose;
    Position = new Microsoft.Xna.Framework.Vector3(pose.Position.X, pose.Position.Y, pose.Position.Z);
    _physicsOrientation = pose.Orientation;

    float sinYaw = 2f * (_physicsOrientation.W * _physicsOrientation.Y + _physicsOrientation.X * _physicsOrientation.Z);
    float cosYaw = 1f - 2f * (_physicsOrientation.X * _physicsOrientation.X + _physicsOrientation.Y * _physicsOrientation.Y);
    RotationY = MathF.Atan2(sinYaw, cosYaw);

    if (HasImpact) ImpactPointWorld = Vector3.Transform(ImpactPointLocal, WorldMatrix);
}
}