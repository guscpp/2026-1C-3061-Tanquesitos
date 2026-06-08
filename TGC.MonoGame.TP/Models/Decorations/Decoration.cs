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
    public const string ContentFolder3D = "Models/";
    public const string ContentFolderTextures = "Textures/";

    protected Model _model;
    protected Texture2D _texture;
    protected Matrix _world;
    protected Vector3 _position; //Vector3 de monogame por si en la terminal me vuelve a tirar "qui ni sibi bujuju"
    protected float _visualScale;
    protected string _path;
    protected BoundingBox _boundingBox; //la cajita xd
    protected Vector3 _dimensions; //guarda el ancho, alto y largo del modelo
    protected Vector3 _modelCenter; //ubicacion del pivote

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
        _model = content.Load<Model>(ContentFolder3D + _path);
        _texture = content.Load<Texture2D>(ContentFolderTextures + "paleta_256x512"); //Aprovechando que todos usan la misma imagen
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

        _boundingBox = BoundingVolumesUtils.CreateBoundingBox(_model);
        _dimensions = _boundingBox.Max - _boundingBox.Min; //tomo el punto maximo y el punto minimo de mi caja y luego calculo la diferencia para saber la distancia, se usa el Min porque el modelo puede estar un poquito mal posicionado y no lo voy andar corrigiendo 80 veces en blender, ya lo intente
        _modelCenter = (_boundingBox.Max + _boundingBox.Min) / 2f; //ajustamos el pivote que originalmente esta en los pies del modelo visual para que concuerde con el del modelo fisico que es en el centro
    }

    //ACTUALIZO (Modificable)
    public virtual void Update(Simulation simulation) { } //Varia de modelo a modelo

    //DIBUJO LAS COLISIONES (Modificable)
    public virtual void DrawCollisionChamber(Gizmo gizmos, Simulation simulation) {}

    //DIBUJO (Modificable)
    public virtual void Draw(Matrix view, Matrix projection)
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