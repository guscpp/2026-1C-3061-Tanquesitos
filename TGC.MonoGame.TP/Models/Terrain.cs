using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Data;

namespace TGC.MonoGame.TP.Models;

/// <summary>
///     Terreno generado a partir de un heightmap con alturas variables
/// </summary>
public class Terrain
{
    //escalas para controlar el tamano y relieve del terreno
    private const float TerrainScale = 100f;
    private const float HeightScale = 3500f;
    private float _heightmapWidth;
    private float _heightmapHeight;

    public float WidthUnits => 518f * TerrainScale / 2;   //unidades de ancho 

    private Texture2D _heightmapTexture;

    private readonly GraphicsDevice _graphicsDevice;
    private VertexBuffer _terrainVertexBuffer;
    private IndexBuffer _terrainIndexBuffer;
    private BasicEffect _terrainEffect;
    private int _primitiveCount;

    //matriz que guardará los valores del mapa para que pueda usarlos fuera de la clase
    private float[,] _heights;
    private int _width;
    private int _height;

    public Terrain(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }

    public void LoadContent(ContentManager content)
    {
        var heightmapTexture = content.Load<Texture2D>("Models/heightmaps/heightmap_512x512");
        _heightmapTexture = heightmapTexture;
        _heightmapWidth = heightmapTexture.Width;
        _heightmapHeight = heightmapTexture.Height;

        CreateHeightmapMesh(heightmapTexture);

        _terrainEffect = new BasicEffect(_graphicsDevice)
        {
            VertexColorEnabled = true,
            LightingEnabled = false,
            DiffuseColor = new Vector3(1f, 1f, 1f)
        };
    }

    private void CreateHeightmapMesh(Texture2D heightmapTexture)
    {
        _width = heightmapTexture.Width;
        _height = heightmapTexture.Height;

        //matriz con las alturas
        _heights = new float[_width, _height];

        Color[] heightmapData = new Color[_width * _height];
        heightmapTexture.GetData(heightmapData);

        //crear vertices con posicion y color
        var vertices = new VertexPositionColor[_width * _height];
        int index = 0;

        for (int z = 0; z < _height; z++)
        {
            for (int x = 0; x < _width; x++)
            {
                float posX = (x - _width / 2f) * TerrainScale;
                float posZ = (z - _height / 2f) * TerrainScale;

                //altura basada en el canal rojo del heightmap
                float heightValue = heightmapData[index].R / 255f * HeightScale;

                //guardo el valor en la matriz
                _heights[x, z] = heightValue;

                Vector3 position = new Vector3(posX, heightValue, posZ);
                Color vertexColor = GetColorForHeight(heightValue);

                vertices[index] = new VertexPositionColor(position, vertexColor);
                index++;
            }
        }

        //crear indices para triangulos (1 quad = triangulo 1 + triangulo 2)
        int indexCount = (_width - 1) * (_height - 1) * 6;
        var indices = new uint[indexCount];
        index = 0;

        for (int z = 0; z < _height - 1; z++)
        {
            for (int x = 0; x < _width - 1; x++)
            {
                int topLeft = z * _width + x;
                int topRight = topLeft + 1;
                int bottomLeft = (z + 1) * _width + x;
                int bottomRight = bottomLeft + 1;

                // triangulo 1
                indices[index++] = (uint)topLeft;
                indices[index++] = (uint)bottomLeft;
                indices[index++] = (uint)topRight;

                // triangulo 2
                indices[index++] = (uint)topRight;
                indices[index++] = (uint)bottomLeft;
                indices[index++] = (uint)bottomRight;
            }
        }

        _primitiveCount = index / 3;

        //pasar datos a la gpu
        _terrainVertexBuffer = new VertexBuffer(
            _graphicsDevice,
            VertexPositionColor.VertexDeclaration,
            vertices.Length,
            BufferUsage.WriteOnly);
        _terrainVertexBuffer.SetData(vertices);

        //usar 32 bits para soportar mas de 65k vertices, despues que me paso me acorde que lo dijeron los profes
        _terrainIndexBuffer = new IndexBuffer(
            _graphicsDevice,
            IndexElementSize.ThirtyTwoBits,
            indices.Length,
            BufferUsage.WriteOnly);
        _terrainIndexBuffer.SetData(indices);
    }

