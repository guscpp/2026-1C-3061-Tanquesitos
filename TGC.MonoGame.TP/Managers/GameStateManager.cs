using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System;
using TGC.MonoGame.TP.Models;

namespace TGC.MonoGame.TP.Managers;

public class GameStateManager
{
    public const string ContentFolder3D = "Models/";
    public const string ContentFolderEffects = "Effects/";
    public const string ContentFolderMusic = "Music/";
    public const string ContentFolderSounds = "Sounds/";
    public const string ContentFolderSpriteFonts = "SpriteFonts/";
    public const string ContentFolderTextures = "Textures/";
    public GameState CurrentState { get; private set; } = GameState.Menu;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly ContentManager _content;
    private readonly SpriteBatch _spriteBatch;
    private readonly SpriteFont _fontArial;
    private readonly SpriteFont _fontConsolas;
    private readonly Texture2D _whitePixel;
    private Texture2D _menuBackground;
    private float _introTimer = 0f;
    private float _menuInputLockTime = 0f;

    //SoundManager
    private readonly SoundManager _soundManager;
    private bool _menuMusicStarted = false;

    // 3D Menu Variables
    private Model _currentMenuTankModel;
    private Texture2D _menuTankTexture;
    private Texture2D _menuTracksTexture;
    private Effect _menuTankEffect;
    private float _menuTankRotation = 0f;
    private float _menuTankRotationSpeed = 0.015f;

    //En algun momento consideramos usar 3 modelos distintos para los 3 tipos
    private readonly string[] _menuTankModelPaths = {
        "Models/tanques/tank v5", // Scout
        "Models/tanques/tank v5", // Medium
        "Models/tanques/tank v5"  // Heavy
    };

    // Opciones de menu actualizadas para elegir el tipo de tanque
    private readonly string[] _menuOptions = {
        "Iniciar (Tanque Scout)",
        "Iniciar (Tanque Medio)",
        "Iniciar (Tanque Pesado)",
        "Salir"
    };
    private int _selectedIndex = 0; //Preselecciona Iniciar en el menu
    private int _lastHoveredIndex = -1; //preseleccion del mouse

    private MouseState _lastMouseState;

    private ContentManager _menuContent; //evita cache compartido


    //Constante compartida de separacion vertical para las opciones del menu
    private const float OptionSpacing = 20f;

    private string _cachedSpecsText = string.Empty;
    private int _specsCachedIndex = -1;

    // Idle animation variables
    private float _idleTime = 0f;
    private const float IdleAnimationSpeed = 2.5f; // Controls how fast the pulse is

    public GameStateManager(GraphicsDevice graphicsDevice, ContentManager content, SoundManager soundManager)
    {
        _graphicsDevice = graphicsDevice;
        _content = content;
        _soundManager = soundManager;
        _spriteBatch = new SpriteBatch(graphicsDevice);
        // Coincide con la ruta compilada en Content.mgcb
        _fontArial = content.Load<SpriteFont>("SpriteFonts/ArialFont");
        _fontConsolas = content.Load<SpriteFont>("SpriteFonts/ConsolasFont");
        _menuBackground = content.Load<Texture2D>("Textures/ConceptArt6");

        //Textura 1x1 para overlays
        _whitePixel = new Texture2D(graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });

        LoadMenu3DAssets();

