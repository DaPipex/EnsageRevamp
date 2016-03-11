using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using Ensage.Common.Objects;
using Ensage.Common.Menu;

using SharpDX;

namespace PippyLastHitter
{
    class Program
    {

        private static Hero LocalHero;

        private static Creep PossibleMinion;

        private static bool _debug = false;
        private static bool _debugMinion = false;

        private static bool Deny = false;

        private static Menu PipLHMenu;

        private static bool OnLoad = false;
        private const FontFlags DropS = FontFlags.DropShadow;

        private static int lastMoveT;

        private static List<uint> minionAttackPointList = new List<uint>();
        private static List<uint> minionAttackBackswingList = new List<uint>();

        private static List<Creep> NearMeleeCreeps = new List<Creep>();
        private static List<Creep> NearRangedCreeps = new List<Creep>();

        //Debug Vars
        private static bool _dPossibleMinion = false;
        private const int _debugFontSize = 20;


        static void Main(string[] args)
        {
            Game.OnUpdate += PippyLHUpdate;
            Game.OnIngameUpdate += PippyLHIngameUpdate;
            Drawing.OnDraw += PippyLHDraw;
            //ObjectManager.OnAddTrackingProjectile += PippyLHAddProj;
            //ObjectManager.OnRemoveTrackingProjectile += PippyLHRemoveProj;
        }

        private static void PippyLHUpdate(EventArgs args)
        {
            if (Game.GameState == GameState.NotInGame)
            {
                if (LocalHero != null)
                {
                    LocalHero = null;
                }
            }
        }

        private static void PippyLHIngameUpdate(EventArgs args)
        {
            if (!OnLoad)
            {
                OnLoad = true;

                PipLHMenu = new Menu("Pippy Last Hitter", "PipLH", true);

                LoadMenu();
            }

            LocalHero = ObjectManager.LocalHero;

            MinionAAInfoMethod();

            _debug = PipLHMenu.Item("useDebug").GetValue<bool>();
            _debugMinion = PipLHMenu.Item("useMinionDebug").GetValue<bool>();

            Deny = PipLHMenu.Item("DNToggle").GetValue<KeyBind>().Active;

            List<Creep> onlyEnemyCreepsList = ObjectManager.GetEntities<Creep>().Where(creep => creep.IsAlive && creep.IsValid && LocalHero.Distance2D(creep) < LocalHero.GetAttackRange() && creep.Team == LocalHero.GetEnemyTeam()).OrderBy(creep => creep.Health).ToList();

            List<Creep> allCreepsList = ObjectManager.GetEntities<Creep>().Where(creep => creep.IsAlive && creep.IsValid && LocalHero.Distance2D(creep) < LocalHero.GetAttackRange()).OrderBy(creep => creep.Health).ToList();

            Creep onlyEnemyCreeps = null;
            Creep allCreeps = null;

            if (onlyEnemyCreepsList.Any())
            {
                onlyEnemyCreeps = onlyEnemyCreepsList.First();
            }

            if (allCreepsList.Any())
            {
                allCreeps = allCreepsList.First();
            }

            PossibleMinion = Deny ? allCreeps : onlyEnemyCreeps;

            if (PipLHMenu.Item("LHHold").GetValue<KeyBind>().Active || PipLHMenu.Item("LHToggle").GetValue<KeyBind>().Active)
            {
                LastHit();
            }
        }

