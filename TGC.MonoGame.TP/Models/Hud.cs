using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;
using TGC.MonoGame.TP.Managers;

namespace TGC.MonoGame.TP.Models;

/// <summary>
///     hud simple que muestra los controles del juego en pantalla.
///     Implementacion responsive y indicador de enfriamiento del canon.
/// </summary>
public class Hud
{
    private SpriteBatch _spriteBatch;
    private SpriteFont _font;
    private readonly string[] _instructions;
    private readonly Color _textColor = Color.White;

    // Parámetros de diseño responsive
    private readonly float _paddingPercentX = 0.02f;
    private readonly float _paddingPercentY = 0.02f;
    private readonly float _spacingPercent = 0.008f;

    // Elementos gráficos para la barra de progreso vectorial
    private Texture2D _whitePixel;
    private const int ProgressBarWidth = 150;
    private const int ProgressBarHeight = 12;

    // Variables de cálculo para los FPS
    private float _fps;
    private float _fpsAccumulator;
    private int _fpsFrameCount;

    // --- VARIABLES DE CACHÉ REACTIVA (Evitan miles de allocations de strings por segundo) ---
    private string _cachedFpsText = "FPS: 0";
    private int _lastDisplayedFps = -1;

    private string _cachedFuelText = "FUEL: 100 / 100";
    private int _lastDisplayedFuel = -1;

    private string _cachedPosText = "X: 0.0  Y: 0.0  Z: 0.0";
    private int _lastDisplayedPosX = -9999;
    private int _lastDisplayedPosZ = -9999;

    private string _cachedCooldownText = "DISPARO: LISTO";
    private float _lastRemainingSeconds = -1f;

    private string _cachedEnemiesCount = "KILL COUNT: 0 / " + GameConfig.Enemies.EnemiesCount.ToString();
    private int _lastDisplayedEnemies = -1;
    private int _playerHealth = 100;

    private string _cachedPlayerHealth = "Health : / ";
    private float _lastDisplayedHealth = -1.0f;

    // Propiedades expuestas que actualiza el juego en cada frame
    public Vector3 TankPosition { get; set; }
    public float TankFuel { get; set; }
    public float CannonCurrentCooldown { get; set; }
    public float CannonMaxCooldown { get; set; } = 0.5f;

    public Hud()
    {
        _instructions = new[]
        {
            "movimiento: w / a / s / d",
            "pausa: p",
            "zoom: rueda del mouse",
            "salir: escape"
        };

    }

    public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _font = content.Load<SpriteFont>("SpriteFonts/ArialFont");

