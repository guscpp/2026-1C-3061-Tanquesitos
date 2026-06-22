using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
namespace TGC.MonoGame.TP.Models.Terrains;

/// <summary>
///     Terreno generado a partir de un heightmap con alturas variables
/// </summary>
public class Terrain
{
    //escalas para controlar el tamano y relieve del terreno
    private const float TerrainScale = GameConfig.Terrain.CellSizeMeters;       //100f
    private const float HeightScale = GameConfig.Terrain.MaxHeightMeters;       //3500f

    public float WidthUnits => 518f * TerrainScale / 2;   //unidades de ancho 

    private Texture2D _groundTexture;

    private readonly GraphicsDevice _graphicsDevice;
    private VertexBuffer _terrainVertexBuffer;
    private IndexBuffer _terrainIndexBuffer;
    private Effect _terrainEffect;
    private int _primitiveCount;

    //matriz que guardará los valores del mapa para que pueda usarlos fuera de la clase
    private float[,] _heights;
    private int _width;
    private int _height;

    private float _normalOffsetScale;

    public Terrain(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _normalOffsetScale = 0.2f;
    }

    public void LoadContent(Texture2D heightmapTexture, Texture2D groundTexture, Effect BasicShader)
    {
        CreateHeightmapMesh(heightmapTexture);

        _groundTexture = groundTexture;

        _terrainEffect = BasicShader;
    }

    private void CreateHeightmapMesh(Texture2D heightmapTexture)
    {
        _width = heightmapTexture.Width;
        _height = heightmapTexture.Height;
        _heights = new float[_width, _height];

        Color[] heightmapData = new Color[_width * _height];
        heightmapTexture.GetData(heightmapData);

        // 1. Usamos VertexPositionNormalTexture: listo para iluminación y texturas
        VertexPositionNormalTexture[] vertices = new VertexPositionNormalTexture[_width * _height];
        uint[] indices = new uint[(_width - 1) * (_height - 1) * 6];

        float halfWidth = _width * TerrainScale / 2f;
        float halfHeight = _height * TerrainScale / 2f;
        int index = 0;

        for (int z = 0; z < _height; z++)
        {
            for (int x = 0; x < _width; x++)
            {
                float height = heightmapData[z * _width + x].R / 255f * HeightScale;
                _heights[x, z] = height;

                Vector3 position = new Vector3(x * TerrainScale - halfWidth, height, z * TerrainScale - halfHeight);

                // 2. Cálculo de Normales (Diferencia finita central)
                // Obtenemos la altura de los vecinos para calcular la pendiente
                float hL = GetHeightAtPixel(Math.Max(0, x - 1), z);
                float hR = GetHeightAtPixel(Math.Min(_width - 1, x + 1), z);
                float hD = GetHeightAtPixel(x, Math.Max(0, z - 1));
                float hU = GetHeightAtPixel(x, Math.Min(_height - 1, z + 1));

                // El vector normal no normalizado. Y es la altura, por eso el componente Y es 2 * TerrainScale
                Vector3 normal = new Vector3(hL - hR, 2.0f * TerrainScale, hD - hU);
                normal.Normalize();

                // 3. Cálculo de UVs para Tiling
                float u = x * TerrainScale / GameConfig.Terrain.TextureTileSize;
                float v = z * TerrainScale / GameConfig.Terrain.TextureTileSize;

                vertices[z * _width + x] = new VertexPositionNormalTexture(position, normal, new Vector2(u, v));

                // Generación de índices (sin cambios, ya era correcta)
                if (x < _width - 1 && z < _height - 1)
                {
                    int topLeft = z * _width + x;
                    int topRight = topLeft + 1;
                    int bottomLeft = (z + 1) * _width + x;
                    int bottomRight = bottomLeft + 1;

                    indices[index++] = (uint)topLeft;
                    indices[index++] = (uint)topRight;
                    indices[index++] = (uint)bottomLeft;
                    indices[index++] = (uint)topRight;
                    indices[index++] = (uint)bottomRight;
                    indices[index++] = (uint)bottomLeft;
                }
            }
        }

        _primitiveCount = index / 3;
        _terrainVertexBuffer = new VertexBuffer(_graphicsDevice, VertexPositionNormalTexture.VertexDeclaration, vertices.Length, BufferUsage.WriteOnly);
        _terrainVertexBuffer.SetData(vertices);

        _terrainIndexBuffer = new IndexBuffer(_graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Length, BufferUsage.WriteOnly);
        _terrainIndexBuffer.SetData(indices);
    }

