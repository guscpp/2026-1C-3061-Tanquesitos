using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Viewer.Gizmos.Geometry;

namespace TGC.MonoGame.TP.Viewer.Gizmos
{
    public class Gizmos
    {
        public Gizmos()
        {
            _noDepth = new DepthStencilState();
            _noDepth.DepthBufferEnable = false;
            _noDepth.DepthBufferFunction = CompareFunction.Always;            
        }

        private EffectPass _backgroundPass;
        private EffectPass _foregroundPass;
        private Effect _effect;
        private EffectParameter _worldViewProjectionParameter;

        private EffectParameter _colorParameter;    // color del gizmo?
        private Color _baseColor;
        private CubeGeometry _cube;
        private CylinderGeometry _cylinder;

        // matrices:
        private Matrix _view;                       // matriz vista
        private Matrix _viewProjection;             
        private Matrix _projection;                  // matriz proyeccion

        private GraphicsDevice _graphicsDevice; 
        private ContentManager _content;

        private readonly Dictionary<GizmoGeometry, Dictionary<Color, List<Matrix>>> _drawInstances = new ();

        private readonly DepthStencilState _noDepth;

        public bool Enabled { get ; set; } = true;

        public void LoadContent(GraphicsDevice device, ContentManager content)
        {
            _graphicsDevice = device;
            _content = content;
            // shader para gizmos
            _effect = _content.Load<Effect>("Effects/Gizmos");

            // dibuja el gizmo detras de los objetos
            _backgroundPass = _effect.CurrentTechnique.Passes["Background"];
            // dibuja el gizmo delante del objeto 
            _foregroundPass = _effect.CurrentTechnique.Passes["Foreground"];
            _worldViewProjectionParameter = _effect.Parameters["WorldViewProjection"];
            _colorParameter = _effect.Parameters["Color"];

            _cube = new CubeGeometry(_graphicsDevice);
            _cylinder = new CylinderGeometry(_graphicsDevice, 20);

            _drawInstances[_cube] = new Dictionary<Color, List<Matrix>>();
            _drawInstances[_cylinder] = new Dictionary<Color, List<Matrix>>();
        }    

        /// <summary>
        ///     Adds a draw instance specifying the geometry, its color and the world matrix to use when drawing.
        /// </summary>
        /// <param name="type">The GizmoGeometry to be drawn.</param>
        /// <param name="color">The color of the geometry.</param>
        /// <param name="world">The world matrix to be used when drawing.</param>
        private void AddDrawInstance(GizmoGeometry type, Color color, Matrix world)
        {
            var instancesByType = _drawInstances[type];
            instancesByType.TryAdd(color, new List<Matrix>());
            instancesByType[color].Add(world * _viewProjection);
        }

        public void SetColor(Color color)
        {
            _baseColor = color;
        }

        /// <summary>
        ///     Draws a wire cube with an origin and size using the Gizmos color.
        /// </summary>
        /// <param name="origin">The position of the cube.</param>
        /// <param name="size">The size of the cube.</param>
        public void DrawCube(Vector3 origin, Vector3 size)
        {
            DrawCube(origin, size, _baseColor);
        }

        /// <summary>
        ///     Draws a wire cube with a World matrix using the Gizmos color.
        /// </summary>
        /// <param name="world">The World matrix of the cube.</param>
        public void DrawCube(Matrix world)
        {
            DrawCube(world, _baseColor);
        }

        /// <summary>
        ///     Draws a wire cube with a World matrix using the specified color.
        /// </summary>
        /// <param name="world">The World matrix of the cube.</param>
        /// <param name="color">The color of the cube.</param>
        public void DrawCube(Matrix world, Color color)
        {
            AddDrawInstance(_cube, color, world);
        }

        /// <summary>
        ///     Draws a wire cube with an origin and size using the specified color.
        /// </summary>
        /// <param name="origin">The position of the cube.</param>
        /// <param name="size">The size of the cube.</param>
        /// <param name="color">The color of the cube.</param>
        public void DrawCube(Vector3 origin, Vector3 size, Color color)
        {
            var world = CubeGeometry.CalculateWorld(origin, size);
            AddDrawInstance(_cube, color, world);
        }

