using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using BepuPhysics;
using BepuPhysics.Collidables;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using BepuVector3 = System.Numerics.Vector3;
using TGC.MonoGame.TP.Gizmos;

namespace TGC.MonoGame.TP.Models.Decorations
{//Estaticos(4 tipos) - Caja
    public class House : Static
    {
        private float _width;
        private float _height;
        private float _lenght;

        public House(Vector3 position, string path) : base(position, path) { } //Decoration ya hace lo necesario

        //CARGO EL CONTENIDO (Modificacion de la funcion en DECORATION)
        public override void LoadContent(ContentManager content, Simulation simulation, Effect effect)
        {
            base.LoadContent(content, simulation, effect);
            _normalOffsetScale = 0.6f;
            // Calculo de escala (Usando una funcion auxiliar para obtener vertices)
            _width = _dimensions.X;
            _height = _dimensions.Z; //en los fbx la altura esta 'acostada' por usar Y-up
            _lenght = _dimensions.Y;
            _visualScale = 1f; // Valor de ejemplo, esto lo cambio con lo que haga de BoundingBox

            // Creo el cuerpo en Bepu (Es la configuracion de la fisica)
            var shape = new Box(_width, _height, _lenght);
            var shapeIndex = simulation.Shapes.Add(shape);

            //Como es estatico no necesito la inercia

            // Posicion inicial, se ajusta el centro (Bepu usa el centro, MonoGame la base)
            //Uso la posicion del modelo visual para definir donde ubico el modelo fisico al inicio, pero la altura no por lo del pivote (el centro del modelo)
            var initialPos = new BepuVector3(_position.X, _position.Y+(_height/2), _position.Z);
            
            //Añado el cuerpo estatico a la simulacion
            _staticHandle = simulation.Statics.Add(new StaticDescription(initialPos, shapeIndex));

            //Problema de que el modelo este acostado
            Matrix rotation = Matrix.CreateRotationX(MathHelper.ToRadians(-90));

            //Como mi modelo es estatico calculo la matriz de mundo una sola vez
            modificarMatrixWorld(rotation, _height / 2f);
        }
        
        //DIBUJO LAS COLISIONES (Modificacion de la funcion en DECORATION)
        public override void DrawCollisionChamber(Gizmo gizmos, Simulation simulation)
        {
            Matrix gizmoWorld = Matrix.CreateScale(_width, _height, _lenght) 
                                * Matrix.CreateTranslation(new Vector3(_position.X, _position.Y+(_height/2), _position.Z));

            gizmos.DrawAxes(gizmoWorld);
            gizmos.DrawCube(gizmoWorld, Color.Violet);
        }

    }
} 