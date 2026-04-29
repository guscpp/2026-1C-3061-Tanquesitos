using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.Viewer.Gizmos.Geometry
{
    class CubeGeometry : GizmoGeometry
    {
        // usa el mismo device que la clase de la que hereda
        public CubeGeometry(GraphicsDevice device) : base(device)
        {
            var cubeVertices = new VertexPosition[8]
            { // vertices del cubo!
                new VertexPosition(new Vector3(0.5f, 0.5f, 0.5f)),
                new VertexPosition(new Vector3(-0.5f, 0.5f, 0.5f)),
                new VertexPosition(new Vector3(0.5f, -0.5f, 0.5f)),
                new VertexPosition(new Vector3(-0.5f, -0.5f, 0.5f)),
                new VertexPosition(new Vector3(0.5f, 0.5f, -0.5f)),
                new VertexPosition(new Vector3(-0.5f, 0.5f, -0.5f)),
                new VertexPosition(new Vector3(0.5f, -0.5f, -0.5f)),
                new VertexPosition(new Vector3(-0.5f, -0.5f, -0.5f))               
            };
            var indices = new ushort[24]    // 12 aristas
            {
                0, 1, 
                0, 2,  
                1, 3,
                3, 2, 

                4, 5, 
                4, 6, 
                5, 7,  
                7, 6,   

                0, 4,
                1, 5,
                2, 6,
                3, 7
            };
            InitializeIndex(indices);
            InitializeVertix(cubeVertices);
        }   
                // Util para entenderlo!
                //   6----------3
                //  /|         /|
                // 1----------0 |
                // | |        | |
                // | 7--------|-5
                // |/         |/
                // 4----------2

        public static Matrix CalculateWorld(Vector3 origin, Vector3 size)
        {
            return Matrix.CreateScale(size) * Matrix.CreateTranslation(origin);
        }
        
    }
}