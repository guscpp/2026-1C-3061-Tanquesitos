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
    public float MaxSpeed { get; set; } = GameConfig.Tank.MaxSpeed; //25000f;
    public float Acceleration { get; set; } = GameConfig.Tank.Acceleration; //3500f;
    public float Friction { get; set; } = GameConfig.Tank.Friction; //0.96f;
    public float TurnSpeed { get; set; } = GameConfig.Tank.TurnSpeed; //2.8f;
    public float VerticalSpeed = GameConfig.Tank.VerticalSpeed; //1000f;

    //estado interno
    public Vector3 Position { get; private set; }
    public float RotationY { get; private set; }
    public float Speed { get; private set; }

    //Propieda de escalado - el valor puede variar
    public float Scale { get; set; } = GameConfig.Tank.TankScale;

    public float CollisionChamberScale { get; set; } = GameConfig.Tank.TankChamberScale;

    public BodyHandle TankHandler;

    /// <summary>
    ///     Matriz de mundo lista para pasar al Draw de un Model.
    /// </summary>
    public Matrix WorldMatrix =>
        Matrix.CreateScale(Scale) *                             //Primero lo escalo porque sino se ve diminuto
        Matrix.CreateRotationX(MathHelper.ToRadians(-90f)) *    //Para que no se vea acostado xd
        Matrix.CreateRotationY(RotationY) *                     //Luego lo roto
        Matrix.CreateTranslation(Position);                     //Finalmente lo traslado

    /// <summary>
    ///     Carga el modelo compilado y aplica la iluminacion basica.
    /// </summary>
    public void Load(Model model, Texture2D texture, Effect effect, Simulation simulation)
    {
        Model = model;
        _effect = effect; //Mi efecto ahora es el BasicShader que le pase por parametro
        _texture = texture;

        InitializeCollisionChamber(model);

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
        var sphere = new Sphere(2f);

        var shape = simulation.Shapes.Add(sphere);

        var inertia = sphere.ComputeInertia(1f);

        TankHandler = simulation.Bodies.Add(
            BodyDescription.CreateDynamic(new RigidPose(new System.Numerics.Vector3(position.X, position.Y, position.Z)),
                inertia,
                new CollidableDescription(shape, 0.1f),
                new BodyActivityDescription(0.01f)
            )
        );
    }

    /// <summary>
    ///     Dibuja el tanque usando las matrices de la camara.
    /// </summary>
    public void InitializeCollisionChamber(Model model)
    {
        // TEMPORAL!!
        _tankSphere = BoundingVolumesUtils.CreateSphereFrom(model);
        _tankSphere = BoundingVolumesUtils.Scale(_tankSphere, CollisionChamberScale);
        _tankSphere.Radius = _tankSphere.Radius * 0.7f;
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

    public void DrawCollisionChamber(Gizmo gizmos)
    {
        gizmos.DrawSphere(_tankSphere.Center, _tankSphere.Radius * Vector3.One, Color.Blue);
    }

    public void Update(GameTime gameTime, KeyboardState keyboard, Simulation simulation)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (keyboard.IsKeyDown(Keys.A)) RotationY += TurnSpeed * dt;
        if (keyboard.IsKeyDown(Keys.D)) RotationY -= TurnSpeed * dt;

        float forwardInput = 0f;
        if (keyboard.IsKeyDown(Keys.W)) forwardInput += 1f;
        if (keyboard.IsKeyDown(Keys.S)) forwardInput -= 1f;

        if (keyboard.IsKeyDown(Keys.Q)) Position += Vector3.Up * VerticalSpeed * dt;
        if (keyboard.IsKeyDown(Keys.E)) Position -= Vector3.Up * VerticalSpeed * dt;

        //fisica sencilla
        Speed += forwardInput * Acceleration * dt;
        Speed *= System.MathF.Pow(Friction, dt * 60f);
        Speed = MathHelper.Clamp(Speed, -MaxSpeed * 0.4f, MaxSpeed);
        //actualizar posicion
        Vector3 forward = Vector3.TransformNormal(Vector3.Forward, Matrix.CreateRotationY(RotationY));
        // var increment = forward * Speed * dt;
        // Position += increment; // comentado para realizar el movimiento con Bepu

        var velocity = forward * Speed;

        // colision con Bepu Physics
        var body = simulation.Bodies.GetBodyReference(TankHandler);
        body.Velocity.Linear = new System.Numerics.Vector3(velocity.X, body.Velocity.Linear.Y, velocity.Z);
        body.Awake = true;

        var pose = body.Pose;
        Position = new Vector3(pose.Position.X, pose.Position.Y, pose.Position.Z);

        // SOBRE EL TANK VOLUME -> TEMPORAL!!
        //_tankSphere.Center = Position;
        //assets.UpdateCollisions(_tankSphere);

        //mantener flotando en y = 0
        //Position = new Vector3(Position.X, 0f, Position.Z);
    }

    /// <summary>
    ///     Corrige la Y de la posicion, como Tanque no tiene una referencial al terreno, no puedo ponerlo en Update (Aunque sería lo ideal... creo xd)
    /// </summary>
    public void SetHeight(float y)
    {
        Position = new Vector3(Position.X, y, Position.Z);
    }
}