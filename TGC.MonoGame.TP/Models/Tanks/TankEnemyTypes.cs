using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TGC.MonoGame.TP.Models.Tanks;

public class TankEnemyScout : TankEnemy
{
    public TankEnemyScout() : base(
        hp: GameConfig.TankClasses.Scout.EnemyHealth,
        speed: GameConfig.TankClasses.Scout.MaxSpeed,
        force: GameConfig.TankClasses.Scout.MotorForce,
        turnSpeed: GameConfig.TankClasses.Scout.TurnSpeed,
        damage: GameConfig.TankClasses.Scout.AttackDamage,
        cooldown: GameConfig.Enemies.Cooldown)
    { }
}

public class TankEnemyMedium : TankEnemy
{
    public TankEnemyMedium() : base(
        hp: GameConfig.TankClasses.Medium.EnemyHealth,
        speed: GameConfig.TankClasses.Medium.MaxSpeed,
        force: GameConfig.TankClasses.Medium.MotorForce,
        turnSpeed: GameConfig.TankClasses.Medium.TurnSpeed,
        damage: GameConfig.TankClasses.Medium.AttackDamage,
        cooldown: GameConfig.Enemies.Cooldown)
    { }
}

public class TankEnemyHeavy : TankEnemy
{
    public TankEnemyHeavy() : base(
        hp: GameConfig.TankClasses.Heavy.EnemyHealth,
        speed: GameConfig.TankClasses.Heavy.MaxSpeed,
        force: GameConfig.TankClasses.Heavy.MotorForce,
        turnSpeed: GameConfig.TankClasses.Heavy.TurnSpeed,
        damage: GameConfig.TankClasses.Heavy.AttackDamage,
        cooldown: GameConfig.Enemies.Cooldown)
    { }
}