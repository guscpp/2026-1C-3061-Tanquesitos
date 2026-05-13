using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TGC.MonoGame.TP.Gizmos.Geometry;

namespace TGC.MonoGame.TP.Gizmos
{
    /// <summary>
    ///     Renders Gizmos
    /// </summary>
    public class Gizmo
    {
        /// <summary>
        ///     Creates a GizmosRenderer.
        /// </summary>
        public Gizmo()
        {
            _noDepth = new DepthStencilState();
            _noDepth.DepthBufferEnable = false;
            _noDepth.DepthBufferFunction = CompareFunction.Always;
        }

        private EffectPass _backgroundPass;

        private Color _baseColor;
        private EffectParameter _colorParameter;
        private ContentManager _content;
        private CubeGeometry _cube;
        private CylinderGeometry _cylinder;

        private Effect _effect;
        private EffectPass _foregroundPass;

        private GraphicsDevice _graphicsDevice;

        private readonly DepthStencilState _noDepth;

        private readonly Dictionary<Color, List<Vector3[]>> _polyLinesToDraw = new ();
        private readonly Dictionary<GizmoGeometry, Dictionary<Color, List<Matrix>>> _drawInstances = new ();
        private Matrix _projection;
        private SphereGeometry _sphere;

        private Matrix _view;
        private Matrix _viewProjection;
        private EffectParameter _worldViewProjectionParameter;

        public bool Enabled { get; set; } = true;

        /// <summary>
        ///     Loads all the content necessary for drawing Gizmos.
        /// </summary>
        /// <param name="device">The GraphicsDevice to use when drawing. It is also used to bind buffers.</param>
        /// <param name="content">The ContentManager to manage Gizmos resources.</param>
        public void LoadContent(GraphicsDevice device, ContentManager content)
        {
            _graphicsDevice = device;

            _graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

            _content = content;

            _effect = _content.Load<Effect>("Effects/Gizmos");

            _backgroundPass = _effect.CurrentTechnique.Passes["Background"];
            _foregroundPass = _effect.CurrentTechnique.Passes["Foreground"];
            _worldViewProjectionParameter = _effect.Parameters["WorldViewProjection"];
            _colorParameter = _effect.Parameters["Color"];

            _cube = new CubeGeometry(_graphicsDevice);
            _sphere = new SphereGeometry(_graphicsDevice, 20);
            _cylinder = new CylinderGeometry(_graphicsDevice, 20);

            _drawInstances[_sphere] = new Dictionary<Color, List<Matrix>>();
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
        ///     Draws a wire sphere with an origin and size using the Gizmos color.
        /// </summary>
        /// <param name="origin">The position of the sphere.</param>
        /// <param name="size">The size of the sphere.</param>
        public void DrawSphere(Vector3 origin, Vector3 size)
        {
            DrawSphere(origin, size, _baseColor);
        }

        /// <summary>
        ///     Draws a wire sphere with an origin and size using the specified color.
        /// </summary>
        /// <param name="origin">The position of the sphere.</param>
        /// <param name="size">The size of the sphere.</param>
        /// <param name="color">The color of the sphere.</param>
        public void DrawSphere(Vector3 origin, Vector3 size, Color color)
        {
            var world = SphereGeometry.CalculateWorld(origin, size);
            AddDrawInstance(_sphere, color, world);
        }

        /// <summary>
        ///     Draws a contiguous line joining the given points and using the Gizmos color.
        /// </summary>
        /// <param name="points">The positions of the poly-line points in world space.</param>
        public void DrawPolyLine(Vector3[] points)
        {
            DrawPolyLine(points, _baseColor);
        }

        /// <summary>
        ///     Draws a contiguous line joining the given points and using the specified color.
        /// </summary>
        /// <param name="points">The positions of the poly-line points in world space.</param>
        /// <param name="color">The color of the poly-line.</param>
        public void DrawPolyLine(Vector3[] points, Color color)
        {
            _polyLinesToDraw.TryAdd(color, new List<Vector3[]>());
            _polyLinesToDraw[color].Add(points);
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

        /// <summary>
        ///     Sets the Gizmos color. All Gizmos drawn after are going to use this color if they do not specify one.
        /// </summary>
        /// <param name="color">The Gizmos color to set.</param>
        public void SetColor(Color color)
        {
            _baseColor = color;
        }

        /// <summary>
        ///     Updates the View and Projection matrices. Should be called on an Update loop after the camera is updated.
        /// </summary>
        /// <param name="view">The View matrix of a camera.</param>
        /// <param name="projection">The Projection matrix of a camera or a viewport.</param>
        public void UpdateViewProjection(Matrix view, Matrix projection)
        {
            _view = view;
            _projection = projection;
            _viewProjection = _view * _projection;
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

            CleanDrawInstances();
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
        ///     Clears all the draw instances, so we don't draw the same as the past frame.
        /// </summary>
        private void CleanDrawInstances()
        {
            _polyLinesToDraw.Clear();

            _drawInstances[_sphere].Clear();
            _drawInstances[_cube].Clear();
            _drawInstances[_cylinder].Clear();
        }

        /// <summary>
        ///     Disposes the used resources (geometries and content).
        /// </summary>
        public void Dispose()
        {
            _sphere.Dispose();
            _cube.Dispose();
            _cylinder.Dispose();
            _effect.Dispose();
            _content.Dispose();
        }
    }
}