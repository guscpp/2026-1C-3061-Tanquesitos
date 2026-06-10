using BepuPhysics;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using TGC.MonoGame.TP.Collisions.Bepu;

using BepuVector3 = System.Numerics.Vector3;

namespace TGC.MonoGame.TP.Models.AimAssist;

public static class AimAssist
{   
    private static List<Vector3> _points = new();

    private static List<AimAssistSphere> _visualSpheres = new();

    public static void UpdateTrajectory(BepuVector3 startPosition, BepuVector3 initialVelocity, BepuVector3 gravity, Simulation simulation, BodyHandle tankHandle)
    {
        RayHitHandler rayHandler = new(tankHandle);
        // Limpiamos las listas para que no se mezclen trayectorias de frames diferentes
        _points.Clear();
        _visualSpheres.Clear();
        
        BepuVector3 currentPoint = startPosition;
        _points.Add(currentPoint.ToXna());

        for (int i = 1; !rayHandler.Hit; i++)
        {
            float time = i * 0.05f;  // Multiplicamos por un "time step" (a menor valor, mas suave es la curva)

            BepuVector3 nextPoint = startPosition + initialVelocity * time + gravity * (0.5f * time * time);
            BepuVector3 rayDirection = nextPoint - currentPoint;
            simulation.RayCast(currentPoint, rayDirection, 1f, ref rayHandler);
            
            if (rayHandler.Hit)
            {
                BepuVector3 impactPoint = currentPoint + (rayDirection * rayHandler.T);
                _points.Add(impactPoint.ToXna());
            }
            else
            {
                _points.Add(nextPoint.ToXna());
                currentPoint = nextPoint;  
            }

            AimAssistSphere sphere = new(_points[_points.Count - 1]);
            _visualSpheres.Add(sphere);
        }
    }

    public static void DrawTrajectory(Matrix view, Matrix projection)
    {
        // Si el juego está iniciando y no hay datos estables, salimos temprano y evitamos corromper las matrices del shader.
        if (_points.Count < 2 || _visualSpheres.Count == 0) return;

        foreach (var sphere in _visualSpheres)
        {
            sphere.Draw(view, projection);
        }
    }
}
