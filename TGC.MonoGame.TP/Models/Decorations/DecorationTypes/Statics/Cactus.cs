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
{//Estaticos (3 tipos) - Cilindros

    public class Cactus : Static
    {
        private float _radius;
        private float _height;
        //No tiene bandera porque no muere

        public Cactus(Vector3 position, string path) : base(position, path) { } //Decoration ya hace lo necesario

        //CARGO EL CONTENIDO (Modificacion de la funcion en DECORATION)
        public override void LoadContent(ContentManager content, Simulation simulation, Effect effect)
        {
            base.LoadContent(content, simulation, effect);
            // Calculo de escala (Usando una funcion auxiliar para obtener vertices)
            // BoundingBox box = ... (aun no xd)
            _height = _dimensions.Z/2;
            _radius = Math.Max(_dimensions.X, _dimensions.Y) / 2f;
            _visualScale = 1f; // Valor de ejemplo, esto lo cambio con lo que haga de BoundingBox

            // Creo el cuerpo en Bepu (Es la configuracion de la fisica)
            var shape = new Cylinder(_radius, _height);
            var shapeIndex = simulation.Shapes.Add(shape);

            //Como es estatico no necesito la inercia

            // Posicion inicial, se ajusta el centro (Bepu usa el centro, MonoGame la base)
            //Uso la posicion del modelo visual para definir donde ubico el modelo fisico al inicio, pero la altura no por lo del pivote (el centro del modelo)
            var centerPos = new System.Numerics.Vector3(_position.X, _position.Y + _height, _position.Z);
            
            //Añado el cuerpo estatico a la simulacion
            _staticHandle = simulation.Statics.Add(new StaticDescription(centerPos, shapeIndex));

            //Problema de que el modelo este acostado
            Matrix rotation = Matrix.CreateRotationX(MathHelper.ToRadians(-90));

            //Como mi modelo es estatico calculo la matriz de mundo una sola vez
            modificarMatrixWorld(rotation, _height);
        }

        //ACTUALIZO (Modificacion de la funcion en DECORATION)
        //No hace nada (agradecida)
        /*
            Los objetos dinamicos pueden salir volando en mil pedazos, desaparecer, moverse. Los objetos estaticos no hacen
            nada de eso, por eso no actualizo nada, no necesito hacerlo porque siempre se queda ahi... existiendo
        */
        
        //DIBUJO LAS COLISIONES (Modificacion de la funcion en DECORATION)
        public override void DrawCollisionChamber(Gizmo gizmos, Simulation simulation)
        {   
            //Color colorActual = _touchingDecoration ? Color.Violet : Color.Green; //Violeta si colisiono, verde si es normal - para cuando cree _touchingDecoration

            Matrix gizmoWorld = Matrix.CreateScale(_radius, _height, _radius)
                                * Matrix.CreateTranslation(new Vector3(_position.X, _position.Y + _height, _position.Z));

            gizmos.DrawAxes(gizmoWorld);
            gizmos.DrawCylinder(gizmoWorld, Color.Violet); //Por ahora voy a usar el verde para los dinamicos y el violeta para los estaticos
        }

        //No hace nada si el tanque lo choca por lo que no existe HandleCollision
    }
}