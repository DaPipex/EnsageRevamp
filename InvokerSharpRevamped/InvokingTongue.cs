using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using Ensage.Common.Menu;
using Ensage.Common.Objects;

using SharpDX;
using Ensage.Common.Extensions.SharpDX;

namespace InvokerSharpRevamped
{
    internal class InvokingTongue
    {
        public enum InvokerSpells
        {
            Unknown,
            ColdSnap,
            GhostWalk,
            IceWall,
            EMP,
            Tornado,
            Alacrity,
            SunStrike,
            ForgeSpirit,
            ChaosMeteor,
            DeafeningBlast
        }

        private static readonly string[] spellStringList =
            {
                "No Spell", "Cold Snap", "Ghost Walk", "Ice Wall", "EMP", "Tornado",
                "Alacrity", "Sun Strike", "Forge Spirit", "Chaos Meteor",
                "Deafening Blast"
            };

        private static readonly string tornadoModName = "modifier_invoker_tornado";

        private static Hero invokerHero;

        private static Hero invoTarget;

        private static Menu invoMenu;

        private static Ability quas, wex, exort, spellD, spellF, invokeAbility;

        private static int lastInvokeT;

        private static int lastMoveT;

        private static int fleeCastT;

        private static int comboTornadoT;

        private static int willGoDownT;

        private static int chainTornadoT;

        private static int customComboT;

        private static int dynComboT;

        private static int lastRefresherT;

        private static int eulCastT;

        private static int forgedAttackT;

        private static bool justTornadoHit;

        private static bool warningMessage;

        private static bool hasAghanim;

        private static readonly int[] TornadoDurations = { 800, 1100, 1400, 1700, 2000, 2300, 2600, 2900 };

        public static void Init()
        {
            Events.OnLoad += InvokerLoad;
        }

        private static void InvokerLoad(object sender, EventArgs args)
        {
            if (ObjectManager.LocalHero.ClassID != ClassID.CDOTA_Unit_Hero_Invoker)
            {
                return;
            }

            invokerHero = ObjectManager.LocalHero;

            Orbwalking.Load();
            LoadMenu();

            //Events
            Game.OnIngameUpdate += InvokerIngameUpdate;
            Drawing.OnDraw += InvokerDrawing;
        }

        private static void LoadMenu()
        {
            invoMenu = new Menu("Invoker# - Revamped", "invokersharp", true);

            var keysMenu = new Menu("Keys", "keysmenu");
            keysMenu.AddItem(new MenuItem("doDynCombo", "Cast Dynamic Combo"))
                .SetValue(new KeyBind(32, KeyBindType.Press));
            keysMenu.AddItem(new MenuItem("doPrepare", "Prepare Tornado Chain"))
                .SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press));
            keysMenu.AddItem(new MenuItem("fleeKey", "Flee"))
                .SetValue(new KeyBind("A".ToCharArray()[0], KeyBindType.Press));
            invoMenu.AddSubMenu(keysMenu);

            var prepareCombo = new Menu("Combo Settings", "ccombo");
            /*prepareCombo.AddItem(new MenuItem("ccombo1", "Spell 1 to prepare: ")).SetValue(new StringList(spellStringList));
            prepareCombo.AddItem(new MenuItem("ccombo2", "Spell 2 to prepare: ")).SetValue(new StringList(spellStringList));
            customCombo.AddItem(new MenuItem("ccombo3", "Spell 3: ")).SetValue(new StringList(spellStringList));
            customCombo.AddItem(new MenuItem("ccombo4", "Spell 4: ")).SetValue(new StringList(spellStringList));
            customCombo.AddItem(new MenuItem("ccombo5", "Spell 5: ")).SetValue(new StringList(spellStringList));
            customCombo.AddItem(new MenuItem("ccombo6", "Spell 6: ")).SetValue(new StringList(spellStringList));
            customCombo.AddItem(new MenuItem("ccombo7", "Spell 7: ")).SetValue(new StringList(spellStringList));
            customCombo.AddItem(new MenuItem("ccombo8", "Spell 8: ")).SetValue(new StringList(spellStringList));
            customCombo.AddItem(new MenuItem("ccombo9", "Spell 9: ")).SetValue(new StringList(spellStringList));
            customCombo.AddItem(new MenuItem("ccombo10", "Spell 10: ")).SetValue(new StringList(spellStringList));*/
            prepareCombo.AddItem(new MenuItem("comboRange", "Range to look for target"))
                .SetValue(new Slider(1000, 0, 1500));
            invoMenu.AddSubMenu(prepareCombo);

            var drawMenu = new Menu("Drawings", "draw");
            drawMenu.AddItem(new MenuItem("allowDraw", "Show Drawings?")).SetValue(true);
            drawMenu.AddItem(new MenuItem("drawComboRange", "Draw Combo Range?")).SetValue(false);
            drawMenu.AddItem(new MenuItem("CircleFidelity", "Circle Quality")).SetValue(new Slider(200, 50, 400));
            invoMenu.AddSubMenu(drawMenu);

            var debugMenu = new Menu("Debug", "debug");
            debugMenu.AddItem(new MenuItem("debug1", "Debug 1")).SetValue(false);
            debugMenu.AddItem(new MenuItem("debug2", "Debug 2")).SetValue(false);
            invoMenu.AddSubMenu(debugMenu);

            /*var customCombo2 = new Menu("Custom Combo 2", "ccombo2");
            customCombo2.AddItem(new MenuItem("ccombo1", "Spell 1: ")).SetValue(new StringList(spellStringList));
            customCombo2.AddItem(new MenuItem("ccombo2", "Spell 2: ")).SetValue(new StringList(spellStringList));
            customCombo2.AddItem(new MenuItem("ccombo3", "Spell 3: ")).SetValue(new StringList(spellStringList));
            customCombo2.AddItem(new MenuItem("ccombo4", "Spell 4: ")).SetValue(new StringList(spellStringList));
            customCombo2.AddItem(new MenuItem("ccombo5", "Spell 5: ")).SetValue(new StringList(spellStringList));
            customCombo2.AddItem(new MenuItem("ccombo6", "Spell 6: ")).SetValue(new StringList(spellStringList));
            customCombo2.AddItem(new MenuItem("ccombo7", "Spell 7: ")).SetValue(new StringList(spellStringList));
            customCombo2.AddItem(new MenuItem("ccombo8", "Spell 8: ")).SetValue(new StringList(spellStringList));
            customCombo2.AddItem(new MenuItem("ccombo9", "Spell 9: ")).SetValue(new StringList(spellStringList));
            customCombo2.AddItem(new MenuItem("ccombo10", "Spell 10: ")).SetValue(new StringList(spellStringList));
            invoMenu.AddSubMenu(customCombo2);*/

