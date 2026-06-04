using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities.Memory;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Transactions;
using TGC.MonoGame.Samples.Physics.Bepu;
using TGC.MonoGame.TP.Cameras;
using TGC.MonoGame.TP.Gizmos;
using TGC.MonoGame.TP.Models;
using TGC.MonoGame.TP.Models.Decorations;
using TGC.MonoGame.TP.Models.Enemy;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace TGC.MonoGame.TP;

/// <summary>
///     Esta es la clase principal del juego.
///     Inicialmente puede ser renombrado o copiado para hacer mas ejemplos chicos, en el caso de copiar para que se
///     ejecute el nuevo ejemplo deben cambiar la clase que ejecuta Program <see cref="Program.Main()" /> linea 10.
/// </summary>
public class TGCGame : Game
{
    //Carpetas
    public const string ContentFolder3D = "Models/";
    public const string ContentFolderEffects = "Effects/";
    public const string ContentFolderMusic = "Music/";
    public const string ContentFolderSounds = "Sounds/";
    public const string ContentFolderSpriteFonts = "SpriteFonts/";
    public const string ContentFolderTextures = "Textures/";
    //-----------JUEGO
    //graficos
    private readonly GraphicsDeviceManager _graphics;
    //escena
    private Matrix _projection;
    private Matrix _view;
    private Matrix _world;
    //Sonido
    private SoundEffect _klaxonSound;
    private SoundEffect _hornSound;
    //teclado
    private KeyboardState _lastKeyboardState;
    //hud
    private Hud _hud;
    // multiplayer :o
    private bool twoPlayers = false;
    // ----------ENEMIGOS
    private int _enemiesCount = 15;
    public List<Enemy> _enemies = new ();
    private List<BodyHandle> _enemiesHandles = new ();
    private List<Cannonball> _enemiesCanonballs = new();

    //-----------TANQUE
    public Tank _tank;
    private TankFollowCamera _camera;
    //-----------TERRENO
    private Terrain _terrain;
    private Wall _wall;
    //-----------DECORACIONES
    public HousesManager _houses;
    public StaticsManager _statics;
    public DinamicsManager _dinamics;
    public BarrelsManager _barrels;
    private readonly Random _random = new();
    //-----------FISICAS
    private Simulation _simulation;
    private BufferPool _bufferPool;
    private BodyHandle _tankHandle;
    private StaticHandle _terrainStaticHandle;
    public static TGCGame Instance { get; private set; } //Esto es para que lo use NarrowPhaseCallbacks
    //gizmos
    private Gizmo _gizmos = new();
    private Effect _effect;
    private List<Cannonball> _cannonballs = new();

    public List<Cannonball> Cannonballs
    {
        get
        {
            return _cannonballs;
        }
    }
    private MouseState _previousMouseState;
    private Model _cannonballModel;
    public static float _shootCooldown = GameConfig.Tank.Cooldown;
    private float _currentShootCooldown = 0f;

    public TGCGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Instance = this;

        _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width - 100;
        _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height - 100;

        Content.RootDirectory = "Content";

