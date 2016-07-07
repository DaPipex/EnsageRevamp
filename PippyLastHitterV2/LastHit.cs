using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using Ensage.Common.Menu;

using SharpDX;

namespace PippyLastHitterV2
{ 
    internal class LastHit
    {
        private static Hero myHero;

        private static Player myPlayer;

        private static Creep targetLastHit, targetDeny;

        private static Creep debugMinion;

        private static Menu lhMenu;

        private static int lastMoveT;

        private static int lastResetT;

        public static void Initialize()
        {
            Events.OnLoad += LastHitLoad;
        }

        private static void LastHitLoad(object sender, EventArgs args)
        {
            myHero = ObjectManager.LocalHero;
            myPlayer = ObjectManager.LocalPlayer;

            InitMenu();

            Events.OnUpdate += LastHitUpdate;
            Drawing.OnDraw += LastHitDraw;
        }

        private static void LastHitUpdate(EventArgs args)
        {
            if (lhMenu.Item("lastHitKey").GetValue<KeyBind>().Active)
            {
                LastHitting();
            }
            else if (lhMenu.Item("denyKey").GetValue<KeyBind>().Active)
            {
                Deny();
            }
        }

        private static void LastHitting()
        {
            var enemyCreeps =
                ObjectManager.GetEntities<Creep>()
                    .Where(
                        x =>
                        x.Team != myHero.Team && x.IsValid && x.IsAlive && x.IsVisible
                        && x.Distance2D(myHero) <= myHero.GetAttackRange())
                    .ToList();

            targetLastHit = enemyCreeps.Any() ? enemyCreeps.OrderBy(x => x.Health).FirstOrDefault() : null;

            if (targetLastHit != null
                && targetLastHit.Health * 100 / targetLastHit.MaximumHealth
                <= lhMenu.Item("minHealth").GetValue<Slider>().Value)
            {
                if (targetLastHit.Health > GetPhysDamage(targetLastHit))
                {
                    if (Environment.TickCount > lastResetT + lhMenu.Item("delay").GetValue<Slider>().Value)
                    {
                        myHero.Attack(targetLastHit);
                        myHero.Move(myHero.NetworkPosition);
                        lastResetT = Environment.TickCount;
                    }
                }
                else
                {
                    if (!myHero.IsAttacking() && myHero.CanAttack())
                    {
                        myHero.Attack(targetLastHit);
                    }
                }
            }
        }

        private static void Deny()
        {
            var allyCreeps =
                ObjectManager.GetEntities<Creep>()
                    .Where(
                        x =>
                        x.Team == myHero.Team && x.IsValid && x.IsAlive && x.IsVisible
                        && x.Distance2D(myHero) <= myHero.GetAttackRange())
                    .ToList();

            targetDeny = allyCreeps.Any() ? allyCreeps.OrderBy(x => x.Health).FirstOrDefault() : null;

            if (targetDeny != null
                && targetDeny.Health * 100 / targetDeny.MaximumHealth
                <= lhMenu.Item("minHealth").GetValue<Slider>().Value)
            {
                if (targetDeny.Health > GetPhysDamage(targetDeny))
                {
                    if (Environment.TickCount > lastResetT + lhMenu.Item("delay").GetValue<Slider>().Value)
                    {
                        myHero.Attack(targetDeny);
                        myHero.Move(myHero.NetworkPosition);
                        lastResetT = Environment.TickCount;
                    }
                }
                else
                {
                    if (!myHero.IsAttacking() && myHero.CanAttack())
                    {
                        myHero.Attack(targetDeny);
                    }
                }
            }
        }

