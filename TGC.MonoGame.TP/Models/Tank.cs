using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using BepuPhysics.Collidables;
using System.Numerics;

using TGC.MonoGame.TP.Collisions;
using TGC.MonoGame.TP.Gizmos;
using TGC.MonoGame.TP.Gizmos.Geometry;
using TGC.MonoGame.TP.Models.Decorations;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using BepuPhysics;
using BepuPhysics.Constraints;

namespace TGC.MonoGame.TP.Models;

/// <summary>
///     Tanque basico con movimiento WASD. Sin fisica ni colisiones por ahora.
/// </summary>
public class Tank
{
    private Effect _effect;
    private Texture2D _texture;

    public BoundingSphere _tankSphere;

    public Model Model { get; private set; }
    //configuracion de movimiento
    public float MaxSpeed { get; set; } = GameConfig.Tank.MaxSpeed;
    public float Acceleration { get; set; } = GameConfig.Tank.Acceleration;
    public float Friction { get; set; } = GameConfig.Tank.Friction;
    public float TurnSpeed { get; set; } = GameConfig.Tank.TurnSpeed;
    public float VerticalSpeed = GameConfig.Tank.VerticalSpeed;

    //estado interno
    public Vector3 Position { get; set; } = Vector3.Zero;
    public float RotationY { get; private set; }
    public float Speed { get; private set; }
    public float CurrentFuel { get; private set; } = GameConfig.Tank.MaxFuel;
    public float Scale { get; set; } = GameConfig.Tank.TankScale;

    private System.Numerics.Quaternion _physicsOrientation = System.Numerics.Quaternion.Identity;

    public BodyHandle TankHandler;

    /// <summary>
    ///     Matriz de mundo lista para pasar al Draw de un Model.
    /// </summary>
    public Matrix WorldMatrix =>
        Matrix.CreateScale(Scale) *
        Matrix.CreateRotationX(MathHelper.ToRadians(-90f)) *
        Matrix.CreateFromQuaternion(new Microsoft.Xna.Framework.Quaternion(
            _physicsOrientation.X, _physicsOrientation.Y, _physicsOrientation.Z, _physicsOrientation.W)) *
        Matrix.CreateTranslation(Position);

    /// <summary>
    ///     Carga el modelo compilado y aplica la iluminacion basica.
    /// </summary>
    public void Load(Model model, Texture2D texture, Effect effect, Simulation simulation)
    {
        Model = model;
        _effect = effect; //Mi efecto ahora es el BasicShader que le pase por parametro
        _texture = texture;

        //Para cada malla de mi coleccion de mallas del modelo
        foreach (var mesh in Model.Meshes)
        {
            //Para cada parte de la malla de mi coleccion de partes de la malla
            foreach (var meshPart in mesh.MeshParts)
            {
                // Reemplazamos el efecto por defecto del modelo por el nuestro
                meshPart.Effect = _effect;
            }
        }
        CreateTank(VectorExtensions.ToNumerics(Position), simulation);
    }

    public void CreateTank(Vector3 position, Simulation simulation)
    {
        // 1. Inicializamos el CompoundBuilder
        using (var compoundBuilder = new CompoundBuilder(simulation.BufferPool, simulation.Shapes, 3)) // 3 partes ahora (chasis + torreta + base)
        {
            // 2. Creamos las formas de las partes del tanque
            var chassisBox = new Box(
                GameConfig.Tank.PhysicsChassisWidth,
                GameConfig.Tank.PhysicsChassisHeight,
                GameConfig.Tank.PhysicsChassisLength);

            var turretBox = new Box(
                GameConfig.Tank.PhysicsTurretWidth,
                GameConfig.Tank.PhysicsTurretHeight,
                GameConfig.Tank.PhysicsTurretLength);

            // 3. Definimos las poses locales
            var chassisPose = new RigidPose
            {
                Position = new System.Numerics.Vector3(0, -0.4f, 0),    //bajar 40cm el centro de masa
                Orientation = System.Numerics.Quaternion.Identity
            };
            var turretPose = new RigidPose
            {
                Position = new System.Numerics.Vector3(0, GameConfig.Tank.PhysicsTurretOffsetY, 0),
                Orientation = System.Numerics.Quaternion.Identity
            };

            // 3.1 Crear base invisible para mejorar sustentacion del tanque
            var stabilizerBox = new Box(2.6f, 0.3f, 2.6f);
            var stabilizerPose = new RigidPose
            {
                Position = new System.Numerics.Vector3(0, -0.9f, 0), // -0.9f para que quede por debajo del chasis (-0.4f)
                Orientation = System.Numerics.Quaternion.Identity
            };
            compoundBuilder.Add(stabilizerBox, stabilizerPose, 6000f); // 6000kg para bajar aun mas el centro de masa

            // 4. Agregamos las partes al builder con sus masas
            compoundBuilder.Add(chassisBox, chassisPose, GameConfig.Tank.ChassisMass);
            compoundBuilder.Add(turretBox, turretPose, GameConfig.Tank.TurretMass);

            // 5. Construimos el cuerpo compuesto
            compoundBuilder.BuildDynamicCompound(out var compoundChildren, out var compoundInertia, out var compoundCenter);

            // 6. Registramos el Compound shape
            var compoundShapeIndex = simulation.Shapes.Add(new Compound(compoundChildren));

            // 7. Creamos el cuerpo dinámico del tanque
            TankHandler = simulation.Bodies.Add(
                BodyDescription.CreateDynamic(
                    new RigidPose(position.ToNumerics() + compoundCenter, System.Numerics.Quaternion.Identity),
                    compoundInertia,
                    new CollidableDescription(compoundShapeIndex, 0.1f),
                    new BodyActivityDescription(0.01f)
                )
            );
        }
    }

