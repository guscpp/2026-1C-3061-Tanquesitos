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
using TGC.MonoGame.TP.Models.Tanks;
using TGC.MonoGame.TP.Models;

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
                var objetoChocado = TGCGame.Instance._dinamicsManager._dynamicDecorations
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

                var objetoChocado = TGCGame.Instance._dinamicsManager._dynamicDecorations.OfType<Dinamic>().FirstOrDefault(d => d.bodyHandle == obstacleHandle);

                if (objetoChocado != null && !objetoChocado.IsDead)
                {
                    objetoChocado.HandleCollision();
                    cannonball.killCannonball();
                }

                // Reviso si se impacto a un enemigo
                var enemyChocado = TGCGame.Instance._enemiesManager._enemies.FirstOrDefault(e => e.TankHandler == obstacleHandle);
                if(enemyChocado != null && !enemyChocado.IsDead)
                {
                    //enemyChocado.HandleHealth(GameConfig.Tank.AttackDamage);
                    enemyChocado.HandleHealth(0);
                    cannonball.killCannonball();
                }              
            }

        }

        // COLISIONES: Tanque (Dinamico) vs Objetos Estaticos (casas, arboles, rocas)
        if ((pair.A.Mobility == CollidableMobility.Dynamic && pair.B.Mobility == CollidableMobility.Static) ||
            (pair.B.Mobility == CollidableMobility.Dynamic && pair.A.Mobility == CollidableMobility.Static))
        {
            BodyHandle dynamicHandle = pair.A.Mobility == CollidableMobility.Dynamic ? pair.A.BodyHandle : pair.B.BodyHandle;
            StaticHandle staticHandle = pair.A.Mobility == CollidableMobility.Static ? pair.A.StaticHandle : pair.B.StaticHandle;

            //Ignorar el terreno para que no contamine los flags del tracker
            if (staticHandle == TGCGame.Instance.TerrainHandle)
            {
                // No hacemos nada con el tracker, pero dejamos que el codigo siga por si es una bala
            }
            //Solo evaluar si el dinamico es el tanque del jugador
            else if (dynamicHandle == TGCGame.Instance._tank.TankHandler)
            {
                TGCGame.Instance.CollisionTracker.TryPlay(() =>
                {
                    bool isHouse = TGCGame.Instance._housesManager._houses.Any(h => h.StaticHandle == staticHandle);
                    if (isHouse)
                    {
                        TGCGame.Instance.SoundManager.PlaySound("colision_casa");
                        return true; // Retornar true para consumir el flag
                    }

                    bool isTree = TGCGame.Instance._staticsManager._decorationModels.OfType<Tree>().Any(t => t.StaticHandle == staticHandle);
                    if (isTree)
                    {
                        TGCGame.Instance.SoundManager.PlaySound("golpear_arbol");
                        return true;
                    }

                    bool isRock = TGCGame.Instance._staticsManager._decorationModels.OfType<Rock>().Any(r => r.StaticHandle == staticHandle);
                    if (isRock)
                    {
                        TGCGame.Instance.SoundManager.PlaySound("golpear_roca");
                        return true;
                    }

                    bool isCactus = TGCGame.Instance._staticsManager._decorationModels.OfType<Cactus>().Any(c => c.StaticHandle == staticHandle);
                    if (isCactus)
                    {
                        TGCGame.Instance.SoundManager.PlaySound("golpear_arbol");
                        return true;
                    }

                    return false; // No es un objeto que nos interese, NO consumir el flag
                });
            }
        }

        if (pair.A.Mobility == CollidableMobility.Static || pair.B.Mobility == CollidableMobility.Static)
        {
            // si la bala colisiono con el suelo, debe desaparecer
            // chequeo que uno de los dos sea estatico (terreno) y el otro dinamico (bala)
            var terreno = TGCGame.Instance.TerrainHandle;
            Cannonball cannonball = null;
            if (pair.A.Mobility == CollidableMobility.Static && pair.A.StaticHandle == terreno)
                cannonball = TGCGame.Instance.Cannonballs.FirstOrDefault(c => c.BodyHandle == pair.B.BodyHandle);

            if (pair.B.Mobility == CollidableMobility.Static && pair.B.StaticHandle == terreno)
                cannonball = TGCGame.Instance.Cannonballs.FirstOrDefault(c => c.BodyHandle == pair.A.BodyHandle);

            if (cannonball != null) cannonball.killCannonball();
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