using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;


namespace TGC.MonoGame.TP.Models;
/// <summary>
/// Decoraciones dentro de escenario: rocas, arboles, cactus, etc.
/// </summary>
public class Decoration
{
    private Effect _effect;

    public Model Model { get; private set; }
    public Vector3 Position { get; private set; }
    public float Rotation { get; set; }     // rotacion que se le quiera dar para generar variacion entre los modelos

    private const float Scale = GameConfig.Assets.DecorationScale;

    private Matrix _world;

    private Texture2D _texture;

    public void Initialize()
    {
        Position = Vector3.Zero;
    }

    public void LoadContent(Model model, Vector3 position, float angle, Effect effect, Texture2D texture)
    {
        Model = model;

        _effect = effect;

        _texture = texture;

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
                effect.Parameters["ModelTexture"].SetValue(_texture); //Un color porque aun no sé ponerle las texturas
            }
            mesh.Draw();
        }
    }
}