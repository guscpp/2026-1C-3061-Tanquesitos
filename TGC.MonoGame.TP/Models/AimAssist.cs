using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace TGC.MonoGame.TP.Models
{
    public static class AimAssist
    {
        public static List<Vector3> CalculateTrajectory(Vector3 startPosition, Vector3 initialVelocity,
            Vector3 gravity, int sampleCount, float timeStep)
        {
            var points = new List<Vector3>(sampleCount);

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i * timeStep;

                Vector3 point = startPosition + initialVelocity * t + gravity * (0.5f * t * t);

                points.Add(point);
            }

            return points;
        }
    }
}