using BepuPhysics;
using BepuPhysics.Collidables;
using BEVector3 = System.Numerics.Vector3;
using BEQuaternion = System.Numerics.Quaternion;

namespace TGC.MonoGame.TP.Models.Tanks
{
    public static class TankPhysicsHelper
    {
        public static void CreateCompoundBody(Simulation sim, BEVector3 pos, out BodyHandle handle)
        {
            using var builder = new CompoundBuilder(sim.BufferPool, sim.Shapes, 3);
            var chassis = new Box(GameConfig.Tank.PhysicsChassisWidth, GameConfig.Tank.PhysicsChassisHeight, GameConfig.Tank.PhysicsChassisLength);
            var turret = new Box(GameConfig.Tank.PhysicsTurretWidth, GameConfig.Tank.PhysicsTurretHeight, GameConfig.Tank.PhysicsTurretLength);
            var stabilizer = new Box(GameConfig.Tank.Stabilizer.Width, GameConfig.Tank.Stabilizer.Height, GameConfig.Tank.Stabilizer.Length);

            var stabilizerPose = new RigidPose(new BEVector3(0, GameConfig.Tank.Stabilizer.YOffset, 0), BEQuaternion.Identity);
            var chassisPose = new RigidPose(new BEVector3(0, -0.4f, 0), BEQuaternion.Identity);
            var turretPose = new RigidPose(new BEVector3(0, GameConfig.Tank.PhysicsTurretOffsetY, 0), BEQuaternion.Identity);

            builder.Add(stabilizer, stabilizerPose, GameConfig.Tank.Stabilizer.Mass);
            builder.Add(chassis, chassisPose, GameConfig.Tank.ChassisMass);
            builder.Add(turret, turretPose, GameConfig.Tank.TurretMass);

            builder.BuildDynamicCompound(out var children, out var inertia, out var center);
            var shapeIdx = sim.Shapes.Add(new Compound(children));
            handle = sim.Bodies.Add(BodyDescription.CreateDynamic(
                new RigidPose(pos + center, BEQuaternion.Identity),
                inertia, new CollidableDescription(shapeIdx, 0.1f), new BodyActivityDescription(0.01f)));
        }
    }
}