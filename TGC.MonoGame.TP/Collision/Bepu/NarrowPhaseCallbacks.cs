using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;

using TGC.MonoGame.TP;
using TGC.MonoGame.TP.Models.Decorations;

public struct NarrowPhaseCallbacks : INarrowPhaseCallbacks
{
    private SpringSettings ContactSpringiness { get; set; }
    private float MaximumRecoveryVelocity { get; set; }
    private float FrictionCoefficient { get; set; }

    public NarrowPhaseCallbacks(SpringSettings contactSpringiness) : this(contactSpringiness, 2f, 1f)
    {
    }

    public NarrowPhaseCallbacks(SpringSettings contactSpringiness, float maximumRecoveryVelocity,
        float frictionCoefficient)
    {
        ContactSpringiness = contactSpringiness;
        MaximumRecoveryVelocity = maximumRecoveryVelocity;
        FrictionCoefficient = frictionCoefficient;
    }

    public void Initialize(Simulation simulation)
    {
        //Use a default if the springiness value wasn't initialized... at least until struct field initializers are supported outside of previews.
        if (ContactSpringiness.AngularFrequency == 0 && ContactSpringiness.TwiceDampingRatio == 0)
        {
            ContactSpringiness = new SpringSettings(30, 1);
            MaximumRecoveryVelocity = 2f;
            FrictionCoefficient = 1f;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b,
        ref float speculativeMargin)
    {
        //While the engine won't even try creating pairs between statics at all, it will ask about kinematic-kinematic pairs.
        //Those pairs cannot emit constraints since both involved bodies have infinite inertia. Since most of the demos don't need
        //to collect information about kinematic-kinematic pairs, we'll require that at least one of the bodies needs to be dynamic.
        return a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
    {
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold,
        out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        pairMaterial.FrictionCoefficient = FrictionCoefficient;
        pairMaterial.MaximumRecoveryVelocity = MaximumRecoveryVelocity;
        pairMaterial.SpringSettings = ContactSpringiness;

        // Si hay contacto, reviso si los dos elementos que hicieron contacto son dinamicos, si uno no lo es entonces no me interesa lo que ocurra
        if (pair.A.Mobility == CollidableMobility.Dynamic && pair.B.Mobility == CollidableMobility.Dynamic)
        { //Si ambos lo son actuamos yeii
            // Agarro el bodyHandle de cada elemento
            BodyHandle handleA = pair.A.BodyHandle;
            BodyHandle handleB = pair.B.BodyHandle;

            // Agarro el bodyHanldle del tanque para comparar
            BodyHandle tankHandle = TGCGame.Instance._tank.TankHandler;

            // Reviso si alguna de las dos partes es el tanque
            if (handleA == tankHandle || handleB == tankHandle)
            {
                // Identifico el obstaculo
                BodyHandle obstacleHandle = (handleA == tankHandle) ? handleB : handleA;

                // Reviso la lista de decoraciones del AssetsManager
                var objetoChocado = TGCGame.Instance._assets._decorationModels
                    .OfType<Dinamic>() //Tomo solo los dinamicos
                    .FirstOrDefault(d => d.bodyHandle == obstacleHandle); //El objeto de la lista debe ser el chocado

                // Si lo encontre y el objeto vive lo mando a matar
                if (objetoChocado != null && !objetoChocado.IsDead)
                {
                    objetoChocado.HandleCollision(); //Aca lo declaro muerto
                }
            }

            // Buscar si alguno de los handles pertenece a una bala
            var cannonball = TGCGame.Instance.Cannonballs.FirstOrDefault(c => c.BodyHandle == handleA || c.BodyHandle == handleB);

            if (cannonball != null)
            {
                BodyHandle obstacleHandle = (cannonball.BodyHandle == handleA) ? handleB : handleA;

                var objetoChocado = TGCGame.Instance._assets._decorationModels.OfType<Dinamic>().FirstOrDefault(d => d.bodyHandle == obstacleHandle);

                if (objetoChocado != null && !objetoChocado.IsDead)
                {
                    objetoChocado.HandleCollision();
                }
            }

        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB,
        ref ConvexContactManifold manifold)
    {
        return true;
    }

    public void Dispose()
    {
        //Something to be dispose.
    }
}