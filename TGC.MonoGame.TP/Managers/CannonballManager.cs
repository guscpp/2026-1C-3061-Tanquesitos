using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using BepuPhysics;
using TGC.MonoGame.TP.Models.Tanks;

namespace TGC.MonoGame.TP.Managers;

public class CannonballManager
{
    private readonly Simulation _simulation;
    private Model _cannonballModel;
    private Effect _cannonballEffect;
    private readonly List<Cannonball> _cannonballs;

    private readonly float _shootCooldown;
    private float _currentCooldown;
    public float CurrentCooldown => _currentCooldown;
    public bool CanFire => _currentCooldown <= 0f;
    private Dictionary<BodyHandle, Cannonball> _cannonballsByHandle = new();

    public CannonballManager(Simulation simulation, float cooldown)
    {
        _simulation = simulation;
        _shootCooldown = cooldown;
        _currentCooldown = 0f;
        _cannonballs = new List<Cannonball>();
    }

    public void LoadContent(ContentManager content, string modelPath, string effectPath)
    {
        _cannonballModel = content.Load<Model>(modelPath);
        _cannonballEffect = content.Load<Effect>(effectPath);
    }

    public void UpdateCooldown(float deltaTime)
    {
        if (_currentCooldown > 0f)
        {
            _currentCooldown -= deltaTime;
        }
    }

    public void Fire(Vector3 spawnPosition, Vector3 direction, float damage, SoundManager soundManager, Vector3 listenerPos, Vector3 listenerForward, bool isPlayer = false)
    {
        if (isPlayer && !CanFire) return;

        var cannonball = new Cannonball(_cannonballModel, damage, _cannonballEffect, spawnPosition, direction, _simulation);
        _cannonballs.Add(cannonball);
        _cannonballsByHandle[cannonball.BodyHandle] = cannonball;

        if (isPlayer) _currentCooldown = _shootCooldown;

        soundManager.PlaySound3D("cannon_fire", spawnPosition, listenerPos, listenerForward);
    }

    public Vector3 GetCannonballPosition(BodyHandle handle)
    {
        if (_cannonballsByHandle.TryGetValue(handle, out var cb))
        {
            var body = _simulation.Bodies[cb.BodyHandle];
            return new Vector3(body.Pose.Position.X, body.Pose.Position.Y, body.Pose.Position.Z);
        }
        return Vector3.Zero;
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        UpdateCooldown(dt);

        for (int i = _cannonballs.Count - 1; i >= 0; i--)
        {
            var cb = _cannonballs[i];
            cb.Update(gameTime, _simulation);

            if (cb.IsDead)
            {
                _simulation.Bodies.Remove(cb.BodyHandle);
                _cannonballsByHandle.Remove(cb.BodyHandle);
                _cannonballs.RemoveAt(i);
            }
        }
    }

    public void Draw(Matrix view, Matrix projection)
    {
        foreach (var cannonball in _cannonballs)
        {
            cannonball.Draw(view, projection);
        }
    }

    public void DrawDepth(Matrix lightViewProjection)
    {
        foreach (var cannonball in _cannonballs)
        {
            cannonball.DrawDepth(lightViewProjection);
        }
    }

    public void Clear()
    {
        for (int i = _cannonballs.Count - 1; i >= 0; i--)
        {
            _simulation.Bodies.Remove(_cannonballs[i].BodyHandle);
            _cannonballs.RemoveAt(i);
        }
        _cannonballsByHandle.Clear();
        _currentCooldown = 0f;
    }

    public bool TryGetCannonball(BodyHandle handle, out Cannonball cannonball)
    {
        return _cannonballsByHandle.TryGetValue(handle, out cannonball);
    }
}