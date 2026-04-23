using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TGC.MonoGame.TP.Models;

/// <summary>
///     Tanque basico con movimiento WASD. Sin fisica ni colisiones por ahora.
/// </summary>
public class Tank
{
    public Model Model { get; private set; }
    //configuracion de movimiento
    public float MaxSpeed { get; set; } = 25000f;
    public float Acceleration { get; set; } = 3500f;
    public float Friction { get; set; } = 0.96f;
    public float TurnSpeed { get; set; } = 2.8f;

    //estado interno
    public Vector3 Position { get; private set; }
    public float RotationY { get; private set; }
    public float Speed { get; private set; }

    /// <summary>
    ///     Matriz de mundo lista para pasar al Draw de un Model.
    /// </summary>
    public Matrix WorldMatrix => 
        Matrix.CreateRotationY(RotationY) * Matrix.CreateTranslation(Position);

    /// <summary>
    ///     Carga el modelo compilado y aplica la iluminacion basica.
    /// </summary>
    public void Load(Model model)
    {
        Model = model;
        //habilitar iluminacion por defecto en todos los meshes
        foreach (var mesh in model.Meshes)
            foreach (BasicEffect effect in mesh.Effects)
                effect.EnableDefaultLighting();
    }

    /// <summary>
    ///     Dibuja el tanque usando las matrices de la camara.
    /// </summary>
    public void Draw(Matrix view, Matrix projection)
    {
        if (Model == null) return;
        Model.Draw(WorldMatrix, view, projection);
    }

    public void Update(GameTime gameTime, KeyboardState keyboard)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (keyboard.IsKeyDown(Keys.A)) RotationY += TurnSpeed * dt;
        if (keyboard.IsKeyDown(Keys.D)) RotationY -= TurnSpeed * dt;

        float forwardInput = 0f;
        if (keyboard.IsKeyDown(Keys.W)) forwardInput += 1f;
        if (keyboard.IsKeyDown(Keys.S)) forwardInput -= 1f;

        //fisica sencilla
        Speed += forwardInput * Acceleration * dt;
        Speed *= System.MathF.Pow(Friction, dt * 60f);
        Speed = MathHelper.Clamp(Speed, -MaxSpeed * 0.4f, MaxSpeed);

        //actualizar posicion
        Vector3 forward = Vector3.TransformNormal(Vector3.Forward, Matrix.CreateRotationY(RotationY));
        Position += forward * Speed * dt;

        //mantener flotando en y = 0
        Position = new Vector3(Position.X, 0f, Position.Z);
    }
}