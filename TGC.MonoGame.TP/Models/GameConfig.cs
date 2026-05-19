using Microsoft.Xna.Framework;

namespace TGC.MonoGame.TP;

/// <summary>
///     Configuracion centralizada de escalas, dimensiones y parametros físicos.
///     1 unidad del mundo = 1 metro
/// </summary>
public static class GameConfig
{
    // ESCALA GLOBAL
    public const float WorldUnitsPerMeter = 1f;
    public const float MetersPerWorldUnit = 1f / WorldUnitsPerMeter;

    // CONVERSION (util si en el futuro cambiamos la escala base)
    public static float ToMeters(float worldUnits) => worldUnits * MetersPerWorldUnit;
    public static float ToWorldUnits(float meters) => meters * WorldUnitsPerMeter;
    public static Vector3 ToMeters(Vector3 worldPos) => new Vector3(
        ToMeters(worldPos.X), ToMeters(worldPos.Y), ToMeters(worldPos.Z));
    public static Vector3 ToWorldUnits(Vector3 metersPos) => new Vector3(
        ToWorldUnits(metersPos.X), ToWorldUnits(metersPos.Y), ToWorldUnits(metersPos.Z));

    // TANQUEsITO (valores en unidades SI (o algo asi))
    public static class Tank
    {
        public const float TankScale = 1f;      // metros
        public const float Length = 2f;         // metros
        public const float Width = 2f;          // metros
        public const float Height = 2.25f;      // metros
        public const float ChassisMass = 2000f; // kg (tanque real ~60t, ya lo vamos a ir ajustando)
        public const float TurretMass = 500f;   // kg

        public const float MaxSpeed = 90f;      // m/s (referencia: 100 m/s = 360 km/h)
        public const float VerticalSpeed = 25f; // m/s (~90 km/h) para God Mode
        public const float Acceleration = 40f;  // m/s²
        public const float TurnSpeed = 1.2f;    // rad/s
        public const float Friction = 0.95f;    // coeficiente por frame
        public const float SpawnZMargin = 7f;   // metros, el tanque spawnea esta altura por encima del terreno

        public const float MaxFuel = 100f;              // litros
        public const float FuelConsumptionRate = 1f;    // litros

        //Bepu
        public const float PhysicsChassisWidth  = 2f;       // metros
        public const float PhysicsChassisLength = 2f;       // metros
        public const float PhysicsChassisHeight = 1.2f;     // metros
        public const float PhysicsTurretWidth   = 1.4f;     // metros
        public const float PhysicsTurretLength  = 2f;       // metros
        public const float PhysicsTurretHeight  = 1f;       // metros
        public const float PhysicsTurretOffsetY = PhysicsChassisHeight;     // justo encima del chasis
        public const float MotorForce = 150000f;            // Newton
        public const float EnemyMotorForce = 110000f;       // Newton
        public const float ForwardDrag = 5000f;              // Coeficiente de arrastre (para velocidad terminal = MotorForce / Drag)
        public const float LateralDrag = 250000f;            // evitar derrape

        public const float HealthPoints = 20f;
        public const float AttackDamage = 1f;               // por ahora, jugador y enemigo tienen el mismo ataque
        public const float EnemyHealthPoints = 10f;
        public const float EnemyAttackRadius = 50f;
        public const float Cooldown = 0.5f;
        public const float EnemyCooldown = 1f;
        
    }

    // TERRENO
    public static class Terrain
    {
        public const float CellSizeMeters = 1f;   // 1 píxel del heightmap = 1 metro
        public const float MaxHeightMeters = 35f; // relieve maximo
        public const float PhysicsMargin = 0.2f;  // margen de seguridad para Bepu
        public const int PhysicsSubsampleStep = 4;// cuanto dividir la resolucion del heightmap para el mesh de Bepu , (1, 2, 4, 8, ...)
    }

    // CAMARA
    public static class Camera
    {
        public const float DefaultDistance = 10f;
        public const float HeightOffset = 6f;
        public const float MinDistance = 4f;
        public const float MaxDistance = 25f;
        public const float ZoomSensitivity = 1.5f;
        public const float Smoothness = 10f;
        public const float LookAtHeight = 2.5f;
        public const float NearPlaneDist = 0.5f;
        public const float FarPlaneDist = 250;
    }

    // ASSETS (CASAS, DECORACIONES)
    public static class Assets
    {
        public const float DefaultScale = 1f;       // exportar 1:1 desde Blender
        public const float MinSpacingMeters = 10f;
        public const float HouseScale = 2f;
        public const float HouseChamberScale = 0.1f;
        public const float DecorationScale = 1.5f;
        public const float DecorationChamberScale = DecorationScale/100f;
        public const float DynamicSpawnOffset = 0.5f;
    }

    // POWERUPS - BARRIL DE COMBUSTIBLE
    public static class FuelBarrel
    {
        public const int SpawnCount = 30;               // unidades
        public const float FuelAmount = 25f;            // litros
        public const float RechargeDuration = 1f;       // segundos
        public const float CollectionDistance = 2.5f;   // metros
        public const float Radius = 0.5f;               // metros
        public const float Height = 2f;                 // metros
    }
}