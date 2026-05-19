using System.Numerics;

public static class VectorExtensions
{
    /// <summary>
    /// Microsoft.Xna.Framework.Vector3 to System.Numerics.Vector3
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public static Vector3 ToNumerics(
        this Microsoft.Xna.Framework.Vector3 v)
    {
        return new Vector3(v.X, v.Y, v.Z);
    }

    // <summary>
    /// System.Numerics.Vector3 to Microsoft.Xna.Framework.Vector3
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public static Microsoft.Xna.Framework.Vector3 ToXNA(
        this Vector3 v)
    {
        return new Microsoft.Xna.Framework.Vector3(
            v.X,
            v.Y,
            v.Z
        );
    }
}