        private static void MinionAAInfoMethod()
        {
            if (PossibleMinion != null)
            {
                NearMeleeCreeps =
                    ObjectManager.GetEntities<Creep>()
                        .Where(
                            creep =>
                                creep.IsAlive && creep.IsMelee && creep.IsValid &&
                                creep.Distance2D(PossibleMinion) <= creep.GetAttackRange() &&
                                creep.Team == PossibleMinion.GetEnemyTeam())
                        .ToList();

                NearRangedCreeps =
                    ObjectManager.GetEntities<Creep>()
                        .Where(
                            creep =>
                                creep.IsAlive && !creep.IsMelee && creep.IsValid &&
                                creep.Distance2D(PossibleMinion) <= (creep.AttackRange + creep.HullRadius/2) &&
                                creep.Team == PossibleMinion.GetEnemyTeam())
                        .ToList();

                if (NearMeleeCreeps.Any())
                {
                    foreach (Creep creep in NearMeleeCreeps)
                    {
                        if (StartedAttack(creep))
                        {
                            minionAttackPointList.Add(creep.Handle);

                            var creepAttackPoint = MinionAAData.GetAttackPoint(creep)*1000;
                            var creepAttackBackswing = MinionAAData.GetAttackBackswing(creep)*1000;

                            DelayAction.Add(creepAttackPoint, () =>
                            {
                                minionAttackPointList.Remove(creep.Handle);
                                minionAttackBackswingList.Add(creep.Handle);
                            });

                            DelayAction.Add(creepAttackPoint + creepAttackBackswing, () =>
                            {
                                minionAttackBackswingList.Remove(creep.Handle);
                            });
                        }
                    }
                }

                if (NearRangedCreeps.Any())
                {
                    foreach (Creep creep in NearRangedCreeps)
                    {
                        if (StartedAttack(creep))
                        {
                            minionAttackPointList.Add(creep.Handle);

                            var creepAttackPoint = MinionAAData.GetAttackPoint(creep)*1000;
                            var creepAttackBackswing = MinionAAData.GetAttackBackswing(creep)*1000;

                            DelayAction.Add(creepAttackPoint, () =>
                            {
                                minionAttackPointList.Remove(creep.Handle);
                                minionAttackBackswingList.Add(creep.Handle);
                            });

                            DelayAction.Add(creepAttackPoint + creepAttackBackswing, () =>
                            {
                                minionAttackBackswingList.Remove(creep.Handle);
                            });
                        }
                    }
                }
            }
        }

        private static void PippyLHDraw(EventArgs args)
        {
            if (PossibleMinion != null)
            {
                Drawing.DrawText("Target Minion", Drawing.WorldToScreen(PossibleMinion.Position), new Vector2(_debugFontSize), Color.White, DropS);

                if (NearMeleeCreeps.Any())
                {
                    foreach (Creep creep in NearMeleeCreeps)
                    {
                        if (_debugMinion)
                        {
                            if (minionAttackPointList.Contains(creep.Handle))
                            {
                                Drawing.DrawText("ATTACK POINT", Drawing.WorldToScreen(creep.Position), new Vector2(_debugFontSize), Color.IndianRed, DropS);
                            }
                            else if (minionAttackBackswingList.Contains(creep.Handle))
                            {
                                Drawing.DrawText("ATTACK BACKSWING", Drawing.WorldToScreen(creep.Position), new Vector2(_debugFontSize), Color.LimeGreen, DropS);
                            }
                        }
                    }
                }

                if (NearRangedCreeps.Any())
                {
                    foreach (Creep creep in NearRangedCreeps)
                    {
                        if (_debugMinion)
                        {
                            if (minionAttackPointList.Contains(creep.Handle))
                            {
                                Drawing.DrawText("ATTACK POINT", Drawing.WorldToScreen(creep.Position), new Vector2(_debugFontSize), Color.IndianRed, DropS);
                            }
                            else if (minionAttackBackswingList.Contains(creep.Handle))
                            {
                                Drawing.DrawText("ATTACK BACKSWING", Drawing.WorldToScreen(creep.Position), new Vector2(_debugFontSize), Color.LimeGreen, DropS);
                            }
                        }
                    }
                }
            }

            if (!PipLHMenu.Item("DisableAll").GetValue<bool>())
            {
                if (PipLHMenu.Item("AttackRange").GetValue<bool>())
                {
                    PippyDrawCircle(LocalHero, (int) LocalHero.GetAttackRange(), 1,
                        new Color(PipLHMenu.Item("RGBr").GetValue<Slider>().Value,
                            PipLHMenu.Item("RGBg").GetValue<Slider>().Value,
                            PipLHMenu.Item("RGBb").GetValue<Slider>().Value));
                }
            }
        }