    public float GetHeightAtPixel(int x, int z)
    {
        x = MathHelper.Clamp(x, 0, _width - 1);
        z = MathHelper.Clamp(z, 0, _height - 1);
        return _heights[x, z];
    }

    public float GetHeight(float posX, float posZ)
    {
        float x = (posX / TerrainScale) + (_width / 2f);
        float z = (posZ / TerrainScale) + (_height / 2f);

        int x0 = (int)Math.Floor(x);
        int x1 = MathHelper.Clamp(x0 + 1, 0, _width - 1);
        int z0 = (int)Math.Floor(z);
        int z1 = MathHelper.Clamp(z0 + 1, 0, _height - 1);

        x0 = MathHelper.Clamp(x0, 0, _width - 1);
        z0 = MathHelper.Clamp(z0, 0, _height - 1);

        float tx = x - x0;
        float tz = z - z0;

        float h00 = _heights[x0, z0];
        float h10 = _heights[x1, z0];
        float h01 = _heights[x0, z1];
        float h11 = _heights[x1, z1];

        float h0 = MathHelper.Lerp(h00, h10, tx);
        float h1 = MathHelper.Lerp(h01, h11, tx);
        return MathHelper.Lerp(h0, h1, tz);
    }

    /// <summary>
    ///     Crea una malla de colision para BepuPhysics usando submuestreo configurable.
    ///     Usa el mismo heightmap visual, pero saltea celdas según 'step' para optimizar CPU.
    /// </summary>
    public StaticHandle CreatePhysicsTerrain(Simulation simulation)
    {
        const int physicsStep = GameConfig.Terrain.PhysicsSubsampleStep;

        // 1. Calcular la cantidad exacta de quads, redondeando hacia arriba para cubrir los bordes
        int quadsX = (int)Math.Ceiling((float)(_width - 1) / physicsStep);
        int quadsZ = (int)Math.Ceiling((float)(_height - 1) / physicsStep);
        int triangleCount = quadsX * quadsZ * 2;

        var pool = simulation.BufferPool;
        pool.Take(triangleCount, out Buffer<Triangle> triangles);
        int idx = 0;

        float halfWidth = _width * TerrainScale / 2f;
        float halfHeight = _height * TerrainScale / 2f;

        // 2. Usar Math.Min para asegurar que el último paso aterrice exactamente en el borde del mapa (píxel 511)
        for (int i = 0; i < quadsZ; i++)
        {
            int z = i * physicsStep;
            int nextZ = Math.Min(z + physicsStep, _height - 1);

            for (int j = 0; j < quadsX; j++)
            {
                int x = j * physicsStep;
                int nextX = Math.Min(x + physicsStep, _width - 1);

                // Leemos alturas EXACTAS (sin interpolar) para la física
                float h00 = _heights[x, z];
                float h10 = _heights[nextX, z];
                float h01 = _heights[x, nextZ];
                float h11 = _heights[nextX, nextZ];

                // Mismas coordenadas de mundo que el terreno visual
                System.Numerics.Vector3 v00 = new System.Numerics.Vector3(x * TerrainScale - halfWidth, h00, z * TerrainScale - halfHeight);
                System.Numerics.Vector3 v10 = new System.Numerics.Vector3(nextX * TerrainScale - halfWidth, h10, z * TerrainScale - halfHeight);
                System.Numerics.Vector3 v01 = new System.Numerics.Vector3(x * TerrainScale - halfWidth, h01, nextZ * TerrainScale - halfHeight);
                System.Numerics.Vector3 v11 = new System.Numerics.Vector3(nextX * TerrainScale - halfWidth, h11, nextZ * TerrainScale - halfHeight);

                triangles[idx++] = new Triangle(v00, v10, v01);
                triangles[idx++] = new Triangle(v10, v11, v01);
            }
        }

        // Bepu toma ownership del buffer y genera el BVH internamente
        var meshShape = new Mesh(triangles, System.Numerics.Vector3.One, pool);
        TypedIndex shapeIndex = simulation.Shapes.Add(meshShape);
        return simulation.Statics.Add(new StaticDescription(System.Numerics.Vector3.Zero, shapeIndex));
    }

