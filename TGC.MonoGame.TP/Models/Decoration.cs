using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using TGC.MonoGame.TP.Collisions;
using TGC.MonoGame.TP.Gizmos;

namespace TGC.MonoGame.TP.Models.Decorations;
/// <summary>
/// Decoraciones dentro de escenario: rocas, arboles, cactus, etc.
/// </summary>
public class Decoration
{
    public Decoration(Vector3 position, string name)
    {
        Position = position;
        _path = name;
    }
    private Effect _effect;

    public string _path;

    public bool _touchingDecoration = false;

    public Color CollisionChamberColor => Color.Green;
    public Color CollisionedChamberColor => Color.Violet;

    public bool IsPassthrought;

    public Model Model { get; private set; }
    public Vector3 Position;
    public float Rotation { get; set; }     // rotacion que se le quiera dar para generar variacion entre los modelos

    public const float Scale = GameConfig.Assets.DecorationScale;

    public const float CollisionChamberScale = GameConfig.Assets.DecorationChamberScale;

    private Matrix _world;

    private Texture2D _texture;

    public void LoadContent(Model model, float angle, Effect effect, Texture2D texture)
    {
        Model = model;

        _effect = effect;

        _texture = texture;

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

        InitializeCollisionChamber(model);
    }

    public virtual void InitializeCollisionChamber(Model model) {}

    public virtual bool UpdateCollisions(BoundingSphere tankSphere) { return false; }

    public virtual void Update()
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

    public virtual void DrawCollisionChamber(Gizmo gizmos) {}
}