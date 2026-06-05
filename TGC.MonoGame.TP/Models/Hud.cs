using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

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

    // Porcentajes para calcular margenes y espaciado de forma dinamica
    private readonly float _paddingPercentX = 0.02f;
    private readonly float _paddingPercentY = 0.02f;
    private readonly float _spacingPercent = 0.008f;

    // === VARIABLES PARA FPS ===
    private float _fps;
    private float _fpsAccumulator;
    private int _fpsFrameCount;

    // === VARIABLES PARA DEBUG ===
    public Vector3 TankPosition { get; set; }
    public float TankFuel { get; set; }

    // === VARIABLES PARA COOLDOWN DEL CANON ===
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

        // === INDICADOR DE FPS (esquina superior derecha) ===
        string fpsText = $"FPS: {_fps:F0}";
        var fpsSize = _font.MeasureString(fpsText);
        var fpsPosition = new Vector2(
            screenWidth - fpsSize.X - padX,
            padY
        );
        // Color segun rendimiento (verde/amarillo/rojo)
        Color fpsColor = _fps >= 60 ? Color.Lime :
                         _fps >= 30 ? Color.Yellow :
                         Color.Red;
        // Sombra para mejor legibilidad
        _spriteBatch.DrawString(_font, fpsText, fpsPosition + Vector2.One, Color.Black);
        _spriteBatch.DrawString(_font, fpsText, fpsPosition, fpsColor);

        // === COORDENADAS DEL TANQUE (debajo del FPS) ===
        string posText = $"X: {TankPosition.X:F1}  Y: {TankPosition.Y:F1}  Z: {TankPosition.Z:F1}";
        var posSize = _font.MeasureString(posText);
        var posPosition = new Vector2(fpsPosition.X - 150f, fpsPosition.Y + fpsSize.Y + spacing);
        _spriteBatch.DrawString(_font, posText, posPosition + Vector2.One, Color.Black);
        _spriteBatch.DrawString(_font, posText, posPosition, _textColor);

        // === COMBUSTIBLE DEL TANQUE (debajo de coordenadas) ===
        string fuelText = $"FUEL: {(int)TankFuel} / 100";
        var fuelSize = _font.MeasureString(fuelText);
        var fuelPosition = new Vector2(posPosition.X, posPosition.Y + posSize.Y + spacing);
        Color fuelColor = TankFuel > 30f ? Color.Lime : TankFuel > 10f ? Color.Yellow : Color.Red;
        _spriteBatch.DrawString(_font, fuelText, fuelPosition + Vector2.One, Color.Black);
        _spriteBatch.DrawString(_font, fuelText, fuelPosition, fuelColor);

        // === COOLDOWN DEL CANON ===
        float remaining = MathHelper.Max(0f, CannonCurrentCooldown);
        string cooldownText = remaining > 0f ? $"DISPARO: {remaining:F1}s" : "DISPARO: LISTO";
        var cooldownSize = _font.MeasureString(cooldownText);

        // Posicion calculada desde la esquina inferior derecha para mantener margen
        var cooldownPosition = new Vector2(
            fuelPosition.X,
            fuelPosition.Y + fuelSize.Y + spacing
        );
        // Color segun estado (verde si esta listo, naranja si se esta enfriando)
        Color cooldownColor = remaining <= 0f ? Color.Lime : Color.Orange;
        // Sombra para mejor legibilidad
        _spriteBatch.DrawString(_font, cooldownText, cooldownPosition + Vector2.One, Color.Black);
        _spriteBatch.DrawString(_font, cooldownText, cooldownPosition, cooldownColor);

        // Barra de progreso textual ASCII debajo del indicador
        // Podria usar un font monospace pero no queda mal asi
        float barPercent = CannonMaxCooldown > 0f ? (1f - (remaining / CannonMaxCooldown)) : 1f;
        int barLength = 15;
        int filledChars = (int)(barPercent * barLength);
        string barVisual = "[" + new string('|', filledChars).PadRight(barLength, ' ') + "]";
        Vector2 barPos = new Vector2(cooldownPosition.X, cooldownPosition.Y + cooldownSize.Y + spacing);
        _spriteBatch.DrawString(_font, barVisual, barPos + Vector2.One, Color.Black);
        _spriteBatch.DrawString(_font, barVisual, barPos, cooldownColor);

        _spriteBatch.End();
    }

    public void Dispose()
    {
        _spriteBatch?.Dispose();
    }
}