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

    public static void CalculateTrajectory(BepuVector3 startPosition, BepuVector3 initialVelocity, BepuVector3 gravity, Simulation simulation, BodyHandle tankHandle)
    {
        RayHitHandler rayHandler = new(tankHandle);
        _points.Clear();  // Limpiamos la lista para que no se mezclen las trayectorias de frames diferentes
        
        BepuVector3 currentPoint = startPosition;
        _points.Add(currentPoint);

        for (int i = 1; !rayHandler.Hit; i++)
        {
            float time = i * 0.05f;    // Multiplicamos por un "time step" (a menor valor, mas suave es la curva)

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
        }
    }

    public static void DrawTrajectory(Matrix view, Matrix projection)
    {
        _visualSpheres.Clear();  // Borramos las esferas correspondientes a la trayectoria anterior

        for (int i = 1; i < _points.Count; i++)
        {
            AimAssistSphere sphere = new(_points[i]);
            _visualSpheres.Add(sphere);
            sphere.Draw(view, projection);
        }
    }
}
