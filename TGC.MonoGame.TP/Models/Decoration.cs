using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using BepuPhysics;
using BepuPhysics.Collidables;
// Alias para evitar la ambigüedad molesta entre los dos motores que no se como solucionar ;_;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using BepuVector3 = System.Numerics.Vector3;
//No entiendo por que debo agregar otra vez estas librerias si ya estan en decorationnnn
using TGC.MonoGame.TP.Collisions;
using TGC.MonoGame.TP.Gizmos;

namespace TGC.MonoGame.TP.Models.Decorations;
/// <summary>
/// Decoraciones dentro de escenario: rocas, arboles, cactus, etc.
/// </summary>
public class Decoration
{

    protected Model _model;
    protected Texture2D _texture;
    protected Matrix _world;
    protected Vector3 _position; //Vector3 de monogame por si en la terminal me vuelve a tirar "qui ni sibi bujuju"
    protected float _visualScale;
    protected string _path;

    public Vector3 Position => _position; //Es la variable de solo lectura de la posicion

    public Decoration(Vector3 position, string path)
    {
        _position = position;
        _path = path;
        _visualScale = 1f; //Hasta delimitar el tamaño de cada modelo con el modelo fisico
    }

    //CARGA DE CONTENIDO (Modificable)
    public virtual void LoadContent(ContentManager content, Simulation simulation, Effect effect)
    {
        _model = content.Load<Model>(AssetsManager.ContentFolder3D + _path);
        _texture = content.Load<Texture2D>(AssetsManager.ContentFolderTextures + "paleta_256x512"); //Aprovechando que todos usan la misma imagen
        var instanciaEffect = effect.Clone(); //Como lo clono en vez de usar el mismo comparto el codigo pero no el parametro world ni view que varian de modelo a modelo
        //Para cada malla de mi coleccion de mallas del modelo
        foreach (var mesh in _model.Meshes) 
        {
           //Para cada parte de la malla de mi coleccion de partes de la malla
            foreach (var meshPart in mesh.MeshParts)
            {
                // Reemplazamos el efecto por defecto del modelo por el nuestro
                meshPart.Effect = instanciaEffect;
            } 
        }
    }

    //ACTUALIZO (Modificable)
    public virtual void Update(Simulation simulation) { } //Varia de modelo a modelo

    //DIBUJO LAS COLISIONES (Modificable)
    public virtual void DrawCollisionChamber(Gizmo gizmos, Simulation simulation) {}

    //DIBUJO (No se toca por los hijos)
    public void Draw(Matrix view, Matrix projection)
    {
        if (_model == null) return;

        //Para cada malla en la coleccion de mallas del modelo
        foreach (var mesh in _model.Meshes)
        {
            //En contraposicion a lo que estabamos haciendo en la clase Decoration, ahora buscamos el efecto de cada parte ya que lo asignamos en el LoadContent
            foreach (var meshPart in mesh.MeshParts)
            {
                var effect = meshPart.Effect;
                //Coloco los parametros de world, view y projection
                effect.Parameters["World"].SetValue(_world);
                effect.Parameters["View"].SetValue(view);
                effect.Parameters["Projection"].SetValue(projection);
                effect.Parameters["ModelTexture"].SetValue(_texture);
            }
            mesh.Draw();
        }
    }
}