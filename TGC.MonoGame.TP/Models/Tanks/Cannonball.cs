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

    private float _radius = 0.3f;

    private float _lifeTime = 5f;

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

        float shootForce = 25f;

        simulation.Bodies[_bodyHandle].Velocity.Linear = new BepuVector3(direction.X * shootForce, direction.Y * shootForce, direction.Z * shootForce);

        // Matriz mundo
        _world = Matrix.CreateScale(0.015f) * Matrix.CreateTranslation(position);
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

        _world = Matrix.CreateScale(0.015f) * Matrix.CreateTranslation(position.X, position.Y, position.Z);
    }

    public void Draw(Matrix view, Matrix projection)
    {
        _effect.Parameters["World"]?.SetValue(_world);

        _effect.Parameters["View"]?.SetValue(view);

        _effect.Parameters["Projection"]?.SetValue(projection);

        // gris oscuro
        _effect.Parameters["DiffuseColor"]?.SetValue(new Vector3(0.1f, 0.1f, 0.1f));

        Matrix[] transforms = new Matrix[_model.Bones.Count];

        _model.CopyAbsoluteBoneTransformsTo(transforms);

        foreach (var mesh in _model.Meshes)
        {
            Matrix localWorld = transforms[mesh.ParentBone.Index] * _world;

            _effect.Parameters["World"]?.SetValue(localWorld);

            mesh.Draw();
        }
    }
}