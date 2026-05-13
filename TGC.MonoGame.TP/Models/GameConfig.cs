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

    // TANQUEsITO (valores en unidades SI)
    public static class Tank
    {
        public const float TankScale = 1f;
        public const float TankChamberScale = TankScale/100;
        public const float Length = 2f;         // metros
        public const float Width = 2f;          // metros
        public const float Height = 2.25f;      // metros
        public const float Mass = 5000f;        // kg (tanque real ~60t, ya lo vamos a ir ajustando)
        public const float MaxSpeed = 100f;     // m/s (referencia: 100 m/s = 360 km/h)
        public const float VerticalSpeed = 25f; // m/s (~90 km/h) para God Mode
        public const float Acceleration = 75f;  // m/s²
        public const float TurnSpeed = 1.2f;    // rad/s
        public const float Friction = 0.95f;    // coeficiente por frame
    }

    // TERRENO
    public static class Terrain
    {
        public const float CellSizeMeters = 1f;   // 1 píxel del heightmap = 1 metro
        public const float MaxHeightMeters = 35f; // relieve maximo
        public const float PhysicsMargin = 0.2f;  // margen de seguridad para Bepu
    }

    // CAMARA
    public static class Camera
    {
        public const float DefaultDistance = 18f;
        public const float HeightOffset = 12f;
        public const float MinDistance = 2f;
        public const float MaxDistance = 45f;
        public const float ZoomSensitivity = 2.5f;
        public const float Smoothness = 8f;
        public const float LookAtHeight = 8f;
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
    }
}