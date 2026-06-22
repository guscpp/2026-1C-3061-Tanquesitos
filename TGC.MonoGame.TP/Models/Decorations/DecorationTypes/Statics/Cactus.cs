using System;
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

namespace TGC.MonoGame.TP.Models.Decorations
{

    public class Cactus : Static
    {
        private float _radius;
        private float _height;

        public Cactus(Vector3 position, string path) : base(position, path) { } //Decoration ya hace lo necesario

        public override void LoadContent(ContentManager content, Simulation simulation, Effect effect)
        {
            //_normalOffsetScale = 0.1f;
            base.LoadContent(content, simulation, effect);
            
            // La altura la dejamos igual
            _height = _dimensions.Z / 2f;
            
            // MODIFICACIÓN: Tomamos el promedio del ancho en X e Y, y lo achicamos a la mitad (factor 0.5f)
            // Esto hace que el cilindro físico se concentre en el tronco y no en las ramas externas
            _radius = ((_dimensions.X + _dimensions.Y) / 4f) * 0.5f; 

            _visualScale = 1f; 

            // Creo el cuerpo en Bepu usando el nuevo radio corregido
            var shape = new Cylinder(_radius, _height);
            var shapeIndex = simulation.Shapes.Add(shape);

            // Posicion inicial, se ajusta el centro
            var centerPos = new System.Numerics.Vector3(_position.X, _position.Y + _height, _position.Z);
            
            _staticHandle = simulation.Statics.Add(new StaticDescription(centerPos, shapeIndex));

            // Matriz de mundo para el modelo visual
            Matrix rotation = Matrix.CreateRotationX(MathHelper.ToRadians(-90));
            modificarMatrixWorld(rotation, _height);
        }
        
        //DIBUJO LAS COLISIONES (Modificacion de la funcion en DECORATION)
        public override void DrawCollisionChamber(Gizmo gizmos, Simulation simulation)
        {
            Matrix gizmoWorld = Matrix.CreateScale(_radius, _height, _radius)
                                * Matrix.CreateTranslation(new Vector3(_position.X, _position.Y + _height, _position.Z));

            gizmos.DrawAxes(gizmoWorld);
            gizmos.DrawCylinder(gizmoWorld, Color.Violet);
        }

    }
}