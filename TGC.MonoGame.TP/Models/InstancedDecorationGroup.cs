using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace TGC.MonoGame.TP.Models;
public class InstancedDecorationGroup
{
    private readonly Model _model;
    private GraphicsDevice _graphicsDevice;
    private readonly VertexBuffer _instanceBuffer;
    private readonly int _instanceCount;
    private Effect _effect;
    private readonly Texture2D _texture;

    public InstancedDecorationGroup(Model model, List<Matrix> worldMatrices, GraphicsDevice graphicsDevice, Texture2D texture, Effect effect)
    {
        _model = model;
        _instanceCount = worldMatrices.Count;
        _graphicsDevice = graphicsDevice;
        _texture = texture;
        _effect = effect.Clone();

        var instanceDeclaration = new VertexDeclaration(
            new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1),
            new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 2),
            new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 3),
            new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 4)
        );

        // Crear buffer de instancias
        _instanceBuffer = new VertexBuffer(
            _graphicsDevice, 
            instanceDeclaration, 
            _instanceCount, 
            BufferUsage.WriteOnly
        );
        _instanceBuffer.SetData(worldMatrices.ToArray());
    }

    public void Draw(Matrix view, Matrix projection)
    {
        var smm = TGCGame.Instance.ShadowMapManager;

        _effect.Parameters["View"]?.SetValue(view);
        _effect.Parameters["Projection"]?.SetValue(projection);
        _effect.Parameters["ModelTexture"]?.SetValue(_texture);
        _effect.Parameters["DiffuseColor"]?.SetValue(Vector3.One);
        _effect.Parameters["normalOffsetScale"]?.SetValue(0.05f);
        _effect.Parameters["Shininess"]?.SetValue(16f);
        _effect.Parameters["IsDeformable"]?.SetValue(0);
        _effect.Parameters["TrackOffset"]?.SetValue(0f);

        if (smm != null)
        {
            _effect.Parameters["LightViewProjection"]?.SetValue(smm.LightViewProjection);
            _effect.Parameters["lightPosition"]?.SetValue(smm.LightPosition);
            _effect.Parameters["shadowMapStatic"]?.SetValue(smm.StaticShadowRenderTarget);
            _effect.Parameters["shadowMapDynamic"]?.SetValue(smm.DynamicShadowRenderTarget);
            if (smm.StaticShadowRenderTarget != null)
                _effect.Parameters["shadowMapSize"]?.SetValue(
                    new Vector2(smm.StaticShadowRenderTarget.Width, smm.StaticShadowRenderTarget.Height));
        }

        _effect.CurrentTechnique = _effect.Techniques["DrawInstanced"];

        foreach (var mesh in _model.Meshes)
        {
            foreach (var meshPart in mesh.MeshParts)
            {
                _graphicsDevice.SetVertexBuffers(
                    new VertexBufferBinding(meshPart.VertexBuffer, 0, 0),
                    new VertexBufferBinding(_instanceBuffer, 0, 1)
                );
                _graphicsDevice.Indices = meshPart.IndexBuffer;

                foreach (var pass in _effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    _graphicsDevice.DrawInstancedPrimitives(
                        PrimitiveType.TriangleList,
                        meshPart.VertexOffset,
                        0,
                        meshPart.PrimitiveCount,
                        _instanceCount
                    );
                }
            }
        }
    }

    public void DrawDepth(Matrix lightViewProjection)
    {
        _effect.Parameters["LightViewProjection"]?.SetValue(lightViewProjection);
        _effect.Parameters["normalOffsetScale"]?.SetValue(0.05f);
        _effect.Parameters["IsDeformable"]?.SetValue(0);

        _effect.CurrentTechnique = _effect.Techniques["DepthPassInstanced"];

        foreach (var mesh in _model.Meshes)
        {
            foreach (var meshPart in mesh.MeshParts)
            {
                _graphicsDevice.SetVertexBuffers(
                    new VertexBufferBinding(meshPart.VertexBuffer, 0, 0),
                    new VertexBufferBinding(_instanceBuffer, 0, 1)
                );
                _graphicsDevice.Indices = meshPart.IndexBuffer;

                foreach (var pass in _effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    _graphicsDevice.DrawInstancedPrimitives(
                        PrimitiveType.TriangleList,
                        meshPart.VertexOffset,
                        0,
                        meshPart.PrimitiveCount,
                        _instanceCount
                    );
                }
            }
        }
    }

    public void Dispose()
    {
        _instanceBuffer?.Dispose();
    }
}