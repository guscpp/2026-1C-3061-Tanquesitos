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
{//Dinamico - Esfera
    public class Plant : Dinamic
    {
        //private BodyHandle bodyHandle;
        private float _radius;
        private readonly Random _random = new();
        //public bool IsDead { get; private set; } //La banderita que determina si fue o no colisionado

        public Plant(Vector3 position, string path) : base(position, path) { } //Decoration ya hace lo necesario

        //CARGO EL CONTENIDO (Modificacion de la funcion en DECORATION)
        public override void LoadContent(ContentManager content, Simulation simulation, Effect effect)
        {
            base.LoadContent(content, simulation, effect);
            // Calculo de escala (Usando una funcion auxiliar para obtener vertices)
            // BoundingBox box = ... (aun no xd)
            _radius = Math.Max(_dimensions.X, Math.Max(_dimensions.Y, _dimensions.Z)) / 2f;
            _visualScale = 1f; // Valor de ejemplo, esto lo cambio con lo que haga de BoundingBox

            // Creo el cuerpo en Bepu (Es la configuracion de la fisica)
            var shape = new Sphere(_radius);
            var shapeIndex = simulation.Shapes.Add(shape);

            //Calculo de la inercia (masa de 1kg porque si) - en el caso de la esfera la inercia se distribuye igual en todos los lados
            var inertia = shape.ComputeInertia(1f);

            // Posicion inicial, se ajusta el centro (Bepu usa el centro, MonoGame la base)
            //Uso la posicion del modelo visual para definir donde ubico el modelo fisico al inicio, pero la altura no por lo del pivote (el centro del modelo)
            var initialPos = new System.Numerics.Vector3(_position.X, _position.Y + _radius, _position.Z);
            
            //Añado el cuerpo dinamico a la simulacion
            bodyHandle = simulation.Bodies.Add(BodyDescription.CreateDynamic(
                initialPos, inertia, shapeIndex, 0.01f));

            // Le doy una identidad al cuerpo para reconocerlo en colisiones
            simulation.Bodies[bodyHandle].Collidable.Continuity = ContinuousDetection.Passive;
        }

        //ACTUALIZO (Modificacion de la funcion en DECORATION)
        public override void Update(Simulation simulation)
        {
            if (IsDead) return;

            // Tomo la posicion actual en la simulacion
            var bodyReference = simulation.Bodies[bodyHandle];
            var pose = bodyReference.Pose;

            //Reviso la velocidad del objeto, si su velocidad disminuyo hasta ser menor a 5 le doy un empujoncito
            if (bodyReference.Velocity.Linear.Length() < 5f) 
            {
                //Use el random para que el empuje sea hacia distintos lados (Ahora estan locas)
                bodyReference.ApplyLinearImpulse(new BepuVector3(_random.Next(-5,6), 0, 0)); // Empujoncito diagonal a derecha o izquierda
                bodyReference.ApplyAngularImpulse(new BepuVector3(0.5f, 0, 0.5f)); // Esto hace que la planta ruede, impulsando a que siga moviendose luego del empujoncito
            }

            // Convierto la orientacion (uso Quaternianos para que gire como se debe) y pose de Bepu a Monogame
            Matrix rotation = Matrix.CreateFromQuaternion(new Quaternion(pose.Orientation.X, pose.Orientation.Y, pose.Orientation.Z, pose.Orientation.W));
            Vector3 position = new Vector3(pose.Position.X, pose.Position.Y, pose.Position.Z);

            // Calculo de la matriz de mundo
            modificarMatrixWorld(rotation, position);
        }
        
        //DIBUJO LAS COLISIONES (Modificacion de la funcion en DECORATION)
        public override void DrawCollisionChamber(Gizmo gizmos, Simulation simulation)
        {   
            if (IsDead) return; //Si el modelo desaparece no hay que dibujarlo
            //Color colorActual = _touchingDecoration ? Color.Violet : Color.Green; //Violeta si colisiono, verde si es normal

            // Tomo solo la rotacion y posicion que vienen de la Pose de Bepu (el modelo se supone que ya concuerda con el modelo fisico).            
            var pose = simulation.Bodies[bodyHandle].Pose;
            
            Vector3 position = new Vector3(pose.Position.X, pose.Position.Y, pose.Position.Z);

            //Tamaño (yo pensaba que se dibujaba igual que el resto XD)
            Vector3 sphereSize = new Vector3(_radius);

            gizmos.DrawSphere(position, sphereSize, Color.Green);
        }

    }
}