    /// <summary>
    ///     Retorna la altura correspondiente respecto de coordenadas X, Z
    /// </summary>
    public float GetHeight(float X, float Z)
    {
        // obtengo los colores del mapa
        Color[] heightmapData = new Color[_heightmapTexture.Width * _heightmapTexture.Height];
        _heightmapTexture.GetData(heightmapData);

        // pasaje de las coordenadas de mundo a "coordenadas de heightmap" --> el pixel dentro del mapa
        int xInMap = (int) (X / TerrainScale + _heightmapWidth / 2);
        int zInMap = (int) (Z / TerrainScale + _heightmapHeight / 2);

        var x = (int) MathHelper.Clamp(xInMap, 0, _heightmapWidth - 1);
        var z = (int) MathHelper.Clamp(zInMap, 0, _heightmapHeight - 1);
        
        var Y = heightmapData[z * _heightmapTexture.Width + x].R / 255f * HeightScale;
        return Y;
    }

    /// <summary>
    ///     Retorna la altura real segun la posicion que le mandemos, de esa forma el tanque no atraviesa el suelo
    /// </summary>
    public float GetHeight(Vector3 position)
    {
        // Tomo los valores de x e y de la posicion
        //Primero convierto el valor de la coordenada a pixel luego lo hago coincidir con el origen de la matriz
        float x = (position.X / TerrainScale) + (_width / 2f);
        float z = (position.Z / TerrainScale) + (_height / 2f);

        //Tomo los 4 puntos más cercanos al tanque
        int x0 = (int)Math.Floor(x); //vertice superior izquierdo
        int x1 = MathHelper.Clamp(x0 + 1, 0, _width - 1); //vertice adyacente
        int z0 = (int)Math.Floor(z); //vertice superior izquierdo
        int z1 = MathHelper.Clamp(z0 + 1, 0, _height - 1); //vertice adyacente
        x0 = MathHelper.Clamp(x0, 0, _width - 1);
        z0 = MathHelper.Clamp(z0, 0, _height - 1);

        // Fracciones para interpolar
        float tx = x - x0;
        float tz = z - z0;

        // Obtener las alturas de los 4 puntos
        float h00 = _heights[x0, z0];
        float h10 = _heights[x1, z0];
        float h01 = _heights[x0, z1];
        float h11 = _heights[x1, z1];

        // Interpolación bilineal (suaviza la pendiente)
        //Permite que el movimiento por el terreno sea fluido y no parezca que sube y baja escaleras
        float h0 = MathHelper.Lerp(h00, h10, tx);
        float h1 = MathHelper.Lerp(h01, h11, tx);
        return MathHelper.Lerp(h0, h1, tz);
    }

    /// <summary>
    ///     Retorna un color segun la altura para simular tonos de desierto
    /// </summary>
    private Color GetColorForHeight(float height)
    {
        float t = MathHelper.Clamp(height / HeightScale, 0f, 1f);

        if (t < 0.2f) return new Color(255, 248, 220); // arena muy clara
        if (t < 0.4f) return new Color(255, 239, 194); // arena clara
        if (t < 0.6f) return new Color(244, 212, 150); // arena mas o menos clara
        if (t < 0.8f) return new Color(222, 184, 135); // arena apenas clara
                      return new Color(160, 82, 45);   // arena no clara :D
    }

    public void Draw(Matrix view, Matrix projection)
    {
        _terrainEffect.World = Matrix.Identity;
        _terrainEffect.View = view;
        _terrainEffect.Projection = projection;

        _graphicsDevice.SetVertexBuffer(_terrainVertexBuffer);
        _graphicsDevice.Indices = _terrainIndexBuffer;

        //configurar rendering
        _graphicsDevice.RasterizerState = RasterizerState.CullNone;
        _graphicsDevice.DepthStencilState = DepthStencilState.Default;
        _graphicsDevice.BlendState = BlendState.Opaque;

        foreach (var pass in _terrainEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawIndexedPrimitives(
                PrimitiveType.TriangleList,
                0,
                0,
                _primitiveCount
            );
        }
    }

    public void Dispose()
    {
        _terrainVertexBuffer?.Dispose();
        _terrainIndexBuffer?.Dispose();
        _terrainEffect?.Dispose();
    }
}