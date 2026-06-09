using System.Runtime.CompilerServices;

using XnaVector3 = Microsoft.Xna.Framework.Vector3;
using BepuVector3 = System.Numerics.Vector3;

namespace TGC.MonoGame.TP.Collisions.Bepu;

public static class VectorExtensions
{
    /// <summary>
    /// Converts a MonoGame Vector3 to a BEPU Vector3
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BepuVector3 ToNumerics(this XnaVector3 vector3D)
    {
        return new BepuVector3(vector3D.X, vector3D.Y, vector3D.Z);
    }

    /// <summary>
    /// Converts a BEPU Vector3 to a MonoGame Vector3
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static XnaVector3 ToXna(this BepuVector3 vector3D)
    {
        return new XnaVector3(vector3D.X, vector3D.Y, vector3D.Z);
    }
}