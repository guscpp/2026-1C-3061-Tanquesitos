using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TGC.MonoGame.TP.Models;

public class GameStateManager
{
    public GameState CurrentState { get; private set; } = GameState.Menu;

    private readonly SpriteBatch _spriteBatch;
    private readonly SpriteFont _font;

    public GameStateManager(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _spriteBatch = new SpriteBatch(graphicsDevice);
        // Coincide con la ruta compilada en Content.mgcb
        _font = content.Load<SpriteFont>("SpriteFonts/ArialFont");
    }

    public void Update(KeyboardState kb, KeyboardState lastKb)
    {
        switch (CurrentState)
        {
            case GameState.Menu:
                if (kb.IsKeyDown(Keys.Enter) && lastKb.IsKeyUp(Keys.Enter))
                    CurrentState = GameState.Playing;
                break;

            case GameState.Playing:
                if (kb.IsKeyDown(Keys.P) && lastKb.IsKeyUp(Keys.P))
                    CurrentState = GameState.Paused;
                // Descomenta y ajusta cuando tengas la condición de derrota/victoria:
                // if (_tank.IsDead) CurrentState = GameState.GameOver;
                break;

            case GameState.Paused:
                if (kb.IsKeyDown(Keys.P) && lastKb.IsKeyUp(Keys.P))
                    CurrentState = GameState.Playing;
                break;

            case GameState.GameOver:
                if (kb.IsKeyDown(Keys.Enter) && lastKb.IsKeyUp(Keys.Enter))
                    CurrentState = GameState.Menu;
                break;
        }
    }

    public void Draw(string extraInfo = "")
    {
        if (CurrentState == GameState.Playing) return; // En Playing no dibuja nada extra

        _spriteBatch.Begin();
        var vp = _spriteBatch.GraphicsDevice.Viewport;
        Vector2 center = new Vector2(vp.Width / 2f, vp.Height / 2f);

        string text = CurrentState switch
        {
            GameState.Menu => "TANQUEsITOS\n\nPresiona ENTER para jugar",
            GameState.Paused => "PAUSA\n\nPresiona P para continuar",
            GameState.GameOver => $"GAME OVER\n{extraInfo}\n\nPresiona ENTER para volver",
            _ => ""
        };

        DrawCenteredText(text, center);
        _spriteBatch.End();
    }

    private void DrawCenteredText(string text, Vector2 center)
    {
        var size = _font.MeasureString(text);
        var pos = center - size / 2;
        _spriteBatch.DrawString(_font, text, pos + new Vector2(2), Color.Black);
        _spriteBatch.DrawString(_font, text, pos, Color.White);
    }

    public void ForceState(GameState state) => CurrentState = state;
}