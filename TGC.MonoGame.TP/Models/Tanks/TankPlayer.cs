using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using BepuPhysics;

namespace TGC.MonoGame.TP.Models;

public class TankPlayer : TankBase
{
    public float CurrentFuel { get; private set; } = GameConfig.Tank.MaxFuel;

    public TankPlayer()
    {
        MaxSpeed = GameConfig.Tank.MaxSpeed;
        MotorForce = GameConfig.Tank.MotorForce;
        TurnSpeed = GameConfig.Tank.TurnSpeed;
        ForwardDrag = GameConfig.Tank.ForwardDrag;
        LateralDrag = GameConfig.Tank.LateralDrag;
        HealthPoints = GameConfig.Tank.HealthPoints;
        AttackDamage = GameConfig.Tank.AttackDamage;
    }

    public void AddFuel(float amount) => CurrentFuel = MathHelper.Clamp(CurrentFuel + amount, 0f, GameConfig.Tank.MaxFuel);

    public void Update(GameTime gameTime, KeyboardState keyboard, Simulation simulation)
    {
        if (IsDead) return;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        float forwardInput = 0f;
        if (keyboard.IsKeyDown(Keys.W)) forwardInput += 1f;
        if (keyboard.IsKeyDown(Keys.S)) forwardInput -= 1f;

        if (CurrentFuel <= 0f) forwardInput = 0f;
        else if (forwardInput != 0f) CurrentFuel -= GameConfig.Tank.FuelConsumptionRate * dt;
        CurrentFuel = MathHelper.Clamp(CurrentFuel, 0f, GameConfig.Tank.MaxFuel);

        float turnInput = 0f;
        if (keyboard.IsKeyDown(Keys.A)) turnInput += 1f;
        if (keyboard.IsKeyDown(Keys.D)) turnInput -= 1f;

        // Control de torreta con mouse
        if (TGCGame.Instance.IsActive)
        {
            TGCGame.Instance.IsMouseVisible = false;
            var ms = Mouse.GetState();
            int cx = TGCGame.Instance.GraphicsDevice.Viewport.Width / 2;
            int cy = TGCGame.Instance.GraphicsDevice.Viewport.Height / 2;
            _turretRotation -= (ms.X - cx) * 0.0015f;
            _cannonRotation -= (ms.Y - cy) * 0.0015f;
            _cannonRotation = MathHelper.Clamp(_cannonRotation, _minCannonPitch, _maxCannonPitch);
            Mouse.SetPosition(cx, cy);
        }
        else TGCGame.Instance.IsMouseVisible = true;

        ApplyPhysics(simulation, dt, forwardInput, turnInput);
    }
}