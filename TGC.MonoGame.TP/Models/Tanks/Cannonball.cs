using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using BepuPhysics;
using BepuPhysics.Collidables;

using BepuVector3 = System.Numerics.Vector3;

namespace TGC.MonoGame.TP.Models.Tanks;

public class Cannonball
{
    private Model _model;

    private Effect _effect;

    private Matrix _world;

    private BodyHandle _bodyHandle;

    public BodyHandle BodyHandle
    {
        get
        {
            return _bodyHandle;
        }
    }

    private float _radius = GameConfig.CannonBall.Radius;

    private float _lifeTime = GameConfig.CannonBall.LifetimeSeconds;

    private float _currentLifeTime = 0f;

    private bool _isDead = false;

    // indica de que tipo de tanque proviene la bala (esto afecta el daño que provoca)
    public float AttackDamage { get; }

    public bool IsDead => _isDead;
    public void killCannonball()
    {
        _isDead = true;
    }

    public Cannonball(Model model, float damage, Effect effect, Vector3 position, Vector3 direction, Simulation simulation)
    {
        _model = model;

        _effect = effect;

        // Asignamos el shader a cada mesh
        foreach (var mesh in _model.Meshes)
        {
            foreach (var part in mesh.MeshParts)
            {
                part.Effect = _effect;
            }
        }

        AttackDamage = damage;

        // =====================================
        // CUERPO FISICO
        // =====================================

        Sphere sphere = new Sphere(_radius);

        var shapeIndex = simulation.Shapes.Add(sphere);

        var inertia = sphere.ComputeInertia(10f);

        _bodyHandle = simulation.Bodies.Add(BodyDescription.CreateDynamic(new BepuVector3(position.X, position.Y, position.Z), inertia, shapeIndex, 0.01f));

        float initialVelocity = GameConfig.CannonBall.InitialVelocity;

        simulation.Bodies[_bodyHandle].Velocity.Linear = new BepuVector3(direction.X * initialVelocity, direction.Y * initialVelocity, direction.Z * initialVelocity);

        // Matriz mundo
        _world = Matrix.CreateScale(GameConfig.CannonBall.VisualScale) * Matrix.CreateTranslation(position);
    }

    public void Update(GameTime gameTime, Simulation simulation)  
    {
        if (_isDead)
            return;

        _currentLifeTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

        // destruir luego de X segundos
        if (_currentLifeTime >= _lifeTime)
        {
            simulation.Bodies.Remove(_bodyHandle);

            _isDead = true;

            return;
        }

        var body = simulation.Bodies[_bodyHandle];

        var position = body.Pose.Position;

        _world = Matrix.CreateScale(GameConfig.CannonBall.VisualScale) * Matrix.CreateTranslation(position.X, position.Y, position.Z);
    }

    public void Draw(Matrix view, Matrix projection)
    {
        if (_model == null || _isDead) return;

        _effect.CurrentTechnique = _effect.Techniques["DrawShadowedHibrido"];

        var smm = TGCGame.Instance.ShadowMapManager;
        _effect.Parameters["View"]?.SetValue(view);
        _effect.Parameters["Projection"]?.SetValue(projection);
        _effect.Parameters["DiffuseColor"]?.SetValue(new Vector3(0.1f, 0.1f, 0.1f));
        
        _effect.Parameters["LightViewProjection"]?.SetValue(smm.StaticLightViewProjection);
        _effect.Parameters["DynamicLightViewProjection"]?.SetValue(smm.DynamicLightViewProjection);
        _effect.Parameters["lightPosition"]?.SetValue(smm.LightPosition);
        _effect.Parameters["shadowMapStatic"]?.SetValue(smm.StaticShadowRenderTarget);
        _effect.Parameters["shadowMapDynamic"]?.SetValue(smm.DynamicShadowRenderTarget);
        _effect.Parameters["shadowMapSize"]?.SetValue(new Vector2(smm.StaticShadowRenderTarget.Width, smm.StaticShadowRenderTarget.Height));

        Matrix[] transforms = new Matrix[_model.Bones.Count];
        _model.CopyAbsoluteBoneTransformsTo(transforms);

        foreach (var mesh in _model.Meshes)
        {
            Matrix localWorld = transforms[mesh.ParentBone.Index] * _world;
            _effect.Parameters["World"]?.SetValue(localWorld);
            _effect.Parameters["InverseTransposeWorld"]?.SetValue(Matrix.Transpose(Matrix.Invert(localWorld)));
            mesh.Draw();
        }
    }

    public void DrawDepth(Matrix lightViewProjection)
    {
        if (_model == null || _effect == null || _isDead) return;

        _effect.CurrentTechnique = _effect.Techniques["DepthPass"];

        Matrix[] transforms = new Matrix[_model.Bones.Count];
        _model.CopyAbsoluteBoneTransformsTo(transforms);

        foreach (var mesh in _model.Meshes)
        {
            Matrix localWorld = transforms[mesh.ParentBone.Index] * _world;
            _effect.Parameters["WorldViewProjection"]?.SetValue(localWorld * lightViewProjection);

            foreach (var meshPart in mesh.MeshParts)
            {
                foreach (var pass in _effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    var gd = _effect.GraphicsDevice;
                    gd.SetVertexBuffer(meshPart.VertexBuffer);
                    gd.Indices = meshPart.IndexBuffer;
                    gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, meshPart.VertexOffset, meshPart.StartIndex, meshPart.PrimitiveCount);
                }
            }
        }
    }
    
}