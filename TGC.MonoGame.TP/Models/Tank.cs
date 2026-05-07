using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TGC.MonoGame.TP.Models;

/// <summary>
///     Tanque basico con movimiento WASD. Sin fisica ni colisiones por ahora.
/// </summary>
public class Tank
{
    private Effect _effect;

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
    public float Scale { get; set; } = GameConfig.Assets.DefaultScale; //100f;

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
    public void Load(Model model, Texture2D texture, Effect effect)
    {
        Model = model;
        _effect = effect; //Mi efecto ahora es el BasicShader que le pase por parametro

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
                effect.Parameters["DiffuseColor"].SetValue(Color.Red.ToVector3()); //Un color porque aun no sé ponerle las texturas
            }
            mesh.Draw();
        }
    }

    public void Update(GameTime gameTime, KeyboardState keyboard)
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
        Position += forward * Speed * dt;

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