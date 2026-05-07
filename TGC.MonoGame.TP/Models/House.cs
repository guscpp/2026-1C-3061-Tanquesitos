using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.Models;
/// <summary>
/// Casas dentro de escenario 
/// </summary>
public class House
{
    private Effect _effect;

    public Model Model { get; private set; }
    public Vector3 Position { get; private set; }
    public float Rotation { get; set; }     // rotacion que se le quiera dar para generar variacion entre los modelos

    public string Path { get; set; }

    private Color _color;

    private const float Scale = GameConfig.Assets.HouseScale;

    private Matrix _world;

    public void Initialize()
    {
        Position = Vector3.Zero;
    }

    public void LoadContent(Model model, Vector3 position, float angle, Effect effect, Color color)
    {
        Model = model;

        _effect = effect;

        _color = color;

        Position = position;
        _world = Matrix.CreateScale(Scale) * 
            Matrix.CreateRotationX(MathHelper.ToRadians(-90f)) * 
            Matrix.CreateRotationY(angle) * 
            Matrix.CreateTranslation(Position);

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

    public void Update()
    {
        
    }

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
                effect.Parameters["World"].SetValue(_world);
                effect.Parameters["View"].SetValue(view);
                effect.Parameters["Projection"].SetValue(projection);
                effect.Parameters["DiffuseColor"].SetValue(_color.ToVector3()); //Un color porque aun no sé ponerle las texturas
            }
            mesh.Draw();
        }
    }
}