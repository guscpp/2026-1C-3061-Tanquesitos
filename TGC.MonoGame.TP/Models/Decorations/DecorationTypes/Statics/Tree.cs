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
{//Estaticos - Cilindro

    public class Tree : Static
    {
        private float _radius;
        private float _height;

        public Tree(Vector3 position, string path) : base(position, path) { } //Decoration ya hace lo necesario

        // CARGO EL CONTENIDO (Modificacion de la funcion en DECORATION)
        public override void LoadContent(ContentManager content, Simulation simulation, Effect effect)
        {
            //_normalOffsetScale = 0.4f;
            base.LoadContent(content, simulation, effect);
            
            _height = _dimensions.Z / 2;
            
            _radius = 0.70f; 
            
            _visualScale = 1f; 

            // Creo el cuerpo en Bepu (Es la configuracion de la fisica)
            var shape = new Cylinder(_radius, _height);
            var shapeIndex = simulation.Shapes.Add(shape);

            // Posicion inicial, se ajusta el centro
            var centerPos = new System.Numerics.Vector3(_position.X, _position.Y + _height / 2f, _position.Z);
            
            // Añado el cuerpo estatico a la simulacion
            _staticHandle = simulation.Statics.Add(new StaticDescription(centerPos, shapeIndex));

            // Problema de que el modelo este acostado
            Matrix rotation = Matrix.CreateRotationX(MathHelper.ToRadians(-90));

            // Como mi modelo es estatico calculo la matriz de mundo una sola vez
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