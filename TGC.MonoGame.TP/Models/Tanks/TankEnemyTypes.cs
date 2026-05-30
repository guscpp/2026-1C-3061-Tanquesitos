using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TGC.MonoGame.TP.Models;

public class TankEnemyScout : TankEnemy
{
    public TankEnemyScout() : base(
        GameConfig.Enemies.Scout.HealthPoints, GameConfig.Enemies.Scout.MaxSpeed,
        GameConfig.Enemies.Scout.MotorForce, GameConfig.Enemies.Scout.AttackDamage, GameConfig.Enemies.Cooldown)
    { }
}

public class TankEnemyMedium : TankEnemy
{
    public TankEnemyMedium() : base(
        GameConfig.Enemies.Standard.HealthPoints, GameConfig.Enemies.Standard.MaxSpeed,
        GameConfig.Enemies.Standard.MotorForce, GameConfig.Enemies.Standard.AttackDamage, GameConfig.Enemies.Cooldown)
    { }
}

public class TankEnemyHeavy : TankEnemy
{
    public TankEnemyHeavy() : base(
        GameConfig.Enemies.Heavy.HealthPoints, GameConfig.Enemies.Heavy.MaxSpeed,
        GameConfig.Enemies.Heavy.MotorForce, GameConfig.Enemies.Heavy.AttackDamage, GameConfig.Enemies.Cooldown)
    { }
}