using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Trees;
using System.Numerics;

namespace TGC.MonoGame.TP.Collisions.Bepu;

public struct RayHitHandler : IRayHitHandler {
    
    public bool Hit;
    public Vector3 Normal;
    public float T;
    public CollidableReference HitCollidable;
    public int HitChildIndex;

    private BodyHandle _originTank;

    public RayHitHandler(BodyHandle tankHandle)
    {
        _originTank = tankHandle;
        
        // Inicializamos los campos por defecto (Requerido en C# para structs)
        Hit = false;
        Normal = Vector3.Zero;
        T = 0f;
        HitCollidable = default;
        HitChildIndex = 0;
    }

    // 1. Filtro rápido para objetos principales (Tanques, edificios, suelo)
    public bool AllowTest(CollidableReference collidable)
    {
        // En BEPU, un CollidableReference puede ser un objeto estático o un cuerpo dinámico (Body).
        // Si es un Body, verificamos si su BodyHandle es igual al del tanque que dispara.
        if (collidable.Mobility == CollidableMobility.Dynamic)
        {
            if (collidable.BodyHandle == _originTank)
            {
                return false; // ¡Es el tanque propio! Ignoramos la colisión por completo.
            }
        }
        
        return true; // Si es cualquier otro objeto, procedemos con el test   
    }

    // 2. Filtro detallado para sub-objetos (Hijos de compuestos y demás)
    public bool AllowTest(CollidableReference collidable, int childIndex)
    {
        return true; // Por defecto dejamos que evalúe todos los sub-objetos
    }

    // 3. Confirmación de impacto real contra la geometría
    public void OnRayHit(in RayData ray, ref float maximumT, float t, in Vector3 normal, CollidableReference collidable, int childIndex)
    {
        // TRUCO DE OPTIMIZACIÓN DE BEPU: 
        // Reducimos el alcance máximo permitido para futuros tests al valor de 't' actual.
        // Así el motor ignorará automáticamente cualquier objeto que esté más lejos que este choque.
        maximumT = t;

        // Guardamos los datos de este impacto (que por la línea anterior, terminará siendo el más cercano)
        Hit = true;
        Normal = normal;
        T = t;
        HitCollidable = collidable;
        HitChildIndex = childIndex; 
    }
}