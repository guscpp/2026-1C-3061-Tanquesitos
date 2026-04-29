using Microsoft.Xna.Framework.Graphics;

namespace TGC.MonoGame.TP.Viewer.Gizmos.Geometry
{
    abstract class GizmoGeometry
    {
        private readonly GraphicsDevice _graphicsDevice;
        private VertexBuffer _vertexBuffer;                 // arr de vertices
        private IndexBuffer _indexBuffer;                   // arr de indices
        private int _primitiveCount;                        // cantidad de lineas a dibujar 

        public GizmoGeometry(GraphicsDevice device)
        {
            _graphicsDevice = device;
        }

        // inicializa los vertices con un arr pasado por parametro
        protected void InitializeVertix(VertexPosition[] positions)
        {
            _vertexBuffer =
                new VertexBuffer(_graphicsDevice, typeof(VertexPosition), positions.Length, BufferUsage.WriteOnly);
            _vertexBuffer.SetData(positions);
        }
        // inicializa los indices con un arr pasado por parametro
        protected void InitializeIndex(ushort[] indices)
        {
            _indexBuffer = 
                new IndexBuffer(_graphicsDevice, IndexElementSize.SixteenBits, indices.Length, BufferUsage.WriteOnly);
            _indexBuffer.SetData(indices);

            _primitiveCount = indices.Length/2;     // por cada linea hay dos indices?
        }

        /// <summary>
        ///     Binds the geometry to the Graphics Device.
        /// </summary>
        public virtual void Bind()
        {
            _graphicsDevice.SetVertexBuffer(_vertexBuffer);
            _graphicsDevice.Indices = _indexBuffer;
        }

        // DIBUJA!! :D
        public virtual void Draw()
        {
            _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, _primitiveCount);
        }

        // libera
        public void Dispose()
        {
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
        }
    }
        
}