        private static void PippyDrawCircle(float x, float y, float z, int radius, int width, Color color)
        {
            //var position = Drawing.WorldToScreen(new Vector3(x - radius, y, z + radius));
            var newRadius = radius*.92;

            const double fid = Math.PI*2/40;

            //var pointsList = new List<Vector2>();

            var startPoint =
                Drawing.WorldToScreen(new Vector3((float) (x - newRadius*Math.Cos(0)), y,
                    (float) (z + newRadius*Math.Sin(0))));

            for (var theta = fid; theta < Math.PI*2 + fid/2; theta += fid)
            {
                var endPoint =
                    Drawing.WorldToScreen(new Vector3((float)(x - newRadius*Math.Cos(theta)), y, (float)(z + newRadius*Math.Sin(theta))));

                Drawing.DrawLine(startPoint, endPoint, color);

                startPoint = endPoint;
            }
        }

        private static void PippyDrawCircle2(float x, float y, float z, int radius, int width, Color color, float fidelity)
        {
            var fid = Math.Max(10, Math.Round(180/MathUtil.RadiansToDegrees((float) Math.Asin(fidelity/(2*radius)))));

            fid = 2*Math.PI/fid;

            var newRadius = radius;

            List<Vector2> points = new List<Vector2>();

            for (var theta = 0d; theta < 2*Math.PI + fid; theta += fid)
            {
                var p =
                    Drawing.WorldToScreen(new Vector3((float) (x + newRadius*Math.Cos(theta)), y,
                        (float) (z - newRadius*Math.Sin(theta))));

                var p2 =
                    Drawing.WorldToScreen(new Vector3((float) (x + newRadius*Math.Cos(theta)), (float) (y - newRadius*Math.Sin(theta)), z));

                points.Add(new Vector2(p2.X, p2.Y));
            }

            for (int i = 0; i < points.Count; i++)
            {
                if (i + 1 < points.Count)
                {
                    Drawing.DrawLine(points[i], points[i + 1], color);
                }
            }
        }

        private static void PippyDrawCircle(Unit unit, int radius, int width, Color color)
        {
            PippyDrawCircle2(unit.Position.X, unit.Position.Y, unit.Position.Z, radius, width, color, PipLHMenu.Item("CircleFidelity").GetValue<Slider>().Value);
        }

        private static void LoadMenu()
        {
            var HotkeysMenu = new Menu("Hotkeys", "hotkeys");
            HotkeysMenu.AddItem(new MenuItem("LHHold", "Last Hit Hold Key")).SetValue(new KeyBind(65, KeyBindType.Press));
            HotkeysMenu.AddItem(new MenuItem("LHToggle", "Last Hit Toggle Key")).SetValue(new KeyBind(84, KeyBindType.Toggle));
            HotkeysMenu.AddItem(new MenuItem("DNToggle", "Deny Toggle Key")).SetValue(new KeyBind(75, KeyBindType.Toggle, true));
            PipLHMenu.AddSubMenu(HotkeysMenu);

            var DrawingsMenu = new Menu("Drawings", "drawings");
            DrawingsMenu.AddItem(new MenuItem("DisableAll", "Disable All Drawings")).SetValue(false);
            DrawingsMenu.AddItem(new MenuItem("AttackRange", "Draw Attack Range")).SetValue(true);
            DrawingsMenu.AddItem(new MenuItem("RGBr", "R").SetValue(new Slider(255, 0, 255)));
            DrawingsMenu.AddItem(new MenuItem("RGBg", "G").SetValue(new Slider(255, 0, 255)));
            DrawingsMenu.AddItem(new MenuItem("RGBb", "B").SetValue(new Slider(255, 0, 255)));
            DrawingsMenu.AddItem(new MenuItem("CircleFidelity", "Circle Fidelity")).SetValue(new Slider(200, 50, 400));
            //DrawingsMenu.AddItem(new MenuItem("KillableMinion", "Draw Killable Creep")).SetValue(true);
            PipLHMenu.AddSubMenu(DrawingsMenu);

            PipLHMenu.AddItem(new MenuItem("delay", "Delay Offset")).SetValue(new Slider(0, -200, 200))
                .SetTooltip("[Attack Delay] Negative = Attack Earlier - Positive = Attack Later");
            PipLHMenu.AddItem(new MenuItem("useDebug", "Debug (Dev only)")).SetValue(false).SetTooltip("Lots of info!");
            PipLHMenu.AddItem(new MenuItem("useMinionDebug", "Minion Debug (Dev only)")).SetValue(false).SetTooltip("Displays minion info");

            PipLHMenu.AddToMainMenu();
        }