        CurrentState = GameState.Intro;
        _introTimer = 0f;
    }

    private void LoadMenu3DAssets()
    {
        try
        {
            //De este modo el modelo del menu es distinto al del juego
            _menuContent = new ContentManager(_content.ServiceProvider, "Content");

            //_menuTankEffect = _content.Load<Effect>("Effects/BasicShaderTexture");
            _menuTankEffect = _menuContent.Load<Effect>("Effects/BlinnPhong");
            _menuTankTexture = _menuContent.Load<Texture2D>(ContentFolderTextures + "paleta_256x512");
            _menuTracksTexture = _menuContent.Load<Texture2D>(ContentFolderTextures + GameConfig.Tank.TankTracksTexture);
            _currentMenuTankModel = _menuContent.Load<Model>(ContentFolder3D + GameConfig.Tank.TankModelPath);

            UpdateMenuTankModel(0);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading 3D menu assets: {ex.Message}");
        }
    }

    private void UpdateMenuTankModel(int tankIndex)
    {
        if (tankIndex >= 0 && tankIndex < _menuTankModelPaths.Length)
        {
            try
            {
                _currentMenuTankModel = _menuContent.Load<Model>("Models/" + GameConfig.Tank.TankModelPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading model {tankIndex}: {ex.Message}");
                _currentMenuTankModel = null;
            }
        }
    }

    public void HandleMenuState(KeyboardState kb, KeyboardState lastkb)
    {
        HandleMenuInput(kb, lastkb);
    }

    public void Update(GameTime gameTime, KeyboardState kb, KeyboardState lastKb)
    {
        HandleMusic();
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (CurrentState == GameState.Intro)
        {
            _introTimer += dt;

            bool keyPressed = kb.GetPressedKeys().Length > 0 && lastKb.GetPressedKeys().Length == 0;

            MouseState currentMouse = Mouse.GetState();
            bool mouseClicked = currentMouse.LeftButton == ButtonState.Pressed && _lastMouseState.LeftButton == ButtonState.Released;
            _lastMouseState = currentMouse;

            if (_introTimer >= 8f || keyPressed || mouseClicked)
            {
                CurrentState = GameState.Menu;
                _introTimer = 0f;
                _menuInputLockTime = 0.3f; //impide input bleed en GameState.Menu
            }
        }

        _menuTankRotation += _menuTankRotationSpeed;
        _idleTime += dt;

        //el menu maneja su propia logica, early return
        if (CurrentState == GameState.Menu)
        {
            //Bloquear input bleed por 0,3 seg
            if (_menuInputLockTime > 0f)
            {
                _menuInputLockTime -= dt;
                return;
            }

            HandleMenuState(kb, lastKb);
            return;
        }

        switch (CurrentState)
        {
            case GameState.Playing:
                if (kb.IsKeyDown(Keys.P) && lastKb.IsKeyUp(Keys.P))
                    CurrentState = GameState.Paused;
                break;
            case GameState.Paused:
                // SoundManager.StopMusic();
                if (kb.IsKeyDown(Keys.P) && lastKb.IsKeyUp(Keys.P))
                    CurrentState = GameState.Playing;
                break;
            case GameState.GameOver:
            case GameState.Win: // por ahora, mismo comportamiento que cuando se pierde
                SoundManager.StopMusic();
                if (kb.IsKeyDown(Keys.Enter) && lastKb.IsKeyUp(Keys.Enter))
                {
                    CurrentState = GameState.Menu;
                    _selectedIndex = 0;
                    _menuMusicStarted = false;
                    _currentMenuTankModel = null;
                    UpdateMenuTankModel(0);
                }
                break;
        }
    }

    private void HandleMenuInput(KeyboardState kb, KeyboardState lastKb)
    {
        // Teclado: flechas arriba/abajo
        if ((kb.IsKeyDown(Keys.Down) || kb.IsKeyDown(Keys.S)) && (lastKb.IsKeyUp(Keys.Down) && lastKb.IsKeyUp(Keys.S)))
            {
            TGCGame.Instance.SoundManager.PlaySound("enemy_cannon_fire");
            _selectedIndex = (_selectedIndex + 1) % _menuOptions.Length;
        }
            
        else if ((kb.IsKeyDown(Keys.Up) || kb.IsKeyDown(Keys.W)) && (lastKb.IsKeyUp(Keys.Up) && lastKb.IsKeyUp(Keys.W)))
        {
            TGCGame.Instance.SoundManager.PlaySound("enemy_cannon_fire");
            _selectedIndex = (_selectedIndex - 1 + _menuOptions.Length) % _menuOptions.Length;
        }
            
        // Teclado: enter
        if (kb.IsKeyDown(Keys.Enter) && lastKb.IsKeyUp(Keys.Enter))
            ApplySelection();

        // Mouse: hover y click
        MouseState currentMouse = Mouse.GetState();
        int hoveredIndex = GetOptionAtPosition(currentMouse.X, currentMouse.Y);
        if (hoveredIndex != -1)
        {
            if (hoveredIndex != _lastHoveredIndex)
                TGCGame.Instance.SoundManager.PlaySound("enemy_cannon_fire");

            _lastHoveredIndex = hoveredIndex;

            _selectedIndex = hoveredIndex; // Feedback de hover
            if (currentMouse.LeftButton == ButtonState.Pressed && _lastMouseState.LeftButton == ButtonState.Released)
                ApplySelection(); // Click selecciona esa opcion
        }
        _lastMouseState = currentMouse;
    }

    /// <summary>
    /// Determina si el cursor del mouse esta sobre alguna opcion del menu.
    /// Devuelve el indice de la opcion bajo el cursor, o -1 si no esta sobre ninguna.
    /// </summary>
    private int GetOptionAtPosition(int mouseX, int mouseY)
    {
        // 1. Obtener las dimensiones actuales de la ventana/pantalla
        var vp = _spriteBatch.GraphicsDevice.Viewport;

        // 2. Calcular el punto central exacto de la pantalla
        Vector2 center = new Vector2(vp.Width / 2f, vp.Height / 2f);

        // 3. Calcular la coordenada Y inicial para que el bloque completo de opciones quede centrado verticalmente.
        //    Se toma la mitad del alto total estimado del texto y se resta del centro.
        float pulse = (MathF.Sin(_idleTime * IdleAnimationSpeed) + 1f) / 2f;
        float scalePulse = 1.0f + pulse * 0.03f;

        // 5. Recorrer cada opción del menú para verificar si el mouse está dentro de su area visual
        for (int i = 0; i < _menuOptions.Length; i++)
        {
            bool isSelected = (i == _selectedIndex);
            float currentScale = isSelected ? scalePulse : 1f;

            Rectangle rect = CalculateOptionRectangle(i, center, currentScale);

            // Verificar si las coordenadas del mouse están dentro de este rectángulo
            if (rect.Contains(mouseX, mouseY))
                return i; // ¡Encontrado! Devolver el índice de la opción
        }

        // Si el bucle termina sin encontrar coincidencia, el mouse no está sobre ninguna opción
        return -1;
    }

    private void ApplySelection()
    {
        switch (_selectedIndex)
        {
            case 0: // Iniciar Scout
                TGCGame.SelectedPlayerTank = GameConfig.TankClass.Scout;
                TGCGame.Instance.ResetGame(); 
                CurrentState = GameState.Playing;
                break;
            case 1: // Iniciar Medio
                TGCGame.SelectedPlayerTank = GameConfig.TankClass.Medium;
                TGCGame.Instance.ResetGame(); 
                CurrentState = GameState.Playing;
                break;
            case 2: // Iniciar Pesado
                TGCGame.SelectedPlayerTank = GameConfig.TankClass.Heavy;
                TGCGame.Instance.ResetGame(); 
                CurrentState = GameState.Playing;
                break;
            case 3: // Salir
                Environment.Exit(0);
                break;
        }
    }

    public void Draw(string extraInfo = "")
    {
        if (CurrentState == GameState.Playing) return; // En Playing no dibuja nada extra

        var vp = _graphicsDevice.Viewport;
        Vector2 center = new Vector2(vp.Width / 2f, vp.Height / 2f);

        if (CurrentState == GameState.Intro)
        {
            _graphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            //Dibujar la textura estirada a toda la pantalla
            _spriteBatch.Draw(_menuBackground, new Rectangle(0, 0, vp.Width, vp.Height), Color.White);

            //Texto de "Presiona cualquier tecla o clic para continuar"
            if (_introTimer > 1.0f)
            {
                //Efecto de "pulso"
                float pulse = (MathF.Sin(_introTimer * 4f) + 1f) / 2f; // Oscila suavemente entre 0 y 1
                float alpha = MathHelper.Clamp((_introTimer - 1.0f) * 2f, 0f, 1f); // Fade-in de 0.5 segundos
                float scale = 1.3f + (pulse * 0.1f); // Escala entre 1.3x y 1.4x

                string hint = "Presiona cualquier tecla o clic para continuar";

                //Usar ConsolasFont
                Vector2 hintSize = _fontConsolas.MeasureString(hint) * scale;
                Vector2 hintPos = new Vector2(vp.Width / 2f - hintSize.X / 2f, vp.Height - 120f);

                //Fondo semitransparente detras del texto
                Rectangle bgRect = new Rectangle((int)hintPos.X - 30, (int)hintPos.Y - 15, (int)hintSize.X + 60, (int)hintSize.Y + 30);
                _spriteBatch.Draw(_whitePixel, bgRect, new Color(0, 0, 0, (int)(alpha * 200)));

                //Sombra gruesa y desplazada
                _spriteBatch.DrawString(_fontConsolas, hint, hintPos + new Vector2(4, 4), new Color(0, 0, 0, (int)(alpha * 255)), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

                //Texto principal pulsante
                Color textColor = Color.Lerp(Color.White, Color.Gold, pulse);
                _spriteBatch.DrawString(_fontConsolas, hint, hintPos, new Color(textColor.R, textColor.G, textColor.B, (int)(alpha * 255)), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }

            _spriteBatch.End();
        }

        if (CurrentState == GameState.Menu)
        {
            _graphicsDevice.Clear(Color.DarkSlateGray);

            if (_selectedIndex < 3 && _currentMenuTankModel != null)
            {

                // Camera and scale adjustments
                Matrix world = Matrix.CreateScale(4f) *
                               Matrix.CreateRotationX(MathHelper.ToRadians(-90f)) *
                               Matrix.CreateRotationY(_menuTankRotation);

                Matrix view = Matrix.CreateLookAt(new Vector3(10, 13f, 18f), new Vector3(0, 4, 0), Vector3.Up);
                Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, vp.AspectRatio, 0.1f, 100f);

                // Fix culling and depth issues
                _graphicsDevice.RasterizerState = RasterizerState.CullNone;
                _graphicsDevice.DepthStencilState = DepthStencilState.Default;
                _graphicsDevice.BlendState = BlendState.Opaque;

                Draw3DTank(world, view, projection);
            }

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            if (_selectedIndex < 3)
                DrawTankSpecs(vp);
            
            DrawMenu(center);
            _spriteBatch.End();
        }
        else if (CurrentState == GameState.Paused || CurrentState == GameState.GameOver || CurrentState == GameState.Win)
        {
            //_graphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            if (CurrentState == GameState.Paused)
            {
                _spriteBatch.Draw(_whitePixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.66f);
                DrawCenteredText("PAUSA\nPresiona P para continuar", center);
            }
            else if (CurrentState == GameState.GameOver)
            {
                _spriteBatch.Draw(_whitePixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.66f);
                DrawCenteredText($"GAME OVER\n{extraInfo}\nPresiona ENTER para volver al menu", center);
            }
            else if (CurrentState == GameState.Win)
            {
                _spriteBatch.Draw(_whitePixel, new Rectangle(0, 0, vp.Width, vp.Height), Color.Black * 0.66f);
                DrawCenteredText($"! GANASTE !\n{extraInfo}\nPresiona ENTER para volver al menu", center);
            }

            _spriteBatch.End();
        }
    }

    private void Draw3DTank(Matrix world, Matrix view, Matrix projection)
    {
        if (_currentMenuTankModel == null || _menuTankEffect == null || _menuTankTexture == null)
        {
            return;
        }

        Microsoft.Xna.Framework.Vector3 whiteColor = Microsoft.Xna.Framework.Vector3.One;
        Microsoft.Xna.Framework.Vector3 menuTankColor = Microsoft.Xna.Framework.Color.White.ToVector3();
        
        if (_selectedIndex == 0) // Scout
            menuTankColor = new Microsoft.Xna.Framework.Color(50, 205, 50).ToVector3();   // Verde
        else if (_selectedIndex == 1) // Medium
            menuTankColor = new Microsoft.Xna.Framework.Color(255, 215, 0).ToVector3();   // Amarillo
        else if (_selectedIndex == 2) // Heavy
            menuTankColor = new Microsoft.Xna.Framework.Color(178, 34, 34).ToVector3();   // Rojo

        _menuTankEffect.Parameters["LightDirection"].SetValue(new Microsoft.Xna.Framework.Vector3(0.5f, 1.0f, 0.3f));
        _menuTankEffect.Parameters["LightColor"].SetValue(Vector3.One);
        _menuTankEffect.Parameters["AmbientColor"].SetValue(new Vector3(0.2f, 0.2f, 0.2f));
        _menuTankEffect.Parameters["EyePosition"].SetValue(new Vector3(10f, 13f, 18f));
        _menuTankEffect.Parameters["Shininess"].SetValue(32f);

        foreach (var mesh in _currentMenuTankModel.Meshes)
        {
            Texture2D activeTexture = _menuTankTexture;
            if (mesh.Name.Contains("Cadena")) activeTexture = _menuTracksTexture;
            else activeTexture = _menuTankTexture;
            _menuTankEffect.Parameters["ModelTexture"].SetValue(activeTexture);

            Microsoft.Xna.Framework.Vector3 colorToApply = whiteColor;

            if (mesh.Name.Contains("Cabeza") || mesh.Name.Contains("Anillo") ||
                mesh.Name.Contains("Proteccion_d") || mesh.Name.Contains("Proteccion_i") ||
                mesh.Name.Contains("Cuerpo") || mesh.Name.Contains("Cubre"))
                colorToApply = menuTankColor;

            foreach (var part in mesh.MeshParts)
            {
                part.Effect = _menuTankEffect;
                _menuTankEffect.Parameters["World"].SetValue(world);
                _menuTankEffect.Parameters["View"].SetValue(view);
                _menuTankEffect.Parameters["Projection"].SetValue(projection);
                _menuTankEffect.Parameters["ModelTexture"].SetValue(activeTexture);
                _menuTankEffect.Parameters["DiffuseColor"].SetValue(colorToApply);

                _menuTankEffect.Parameters["HasImpact"]?.SetValue(0);
                _menuTankEffect.Parameters["ImpactPointWorld"]?.SetValue(Vector3.Zero);
                _menuTankEffect.Parameters["ImpactRadius"]?.SetValue(GameConfig.Tank.ImpactRadius);
                _menuTankEffect.Parameters["ImpactDepth"]?.SetValue(GameConfig.Tank.ImpactDepth);
                _menuTankEffect.Parameters["IsDeformable"]?.SetValue(0);
            }
            mesh.Draw();
        }
    }

    private void DrawTankSpecs(Viewport vp)
    {
        if (_selectedIndex != _specsCachedIndex)
        {
            string className;
            float playerHealth, maxSpeed, motorForce, turnSpeed, attackDamage;

            if (_selectedIndex == 0)
            {
                className = "SCOUT";
                playerHealth = GameConfig.TankClasses.Scout.PlayerHealth;
                maxSpeed = GameConfig.TankClasses.Scout.MaxSpeed;
                motorForce = GameConfig.TankClasses.Scout.MotorForce;
                turnSpeed = GameConfig.TankClasses.Scout.TurnSpeed;
                attackDamage = GameConfig.TankClasses.Scout.AttackDamage;
            }
            else if (_selectedIndex == 1)
            {
                className = "MEDIUM";
                playerHealth = GameConfig.TankClasses.Medium.PlayerHealth;
                maxSpeed = GameConfig.TankClasses.Medium.MaxSpeed;
                motorForce = GameConfig.TankClasses.Medium.MotorForce;
                turnSpeed = GameConfig.TankClasses.Medium.TurnSpeed;
                attackDamage = GameConfig.TankClasses.Medium.AttackDamage;
            }
            else
            {
                className = "HEAVY";
                playerHealth = GameConfig.TankClasses.Heavy.PlayerHealth;
                maxSpeed = GameConfig.TankClasses.Heavy.MaxSpeed;
                motorForce = GameConfig.TankClasses.Heavy.MotorForce;
                turnSpeed = GameConfig.TankClasses.Heavy.TurnSpeed;
                attackDamage = GameConfig.TankClasses.Heavy.AttackDamage;
            }

        // Se genera la cadena de texto una sola vez por cada cambio de tanque
        _cachedSpecsText = $"CLASE: {className}\n\n" +
                        $"HP Jugador:   {playerHealth}\n" +
                        $"Velocidad:    {maxSpeed} m/s\n" +
                        $"Fuerza Motor: {motorForce}\n" +
                        $"Vel. Giro:    {turnSpeed}\n" +
                        $"Danio Ataque: {attackDamage}";

        // Actualizamos el indice de control para recordar que este tanque ya esta procesado
        _specsCachedIndex = _selectedIndex;
        }

        // Posicionamiento de la caja de especificaciones en pantalla
        float padX = vp.Width * 0.05f;
        float padY = vp.Height * 0.15f;
        Vector2 specsPos = new Vector2(padX, padY);

        // Dibujamos usando el string en caché (Costo de CPU y asignaciones de memoria = 0)
        _spriteBatch.DrawString(_fontConsolas, _cachedSpecsText, specsPos + Vector2.One, Color.Black);
        _spriteBatch.DrawString(_fontConsolas, _cachedSpecsText, specsPos, Color.Gold);
    }

    private void DrawMenu(Vector2 center)
    {
        float pulse = (MathF.Sin(_idleTime * IdleAnimationSpeed) + 1f) / 2f;
        float arrowOffset = pulse * 4f;

        Color breathingColor = Color.Lerp(
            new Color(180, 140, 0),
            new Color(255, 223, 0),
            pulse
        );

        float scalePulse = 1.0f + pulse * 0.03f;

        for (int i = 0; i < _menuOptions.Length; i++)
        {
            string option = _menuOptions[i];
            bool isSelected = (i == _selectedIndex);
            float currentScale = isSelected ? scalePulse : 1f;

            // Usamos la unificacion del metodo que calcula la ubicacion y dimensiones exactas
            Rectangle rect = CalculateOptionRectangle(i, center, currentScale);
            Vector2 pos = new Vector2(rect.X, rect.Y);

            Color shadowColor = Color.Black;

            if (isSelected)
            {
                // Sombra con la escala animada aplicada
                _spriteBatch.DrawString(_fontArial, option, pos + new Vector2(2, 2), shadowColor,
                    0f, Vector2.Zero, scalePulse, SpriteEffects.None, 0f);

                // Texto principal con el color oscilante (breathing)
                _spriteBatch.DrawString(_fontArial, option, pos, breathingColor,
                    0f, Vector2.Zero, scalePulse, SpriteEffects.None, 0f);

                // Flechas dinamicas del menú
                var arrowSize = _fontArial.MeasureString("> ");
                
                // Flecha Izquierda
                _spriteBatch.DrawString(_fontArial, "> ",
                    new Vector2(pos.X - arrowSize.X + arrowOffset, pos.Y), breathingColor,
                    0f, Vector2.Zero, scalePulse, SpriteEffects.None, 0f);
                
                // Flecha Derecha
                _spriteBatch.DrawString(_fontArial, " <",
                    pos + new Vector2(rect.Width - arrowOffset, 0), breathingColor,
                    0f, Vector2.Zero, scalePulse, SpriteEffects.None, 0f);
            }
            else
            {
                _spriteBatch.DrawString(_fontArial, option, pos + new Vector2(2, 2), shadowColor);
                _spriteBatch.DrawString(_fontArial, option, pos, Color.White);
            }
        }
    }

    private Rectangle CalculateOptionRectangle(int index, Vector2 center, float scale = 1f)
    {
        float startY = center.Y - (_fontArial.LineSpacing * _menuOptions.Length / 2f);
        float offsetX = center.X * 0.3f;
        
        string option = _menuOptions[index];
        Vector2 originalSize = _fontArial.MeasureString(option);
        Vector2 scaledSize = originalSize * scale;

        // Posición base
        Vector2 basePos = new Vector2(offsetX - originalSize.X / 2f, startY + index * (_fontArial.LineSpacing + OptionSpacing));
        
        // Centrado correctivo para cuando se aplica escala animada
        Vector2 adjustedPos = basePos - (scaledSize - originalSize) / 2f;

        return new Rectangle((int)adjustedPos.X, (int)adjustedPos.Y, (int)scaledSize.X, (int)scaledSize.Y);
    }

    private void DrawCenteredText(string text, Vector2 center)
    {
        var size = _fontArial.MeasureString(text);
        var pos = center - size / 2;
        _spriteBatch.DrawString(_fontArial, text, pos + new Vector2(2), Color.Black);
        _spriteBatch.DrawString(_fontArial, text, pos, Color.White);
    }

    public void ForceState(GameState state) => CurrentState = state;

    // Maneja la reproduccion de musica segun el estado del juego
    private void HandleMusic()
    {
        if (CurrentState == GameState.Menu)
        {
            // Reproducir musica del menu solo si no esta sonando
            if (!_menuMusicStarted && MediaPlayer.State != MediaState.Playing)
            {
                _soundManager.StopMusic();
                _soundManager.PlayMusic("Music/100-song18", true);
                _menuMusicStarted = true;
            }
        }
        else
        {
            // Detener musica del menu al salir de ese estado
            if (_menuMusicStarted && MediaPlayer.State == MediaState.Playing)
            {
                _soundManager.StopMusic();
                _soundManager.PlayMusic("Music/101-Juhani Junkala [Retro Game Music Pack] Level 1", true);
                _menuMusicStarted = false;
            }
        }
    }

    //exponerlo para reproducir efectos 3d
    public SoundManager SoundManager => _soundManager;

    public void Dispose()
    {
        _spriteBatch?.Dispose();
        _whitePixel?.Dispose();
    }
}