            var miscMenu = new Menu("Misc", "misc");
            miscMenu.AddItem(new MenuItem("doOrb", "Orbwalk during combo and prepare?")).SetValue(true);
            invoMenu.AddSubMenu(miscMenu);

            invoMenu.AddToMainMenu();
        }

        private static void InvokerIngameUpdate(EventArgs args)
        {
            quas = invokerHero.Spellbook.SpellQ;
            wex = invokerHero.Spellbook.SpellW;
            exort = invokerHero.Spellbook.SpellE;

            spellD = invokerHero.Spellbook.SpellD;
            spellF = invokerHero.Spellbook.SpellF;

            invokeAbility = invokerHero.Spellbook.SpellR;

            if (quas.Level == 0 || wex.Level == 0 || exort.Level == 0)
            {
                if (!warningMessage)
                {
                    Game.PrintMessage(
                        "Please put at least 1 point in each: Quas, Wex and Exort for the assembly to work",
                        MessageType.ChatMessage);
                    warningMessage = true;
                }
                return;
            }

            invoTarget = GetCustomTarget(invoMenu.Item("comboRange").GetValue<Slider>().Value);

            hasAghanim = invokerHero.HasItem(ClassID.CDOTA_Item_UltimateScepter);

            if (invoMenu.Item("doPrepare").GetValue<KeyBind>().Active)
            {
                PrepareSpells();
                SpiritsAttack();

                if (invoMenu.Item("doOrb").GetValue<bool>())
                {
                    InvoOrbwalk();
                }
            }
            /*else if (invoMenu.Item("doCombo").GetValue<KeyBind>().Active)
            {
                Combo();
                SpiritsAttack();

                if (invoMenu.Item("doOrb").GetValue<bool>())
                {
                    InvoOrbwalk();
                }
            }*/
            else if (invoMenu.Item("doDynCombo").GetValue<KeyBind>().Active)
            {
                DynamicCombo();
                SpiritsAttack();

                if (invoMenu.Item("doOrb").GetValue<bool>())
                {
                    InvoOrbwalk();
                }
            }

            if (invoMenu.Item("fleeKey").GetValue<KeyBind>().Active)
            {
                Flee();
            }

            if (invoMenu.Item("debug1").GetValue<bool>())
            {
                //item_cyclone - 100
                foreach (
                    var pet in
                        ObjectManager.GetEntities<Unit>()
                            .Where(pet => pet.IsControllableByPlayer(ObjectManager.LocalPlayer)))
                {
                    Game.PrintMessage(pet.Name + " - " + pet.ClassID, MessageType.ChatMessage);
                }
            }

            if (invoMenu.Item("debug2").GetValue<bool>())
            {
                if (invoTarget == null)
                {
                    return;
                }

                foreach (var modifier in invoTarget.Modifiers)
                {
                    Game.PrintMessage(modifier.Name, MessageType.ChatMessage);
                }
            }

            if (invoTarget != null)
            {
                if (!invoTarget.HasModifier(tornadoModName))
                {
                    willGoDownT = 0;
                    justTornadoHit = false;
                }
            }

            if (invoTarget == null)
            {
                willGoDownT = 0;
                justTornadoHit = false;
            }
        }

        private static void SpiritsAttack()
        {
            if (invoTarget == null || !HasSpirits() || invoTarget.HasModifier(tornadoModName)
                || invoTarget.HasModifier("modifier_eul_cyclone"))
            {
                return;
            }

            var spirits = GetMySpirits();

            if (Environment.TickCount > forgedAttackT + 1500)
            {
                foreach (var spirit in spirits)
                {
                    spirit.Attack(invoTarget);
                }

                forgedAttackT = Environment.TickCount;
            }
        }

        private static void DynamicCombo()
        {
            if (invoTarget == null)
            {
                return;
            }

            var coldSnapAbility = invokerHero.FindSpell(EnumToString(InvokerSpells.ColdSnap));
            var ghostWalkAbility = invokerHero.FindSpell(EnumToString(InvokerSpells.GhostWalk));
            var iceWallAbility = invokerHero.FindSpell(EnumToString(InvokerSpells.IceWall));
            var empAbility = invokerHero.FindSpell(EnumToString(InvokerSpells.EMP));
            var tornadoAbility = invokerHero.FindSpell(EnumToString(InvokerSpells.Tornado));
            var alacrityAbility = invokerHero.FindSpell(EnumToString(InvokerSpells.Alacrity));
            var sunStrikeAbility = invokerHero.FindSpell(EnumToString(InvokerSpells.SunStrike));
            var forgeSpiritAbility = invokerHero.FindSpell(EnumToString(InvokerSpells.ForgeSpirit));
            var chaosMeteorAbility = invokerHero.FindSpell(EnumToString(InvokerSpells.ChaosMeteor));
            var deafeningBlastAbility = invokerHero.FindSpell(EnumToString(InvokerSpells.DeafeningBlast));

            if (invokerHero.HasItem(ClassID.CDOTA_Item_UltimateScepter))
            {
                if (HasInvokerSpell(InvokerSpells.Tornado) && tornadoAbility.CanBeCasted()
                    && HasInvokerSpell(InvokerSpells.EMP) && empAbility.CanBeCasted())
                {
                    if (Environment.TickCount > comboTornadoT + 500)
                    {
                        var castedTornado = tornadoAbility.CastSkillShot(invoTarget, invokerHero.NetworkPosition);

                        if (castedTornado)
                        {
                            comboTornadoT = Environment.TickCount;
                        }
                    }
                }
                else if (Environment.TickCount < comboTornadoT + 6500 && empAbility.CanBeCasted())
                {
                    if (invoTarget.HasModifier(tornadoModName) && !justTornadoHit)
                    {
                        justTornadoHit = true;
                        willGoDownT = Environment.TickCount + TornadoUpTime(quas.Level);
                    }

                    if (willGoDownT != 0)
                    {
                        ChainTornado(InvokerSpells.EMP, willGoDownT);
                    }
                }
                else
                {
                    if (!HasInvokerSpell(InvokerSpells.EMP) && empAbility.AbilityState == AbilityState.Ready
                        && invokeAbility.CanBeCasted())
                    {
                        PrepareSpell(InvokerSpells.EMP);
                    }
                    else if (HasInvokerSpell(InvokerSpells.EMP) && empAbility.CanBeCasted()
                             && Environment.TickCount > dynComboT + 450)
                    {
                        empAbility.CastSkillShot(invoTarget, invokerHero.NetworkPosition);
                        dynComboT = Environment.TickCount;
                    }
                    else if (!HasInvokerSpell(InvokerSpells.Tornado) && tornadoAbility.AbilityState == AbilityState.Ready
                        && invokeAbility.CanBeCasted())
                    {
                        PrepareSpell(InvokerSpells.Tornado);
                    }
                    else if (HasInvokerSpell(InvokerSpells.Tornado) && tornadoAbility.CanBeCasted()
                             && Environment.TickCount > dynComboT + 450)
                    {
                        tornadoAbility.CastSkillShot(invoTarget, invokerHero.NetworkPosition);
                        dynComboT = Environment.TickCount;
                    }
                    else if (!HasInvokerSpell(InvokerSpells.ChaosMeteor)
                             && chaosMeteorAbility.AbilityState == AbilityState.Ready && invokeAbility.CanBeCasted())
                    {
                        PrepareSpell(InvokerSpells.ChaosMeteor);
                    }
                    else if (HasInvokerSpell(InvokerSpells.ChaosMeteor) && chaosMeteorAbility.CanBeCasted()
                             && Environment.TickCount > dynComboT + 450)
                    {
                        chaosMeteorAbility.CastSkillShot(invoTarget, invokerHero.NetworkPosition);
                        dynComboT = Environment.TickCount;
                    }
                    else if (!HasInvokerSpell(InvokerSpells.DeafeningBlast)
                             && deafeningBlastAbility.AbilityState == AbilityState.Ready && invokeAbility.CanBeCasted())
                    {
                        PrepareSpell(InvokerSpells.DeafeningBlast);
                    }
                    else if (HasInvokerSpell(InvokerSpells.DeafeningBlast)
                             && deafeningBlastAbility.CanBeCasted()
                             && Environment.TickCount > dynComboT + 450)
                    {
                        deafeningBlastAbility.CastSkillShot(invoTarget, invokerHero.NetworkPosition);
                        dynComboT = Environment.TickCount;
                    }
                    else if (invoTarget.Distance2D(invokerHero) > 350)
                    {
                        if (!HasInvokerSpell(InvokerSpells.ColdSnap)
                            && coldSnapAbility.AbilityState == AbilityState.Ready && invokeAbility.CanBeCasted())
                        {
                            PrepareSpell(InvokerSpells.ColdSnap);
                        }
                        else if (HasInvokerSpell(InvokerSpells.ColdSnap)
                                 && coldSnapAbility.CanBeCasted()
                                 && Environment.TickCount > dynComboT + 450)
                        {
                            coldSnapAbility.UseAbility(invoTarget);
                            dynComboT = Environment.TickCount;
                        }
                        else if (!HasInvokerSpell(InvokerSpells.ForgeSpirit)
                                 && forgeSpiritAbility.AbilityState == AbilityState.Ready && invokeAbility.CanBeCasted())
                        {
                            PrepareSpell(InvokerSpells.ForgeSpirit);
                        }
                        else if (HasInvokerSpell(InvokerSpells.ForgeSpirit)
                                 && forgeSpiritAbility.CanBeCasted()
                                 && Environment.TickCount > dynComboT + 450)
                        {
                            forgeSpiritAbility.UseAbility();
                            dynComboT = Environment.TickCount;
                        }
                        else if (!HasInvokerSpell(InvokerSpells.Alacrity)
                                 && alacrityAbility.AbilityState == AbilityState.Ready && invokeAbility.CanBeCasted())
                        {
                            PrepareSpell(InvokerSpells.Alacrity);
                        }
                        else if (HasInvokerSpell(InvokerSpells.Alacrity)
                                 && alacrityAbility.CanBeCasted()
                                 && Environment.TickCount > dynComboT + 450)
                        {
                            alacrityAbility.UseAbility(invokerHero);
                        }
                    }
                    else if (invoTarget.Distance2D(invokerHero) <= 350)
                    {
                        if (!HasInvokerSpell(InvokerSpells.IceWall)
                            && iceWallAbility.AbilityState == AbilityState.Ready && invokeAbility.CanBeCasted())
                        {
                            PrepareSpell(InvokerSpells.IceWall);
                        }
                        else if (HasInvokerSpell(InvokerSpells.IceWall)
                                 && iceWallAbility.CanBeCasted()
                                 && Environment.TickCount > dynComboT + 450)
                        {
                            if (Prediction.InFront(invokerHero, 200).Distance2D(invoTarget)
                                < 105)
                            {
                                iceWallAbility.UseAbility();
                                dynComboT = Environment.TickCount;
                            }
                        }
                        else if (!HasInvokerSpell(InvokerSpells.SunStrike)
                                 && sunStrikeAbility.AbilityState == AbilityState.Ready && invokeAbility.CanBeCasted())
                        {
                            PrepareSpell(InvokerSpells.SunStrike);
                        }
                        else if (HasInvokerSpell(InvokerSpells.SunStrike)
                                 && sunStrikeAbility.CanBeCasted()
                                 && Environment.TickCount > dynComboT + 450)
                        {
                            sunStrikeAbility.CastSkillShot(
                                invoTarget,
                                invokerHero.NetworkPosition);
                            dynComboT = Environment.TickCount;
                        }
                    }
                }
            }
            else
            {
                if (wex.Level < exort.Level)
                {
                    if (HasInvokerSpell(InvokerSpells.Tornado) && tornadoAbility.CanBeCasted()
                        && HasInvokerSpell(InvokerSpells.ChaosMeteor) && chaosMeteorAbility.CanBeCasted())
                    {
                        if (Environment.TickCount > comboTornadoT + 500)
                        {
                            var castedTornado = tornadoAbility.CastSkillShot(invoTarget, invokerHero.NetworkPosition);

                            if (castedTornado)
                            {
                                comboTornadoT = Environment.TickCount;
                            }
                        }
                    }
                    else if (Environment.TickCount < comboTornadoT + 6500 && chaosMeteorAbility.CanBeCasted())
                    {
                        if (invoTarget.HasModifier(tornadoModName) && !justTornadoHit)
                        {
                            justTornadoHit = true;
                            willGoDownT = Environment.TickCount + TornadoUpTime(quas.Level);
                        }

                        if (willGoDownT != 0)
                        {
                            ChainTornado(InvokerSpells.ChaosMeteor, willGoDownT);
                        }
                    }
                    else
                    {
                        if (!HasInvokerSpell(InvokerSpells.ChaosMeteor)
                                 && chaosMeteorAbility.AbilityState == AbilityState.Ready && invokeAbility.CanBeCasted())
                        {
                            PrepareSpell(InvokerSpells.ChaosMeteor);
                        }
                        else if (HasInvokerSpell(InvokerSpells.ChaosMeteor) && chaosMeteorAbility.CanBeCasted()
                                 && Environment.TickCount > dynComboT + 450)
                        {
                            chaosMeteorAbility.CastSkillShot(invoTarget, invokerHero.NetworkPosition);
                        }
                        else if (!HasInvokerSpell(InvokerSpells.Tornado) && tornadoAbility.AbilityState == AbilityState.Ready
                            && invokeAbility.CanBeCasted())
                        {
                            PrepareSpell(InvokerSpells.Tornado);
                        }
                        else if (HasInvokerSpell(InvokerSpells.Tornado) && tornadoAbility.CanBeCasted()
                                 && Environment.TickCount > dynComboT + 450)
                        {
                            tornadoAbility.CastSkillShot(invoTarget, invokerHero.NetworkPosition);
                            dynComboT = Environment.TickCount;
                        }
                        else if (!HasInvokerSpell(InvokerSpells.DeafeningBlast)
                             && deafeningBlastAbility.AbilityState == AbilityState.Ready && invokeAbility.CanBeCasted())
                        {
                            PrepareSpell(InvokerSpells.DeafeningBlast);
                        }
                        else if (HasInvokerSpell(InvokerSpells.DeafeningBlast) && deafeningBlastAbility.CanBeCasted()
                                 && Environment.TickCount > dynComboT + 450)
                        {
                            deafeningBlastAbility.CastSkillShot(invoTarget, invokerHero.NetworkPosition);
                            dynComboT = Environment.TickCount;
                        }
                        else if (invoTarget.Distance2D(invokerHero) > 350)
                        {
                            if (!HasInvokerSpell(InvokerSpells.ColdSnap)
                                && coldSnapAbility.AbilityState == AbilityState.Ready && invokeAbility.CanBeCasted())
                            {
                                PrepareSpell(InvokerSpells.ColdSnap);
                            }
                            else if (HasInvokerSpell(InvokerSpells.ColdSnap) && coldSnapAbility.CanBeCasted()
                                     && Environment.TickCount > dynComboT + 450)
                            {
                                coldSnapAbility.UseAbility(invoTarget);
                                dynComboT = Environment.TickCount;
                            }
                            else if (!HasInvokerSpell(InvokerSpells.ForgeSpirit)
                                     && forgeSpiritAbility.AbilityState == AbilityState.Ready && invokeAbility.CanBeCasted())
                            {
                                PrepareSpell(InvokerSpells.ForgeSpirit);
                            }
                            else if (HasInvokerSpell(InvokerSpells.ForgeSpirit)
                                     && forgeSpiritAbility.CanBeCasted()
                                     && Environment.TickCount > dynComboT + 450)
                            {
                                forgeSpiritAbility.UseAbility();
                                dynComboT = Environment.TickCount;
                            }
                            else if (!HasInvokerSpell(InvokerSpells.Alacrity)
                                     && alacrityAbility.AbilityState == AbilityState.Ready && invokeAbility.CanBeCasted())
                            {
                                PrepareSpell(InvokerSpells.Alacrity);
                            }
                            else if (HasInvokerSpell(InvokerSpells.Alacrity)
                                     && alacrityAbility.CanBeCasted()
                                     && Environment.TickCount > dynComboT + 450)
                            {
                                alacrityAbility.UseAbility(invokerHero);
                            }
                        }
                        else if (invoTarget.Distance2D(invokerHero) <= 350)
                        {
                            if (!HasInvokerSpell(InvokerSpells.IceWall)
                                && iceWallAbility.AbilityState == AbilityState.Ready && invokeAbility.CanBeCasted())
                            {
                                PrepareSpell(InvokerSpells.IceWall);
                            }
                            else if (HasInvokerSpell(InvokerSpells.IceWall) && iceWallAbility.CanBeCasted()
                                     && Environment.TickCount > dynComboT + 450)
                            {
                                if (Prediction.InFront(invokerHero, 200).Distance2D(invoTarget) < 105)
                                {
                                    iceWallAbility.UseAbility();
                                    dynComboT = Environment.TickCount;
                                }
                            }
                            else if (!HasInvokerSpell(InvokerSpells.SunStrike)
                                     && sunStrikeAbility.AbilityState == AbilityState.Ready && invokeAbility.CanBeCasted())
                            {
                                PrepareSpell(InvokerSpells.SunStrike);
                            }
                            else if (HasInvokerSpell(InvokerSpells.SunStrike)
                                     && sunStrikeAbility.CanBeCasted()
                                     && Environment.TickCount > dynComboT + 450)
                            {
                                sunStrikeAbility.CastSkillShot(
                                    invoTarget,
                                    invokerHero.NetworkPosition);
                                dynComboT = Environment.TickCount;
                            }
                        }
                    }
                }
                else if (wex.Level >= exort.Level)
                {
                    if (HasInvokerSpell(InvokerSpells.Tornado) && tornadoAbility.CanBeCasted()
                        && HasInvokerSpell(InvokerSpells.EMP) && empAbility.CanBeCasted())
                    {
                        if (Environment.TickCount > comboTornadoT + 500)
                        {
                            var castedTornado = tornadoAbility.CastSkillShot(invoTarget, invokerHero.NetworkPosition);

                            if (castedTornado)
                            {
                                comboTornadoT = Environment.TickCount;
                            }
                        }
                    }
                    else if (Environment.TickCount < comboTornadoT + 6500 && empAbility.CanBeCasted())
                    {
                        if (invoTarget.HasModifier(tornadoModName) && !justTornadoHit)
                        {
                            justTornadoHit = true;
                            willGoDownT = Environment.TickCount + TornadoUpTime(quas.Level);
                        }

                        if (willGoDownT != 0)
                        {
                            ChainTornado(InvokerSpells.EMP, willGoDownT);
                        }
                    }
                    else
                    {
                        if (!HasInvokerSpell(InvokerSpells.EMP) && empAbility.AbilityState == AbilityState.Ready && invokeAbility.CanBeCasted())
                        {
                            PrepareSpell(InvokerSpells.EMP);
                        }
                        else if (HasInvokerSpell(InvokerSpells.EMP) && empAbility.CanBeCasted()
                                 && Environment.TickCount > dynComboT + 450)
                        {
                            empAbility.CastSkillShot(invoTarget, invokerHero.NetworkPosition);
                            dynComboT = Environment.TickCount;
                        }
                        else if (!HasInvokerSpell(InvokerSpells.Tornado) && tornadoAbility.AbilityState == AbilityState.Ready
                            && invokeAbility.CanBeCasted())
                        {
                            PrepareSpell(InvokerSpells.Tornado);
                        }
                        else if (HasInvokerSpell(InvokerSpells.Tornado) && tornadoAbility.CanBeCasted()
                                 && Environment.TickCount > dynComboT + 450)
                        {
                            tornadoAbility.CastSkillShot(invoTarget, invokerHero.NetworkPosition);
                            dynComboT = Environment.TickCount;
                        }

                        if (invoTarget.Distance2D(invokerHero) <= 350)
                        {
                            if (!HasInvokerSpell(InvokerSpells.IceWall)
                                && iceWallAbility.AbilityState == AbilityState.Ready && invokeAbility.CanBeCasted())
                            {
                                PrepareSpell(InvokerSpells.IceWall);
                            }
                            else if (HasInvokerSpell(InvokerSpells.IceWall) && iceWallAbility.CanBeCasted()
                                     && Environment.TickCount > dynComboT + 450)
                            {
                                if (Prediction.InFront(invokerHero, 200).Distance2D(invoTarget) < 105)
                                {
                                    iceWallAbility.UseAbility();
                                    dynComboT = Environment.TickCount;
                                }
                            }
                            else if (!HasInvokerSpell(InvokerSpells.SunStrike)
                                     && sunStrikeAbility.AbilityState == AbilityState.Ready && invokeAbility.CanBeCasted())
                            {
                                PrepareSpell(InvokerSpells.SunStrike);
                            }
                            else if (HasInvokerSpell(InvokerSpells.SunStrike)
                                     && sunStrikeAbility.CanBeCasted()
                                     && Environment.TickCount > dynComboT + 450)
                            {
                                sunStrikeAbility.CastSkillShot(
                                    invoTarget,
                                    invokerHero.NetworkPosition);
                                dynComboT = Environment.TickCount;
                            }
                        }
                        else if (invoTarget.Distance2D(invokerHero) > 350)
                        {
                            if (!HasInvokerSpell(InvokerSpells.DeafeningBlast)
                                && deafeningBlastAbility.AbilityState == AbilityState.Ready && invokeAbility.CanBeCasted())
                            {
                                PrepareSpell(InvokerSpells.DeafeningBlast);
                            }
                            else if (HasInvokerSpell(InvokerSpells.DeafeningBlast) && deafeningBlastAbility.CanBeCasted()
                                     && Environment.TickCount > dynComboT + 450)
                            {
                                deafeningBlastAbility.CastSkillShot(invoTarget, invokerHero.NetworkPosition);
                                dynComboT = Environment.TickCount;
                            }
                            else if (!HasInvokerSpell(InvokerSpells.ColdSnap)
                                && coldSnapAbility.AbilityState == AbilityState.Ready && invokeAbility.CanBeCasted())
                            {
                                PrepareSpell(InvokerSpells.ColdSnap);
                            }
                            else if (HasInvokerSpell(InvokerSpells.ColdSnap) && coldSnapAbility.CanBeCasted()
                                     && Environment.TickCount > dynComboT + 450)
                            {
                                coldSnapAbility.UseAbility(invoTarget);
                                dynComboT = Environment.TickCount;
                            }
                            else if (!HasInvokerSpell(InvokerSpells.ForgeSpirit)
                                     && forgeSpiritAbility.AbilityState == AbilityState.Ready && invokeAbility.CanBeCasted())
                            {
                                PrepareSpell(InvokerSpells.ForgeSpirit);
                            }
                            else if (HasInvokerSpell(InvokerSpells.ForgeSpirit)
                                     && forgeSpiritAbility.CanBeCasted()
                                     && Environment.TickCount > dynComboT + 450)
                            {
                                forgeSpiritAbility.UseAbility();
                                dynComboT = Environment.TickCount;
                            }
                            else if (!HasInvokerSpell(InvokerSpells.Alacrity)
                                     && alacrityAbility.AbilityState == AbilityState.Ready && invokeAbility.CanBeCasted())
                            {
                                PrepareSpell(InvokerSpells.Alacrity);
                            }
                            else if (HasInvokerSpell(InvokerSpells.Alacrity)
                                     && alacrityAbility.CanBeCasted()
                                     && Environment.TickCount > dynComboT + 450)
                            {
                                alacrityAbility.UseAbility(invokerHero);
                            }
                        }
                    }
                }
            }

            if (coldSnapAbility.AbilityState != AbilityState.Ready
                && iceWallAbility.AbilityState != AbilityState.Ready
                && empAbility.AbilityState != AbilityState.Ready 
                && tornadoAbility.AbilityState != AbilityState.Ready
                && alacrityAbility.AbilityState != AbilityState.Ready
                && sunStrikeAbility.AbilityState != AbilityState.Ready
                && forgeSpiritAbility.AbilityState != AbilityState.Ready
                && chaosMeteorAbility.AbilityState != AbilityState.Ready
                && deafeningBlastAbility.AbilityState != AbilityState.Ready)
            {
                if (HasItemById(110) && invokerHero.FindItem("item_refresher").CanBeCasted()
                    && Environment.TickCount > lastRefresherT + 500)
                {
                    invokerHero.FindItem("item_refresher").UseAbility();
                    lastRefresherT = Environment.TickCount;
                }
            }

            if (ReadySpells() <= 4)
            {
                if (HasItemById(100) && invokerHero.FindItem("item_cyclone").CanBeCasted()
                    && Environment.TickCount > eulCastT + 500)
                {
                    invokerHero.FindItem("item_cyclone").UseAbility(invoTarget);
                    eulCastT = Environment.TickCount;
                }
            }
        }

        private static void Combo()
        {
            if (invoTarget == null)
            {
                return;
            }

            var spell1 = (InvokerSpells)invoMenu.Item("ccombo1").GetValue<StringList>().SelectedIndex;
            var spell2 = (InvokerSpells)invoMenu.Item("ccombo2").GetValue<StringList>().SelectedIndex;
            var spell3 = (InvokerSpells)invoMenu.Item("ccombo3").GetValue<StringList>().SelectedIndex;
            var spell4 = (InvokerSpells)invoMenu.Item("ccombo4").GetValue<StringList>().SelectedIndex;
            var spell5 = (InvokerSpells)invoMenu.Item("ccombo5").GetValue<StringList>().SelectedIndex;
            var spell6 = (InvokerSpells)invoMenu.Item("ccombo6").GetValue<StringList>().SelectedIndex;
            var spell7 = (InvokerSpells)invoMenu.Item("ccombo7").GetValue<StringList>().SelectedIndex;
            var spell8 = (InvokerSpells)invoMenu.Item("ccombo8").GetValue<StringList>().SelectedIndex;
            var spell9 = (InvokerSpells)invoMenu.Item("ccombo9").GetValue<StringList>().SelectedIndex;
            var spell10 = (InvokerSpells)invoMenu.Item("ccombo10").GetValue<StringList>().SelectedIndex;

            var ability1 = invokerHero.FindSpell(EnumToString(spell1));
            var ability2 = invokerHero.FindSpell(EnumToString(spell2));

            var abilityList = new[] { spell1, spell2, spell3, spell4, spell5, spell6, spell7, spell8, spell9, spell10 };

            if (spell2 == InvokerSpells.Tornado && ability2.CanBeCasted())
            {
                if (IsTornadoChain(spell1) && ability1.CanBeCasted())
                {
                    if (Environment.TickCount > comboTornadoT + 500)
                    {
                        var castedTornado = ability2.CastSkillShot(invoTarget, invokerHero.NetworkPosition);

                        if (castedTornado)
                        {
                            comboTornadoT = Environment.TickCount;
                        }
                    }
                }
            }

            if (Environment.TickCount < comboTornadoT + 6500 && ability1.CanBeCasted())
            {
                if (invoTarget.HasModifier(tornadoModName) && !justTornadoHit)
                {
                    justTornadoHit = true;
                    willGoDownT = Environment.TickCount + TornadoUpTime(quas.Level);
                }

                if (willGoDownT != 0)
                {
                    ChainTornado(spell1, willGoDownT);
                }
            }
            else
            {
                for (int i = 0; i < abilityList.Length; i++)
                {
                    var ability = invokerHero.FindSpell(EnumToString(abilityList[i]));

                    if (ability.AbilityState == AbilityState.Ready && !ability.CanBeCasted()
                        && invokeAbility.CanBeCasted())
                    {
                        PrepareSpell(abilityList[i]);
                    }
                    else if (ability.CanBeCasted() && Environment.TickCount > customComboT + 450)
                    {
                        CastSpell(abilityList[i]);
                    }
                }
            }
        }

        private static void CastSpell(InvokerSpells spell)
        {
            var ability = invokerHero.FindSpell(EnumToString(spell));

            switch (spell)
            {
                case InvokerSpells.ColdSnap:
                    ability.UseAbility(invoTarget);
                    customComboT = Environment.TickCount;
                    break;
                case InvokerSpells.GhostWalk:
                    ability.UseAbility();
                    customComboT = Environment.TickCount;
                    break;
                case InvokerSpells.IceWall:
                    invokerHero.Move(invoTarget.NetworkPosition);
                    if (invoTarget.Distance2D(invokerHero) < 200 + 105)
                    {
                        ability.UseAbility();
                        customComboT = Environment.TickCount;
                    }
                    break;
                case InvokerSpells.EMP:
                    ability.UseAbility(invoTarget.NetworkPosition);
                    customComboT = Environment.TickCount;
                    break;
                case InvokerSpells.Tornado:
                    ability.CastSkillShot(invoTarget, invokerHero.NetworkPosition);
                    customComboT = Environment.TickCount;
                    break;
                case InvokerSpells.Alacrity:
                    ability.UseAbility(invokerHero);
                    customComboT = Environment.TickCount;
                    break;
                case InvokerSpells.SunStrike:
                    ability.CastSkillShot(invoTarget, invokerHero.NetworkPosition);
                    customComboT = Environment.TickCount;
                    break;
                case InvokerSpells.ForgeSpirit:
                    ability.UseAbility();
                    customComboT = Environment.TickCount;
                    break;
                case InvokerSpells.ChaosMeteor:
                    ability.CastSkillShot(invoTarget, invokerHero.NetworkPosition);
                    customComboT = Environment.TickCount;
                    break;
                case InvokerSpells.DeafeningBlast:
                    ability.CastSkillShot(invoTarget, invokerHero.NetworkPosition);
                    customComboT = Environment.TickCount;
                    break;
            }
        }

        private static bool PrepareSpell(InvokerSpells spell)
        {
            var sequence = GetSequence(spell);

            if (invokeAbility.CanBeCasted() && Environment.TickCount > lastInvokeT + 500)
            {
                foreach (var seqElement in sequence)
                {
                    seqElement.UseAbility();
                }

                UseInvokeWithDelay();
                return true;
            }

            return false;
        }

        private static void ChainTornado(InvokerSpells spell, int time)
        {
            float delay = 0;

            switch (spell)
            {
                case InvokerSpells.EMP:
                    delay = 2900;
                    break;
                case InvokerSpells.ChaosMeteor:
                    delay = 1300;
                    break;
                case InvokerSpells.DeafeningBlast:
                    delay = DeafBlastHitTime();
                    Game.PrintMessage(delay.ToString(), MessageType.ChatMessage);
                    break;
                case InvokerSpells.SunStrike:
                    delay = 1700;
                    break;
            }

            if (time < Environment.TickCount + delay && Environment.TickCount > chainTornadoT + 500)
            {
                var chainSpell = invokerHero.FindSpell(EnumToString(spell));

                if (chainSpell.CanBeCasted())
                {
                    chainTornadoT = Environment.TickCount;

                    chainSpell.UseAbility(invoTarget.NetworkPosition);
                }
            }
        }

        private static float DeafBlastHitTime()
        {
            var distance = Math.Max(invokerHero.Distance2D(invoTarget) - 225, 0);
            const int Speed = 1100;

            return distance / Speed;
        }

        private static int TornadoUpTime(uint quasLevel)
        {
            return TornadoDurations[quasLevel];
        }

        private static bool IsTornadoChain(InvokerSpells spell)
        {
            return spell == InvokerSpells.ChaosMeteor || spell == InvokerSpells.DeafeningBlast
                   || spell == InvokerSpells.EMP || spell == InvokerSpells.SunStrike;
        }

        private static int ReadySpells()
        {
            var coldSnapAbility = invokerHero.FindSpell(EnumToString(InvokerSpells.ColdSnap));
            var ghostWalkAbility = invokerHero.FindSpell(EnumToString(InvokerSpells.GhostWalk));
            var iceWallAbility = invokerHero.FindSpell(EnumToString(InvokerSpells.IceWall));
            var empAbility = invokerHero.FindSpell(EnumToString(InvokerSpells.EMP));
            var tornadoAbility = invokerHero.FindSpell(EnumToString(InvokerSpells.Tornado));
            var alacrityAbility = invokerHero.FindSpell(EnumToString(InvokerSpells.Alacrity));
            var sunStrikeAbility = invokerHero.FindSpell(EnumToString(InvokerSpells.SunStrike));
            var forgeSpiritAbility = invokerHero.FindSpell(EnumToString(InvokerSpells.ForgeSpirit));
            var chaosMeteorAbility = invokerHero.FindSpell(EnumToString(InvokerSpells.ChaosMeteor));
            var deafeningBlastAbility = invokerHero.FindSpell(EnumToString(InvokerSpells.DeafeningBlast));

            var abilityList = new[]
                                  {
                                      coldSnapAbility, iceWallAbility, empAbility, tornadoAbility, alacrityAbility,
                                      sunStrikeAbility, forgeSpiritAbility, chaosMeteorAbility, deafeningBlastAbility
                                  };

            int readySpells = abilityList.Count(ability => ability.AbilityState == AbilityState.Ready);

            return readySpells;
        }

        private static bool HasSpirits()
        {
            return
                ObjectManager.GetEntities<Unit>()
                    .Any(
                        spirit =>
                        spirit.ClassID == ClassID.CDOTA_BaseNPC_Invoker_Forged_Spirit && spirit.Team == invokerHero.Team);
        }

        private static IEnumerable<Unit> GetMySpirits()
        {
            return
                ObjectManager.GetEntities<Unit>()
                    .Where(
                        spirit =>
                        spirit.ClassID == ClassID.CDOTA_BaseNPC_Invoker_Forged_Spirit && spirit.Team == invokerHero.Team);
        } 

        private static void Flee()
        {
            bool isAtF;
            if (!HasInvokerSpell(InvokerSpells.GhostWalk, out isAtF) && invokeAbility.CanBeCasted() && Environment.TickCount > lastMoveT + 500)
            {
                var sequence = GetSequence(InvokerSpells.GhostWalk);

                foreach (var spell in sequence)
                {
                    spell.UseAbility();
                }

                UseInvokeWithDelay();
            }
            else
            {
                var ghostSpell = invokerHero.FindSpell("invoker_ghost_walk");

                if (ghostSpell.CanBeCasted() && Environment.TickCount > fleeCastT + 500)
                {
                    wex.UseAbility();
                    wex.UseAbility();
                    wex.UseAbility();

                    ghostSpell.UseAbility();

                    fleeCastT = Environment.TickCount;
                }
            }

            if (Environment.TickCount > lastMoveT + 80)
            {
                invokerHero.Move(Game.MousePosition);
                lastMoveT = Environment.TickCount;
            }
        }

        private static void PrepareSpells()
        {
            var firstSpell = invokerHero.HasItem(ClassID.CDOTA_Item_UltimateScepter)
                                 ? InvokerSpells.EMP
                                 : (wex.Level >= exort.Level ? InvokerSpells.EMP : InvokerSpells.ChaosMeteor);

            const InvokerSpells secondSpell = InvokerSpells.Tornado;

            Ability[] spell1 = GetSequence(firstSpell);
            Ability[] spell2 = GetSequence(secondSpell);

            if (spell1 == null || spell2 == null)
            {
                return;
            }

            if (invokeAbility.CanBeCasted() && Environment.TickCount > lastInvokeT + 500)
            {
                bool isAtF1;
                bool isAtF2;

                if (HasInvokerSpell(spell2, out isAtF2) && !HasInvokerSpell(spell1, out isAtF1))
                {
                    if (isAtF2)
                    {
                        foreach (var spell in spell2)
                        {
                            spell.UseAbility();
                        }

                        UseInvokeWithDelay();
                    }
                    else
                    {
                        foreach (var spell in spell1)
                        {
                            spell.UseAbility();
                        }

                        UseInvokeWithDelay();
                    }
                }
                else if (!HasInvokerSpell(spell2, out isAtF2))
                {
                    if (HasInvokerSpell(spell1, out isAtF1))
                    {
                        if (isAtF1)
                        {
                            foreach (var spell in spell1)
                            {
                                spell.UseAbility();
                            }

                            UseInvokeWithDelay();
                        }
                        else
                        {
                            foreach (var spell in spell2)
                            {
                                spell.UseAbility();
                            }

                            UseInvokeWithDelay();
                        }
                    }
                    else
                    {
                        foreach (var spell in spell1)
                        {
                            spell.UseAbility();
                        }

                        UseInvokeWithDelay();
                    }
                }
            }
        }

        private static string EnumToString(InvokerSpells spell)
        {
            switch (spell)
            {
                case InvokerSpells.ColdSnap:
                    return "invoker_cold_snap";
                case InvokerSpells.GhostWalk:
                    return "invoker_ghost_walk";
                case InvokerSpells.IceWall:
                    return "invoker_ice_wall";
                case InvokerSpells.EMP:
                    return "invoker_emp";
                case InvokerSpells.Tornado:
                    return "invoker_tornado";
                case InvokerSpells.Alacrity:
                    return "invoker_alacrity";
                case InvokerSpells.SunStrike:
                    return "invoker_sun_strike";
                case InvokerSpells.ForgeSpirit:
                    return "invoker_forge_spirit";
                case InvokerSpells.ChaosMeteor:
                    return "invoker_chaos_meteor";
                case InvokerSpells.DeafeningBlast:
                    return "invoker_deafening_blast";
                default:
                    return "invoker_unknown";
            }
        }

        private static Hero GetCustomTarget(float range)
        {
            var chosenTarget =
                ObjectManager.GetEntities<Hero>()
                    .Where(x => x.Team == invokerHero.GetEnemyTeam() && x.IsAlive && x.Distance2D(invokerHero) <= range)
                    .OrderBy(y => y.Health * 100 / y.MaximumHealth)
                    .FirstOrDefault();

            return chosenTarget;
        }

        private static void InvoOrbwalk()
        {
            if (invoTarget == null)
            {
                if (Environment.TickCount > lastMoveT + 80)
                {
                    invokerHero.Move(Game.MousePosition);
                    lastMoveT = Environment.TickCount;
                }
            }
            else
            {
                Orbwalking.Orbwalk(invoTarget);
            }
        }

        private static bool HasInvokerSpell(InvokerSpells spell, out bool inF)
        {
            return HasInvokerSpell(GetSequence(spell), out inF);
        }

        private static bool HasInvokerSpell(InvokerSpells spell)
        {
            bool unused;

            return HasInvokerSpell(GetSequence(spell), out unused);
        }

        private static bool HasInvokerSpell(Ability[] sequence)
        {
            bool unused;

            return HasInvokerSpell(sequence, out unused);
        }

        private static bool HasInvokerSpell(Ability[] sequence, out bool inF)
        {
            const string ColdSnapName = "invoker_cold_snap";
            const string GhostWalkName = "invoker_ghost_walk";
            const string IceWallName = "invoker_ice_wall";
            const string EmpName = "invoker_emp";
            const string TornadoName = "invoker_tornado";
            const string AlacrityName = "invoker_alacrity";
            const string SunStrikeName = "invoker_sun_strike";
            const string ForgeSpiritName = "invoker_forge_spirit";
            const string ChaosMeteorName = "invoker_chaos_meteor";
            const string DeafeningBlastName = "invoker_deafening_blast";

            if (sequence.SequenceEqual(GetSequence(InvokerSpells.ColdSnap)))
            {
                if (spellD.Name == ColdSnapName)
                {
                    inF = false;
                    return true;
                }
                if (spellF.Name == ColdSnapName)
                {
                    inF = true;
                    return true;
                }
            }
            if (sequence.SequenceEqual(GetSequence(InvokerSpells.GhostWalk)))
            {
                if (spellD.Name == GhostWalkName)
                {
                    inF = false;
                    return true;
                }
                if (spellF.Name == GhostWalkName)
                {
                    inF = true;
                    return true;
                }
            }
            if (sequence.SequenceEqual(GetSequence(InvokerSpells.IceWall)))
            {
                if (spellD.Name == IceWallName)
                {
                    inF = false;
                    return true;
                }
                if (spellF.Name == IceWallName)
                {
                    inF = true;
                    return true;
                }
            }
            if (sequence.SequenceEqual(GetSequence(InvokerSpells.EMP)))
            {
                if (spellD.Name == EmpName)
                {
                    inF = false;
                    return true;
                }
                if (spellF.Name == EmpName)
                {
                    inF = true;
                    return true;
                }
            }
            if (sequence.SequenceEqual(GetSequence(InvokerSpells.Tornado)))
            {
                if (spellD.Name == TornadoName)
                {
                    inF = false;
                    return true;
                }
                if (spellF.Name == TornadoName)
                {
                    inF = true;
                    return true;
                }
            }
            if (sequence.SequenceEqual(GetSequence(InvokerSpells.Alacrity)))
            {
                if (spellD.Name == AlacrityName)
                {
                    inF = false;
                    return true;
                }
                if (spellF.Name == AlacrityName)
                {
                    inF = true;
                    return true;
                }
            }
            if (sequence.SequenceEqual(GetSequence(InvokerSpells.SunStrike)))
            {
                if (spellD.Name == SunStrikeName)
                {
                    inF = false;
                    return true;
                }
                if (spellF.Name == SunStrikeName)
                {
                    inF = true;
                    return true;
                }
            }
            if (sequence.SequenceEqual(GetSequence(InvokerSpells.ForgeSpirit)))
            {
                if (spellD.Name == ForgeSpiritName)
                {
                    inF = false;
                    return true;
                }
                if (spellF.Name == ForgeSpiritName)
                {
                    inF = true;
                    return true;
                }
            }
            if (sequence.SequenceEqual(GetSequence(InvokerSpells.ChaosMeteor)))
            {
                if (spellD.Name == ChaosMeteorName)
                {
                    inF = false;
                    return true;
                }
                if (spellF.Name == ChaosMeteorName)
                {
                    inF = true;
                    return true;
                }
            }
            if (sequence.SequenceEqual(GetSequence(InvokerSpells.DeafeningBlast)))
            {
                if (spellD.Name == DeafeningBlastName)
                {
                    inF = false;
                    return true;
                }
                if (spellF.Name == DeafeningBlastName)
                {
                    inF = true;
                    return true;
                }
            }

            inF = false;
            return false;
        }

        private static bool HasItemById(int id)
        {
            return invokerHero.Inventory.Items.Any(item => item.ID == id);
        }

        private static void UseInvokeWithDelay()
        {
            invokeAbility.UseAbility();
            lastInvokeT = Environment.TickCount;
        }

        private static Ability[] GetSequence(InvokerSpells spell)
        {
            switch (spell)
            {
                case InvokerSpells.ColdSnap:
                    return new[] { quas, quas, quas };
                case InvokerSpells.GhostWalk:
                    return new[] { quas, quas, wex };
                case InvokerSpells.IceWall:
                    return new[] { quas, quas, exort };
                case InvokerSpells.EMP:
                    return new[] { wex, wex, wex };
                case InvokerSpells.Tornado:
                    return new[] { wex, wex, quas };
                case InvokerSpells.Alacrity:
                    return new[] { wex, wex, exort };
                case InvokerSpells.SunStrike:
                    return new[] { exort, exort, exort };
                case InvokerSpells.ForgeSpirit:
                    return new[] { exort, exort, quas };
                case InvokerSpells.ChaosMeteor:
                    return new[] { exort, exort, wex };
                case InvokerSpells.DeafeningBlast:
                    return new[] { quas, wex, exort };
                default:
                    return new Ability[3];
            }
        }

        private static void PippyDrawCircle2(float x, float y, float z, int radius, int width, Color color, float fidelity)
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
            PippyDrawCircle2(unit.Position.X, unit.Position.Y, unit.Position.Z, radius, width, color, invoMenu.Item("CircleFidelity").GetValue<Slider>().Value);
        }

        private static void InvokerDrawing(EventArgs args)
        {
            if (invoMenu.Item("allowDraw").GetValue<bool>())
            {
                if (invoMenu.Item("drawComboRange").GetValue<bool>() && invokerHero.NetworkPosition.IsOnScreen())
                {
                    PippyDrawCircle(invokerHero, invoMenu.Item("comboRange").GetValue<Slider>().Value, 0, Color.White);
                }
            }
        }
    }
}