        IsMouseVisible = true; //Oculto el mouse porque da dolor de cabeza
    }

    protected override void Initialize()
    {
        // Se activa el Backface Culling en sentido anti-horario (se renderizan las caras frontales de los triangulos)
        GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

        // Configuramos nuestras matrices de la escena.
        _world = Matrix.Identity;
        _view = Matrix.CreateLookAt(Vector3.UnitZ * 150, Vector3.Zero, Vector3.Up);
        _projection =
            Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 1, 250);

        InitializePhysics();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        //RECURSOS
        //shaders
        _effect = Content.Load<Effect>(ContentFolderEffects + "BasicShader"); //modelos sin texturas
        var effect2 = Content.Load<Effect>(ContentFolderEffects + "BasicShaderTexture"); //modelos con textura
        //texturas
        var terrainTexture = Content.Load<Texture2D>("Models/heightmaps/heightmap_512x512");
        var tankTexture = Content.Load<Texture2D>(ContentFolderTextures + "paleta_256x512");
        //sonidos
        _klaxonSound = Content.Load<SoundEffect>(ContentFolderSounds + "klaxon");
        _hornSound = Content.Load<SoundEffect>(ContentFolderSounds + "horn");
        //modelos
        var tankModel = Content.Load<Model>(ContentFolder3D + "tanques/tank v3");
        _cannonballModel = Content.Load<Model>(ContentFolder3D + "cannonball/cannonball");

        //AUXILIARES
        Vector3 spawnPos = new Vector3(0, 0, 0);

        //TERRENO
            //Creo un terreno (suelo)
        _terrain = new Terrain(GraphicsDevice);
            //Le paso la textura y el efecto
        _terrain.LoadContent(terrainTexture, _effect);
            //fisicas
        _terrainStaticHandle = _terrain.CreatePhysicsTerrain(_simulation);

        _wall = new Wall(_simulation);
        _wall.LoadContent(_terrain);

        //ASSETS DECORATIVOS
        //casas
        _houses = new HousesManager(_terrain);
        _houses.Initialize();
        _houses.LoadContent(Content, _simulation);
        //estaticos
        _statics = new StaticsManager(_terrain, _houses.getHouses());
        _statics.Initialize();
        _statics.LoadContent(Content, _simulation);
        //dinamicos
        _dinamics = new DinamicsManager(_terrain, _statics.GetDecorations(), _houses.getHouses());
        _dinamics.Initialize();
        _dinamics.LoadContent(Content, _simulation);

        //BARRELS
        _barrels = new BarrelsManager(_terrain, _statics.GetDecorations(), _houses.getHouses());
        _barrels.Initialize();
        _barrels.LoadContent(Content, _simulation);

        // ENEMIGOS
        for(int i=0; i<_enemiesCount; i++)
        {   // Inicializo los tanques y sus handles
            var enemy = new Enemy();
            enemy.Position = enemy.GetPosition(_terrain, _random);
            enemy.Load(tankModel, tankTexture, effect2, _simulation);
            _enemies.Add(enemy);
            _enemiesHandles.Add(enemy.TankHandler);
        }
        //TANQUE
            //Creamos el tanque
        _tank = new Tank();
            //Determino una posicion para el tanque
        float terrainY = _terrain.GetHeight(spawnPos.X, spawnPos.Z);//Se spawnea unos metros por encima del terreno
        _tank.Position = new Vector3(spawnPos.X, terrainY + GameConfig.Tank.SpawnZMargin, spawnPos.Z);
            //Cargo el tanque
        _tank.Load(tankModel, tankTexture, effect2, _simulation);
            //fisicas
        _tankHandle = _tank.TankHandler;

        //HUD
        _hud = new Hud();
        _hud.LoadContent(Content, GraphicsDevice);
        
        //CAMARA
        _camera = new TankFollowCamera(GraphicsDevice.Viewport.AspectRatio, _tank.Position);
        //GIZMOS
        _gizmos.LoadContent(GraphicsDevice, Content);

        base.LoadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();
        if (kb.IsKeyDown(Keys.Escape)) Exit();
        if (kb.IsKeyDown(Keys.Space) && _lastKeyboardState.IsKeyUp(Keys.Space))
        {
            int chance = _random.Next(0, 100);
            if (chance < 90) _klaxonSound.Play();
            else _hornSound.Play();
        }

        _lastKeyboardState = kb;

        _simulation.Timestep(1 / 60f);

        _tank.Update(gameTime, kb, _simulation);

        //verificar si pueden recogerse los barriles
        foreach (var barrel in _barrels._fuelBarrels)
        {
            if (!barrel.IsCollected) barrel.TryCollect(_tank, _simulation);
        }

        _houses.Update(gameTime, _simulation);
        _statics.Update(gameTime, _simulation);
        _dinamics.Update(gameTime, _simulation);
        _barrels.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

        foreach(var enemy in _enemies)
        {
            enemy.UpdateEnemy(gameTime, _simulation, _tank.Position.ToNumerics(), _terrain);
        }
        
        // cada frame el tiempo restante de cooldown baja
        _currentShootCooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        MouseState currentMouseState = Mouse.GetState();

        if (currentMouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released && _currentShootCooldown <= 0f)
        {
            Vector3 direction = _tank.CannonForward;

            direction.Normalize();

            // Posición desde donde sale la bala
            Vector3 spawnPosition = _tank.Position + direction * 3f + Vector3.Up * 2f;

            Cannonball cannonball = CreateCannonball(spawnPosition, direction);

            _cannonballs.Add(cannonball);
            _currentShootCooldown = _shootCooldown;
        }

        _previousMouseState = currentMouseState;

        for (int i = _cannonballs.Count - 1; i >= 0; i--)
        {
            _cannonballs[i].Update(gameTime, _simulation);

            if (_cannonballs[i].IsDead)
            {
                _cannonballs.RemoveAt(i);
            }  
        }

        _camera.Update(gameTime, _tank.Position, _tank.TurretRotationWorld); //A la camara ahora le paso la posicion de la torreta en vez de la base
        _gizmos.UpdateViewProjection(_camera.View, _camera.Projection);

        _hud.TankFuel = _tank.CurrentFuel;
        _hud.TankPosition = _tank.Position;
        _hud.Update(gameTime);

        base.Update(gameTime);
    }

    public Cannonball CreateCannonball(Vector3 spawnPosition, Vector3 direction)
    {
        return new Cannonball(_cannonballModel, _effect, spawnPosition, direction, _simulation);
    }

    protected override void Draw(GameTime gameTime)
    {
        // Aca deberiamos poner toda la logia de renderizado del juego.
        var totalTime = (float)gameTime.TotalGameTime.TotalSeconds;
        GraphicsDevice.Clear(Color.Goldenrod);

        // El terreno, al dibujarse, vuelve a activar el Z-Buffer (setea el DepthStencilState en "default")
        _terrain.Draw(_camera.View, _camera.Projection);
        _tank.Draw(_camera.View, _camera.Projection);

        foreach(var enemy in _enemies)
        {
            enemy.Draw(_camera.View, _camera.Projection);
        }
        foreach (var cannonball in _cannonballs)
        {
            cannonball.Draw(_camera.View, _camera.Projection);
        }
        _houses.Draw(_camera.View, _camera.Projection, _gizmos, _simulation);
        _statics.Draw(_camera.View, _camera.Projection, _gizmos, _simulation);
        _dinamics.Draw(_camera.View, _camera.Projection, _gizmos, _simulation);
        _barrels.Draw(_camera.View, _camera.Projection, _gizmos, _simulation);
        // El HUD se debe dibujar a lo ultimo, ya que para esto se desactiva el Z-Buffer, lo que rompe con el dibujado de los demas modelos
        _hud.Draw();

        _gizmos.Draw();
    }

    public void InitializePhysics()
    {
        _bufferPool = new BufferPool();

        _simulation = 
            Simulation.Create(_bufferPool,
            new NarrowPhaseCallbacks(),
            new PoseIntegratorCallbacks(new System.Numerics.Vector3(0, -9.8f, 0)),
            new SolveDescription(8, 1));
    }

    protected override void UnloadContent()
    {
        // Libero los recursos.
        Content.Unload();
        _simulation?.Dispose();
        _simulation = null;
        _bufferPool.Clear();
        _terrain?.Dispose();
        _hud?.Dispose();

        base.UnloadContent();
    }
}