        private static void LastHitDraw(EventArgs args)
        {
            var selectedColor = StringToColor(lhMenu.Item("color").GetValue<StringList>().SelectedIndex);

            if (lhMenu.Item("myRange").GetValue<bool>())
            {
                PippyDrawCircle(myHero, (int)myHero.GetAttackRange(), 0, selectedColor);
            }

            if (lhMenu.Item("enemyRange").GetValue<bool>())
            {
                var enemyHeroes =
                    ObjectManager.GetEntities<Hero>()
                        .Where(x => x.Team != myHero.Team && x.IsVisible && x.IsAlive && x.Distance2D(myHero) <= 2250);

                foreach (var enemy in enemyHeroes)
                {
                    PippyDrawCircle(enemy, (int)enemy.GetAttackRange(), 0, selectedColor);
                }
            }

            if (targetLastHit != null)
            {
                PippyDrawCircle(targetLastHit, 175, 0, Color.Red);
            }

            if (targetDeny != null)
            {
                PippyDrawCircle(targetDeny, 175, 0, selectedColor);
            }
        }

        private static void PippyDrawCircleBehind(float x, float y, float z, int radius, int width, Color color, float fidelity)
        {
            var fid = Math.Max(10, Math.Round(180 / MathUtil.RadiansToDegrees((float)Math.Asin(fidelity / (2 * radius)))));

            fid = 2 * Math.PI / fid;

            var newRadius = radius;

            List<Vector2> points = new List<Vector2>();

            for (var theta = 0d; theta < 2 * Math.PI + fid; theta += fid)
            {
                //var p =
                //    Drawing.WorldToScreen(new Vector3((float) (x + newRadius*Math.Cos(theta)), y,
                //        (float) (z - newRadius*Math.Sin(theta))));

                var p2 =
                    Drawing.WorldToScreen(new Vector3((float)(x + newRadius * Math.Cos(theta)), (float)(y - newRadius * Math.Sin(theta)), z));

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
            PippyDrawCircleBehind(unit.Position.X, unit.Position.Y, unit.Position.Z, radius, width, color, lhMenu.Item("CircleFidelity").GetValue<Slider>().Value);
        }

        private static Color StringToColor(int index)
        {
            switch (index)
            {
                case 0:
                    return Color.Red;
                case 1:
                    return Color.Yellow;
                case 2:
                    return Color.Lime;
                case 3:
                    return Color.Blue;
                case 4:
                    return Color.DarkMagenta;
                default:
                    return Color.White;
            }
        }

        private static float GetPhysDamage(Unit source, Unit target)
        {
            var physDamage = source.MinimumDamage + source.BonusDamage;

            var damageMulti = 1 - 0.06 * target.Armor / (1 + 0.06 * Math.Abs(target.Armor));

            return (float)(physDamage * damageMulti);
        }

        private static float GetPhysDamage(Unit target)
        {
            return GetPhysDamage(myHero, target);
        }

        private static void InitMenu()
        {
            lhMenu = new Menu("Last Hitter V2", "lasthitpippy", true);

            var mainMenu = new Menu("Main Settings", "main");
            mainMenu.AddItem(new MenuItem("info1", "Modes:"));
            mainMenu.AddItem(new MenuItem("lastHitKey", "Last Hit Key"))
                .SetValue(new KeyBind("X".ToCharArray()[0], KeyBindType.Press));
            mainMenu.AddItem(new MenuItem("denyKey", "Deny Key"))
                .SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press));
            mainMenu.AddItem(new MenuItem("minHealth", "Min creep health % to start"))
                .SetValue(new Slider(50, 1))
                .SetTooltip("Creep must have below this % health to start the attack animation resets");
            mainMenu.AddItem(new MenuItem("delay", "Delay between resets (ms)")).SetValue(new Slider(50, 40, 150));
            lhMenu.AddSubMenu(mainMenu);

            var drawMenu = new Menu("Drawings", "drawings");
            drawMenu.AddItem(new MenuItem("color", "Color for Drawings"))
                .SetValue(new StringList(new[] { "Red", "Yellow", "Green", "Blue", "Purple" }));
            drawMenu.AddItem(new MenuItem("CircleFidelity", "Circle Quality")).SetValue(new Slider(100, 50, 150));
            drawMenu.AddItem(new MenuItem("myRange", "Draw my true range")).SetValue(true);
            drawMenu.AddItem(new MenuItem("enemyRange", "Draw enemy true range")).SetValue(false);
            lhMenu.AddSubMenu(drawMenu);

            lhMenu.AddToMainMenu();
        }
    }
}
