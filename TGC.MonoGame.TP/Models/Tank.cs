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

    //Movimiento de la torreta
    private float _turretRotation = 0f; // Rotacion torreta (derecha/izquierda)
    private float _cannonRotation = 0f; // Rotacion cañon (arriba/abajo)
    private readonly float _minCannonPitch = MathHelper.ToRadians(-20f); //Limites para que el cañon no traspase el modelo al subir y bajar
    private readonly float _maxCannonPitch = MathHelper.ToRadians(10f);
    public float TurretRotationWorld => RotationY + _turretRotation; //Necesario para la camara (Torreta + Cañon)

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

        // Matriz de mundo de la base
        Matrix chassisWorld = WorldMatrix;

        // Matriz de mundo de la torreta (cabeza) que hereda su posicion de la base
        Matrix turretWorld = Matrix.CreateRotationZ(_turretRotation) * chassisWorld;

        float distanciaHaciaAtras = -1.5f; //Valor random que uso para empujar el centro del cañon hacia atras

        //movimiento realizado del cañon, tomo el centro del cañon, lo tiro para atras y lo roto en el eje X usando ese centro inventando, luego lo vuelvo a su lugar
        Matrix localCannon = Matrix.CreateTranslation(0f, 0f, distanciaHaciaAtras) 
                            * Matrix.CreateRotationX(_cannonRotation) 
                            * Matrix.CreateTranslation(0f, 0f, -distanciaHaciaAtras); 

        //Luego tomo mi cañon y lo acomodo sobre la torreta
        Matrix cannonWorld = localCannon * turretWorld;

        foreach (var mesh in Model.Meshes)
        {
            Matrix finalWorldMatrix = chassisWorld;

            // Reviso cual es el nombre de la malla para determinar que matriz de mundo debe seguir
            if (mesh.Name.Contains("Cabeza")
                || mesh.Name.Contains("Antena")
                || mesh.Name.Contains("Pistola_i") || mesh.Name.Contains("Pistola_d"))
            {
                finalWorldMatrix = turretWorld; //La cabeza, la antena y las pistolas decorativas se mueven junto a la torreta
            }
            else if (mesh.Name.Contains("Cañon anillo") || mesh.Name.Contains("Cañon.001"))
            {
                finalWorldMatrix = cannonWorld; //el anillo y el cañon se mueven con un "plus"
            } //el resto de elemento se mueven junto a la base

            foreach (var effect in mesh.Effects)
            {
                effect.Parameters["World"].SetValue(finalWorldMatrix);
                effect.Parameters["View"].SetValue(view);
                effect.Parameters["Projection"].SetValue(projection);
                effect.Parameters["ModelTexture"].SetValue(_texture);
            }
            mesh.Draw();
        }
    }

    public void Update(GameTime gameTime, KeyboardState keyboard, Simulation simulation)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var body = simulation.Bodies.GetBodyReference(TankHandler);

        // 1. Lógica de entrada y combustible
        float forwardInput = 0f;
        if (keyboard.IsKeyDown(Keys.W)) forwardInput += 1f;
        if (keyboard.IsKeyDown(Keys.S)) forwardInput -= 1f;

        if (CurrentFuel <= 0f)
            forwardInput = 0f;
        else if (forwardInput != 0f)
            CurrentFuel -= GameConfig.Tank.FuelConsumptionRate * dt;

        CurrentFuel = MathHelper.Clamp(CurrentFuel, 0f, GameConfig.Tank.MaxFuel);

        // 2. Movimiento lineal mediante fuerzas (convertidas a impulso)
        var orientation = body.Pose.Orientation;
        // Dirección "forward" del tanque en coordenadas de mundo (sistema diestro: -Z es adelante)
        System.Numerics.Vector3 forward = System.Numerics.Vector3.Transform(
            new System.Numerics.Vector3(0, 0, -1), orientation);

        // Fuerza del motor
        float motorForceMagnitude = GameConfig.Tank.MotorForce * forwardInput;
        System.Numerics.Vector3 motorForce = forward * motorForceMagnitude;

        // Fuerza de arrastre (drag) proporcional a la velocidad horizontal actual
        // Obtener vectores locales del tanque (en coordenadas mundo)
        var right = System.Numerics.Vector3.Normalize(System.Numerics.Vector3.Cross(new System.Numerics.Vector3(0, 1, 0), forward));

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

        //System.Diagnostics.Debug.WriteLine($"Vel: {body.Velocity.Linear.Length()}");

        // 3. Rotación (giro en el lugar, estilo orugas)
        float turnInput = 0f;
        if (keyboard.IsKeyDown(Keys.A)) turnInput += 1f;
        if (keyboard.IsKeyDown(Keys.D)) turnInput -= 1f;

        body.Velocity.Angular = new System.Numerics.Vector3(0, turnInput * TurnSpeed, 0);

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
        Position = new Vector3(pose.Position.X, pose.Position.Y, pose.Position.Z);
        _physicsOrientation = pose.Orientation;

        // Rotación en Y para la cámara
        float sinYaw = 2f * (_physicsOrientation.W * _physicsOrientation.Z + _physicsOrientation.X * _physicsOrientation.Y);
        float cosYaw = 1f - 2f * (_physicsOrientation.Y * _physicsOrientation.Y + _physicsOrientation.Z * _physicsOrientation.Z);
        RotationY = MathF.Atan2(sinYaw, cosYaw);

        // 6. Torreta, movimiento con mouse
        if(TGCGame.Instance.IsActive){
            TGCGame.Instance.IsMouseVisible = false;

            var currentMouseState = Mouse.GetState();

                    // Busco el centro de la pantalla actual usando el Viewport estatico de la tarjeta grafica
                    int centerX = simulation.Bodies.GetBodyReference(TankHandler).Awake ? TGCGame.Instance.GraphicsDevice.Viewport.Width / 2 : 0;
                    int centerY = TGCGame.Instance.GraphicsDevice.Viewport.Height / 2;
                    centerX = TGCGame.Instance.GraphicsDevice.Viewport.Width / 2;

                    // Calculo del desplazamiento del mouse desde el centro de la pantalla
                    float deltaX = currentMouseState.X - centerX;
                    float deltaY = currentMouseState.Y - centerY;

                    float sensitivity = 0.0015f; //Ajusto la velocidad (sensibilidad)

                    // Determino la rotacion segun el desplazamiento del mouse y la velocidad
                    _turretRotation -= deltaX * sensitivity;
                    _cannonRotation -= deltaY * sensitivity;

                    // Le aplico una correccion al cañon en funcion de los limites que puse arriba
                    _cannonRotation = MathHelper.Clamp(_cannonRotation, _minCannonPitch, _maxCannonPitch);

                    // Centro el mouse de nuevo
                    Mouse.SetPosition(centerX, centerY);
        } 
        else
        {
            TGCGame.Instance.IsMouseVisible = true;
        }
        
    }
}