    /// <summary>
    ///     Hace la recarga con limite, el combustible que sobra se desperdicia
    /// </summary>
    public void AddFuel(float amount)
    {
        CurrentFuel = MathHelper.Clamp(CurrentFuel + amount, 0f, GameConfig.Tank.MaxFuel);
    }

    /// <summary>
    ///     Dibuja el tanque usando las matrices de la camara.
    /// </summary>
    public void Draw(Matrix view, Matrix projection)
    {
        if (Model == null) return;

        //Para cada malla en la coleccion de mallas del modelo
        foreach (var mesh in Model.Meshes)
        {
            //Para cada efecto en la coleccion de efectos de la malla
            foreach (var effect in mesh.Effects)
            {
                //Coloco los parametros de world, view y projection
                effect.Parameters["World"].SetValue(WorldMatrix);
                effect.Parameters["View"].SetValue(view);
                effect.Parameters["Projection"].SetValue(projection);
                effect.Parameters["ModelTexture"].SetValue(_texture); //Un color porque aun no sé ponerle las texturas
            }
            mesh.Draw();
        }
    }

    public void Update(GameTime gameTime, KeyboardState keyboard, Simulation simulation)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        float forwardInput = 0f;
        if (keyboard.IsKeyDown(Keys.W)) forwardInput += 1f;
        if (keyboard.IsKeyDown(Keys.S)) forwardInput -= 1f;

        // La aceleracion del control del jugador (adelante/atras) se aplica solamente si queda combustible
        if (CurrentFuel > 0f)
        {
            Speed += forwardInput * Acceleration * dt;
            if (forwardInput!= 0f) CurrentFuel -= GameConfig.Tank.FuelConsumptionRate * dt;
        }
        else
        {
            forwardInput = 0f;
        }

        CurrentFuel = MathHelper.Clamp(CurrentFuel, 0f, GameConfig.Tank.MaxFuel);

        // La friccion se aplica independientemente de la reserva de combustible, o sea:
        // cuando se queda sin combustible no frena en seco
        Speed *= MathF.Pow(Friction, dt * 60f);
        Speed = MathHelper.Clamp(Speed, -MaxSpeed * 0.4f, MaxSpeed);

        // Dirección actual del tanque (según su orientación en el motor físico)
        // Usamos el quaternion de Bepu para obtener el vector forward
        var body = simulation.Bodies.GetBodyReference(TankHandler);
        var orientation = body.Pose.Orientation;
        Vector3 forwardBepu = Vector3.Transform(Vector3.Forward, orientation); // forward en System.Numerics
        Vector3 forwardXna = new Vector3(forwardBepu.X, forwardBepu.Y, forwardBepu.Z); // convertir a XNA

        // Velocidad lineal deseada
        Vector3 desiredLinearVelocity = forwardXna * Speed;
        // Mantenemos la componente Y actual (para gravedad)
        body.Velocity.Linear = new System.Numerics.Vector3(desiredLinearVelocity.X, body.Velocity.Linear.Y, desiredLinearVelocity.Z);
        body.Awake = true;

        float turnInput = 0f;
        if (keyboard.IsKeyDown(Keys.A)) turnInput += 1f;
        if (keyboard.IsKeyDown(Keys.D)) turnInput -= 1f;

        // Velocidad angular máxima (rad/s)
        float maxAngularSpeed = TurnSpeed;
        body.Velocity.Angular = new System.Numerics.Vector3(0, turnInput * maxAngularSpeed, 0);


        // Correcion para que no se caiga de trompa cuando avanza (al tener proporciones cartoon es inestable)
        // y ya que estamos, que sea mas estable lateralmente sobre desniveles (que no caiga sobre su lateral)
        ref var vel = ref body.Velocity;
        
        vel.Angular.X = MathHelper.Clamp(vel.Angular.X, -0.5f, 0.5f);   //clamp al pitch
        vel.Angular.Z = MathHelper.Clamp(vel.Angular.Z, -0.3f, 0.3f);   //clamp al roll

        //damping
        vel.Angular.X *= 0.88f; //pitch
        vel.Angular.Y *= 0.98f; //yaw (correccion mas suave que X y Z para que el jugador pueda rotarlo) 
        vel.Angular.Z *= 0.88f; //roll

        body.Awake = true; //fuerza que procese los cambios


        // --- Leer la posición y orientación actualizadas por Bepu ---
        var pose = body.Pose;
        Position = new Vector3(pose.Position.X, pose.Position.Y, pose.Position.Z);
        _physicsOrientation = pose.Orientation;

        // Extraer el ángulo Yaw (RotationY) para la cámara
        // Usamos la misma fórmula que ya tenías
        float sinYaw = 2f * (_physicsOrientation.W * _physicsOrientation.Z + _physicsOrientation.X * _physicsOrientation.Y);
        float cosYaw = 1f - 2f * (_physicsOrientation.Y * _physicsOrientation.Y + _physicsOrientation.Z * _physicsOrientation.Z);
        RotationY = MathF.Atan2(sinYaw, cosYaw);
    }
}