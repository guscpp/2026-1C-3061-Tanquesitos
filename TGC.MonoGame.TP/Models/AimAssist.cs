using BepuPhysics;
using System.Numerics;
using System.Collections.Generic;
using TGC.MonoGame.TP.Collisions.Bepu;

using XnaVector3 = Microsoft.Xna.Framework.Vector3;

namespace TGC.MonoGame.TP.Models
{
    public static class AimAssist
    {
        public static List<XnaVector3> CalculateTrajectory(Vector3 startPosition, Vector3 initialVelocity, Vector3 gravity, Simulation simulation, BodyHandle tankHandle)
        {
            var points = new List<XnaVector3>(100);
            RayHitHandler rayHandler = new(tankHandle);
            
            Vector3 currentPoint = startPosition;
            points.Add(currentPoint);

            for (int i = 1; !rayHandler.Hit; i++)
            {
                float time = i * 0.05f;    // Multiplicamos por un "time step" (a menor valor, mas suave es la curva)

                Vector3 nextPoint = startPosition + initialVelocity * time + gravity * (0.5f * time * time);
                Vector3 rayDirection = nextPoint - currentPoint;
                simulation.RayCast(currentPoint, rayDirection, 1f, ref rayHandler);
                
                if (rayHandler.Hit)
                {
                    Vector3 impactPoint = currentPoint + (rayDirection * rayHandler.T);
                    points.Add(impactPoint.ToXna());
                }
                else
                {
                    points.Add(nextPoint.ToXna());
                    currentPoint = nextPoint;  
                }
            }

            return points;
        }
    }
}