        private static void LastHit()
        {

            if (PossibleMinion != null)
            {
                var CheckTime = (int)(UnitDatabase.GetAttackPoint(LocalHero) * 1000 + PipLHMenu.Item("delay").GetValue<Slider>().Value + Game.Ping / 2 +
                    LocalHero.GetTurnTime(PossibleMinion) * 1000 +
                    1000 * Math.Max(0, LocalHero.Distance2D(PossibleMinion)) / FixProjSpeed.ProjSpeed(LocalHero));

                var predHealth = PredictedHealth(PossibleMinion, CheckTime);

                if (_debug)
                {
                    if (Utils.SleepCheck("PrintMyInfo"))
                    {
                        Game.PrintMessage("My attack point: " + UnitDatabase.GetAttackPoint(LocalHero) * 1000, MessageType.LogMessage);
                        Game.PrintMessage("My delay + Ping: " + PipLHMenu.Item("delay").GetValue<Slider>().Value + " - " + Game.Ping / 2, MessageType.LogMessage);
                        Game.PrintMessage("My turntime to creep: " + LocalHero.GetTurnTime(PossibleMinion) * 1000, MessageType.LogMessage);
                        Game.PrintMessage("My proj speed: " + FixProjSpeed.ProjSpeed(LocalHero), MessageType.LogMessage);
                        Game.PrintMessage("My proj arrival time: " + 1000 * Math.Max(0, LocalHero.Distance2D(PossibleMinion) / FixProjSpeed.ProjSpeed(LocalHero)), MessageType.LogMessage);
                        Game.PrintMessage("Total time: " + (int)(UnitDatabase.GetAttackPoint(LocalHero) * 1000 + PipLHMenu.Item("delay").GetValue<Slider>().Value + Game.Ping +
                            LocalHero.GetTurnTime(PossibleMinion) * 1000 + 1000 * Math.Max(0, LocalHero.Distance2D(PossibleMinion) / FixProjSpeed.ProjSpeed(LocalHero))), MessageType.LogMessage);

                        Utils.Sleep(1000, "PrintMyInfo");
                    }
                }


                if (predHealth > 0 && predHealth <= GetPhysDamage(PossibleMinion))
                {
                    if (LocalHero.CanAttack())
                    {
                        LocalHero.Attack(PossibleMinion);
                    }
                }
                else
                {
                    if (PipLHMenu.Item("LHHold").GetValue<KeyBind>().Active && lastMoveT + 80 < Environment.TickCount)
                    {
                        lastMoveT = Environment.TickCount;
                        LocalHero.Move(Game.MousePosition);
                    }
                }
            }
            else
            {
                if (PipLHMenu.Item("LHHold").GetValue<KeyBind>().Active && lastMoveT + 80 < Environment.TickCount)
                {
                    lastMoveT = Environment.TickCount;
                    LocalHero.Move(Game.MousePosition);
                }
            }
        }

        private static bool StartedAttack(Unit unit)
        {
            if (unit.IsAttacking() && !minionAttackPointList.Contains(unit.Handle) && !minionAttackBackswingList.Contains(unit.Handle))
            {
                return true;
            }

            return false;
        }