    public void Draw(Matrix view, Matrix projection, Vector3 cameraPosition)
    {
        if (_terrainEffect == null || _groundTexture == null) return;

        _terrainEffect.CurrentTechnique = _terrainEffect.Techniques["DrawShadowedHibrido"];

        _terrainEffect.Parameters["World"]?.SetValue(Matrix.Identity);
        _terrainEffect.Parameters["View"]?.SetValue(view);
        _terrainEffect.Parameters["Projection"]?.SetValue(projection);
        _terrainEffect.Parameters["ModelTexture"]?.SetValue(_groundTexture);

        _terrainEffect.Parameters["DiffuseColor"]?.SetValue(Vector3.One);
        _terrainEffect.Parameters["EyePosition"]?.SetValue(cameraPosition);
        _terrainEffect.Parameters["normalOffsetScale"]?.SetValue(_normalOffsetScale);

        //_terrainEffect.Parameters["LightColor"]?.SetValue(new Vector3(0.55f, 0.55f, 0.55f));
        //_terrainEffect.Parameters["AmbientColor"]?.SetValue(new Vector3(0.25f, 0.25f, 0.25f));
        _terrainEffect.Parameters["Shininess"]?.SetValue(16f); // el terreno no debería brillar mucho

        var smm = TGCGame.Instance.ShadowMapManager;
        _terrainEffect.Parameters["LightViewProjection"]?.SetValue(smm.LightViewProjection);
        _terrainEffect.Parameters["lightPosition"]?.SetValue(smm.LightPosition);
        _terrainEffect.Parameters["InverseTransposeWorld"]?.SetValue(Matrix.Transpose(Matrix.Invert(Matrix.Identity)));

        _terrainEffect.Parameters["IsDeformable"]?.SetValue(0);

        if (smm != null)
        {
            _terrainEffect.Parameters["shadowMapStatic"]?.SetValue(smm.StaticShadowRenderTarget);
            _terrainEffect.Parameters["shadowMapDynamic"]?.SetValue(smm.DynamicShadowRenderTarget);
            if (smm.StaticShadowRenderTarget != null)
            {
                _terrainEffect.Parameters["shadowMapSize"]?.SetValue(new Vector2(
                    smm.StaticShadowRenderTarget.Width,
                    smm.StaticShadowRenderTarget.Height));
            }
        }

        _graphicsDevice.SetVertexBuffer(_terrainVertexBuffer);
        _graphicsDevice.Indices = _terrainIndexBuffer;
        _graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
        _graphicsDevice.DepthStencilState = DepthStencilState.Default;
        _graphicsDevice.BlendState = BlendState.Opaque;

        foreach (var pass in _terrainEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _primitiveCount);
        }
    }

    public void DrawDepth(Matrix lightViewProjection)
    {
        if (_terrainEffect == null) return;

        _terrainEffect.CurrentTechnique = _terrainEffect.Techniques["DepthPass"];

        _terrainEffect.Parameters["World"]?.SetValue(Matrix.Identity);
        _terrainEffect.Parameters["LightViewProjection"]?.SetValue(lightViewProjection);
        //_terrainEffect.Parameters["normalOffsetScale"]?.SetValue(_normalOffsetScale);
        _terrainEffect.Parameters["IsDeformable"]?.SetValue(0);

        _graphicsDevice.SetVertexBuffer(_terrainVertexBuffer);
        _graphicsDevice.Indices = _terrainIndexBuffer;

        foreach (var pass in _terrainEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _primitiveCount);
        }
    }

    public void Dispose()
    {
        _terrainVertexBuffer?.Dispose();
        _terrainIndexBuffer?.Dispose();
    }
}