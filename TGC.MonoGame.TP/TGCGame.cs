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
    //-----------TANQUE
    public Tank _tank;
    private TankFollowCamera _camera;
    //-----------TERRENO
    private Terrain _terrain;
    //-----------DECORACIONES
    public AssetsManager _assets;
    private readonly Random _random = new();
    //-----------FISICAS
    private Simulation _simulation;
    private BufferPool _bufferPool;
    private List<BodyHandle> _bodyHandlers;
    private BodyHandle _tankHandle;
    private StaticHandle _terrainStaticHandle;
    public static TGCGame Instance { get; private set; } //Esto es para que lo use NarrowPhaseCallbacks
    //gizmos
    private Gizmo _gizmos = new();

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

        IsMouseVisible = false;

        base.Initialize();
    }

    protected override void LoadContent()
    {
        //RECURSOS
        //shaders
        var effect = Content.Load<Effect>(ContentFolderEffects + "BasicShader"); //modelos sin texturas
        var effect2 = Content.Load<Effect>(ContentFolderEffects + "BasicShaderTexture"); //modelos con textura
        //texturas
        var terrainTexture = Content.Load<Texture2D>("Models/heightmaps/heightmap_512x512");
        var tankTexture = Content.Load<Texture2D>(ContentFolderTextures + "paleta_256x512");
        //sonidos
        _klaxonSound = Content.Load<SoundEffect>(ContentFolderSounds + "klaxon");
        _hornSound = Content.Load<SoundEffect>(ContentFolderSounds + "horn");
        //modelos
        var tankModel = Content.Load<Model>(ContentFolder3D + "tanques/tank v3");

        //AUXILIARES
        Vector3 spawnPos = new Vector3(0, 0, 0);

        //TERRENO
            //Creo un terreno (suelo)
        _terrain = new Terrain(GraphicsDevice);
            //Le paso la textura y el efecto
        _terrain.LoadContent(terrainTexture, effect);
            //fisicas
        _terrainStaticHandle = _terrain.CreatePhysicsTerrain(_simulation);

        // 4 paredes invisibles que rodean el mapa e impiden que salgan los objetos
        float halfSize = _terrain.WidthUnits; // ~259 unidades
        float margin = 8f;
        float playAreaLimit = halfSize - margin; // Los muros invisibles quedan un poco adentro del mapa
        float wallHeight = 60f; // Un poco más alto que el terreno máximo (35m)
        float wallThickness = 2f;

        // Shape para muros Norte/Sur (largos en X, finos en Z)
        var wallShapeNS = new Box(playAreaLimit * 2, wallHeight, wallThickness);
        var idxNS = _simulation.Shapes.Add(wallShapeNS);

        // Shape para muros Este/Oeste (finos en X, largos en Z)
        var wallShapeEW = new Box(wallThickness, wallHeight, playAreaLimit * 2);
        var idxEW = _simulation.Shapes.Add(wallShapeEW);

        // Norte (-Z)
        _simulation.Statics.Add(new StaticDescription(new System.Numerics.Vector3(0, wallHeight / 2f, -playAreaLimit), idxNS));
        // Sur (+Z)
        _simulation.Statics.Add(new StaticDescription(new System.Numerics.Vector3(0, wallHeight / 2f, playAreaLimit), idxNS));
        // Oeste (-X)
        _simulation.Statics.Add(new StaticDescription(new System.Numerics.Vector3(-playAreaLimit, wallHeight / 2f, 0), idxEW));
        // Este (+X)
        _simulation.Statics.Add(new StaticDescription(new System.Numerics.Vector3(playAreaLimit, wallHeight / 2f, 0), idxEW));

        // creo modelos
        //ASSETS DECORATIVOS
            //Creamos el manager
        _assets = new AssetsManager(_terrain);
            //Iniciamos
        _assets.Initialize();
        _assets.SpawnFuelBarrels();
            //Cargamos los assets
        _assets.LoadContent(Content, _simulation);
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
        foreach (var barrel in _assets._fuelBarrels)
        {
            if (!barrel.IsCollected) barrel.TryCollect(_tank, _simulation);
        }

        _assets.UpdateFuelBarrels((float)gameTime.ElapsedGameTime.TotalSeconds);

        _assets.Update(gameTime, _simulation);

        _camera.Update(gameTime, _tank.Position, _tank.TurretRotationWorld); //A la camara ahora le paso la posicion de la torreta en vez de la base
        _gizmos.UpdateViewProjection(_camera.View, _camera.Projection);

        _hud.TankFuel = _tank.CurrentFuel;
        _hud.TankPosition = _tank.Position;
        _hud.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // Aca deberiamos poner toda la logia de renderizado del juego.
        var totalTime = (float)gameTime.TotalGameTime.TotalSeconds;
        GraphicsDevice.Clear(Color.Goldenrod);

        // El terreno, al dibujarse, vuelve a activar el Z-Buffer (setea el DepthStencilState en "default")
        _terrain.Draw(_camera.View, _camera.Projection);
        _tank.Draw(_camera.View, _camera.Projection);
        _assets.Draw(_camera.View, _camera.Projection, _gizmos, _simulation);
        // El HUD se debe dibujar a lo ultimo, ya que para esto se desactiva el Z-Buffer, lo que rompe con el dibujado de los demas modelos
        _hud.Draw();

        _gizmos.Draw();
    }

    public void InitializePhysics()
    {
        _bufferPool = new BufferPool();
        if(!twoPlayers)
            _bodyHandlers = new List<BodyHandle>();

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