        private static float PredictedHealth(Unit unit, int time)
        {
            var TimeToCheck = Environment.TickCount + time;

            var rangedProjSpeed = 900;

            var allyMeleeCreeps = ObjectManager.GetEntities<Creep>().Where(creep => creep.IsAlive && creep.IsValid && creep.Team == LocalHero.Team && creep.IsMelee).ToList();
            var enemyMeleeCreeps = ObjectManager.GetEntities<Creep>().Where(creep => creep.IsAlive && creep.IsValid && creep.Team == LocalHero.GetEnemyTeam() && creep.IsMelee).ToList();

            var allyRangedCreeps = ObjectManager.GetEntities<Creep>().Where(creep => creep.IsAlive && creep.IsValid && creep.Team == LocalHero.Team && creep.IsRanged).ToList();
            var enemyRangedCreeps = ObjectManager.GetEntities<Creep>().Where(creep => creep.IsAlive && creep.IsValid && creep.Team == LocalHero.GetEnemyTeam() && creep.IsRanged).ToList();

            if (unit.Team == LocalHero.GetEnemyTeam()) //Enemy Creep
            {
                var rangedDamage = 0f;
                var meleeDamage = 0f;

                foreach (var allyCreep in allyRangedCreeps)
                {
                    var projDamage = 0f;

                    Ray FrontPos = new Ray(allyCreep.NetworkPosition, allyCreep.Vector3FromPolarAngle());

                    BoundingSphere unitPos = new BoundingSphere(unit.NetworkPosition, 25);

                    if (FrontPos.Intersects(unitPos) && Math.Max(0, allyCreep.Distance2D(unit)) < (allyCreep.AttackRange + allyCreep.HullRadius / 2) && StartedAttack(allyCreep))
                    {
                        //Game.PrintMessage("INTERSECTION DETECTED", MessageType.LogMessage);

                        var arrivalTime = Environment.TickCount + 1000 * Math.Max(0, allyCreep.Distance2D(unit)) / rangedProjSpeed + 1000 * MinionAAData.GetAttackPoint(allyCreep);

                        if (arrivalTime < TimeToCheck)
                        {
                            projDamage = GetPhysDamage(allyCreep, unit);
                        }
                    }

                    rangedDamage += projDamage;
                }

                foreach (var allyCreep in allyMeleeCreeps)
                {
                    var hitDamage = 0f;

                    Ray FrontPos = new Ray(allyCreep.NetworkPosition, allyCreep.Vector3FromPolarAngle());

                    BoundingSphere unitPos = new BoundingSphere(unit.NetworkPosition, 25);

                    if (FrontPos.Intersects(ref unitPos) && Math.Max(0, allyCreep.Distance2D(unit)) < allyCreep.GetAttackRange() && StartedAttack(allyCreep))
                    {
                        var arrivalTime = Environment.TickCount + MinionAAData.GetAttackPoint(allyCreep) * 1000;

                        if (arrivalTime < TimeToCheck)
                        {
                            hitDamage = GetPhysDamage(allyCreep, unit);
                        }
                    }

                    meleeDamage += hitDamage;
                }

                return Math.Max(0, unit.Health - (rangedDamage + meleeDamage));
            }

            if (unit.Team == LocalHero.Team) //Ally Creep
            {
                var rangedDamage = 0f;
                var meleeDamage = 0f;

                foreach (var enemyCreep in enemyRangedCreeps)
                {
                    var projDamage = 0f;

                    Ray FrontPos = new Ray(enemyCreep.NetworkPosition, enemyCreep.Vector3FromPolarAngle());

                    BoundingSphere unitPos = new BoundingSphere(unit.NetworkPosition, 25);

                    if (FrontPos.Intersects(ref unitPos) && Math.Max(0, enemyCreep.Distance2D(unit)) < (enemyCreep.AttackRange + enemyCreep.HullRadius / 2) && StartedAttack(enemyCreep))
                    {
                        //Game.PrintMessage("INTERSECTION DETECTED", MessageType.LogMessage);

                        var arrivalTime = Environment.TickCount + 1000 * Math.Max(0, enemyCreep.Distance2D(unit)) / rangedProjSpeed + 1000 * MinionAAData.GetAttackPoint(enemyCreep);

                        if (arrivalTime < TimeToCheck)
                        {
                            projDamage = GetPhysDamage(enemyCreep, unit);
                        }
                    }

                    rangedDamage += projDamage;
                }

                foreach (var enemyCreep in enemyMeleeCreeps)
                {
                    var hitDamage = 0f;

                    Ray FrontPos = new Ray(enemyCreep.NetworkPosition, enemyCreep.Vector3FromPolarAngle());

                    BoundingSphere unitPos = new BoundingSphere(unit.NetworkPosition, 25);

                    if (FrontPos.Intersects(ref unitPos) && Math.Max(0, enemyCreep.Distance2D(unit)) < enemyCreep.GetAttackRange() && StartedAttack(enemyCreep))
                    {
                        var arrivalTime = Environment.TickCount + MinionAAData.GetAttackPoint(enemyCreep) * 1000;

                        if (arrivalTime < TimeToCheck)
                        {
                            hitDamage = GetPhysDamage(enemyCreep, unit);
                        }
                    }

                    meleeDamage += hitDamage;
                }

                return Math.Max(0, unit.Health - (rangedDamage + meleeDamage));
            }

            return unit.Health;
        }