        _whitePixel = new Texture2D(graphicsDevice, 1, 1);
        _whitePixel.SetData(new[] { Color.White });
    }

    /// <summary>
    ///     Actualiza el contador de FPS. Llamar desde TGCGame.Update()
    /// </summary>
    public void Update(GameTime gameTime)
    {
        _fpsAccumulator += (float)gameTime.ElapsedGameTime.TotalSeconds;
        _fpsFrameCount++;

        // Recalcular el promedio cada segundo
        if (_fpsAccumulator >= 1f)
        {
            _fps = _fpsFrameCount / _fpsAccumulator;
            _fpsAccumulator = 0f;
            _fpsFrameCount = 0;
        }

        // calcular la cantidad de kills
        var kills = TGCGame.Instance.EnemiesKilled;
        if (kills != _lastDisplayedEnemies)
        {
            _cachedEnemiesCount = "KILL COUNT: " + kills + " / " + GameConfig.Enemies.EnemiesCount.ToString();
            _lastDisplayedEnemies = kills;
        }

        // calculo la vida del jugador
        _playerHealth = getPlayerHealth();
        if (_playerHealth != _lastDisplayedHealth)
        {
            _cachedPlayerHealth = $"HEALTH: {_playerHealth} / {TGCGame.Instance._tank.initialHealth}";
            _lastDisplayedHealth = _playerHealth;            
        }

    }

    public void Draw()
    {
        // Obtener dimensiones actuales del viewport para adaptar el hud a cualquier resolucion
        var viewport = _spriteBatch.GraphicsDevice.Viewport;
        float screenWidth = viewport.Width;
        float screenHeight = viewport.Height;

        // Calcular margenes y saltos de linea proporcionales al tamano de la pantalla
        float padX = screenWidth * _paddingPercentX;
        float padY = screenHeight * _paddingPercentY;
        float spacing = screenHeight * _spacingPercent;

        // Esta linea es la que desactiva el Z-Buffer
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        // === INSTRUCCIONES (esquina superior izquierda) ===
        var drawPosition = new Vector2(padX, padY);
        foreach (var line in _instructions)
        {
            _spriteBatch.DrawString(_font, line, drawPosition + new Vector2(1, 1), Color.Black);
            _spriteBatch.DrawString(_font, line, drawPosition, _textColor);
            drawPosition.Y += _font.LineSpacing + spacing;
        }

        //Cache Reactivo de String
        // === INDICADOR DE FPS (esquina superior derecha) ===
        int currentFpsInt = (int)_fps;
        if (currentFpsInt != _lastDisplayedFps)
        {
            _cachedFpsText = $"FPS: {currentFpsInt}";
            _lastDisplayedFps = currentFpsInt;
        }

        // Color segun rendimiento (verde/amarillo/rojo)
        var fpsPosition = new Vector2(screenWidth - 100f - padX, padY);
        Color fpsColor = currentFpsInt >= 60 
                        ? Color.Lime : currentFpsInt >= 30 
                        ? Color.Yellow : Color.Red;

        //sombra para mejor legibilidad
        _spriteBatch.DrawString(_font, _cachedFpsText, fpsPosition + Vector2.One, Color.Black);
        _spriteBatch.DrawString(_font, _cachedFpsText, fpsPosition, fpsColor);

        // === COORDENADAS DEL TANQUE (debajo del FPS) ===
        int roundedX = (int)TankPosition.X;
        int roundedZ = (int)TankPosition.Z;
        if (roundedX != _lastDisplayedPosX || roundedZ != _lastDisplayedPosZ)
        {
            _cachedPosText = $"X: {TankPosition.X:F1}  Y: {TankPosition.Y:F1}  Z: {TankPosition.Z:F1}";
            _lastDisplayedPosX = roundedX;
            _lastDisplayedPosZ = roundedZ;
        }

        var posPosition = new Vector2(fpsPosition.X - 150f, fpsPosition.Y + _font.LineSpacing + spacing);
        _spriteBatch.DrawString(_font, _cachedPosText, posPosition + Vector2.One, Color.Black);
        _spriteBatch.DrawString(_font, _cachedPosText, posPosition, _textColor);

        // === COMBUSTIBLE DEL TANQUE (debajo de coordenadas) ===
        int fuelInt = (int)TankFuel;
        if (fuelInt != _lastDisplayedFuel)
        {
            _cachedFuelText = $"FUEL: {fuelInt} / 100";
            _lastDisplayedFuel = fuelInt;
        }

        var fuelPosition = new Vector2(posPosition.X, posPosition.Y + _font.LineSpacing + spacing);
        Color fuelColor = TankFuel > 30f ? Color.Lime : TankFuel > 10f ? Color.Yellow : Color.Red;

        _spriteBatch.DrawString(_font, _cachedFuelText, fuelPosition + Vector2.One, Color.Black);
        _spriteBatch.DrawString(_font, _cachedFuelText, fuelPosition, fuelColor);

        float remaining = MathHelper.Max(0f, CannonCurrentCooldown);
        if (MathF.Abs(remaining - _lastRemainingSeconds) > 0.05f || (remaining == 0f && _lastRemainingSeconds > 0f))
        {
            _cachedCooldownText = remaining > 0f ? $"DISPARO: {remaining:F1}s" : "DISPARO: LISTO";
            _lastRemainingSeconds = remaining;
        }

        var cooldownPosition = new Vector2(fuelPosition.X, fuelPosition.Y + _font.LineSpacing + spacing);
        Color cooldownColor = remaining <= 0f ? Color.Lime : Color.Orange;

        _spriteBatch.DrawString(_font, _cachedCooldownText, cooldownPosition + Vector2.One, Color.Black);
        _spriteBatch.DrawString(_font, _cachedCooldownText, cooldownPosition, cooldownColor);

        // --- BARRA DE PROGRESO VECTORIAL GRÁFICA (Ahorra un 100% de Garbage Collector en este render) ---
        float barPercent = CannonMaxCooldown > 0f ? (1f - (remaining / CannonMaxCooldown)) : 1f;
        int activeWidth = (int)(barPercent * ProgressBarWidth);

        var barY = cooldownPosition.Y + _font.LineSpacing + spacing;
        var barBgRect = new Rectangle((int)cooldownPosition.X, (int)barY, ProgressBarWidth, ProgressBarHeight);
        var barFillRect = new Rectangle((int)cooldownPosition.X, (int)barY, activeWidth, ProgressBarHeight);

        // Dibujar borde/fondo oscuro semitransparente
        _spriteBatch.Draw(_whitePixel, barBgRect, Color.Black * 0.4f);
        // Dibujar relleno dinámico escalado según el cooldown
        _spriteBatch.Draw(_whitePixel, barFillRect, cooldownColor);

        // === ENEMIGOS DERROTADOS ===
        var killsPosition = new Vector2(cooldownPosition.X, barY + ProgressBarHeight + spacing);
        _spriteBatch.DrawString(_font, _cachedEnemiesCount, killsPosition + Vector2.One, Color.Black);
        _spriteBatch.DrawString(_font, _cachedEnemiesCount, killsPosition, Color.White);

        // === VIDA RESTANTE ===
        var healthPorc = getPlayerHealth() / TGCGame.Instance._tank.initialHealth * 100;
        Color healthColor = healthPorc > 50f ? Color.Lime : healthPorc > 25f ? Color.Yellow : Color.Red;
        _spriteBatch.DrawString(_font, _cachedPlayerHealth, drawPosition + Vector2.One, Color.Black);
        _spriteBatch.DrawString(_font, _cachedPlayerHealth, drawPosition, healthColor);

        _spriteBatch.End();
    }

    private int getPlayerHealth() => (int)(TGCGame.Instance._tank.HealthPoints);

    public void Dispose()
    {
        _spriteBatch?.Dispose();
        _whitePixel?.Dispose();
    }
}