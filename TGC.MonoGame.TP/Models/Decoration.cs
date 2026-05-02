using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TGC.MonoGame.TP.Models;
/// <summary>
/// Decoraciones dentro de escenario: rocas, arboles, cactus, etc.
/// </summary>
public class Decoration
{
    public Model Model { get; private set; }
    public Vector3 Position { get; private set; }
    public float Rotation { get; set; }     // rotacion que se le quiera dar para generar variacion entre los modelos

    private Matrix _world;

    public void LoadContent(Model model, Vector3 position)
    {
        Model = model;

        Position = position;
        _world = Matrix.CreateScale(2.5f) * Matrix.CreateTranslation(Position);
    }

    public void Update()
    {
        
    }

    public void Draw(Matrix view, Matrix projection)
    {
        Model.Draw(_world, view, projection);
    }
}