        /*
        private static void PippyLHAddProj(TrackingProjectileEventArgs args)
        {
            Unit sender = args.Projectile.Source as Unit;

            if ((sender.ClassID == ClassID.CDOTA_BaseNPC_Creep_Lane || sender.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege)
                && (args.Projectile.Target is Creep))
            {
                if (sender.Team == LocalHero.Team)
                {
                    AllyCreepProjs.Add(args.Projectile);
                }

                if (sender.Team == LocalHero.GetEnemyTeam())
                {
                    EnemyCreepProjs.Add(args.Projectile);
                }
            }
        }

        private static void PippyLHRemoveProj(TrackingProjectileEventArgs args)
        {
            Unit sender = args.Projectile.Source as Unit;

            if ((sender.ClassID == ClassID.CDOTA_BaseNPC_Creep_Lane || sender.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege)
                && (args.Projectile.Target is Creep))
            {
                if (sender.Team == LocalHero.Team)
                {
                    AllyCreepProjs.Remove(args.Projectile);
                }

                if (sender.Team == LocalHero.GetEnemyTeam())
                {
                    EnemyCreepProjs.Remove(args.Projectile);
                }
            }
        }
        */

        private static float GetPhysDamage(Unit source, Unit target)
        {
            var PhysDamage = source.MinimumDamage + source.BonusDamage;

            var _damageMP = 1 - 0.06 * target.Armor / (1 + 0.06 * Math.Abs(target.Armor));

            return (float)(PhysDamage * _damageMP);
        }

        private static float GetPhysDamage(Unit target)
        {
            return GetPhysDamage(LocalHero, target);
        }
    }

    class MinionAAData
    {
        public static float GetAttackSpeed(Creep creep)
        {
            var attackSpeed = Math.Min(creep.AttacksPerSecond * 1 / 0.01, 600);

            return (float)attackSpeed;
        }

        public static float GetAttackPoint(Creep creep)
        {
            var animationPoint = 0f;

            var attackSpeed = GetAttackSpeed(creep);

            animationPoint = creep.IsRanged ? 0.5f : 0.467f;

            return animationPoint / (1 + (attackSpeed - 100) / 100);
        }

        public static float GetAttackRate(Creep creep)
        {
            var attackSpeed = GetAttackSpeed(creep);

            return 1 / (1 + (attackSpeed - 100) / 100);
        }

        public static float GetAttackBackswing(Creep creep)
        {
            var attackRate = GetAttackRate(creep);

            var attackPoint = GetAttackPoint(creep);

            return attackRate - attackPoint;
        }
    }