        /// <summary>
        ///     Draws a wire cylinder with an origin, rotation and size using the Gizmos color.
        /// </summary>
        /// <param name="origin">The position of the cylinder.</param>
        /// <param name="rotation">A rotation matrix to set the orientation of the cylinder. The cylinder is by default XZ aligned.</param>
        /// <param name="size">The size of the cylinder.</param>
        public void DrawCylinder(Vector3 origin, Matrix rotation, Vector3 size)
        {
            DrawCylinder(origin, rotation, size, _baseColor);
        }

        /// <summary>
        ///     Draws a wire cylinder with an origin, rotation and size using the specified color.
        /// </summary>
        /// <param name="origin">The position of the cylinder.</param>
        /// <param name="rotation">A rotation matrix to set the orientation of the cylinder. The cylinder is by default XZ aligned.</param>
        /// <param name="size">The size of the cylinder.</param>
        /// <param name="color">The color of the cylinder.</param>
        public void DrawCylinder(Vector3 origin, Matrix rotation, Vector3 size, Color color)
        {
            var world = CylinderGeometry.CalculateWorld(origin, rotation, size);
            AddDrawInstance(_cylinder, color, world);
        }

        /// <summary>
        ///     Draws a wire cylinder with a World matrix using the Gizmos color.
        /// </summary>
        /// <param name="world">The World matrix of the cylinder.</param>
        public void DrawCylinder(Matrix world)
        {
            DrawCylinder(world, _baseColor);
        }

        /// <summary>
        ///     Draws a wire cylinder with a World matrix using the specified color.
        /// </summary>
        /// <param name="world">The World matrix of the cylinder.</param>
        /// <param name="color">The color of the cylinder.</param>
        public void DrawCylinder(Matrix world, Color color)
        {
            AddDrawInstance(_cylinder, color, world);
        }
        
        public void UpdateViewProjection(Matrix view, Matrix projection)
        {
            _view = view;
            _projection = projection;
            _viewProjection = _view * _projection;
        }

        /// <summary>
        ///     Draws all Gizmos that are sub-classes of GizmoGeometry.
        /// </summary>
        /// <param name="pass">The pass from an effect to draw the geometry with.</param>
        private void DrawBaseGizmosGeometries(EffectPass pass)
        {
            var count = 0;
            List<Matrix> matrices;
            foreach (var drawInstance in _drawInstances)
            {
                var geometry = drawInstance.Key;
                geometry.Bind();

                foreach (var colorEntry in drawInstance.Value)
                {
                    _colorParameter.SetValue(colorEntry.Key.ToVector3());

                    matrices = colorEntry.Value;
                    count = matrices.Count;

                    for (var index = 0; index < count; index++)
                    {
                        _worldViewProjectionParameter.SetValue(matrices[index]);
                        pass.Apply();
                        geometry.Draw();
                    }
                }
            }
        }

        /// <summary>
        ///     Effectively draws the geometry using the parameters from past draw calls. Should be used after calling the other
        ///     draw methods.
        /// </summary>
        public void Draw()
        {
            if (!Enabled)
                return;

            // Save our depth state, then use ours
            var depth = _graphicsDevice.DepthStencilState;
            _graphicsDevice.DepthStencilState = _noDepth;

            DrawBaseGizmosGeometries(_backgroundPass);
            // Restore our depth
            _graphicsDevice.DepthStencilState = depth;
            // Draw our foreground geometry
            DrawBaseGizmosGeometries(_foregroundPass);

            // limpia las lineas y vuelve a dibujarlas en el siguiente update
            CleanDrawInstances();  
        }

        /// <summary>
        ///     Clears all the draw instances, so we don't draw the same as the past frame.
        /// </summary>
        private void CleanDrawInstances()
        {
            _drawInstances[_cube].Clear();
            _drawInstances[_cylinder].Clear();
        }

        /// <summary>
        ///     Disposes the used resources (geometries and content).
        /// </summary>
        public void Dispose()
        {
            _cube.Dispose();
            _cylinder.Dispose();
            _effect.Dispose();
            _content.Dispose();
        }
    }
}