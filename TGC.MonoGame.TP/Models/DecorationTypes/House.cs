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
{//Estaticos(4 tipos) - Caja
    public class House : Decoration
    {
        private StaticHandle _staticHandle;
        private float _width;
        private float _height;
        private float _lenght;
        //No tiene bandera porque no muere

        public House(Vector3 position, string path) : base(position, path) { } //Decoration ya hace lo necesario

        //CARGO EL CONTENIDO (Modificacion de la funcion en DECORATION)
        public override void LoadContent(ContentManager content, Simulation simulation, Effect effect)
        {
            base.LoadContent(content, simulation, effect);
            // Calculo de escala (Usando una funcion auxiliar para obtener vertices)
            // BoundingBox box = ... (aun no xd)
            _width = _dimensions.X;
            _height = _dimensions.Y;
            _lenght = _dimensions.Z;
            _visualScale = 1f; // Valor de ejemplo, esto lo cambio con lo que haga de BoundingBox

            // Creo el cuerpo en Bepu (Es la configuracion de la fisica)
            var shape = new Box(_width, _height, _lenght);
            var shapeIndex = simulation.Shapes.Add(shape);

            //Como es estatico no necesito la inercia

            // Posicion inicial, se ajusta el centro (Bepu usa el centro, MonoGame la base)
            //Uso la posicion del modelo visual para definir donde ubico el modelo fisico al inicio, pero la altura no por lo del pivote (el centro del modelo)
            var initialPos = new System.Numerics.Vector3(_position.X, _position.Y + (_height / 2), _position.Z);
            
            //Añado el cuerpo estatico a la simulacion
            _staticHandle = simulation.Statics.Add(new StaticDescription(initialPos, shapeIndex));

            //Problema de que el modelo este acostado
            Matrix rotation = Matrix.CreateRotationX(MathHelper.ToRadians(-90));

            //Como mi modelo es estatico calculo la matriz de mundo una sola vez
            modificarMatrixWorld(rotation);
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
            //Color colorActual = _touchingDecoration ? Color.Violet : Color.Green; //Violeta si colisiono, verde si es normal

            Matrix gizmoWorld = Matrix.CreateScale(_width, _height, _lenght) 
                                * Matrix.CreateTranslation(_position + _modelCenter);

            gizmos.DrawCube(gizmoWorld, Color.Violet);
        }

        //No hace nada si el tanque lo choca por lo que no existe HandleCollision
    }
} 