    class FixProjSpeed
    {
        public static float ProjSpeed(Hero hero)
        {
            var projSpeed = 0f;

            switch (hero.StoredName().ToLowerInvariant())
            {
                case "npc_dota_hero_ancient_apparition":
                    projSpeed = 1250;
                    break;
                case "npc_dota_hero_bane":
                case "npc_dota_hero_batrider":
                    projSpeed = 900;
                    break;
                case "npc_dota_hero_chen":
                    projSpeed = 1100;
                    break;
                case "npc_dota_hero_clinkz":
                case "npc_dota_hero_crystal_maiden":
                    projSpeed = 900;
                    break;
                case "npc_dota_hero_dazzle":
                    projSpeed = 1200;
                    break;
                case "npc_dota_hero_death_prophet":
                    projSpeed = 1000;
                    break;
                case "npc_dota_hero_disruptor":
                    projSpeed = 1200;
                    break;
                case "npc_dota_hero_drow_ranger":
                    projSpeed = 1250;
                    break;
                case "npc_dota_hero_enchantress":
                case "npc_dota_hero_enigma":
                    projSpeed = 900;
                    break;
                case "npc_dota_hero_gyrocopter":
                    projSpeed = 3000;
                    break;
                case "npc_dota_hero_huskar":
                    projSpeed = 1400;
                    break;
                case "npc_dota_hero_invoker":
                    projSpeed = 900;
                    break;
                case "npc_dota_hero_wisp":
                    projSpeed = 1200;
                    break;
                case "npc_dota_hero_jakiro":
                    projSpeed = 1100;
                    break;
                case "npc_dota_hero_keeper_of_the_light":
                    projSpeed = 900;
                    break;
                case "npc_dota_hero_leshrac":
                case "npc_dota_hero_lich":
                    projSpeed = 900;
                    break;
                case "npc_dota_hero_lina":
                case "npc_dota_hero_lion":
                    projSpeed = 1000;
                    break;
                case "npc_dota_hero_lone_druid":
                case "npc_dota_hero_luna":
                    projSpeed = 900;
                    break;
                case "npc_dota_hero_medusa":
                    projSpeed = 1200;
                    break;
                case "npc_dota_hero_mirana":
                    projSpeed = 900;
                    break;
                case "npc_dota_hero_morphling":
                    projSpeed = 1300;
                    break;
                case "npc_dota_hero_furion":
                    projSpeed = 1125;
                    break;
                case "npc_dota_hero_necrolyte":
                    projSpeed = 900;
                    break;
                case "npc_dota_hero_oracle":
                case "npc_dota_hero_obsidian_destroyer":
                    projSpeed = 900;
                    break;
                case "npc_dota_hero_phoenix":
                    projSpeed = 1100;
                    break;
                case "npc_dota_hero_puck":
                case "npc_dota_hero_pugna":
                    projSpeed = 900;
                    break;
                case "npc_dota_hero_queenofpain":
                    projSpeed = 1500;
                    break;
                case "npc_dota_hero_razor":
                    projSpeed = 2000;
                    break;
                case "npc_dota_hero_rubick":
                    projSpeed = 1125;
                    break;
                case "npc_dota_hero_shadow_demon":
                    projSpeed = 900;
                    break;
                case "npc_dota_hero_nevermore":
                    projSpeed = 1200;
                    break;
                case "npc_dota_hero_shadow_shaman":
                    projSpeed = 900;
                    break;
                case "npc_dota_hero_silencer":
                case "npc_dota_hero_skywrath_mage":
                    projSpeed = 1000;
                    break;
                case "npc_dota_hero_sniper":
                    projSpeed = 3000;
                    break;
                case "npc_dota_hero_storm_spirit":
                    projSpeed = 1100;
                    break;
                case "npc_dota_hero_techies":
                case "npc_dota_hero_templar_assassin":
                case "npc_dota_hero_tinker":
                    projSpeed = 900;
                    break;
                case "npc_dota_hero_troll_warlord":
                    projSpeed = 1200;
                    break;
                case "npc_dota_hero_vengefulspirit":
                    projSpeed = 1500;
                    break;
                case "npc_dota_hero_venomancer":
                    projSpeed = 900;
                    break;
                case "npc_dota_hero_viper":
                    projSpeed = 1200;
                    break;
                case "npc_dota_hero_visage":
                    projSpeed = 900;
                    break;
                case "npc_dota_hero_warlock":
                    projSpeed = 1200;
                    break;
                case "npc_dota_hero_weaver":
                    projSpeed = 900;
                    break;
                case "npc_dota_hero_windrunner":
                    projSpeed = 1200;
                    break;
                case "npc_dota_hero_winter_wyvern":
                    projSpeed = 700;
                    break;
                case "npc_dota_hero_witch_doctor":
                    projSpeed = 1200;
                    break;
                case "npc_dota_hero_zuus":
                    projSpeed = 1100;
                    break;
                default:
                    projSpeed = float.MaxValue;
                    break;
            }

            return projSpeed;
        }
    }
}
