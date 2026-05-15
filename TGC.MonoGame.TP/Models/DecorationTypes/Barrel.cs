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
{ //Dinamico - Cilindro
    public class Barrel : Decoration
    {
        private BodyHandle _bodyHandle;
        private float _radius;
        private float _height;
        public bool IsDead { get; private set; } //La banderita que determina si fue o no colisionado

        public Barrel(Vector3 position, string path) : base(position, path) { } //Decoration ya hace lo necesario

        //CARGO EL CONTENIDO (Modificacion de la funcion en DECORATION)
        public override void LoadContent(ContentManager content, Simulation simulation, Effect effect)
        {
            base.LoadContent(content, simulation, effect);
            // Calculo de escala (Usando una funcion auxiliar para obtener vertices)
            // BoundingBox box = ... (aun no xd)
            _height = 5f;
            _radius = 2f;
            _visualScale = 1f; // Valor de ejemplo, esto lo cambio con lo que haga de BoundingBox

            // Creo el cuerpo en Bepu (Es la configuracion de la fisica)
            var shape = new Cylinder(_radius, _height);
            var shapeIndex = simulation.Shapes.Add(shape);

            //Calculo de la inercia (masa de 1kg porque si)
            //La variable de inercia no necesito guardarla como el radio del cilindro, es magica :D (problema de bepu)
            var inertia = shape.ComputeInertia(1f);

            // Posicion inicial, se ajusta el centro (Bepu usa el centro, MonoGame la base)
            //Uso la posicion del modelo visual para definir donde ubico el modelo fisico al inicio, pero la altura no por lo del pivote (el centro del modelo)
            var initialPos = new System.Numerics.Vector3(_position.X, _position.Y + (_height / 2), _position.Z);
            
            //Añado el cuerpo dinamico a la simulacion
            _bodyHandle = simulation.Bodies.Add(BodyDescription.CreateDynamic(
                initialPos, inertia, shapeIndex, 0.01f));

            // Le doy una identidad al cuerpo para reconocerlo en colisiones
            simulation.Bodies[_bodyHandle].Collidable.Continuity = ContinuousDetection.Passive;
        }

        //ACTUALIZO (Modificacion de la funcion en DECORATION)
        public override void Update(Simulation simulation)
        {
            if (IsDead) return;

            // Tomo la posicion actual en la simulacion
            var bodyReference = simulation.Bodies[_bodyHandle];
            var pose = bodyReference.Pose;

            // Convierto la orientacion (uso Quaternianos para que gire como se debe) y pose de Bepu a Monogame
            Matrix rotation = Matrix.CreateFromQuaternion(new Quaternion(pose.Orientation.X, pose.Orientation.Y, pose.Orientation.Z, pose.Orientation.W));
            //Problema, mis modelos visuales estan rotados, asi que debo "levantarlos" (girarlos 90 grados)
            Matrix rotationCorrect = Matrix.CreateRotationX(MathHelper.ToRadians(-90)) * rotation;
            Vector3 position = new Vector3(pose.Position.X, pose.Position.Y, pose.Position.Z);

            // Calculo de la matriz de mundo
            _world = Matrix.CreateScale(_visualScale) //Se escala el modelo
                     * Matrix.CreateTranslation(0, -_height / 2f, 0) //Se baja el modelo visual para que coincida con el modelo fisico
                     * rotationCorrect //Lo roto
                     * Matrix.CreateTranslation(position); //Lo muevo a donde esta el modelo fisico
        }
        
        //DIBUJO LAS COLISIONES (Modificacion de la funcion en DECORATION)
        public override void DrawCollisionChamber(Gizmo gizmos, Simulation simulation)
        {   
            if (IsDead) return; //Si el modelo desaparece no hay que dibujarlo
            //Color colorActual = _touchingDecoration ? Color.Violet : Color.Green; //Violeta si colisiono, verde si es normal - para cuando cree _touchingDecoration

            // Tomo solo la rotacion y posicion que vienen de la Pose de Bepu (el modelo se supone que ya concuerda con el modelo fisico).            
            var pose = simulation.Bodies[_bodyHandle].Pose;
            
            Matrix rotation = Matrix.CreateFromQuaternion(new Microsoft.Xna.Framework.Quaternion(
                pose.Orientation.X, pose.Orientation.Y, pose.Orientation.Z, pose.Orientation.W));
            
            Vector3 position = new Vector3(pose.Position.X, pose.Position.Y, pose.Position.Z);

            // Matriz --> volumen real de colision en el motor fisico
            // Uso el radio y altura para escalar el cilindro del Gizmo si es necesario
            Matrix gizmoWorld = Matrix.CreateScale(_radius * 2, _height, _radius * 2) 
                                * rotation 
                                * Matrix.CreateTranslation(position);

            gizmos.DrawCylinder(gizmoWorld, Color.Green);
        }

        //LOGICA DE COLISION
        public void HandleCollision()
        {
            // Aqui ira la logica de la colision, deberia (deberia xd) desaparecer si el tanque lo toca, lo mismo para todos los dinamicos
            IsDead = true;
            // La remocion de Bepu debe hacerse fuera del timestep
        }
    }
}