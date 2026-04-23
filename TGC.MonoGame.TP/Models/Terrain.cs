using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace TGC.MonoGame.TP.Models;

/// <summary>
///     Terreno plano con obstaculos cubicos generados de forma deterministica.
/// </summary>
public class Terrain
{
    private const int RandomSeed = 42;
    private const int ObstacleCount = 50;
    private const float TerrainSize = 10000f;
    private const float MaxObstacleSize = 400f;
    private const float MinObstacleSize = 40f;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly List<Obstacle> _obstacles;

    private VertexBuffer _groundVertexBuffer;
    private IndexBuffer _groundIndexBuffer;
    private BasicEffect _groundEffect;

    private VertexBuffer _cubeVertexBuffer;
    private IndexBuffer _cubeIndexBuffer;
    private BasicEffect _obstacleEffect;

    public Terrain(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _obstacles = new List<Obstacle>();
    }

    public void LoadContent(ContentManager content)
    {
        _groundEffect = new BasicEffect(_graphicsDevice)
        {
            TextureEnabled = false,
            VertexColorEnabled = false,
            LightingEnabled = true,
            DiffuseColor = new Vector3(0.2f, 0.5f, 0.2f)
        };
        _groundEffect.EnableDefaultLighting();

        _obstacleEffect = new BasicEffect(_graphicsDevice)
        {
            TextureEnabled = false,
            VertexColorEnabled = false,
            LightingEnabled = true,
            DiffuseColor = new Vector3(0.6f, 0.4f, 0.2f)
        };
        _obstacleEffect.EnableDefaultLighting();

        CreateGroundGeometry();
        CreateCubeGeometry();
        GenerateObstacles();
    }

    private void CreateGroundGeometry()
    {
        // vertices ya definidos en el plano xz (y = 0)
        var vertices = new VertexPositionNormalTexture[4]
        {
            new VertexPositionNormalTexture(new Vector3(-0.5f, 0f, -0.5f), Vector3.Up, Vector2.Zero),
            new VertexPositionNormalTexture(new Vector3( 0.5f, 0f, -0.5f), Vector3.Up, new Vector2(1, 0)),
            new VertexPositionNormalTexture(new Vector3( 0.5f, 0f,  0.5f), Vector3.Up, Vector2.One),
            new VertexPositionNormalTexture(new Vector3(-0.5f, 0f,  0.5f), Vector3.Up, new Vector2(0, 1))
        };

        var indices = new ushort[6] { 0, 1, 2, 0, 2, 3 };

        _groundVertexBuffer = new VertexBuffer(_graphicsDevice, VertexPositionNormalTexture.VertexDeclaration, 4, BufferUsage.None);
        _groundVertexBuffer.SetData(vertices);

        _groundIndexBuffer = new IndexBuffer(_graphicsDevice, IndexElementSize.SixteenBits, 6, BufferUsage.None);
        _groundIndexBuffer.SetData(indices);
    }

    private void CreateCubeGeometry()
    {
        var vertices = new VertexPositionNormalTexture[8]
        {
            new VertexPositionNormalTexture(new Vector3(-0.5f, -0.5f,  0.5f), Vector3.Forward, Vector2.Zero),
            new VertexPositionNormalTexture(new Vector3( 0.5f, -0.5f,  0.5f), Vector3.Forward, new Vector2(1, 0)),
            new VertexPositionNormalTexture(new Vector3( 0.5f,  0.5f,  0.5f), Vector3.Forward, new Vector2(1, 1)),
            new VertexPositionNormalTexture(new Vector3(-0.5f,  0.5f,  0.5f), Vector3.Forward, new Vector2(0, 1)),
            new VertexPositionNormalTexture(new Vector3(-0.5f, -0.5f, -0.5f), Vector3.Backward, Vector2.Zero),
            new VertexPositionNormalTexture(new Vector3( 0.5f, -0.5f, -0.5f), Vector3.Backward, new Vector2(1, 0)),
            new VertexPositionNormalTexture(new Vector3( 0.5f,  0.5f, -0.5f), Vector3.Backward, new Vector2(1, 1)),
            new VertexPositionNormalTexture(new Vector3(-0.5f,  0.5f, -0.5f), Vector3.Backward, new Vector2(0, 1))
        };

        var indices = new ushort[36]
        {
            0, 1, 2, 0, 2, 3,
            5, 4, 7, 5, 7, 6,
            3, 2, 6, 3, 6, 7,
            1, 0, 4, 1, 4, 5,
            2, 1, 5, 2, 5, 6,
            0, 3, 7, 0, 7, 4
        };

        _cubeVertexBuffer = new VertexBuffer(_graphicsDevice, VertexPositionNormalTexture.VertexDeclaration, 8, BufferUsage.None);
        _cubeVertexBuffer.SetData(vertices);

        _cubeIndexBuffer = new IndexBuffer(_graphicsDevice, IndexElementSize.SixteenBits, 36, BufferUsage.None);
        _cubeIndexBuffer.SetData(indices);
    }

    private void GenerateObstacles()
    {
        var random = new Random(RandomSeed);
        var margin = TerrainSize * 0.45f;

        for (int i = 0; i < ObstacleCount; i++)
        {
            float x = (float)(random.NextDouble() * 2 - 1) * margin;
            float z = (float)(random.NextDouble() * 2 - 1) * margin;
            float size = (float)(random.NextDouble() * (MaxObstacleSize - MinObstacleSize) + MinObstacleSize);
            float rotation = (float)(random.NextDouble() * MathHelper.TwoPi);

            _obstacles.Add(new Obstacle
            {
                Position = new Vector3(x, size * 0.5f, z),
                Size = new Vector3(size),
                RotationY = rotation
            });
        }
    }

    public void Draw(Matrix view, Matrix projection)
    {
        //dibujar terreno sin rotacion -- los vertices ya son horizontales.
        _groundEffect.World = Matrix.CreateScale(TerrainSize);
        _groundEffect.View = view;
        _groundEffect.Projection = projection;

        _graphicsDevice.SetVertexBuffer(_groundVertexBuffer);
        _graphicsDevice.Indices = _groundIndexBuffer;

        foreach (var pass in _groundEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 2);
        }

        _graphicsDevice.SetVertexBuffer(_cubeVertexBuffer);
        _graphicsDevice.Indices = _cubeIndexBuffer;

        foreach (var obstacle in _obstacles)
        {
            var world = Matrix.CreateScale(obstacle.Size) *
                        Matrix.CreateRotationY(obstacle.RotationY) *
                        Matrix.CreateTranslation(obstacle.Position);

            _obstacleEffect.World = world;
            _obstacleEffect.View = view;
            _obstacleEffect.Projection = projection;

            foreach (var pass in _obstacleEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 12);
            }
        }
    }

    public float GetHeightAt(float x, float z)
    {
        return 0f;
    }

    public bool IsWithinBounds(float x, float z)
    {
        float halfSize = TerrainSize * 0.5f;
        return x >= -halfSize && x <= halfSize && z >= -halfSize && z <= halfSize;
    }

    public void Dispose()
    {
        _groundVertexBuffer?.Dispose();
        _groundIndexBuffer?.Dispose();
        _cubeVertexBuffer?.Dispose();
        _cubeIndexBuffer?.Dispose();
        _groundEffect?.Dispose();
        _obstacleEffect?.Dispose();
    }

    private class Obstacle
    {
        public Vector3 Position { get; set; }
        public Vector3 Size { get; set; }
        public float RotationY { get; set; }
    }
}