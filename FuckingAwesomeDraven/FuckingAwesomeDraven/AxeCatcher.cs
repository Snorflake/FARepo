﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace FuckingAwesomeDraven
{

    class Axe
    {
        public Axe(GameObject obj)
        {
            AxeObj = obj;
            EndTick = Environment.TickCount + 1200;
        }

        public int EndTick;
        public GameObject AxeObj;
    }

    class AxeCatcher
    {
        public static List<Axe> AxeSpots = new List<Axe>();
        private static Obj_AI_Minion _prevMinion;
        public static int CurrentAxes;
        public static int LastAA;
        public static int LastQ;
        public static List<String> AxesList = new List<string>()
        {
            "Draven_Base_Q_reticle.troy" , "Draven_Skin01_Q_reticle.troy" ,"Draven_Skin03_Q_reticle.troy"
        };

        public static List<String> QBuffList = new List<string>()
        {
            "Draven_Base_Q_buf.troy", "Draven_Skin01_Q_buf.troy", "Draven_Skin02_Q_buf.troy", "Draven_Skin03_Q_buf.troy"
        };

        public static Orbwalking.Orbwalker Orbwalker { get {return Program.Orbwalker; } }
        public static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        public static int midAirAxes
        {
            get { return AxeSpots.Count(a => a.AxeObj.IsValid && a.EndTick < Environment.TickCount); }
        }
        public static float RealAutoAttack(Obj_AI_Base target){
                return (float) Player.CalcDamage(target, Damage.DamageType.Physical, (ObjectManager.Player.BaseAttackDamage + ObjectManager.Player.FlatPhysicalDamageMod +
                    (((Program.spells[Spells.Q].Level) > 0 && hasQBuff ? new float[] {45, 55, 65, 75, 85 }[Program.spells[Spells.Q].Level - 1] : 0 ) / 100 * (ObjectManager.Player.BaseAttackDamage + ObjectManager.Player.FlatPhysicalDamageMod))));
        }

        public static void GameOnOnWndProc(WndEventArgs args)
        {
            if (args.Msg != (uint)WindowsMessages.WM_LBUTTONDOWN || !Program.Config.Item("clickRemoveAxes").GetValue<bool>())
            {
                return;
            }

            for (var i = 0; i < AxeSpots.Count; i++)
            {
                if (AxeSpots[i].AxeObj.Position.Distance(Game.CursorPos) < 120)
                {
                    AxeSpots.RemoveAt(i);
                    Notifications.AddNotification(new Notification("Removed Axe", 1));
                }
            }
        }

        public static void Draw()
        {
            if (Program.Config.Item("DE").GetValue<Circle>().Active)
            {
                Render.Circle.DrawCircle(Player.Position, Program.spells[Spells.E].Range, Color.White);
            }

            if (Program.Config.Item("DR").GetValue<Circle>().Active)
            {
                Render.Circle.DrawCircle(Player.Position, Program.spells[Spells.R].Range, Color.White);
            }

            if (Program.Config.Item("DCR").GetValue<Circle>().Active)
            {
                var mode = Program.Config.Item("catchRadiusMode").GetValue<StringList>().SelectedIndex;
                var target = TargetSelector.GetTarget(1000, TargetSelector.DamageType.Physical);

                switch (mode)
                {
                    case 1:
                        if (!target.IsValidTarget())
                            break;
                        Render.Circle.DrawCircle(
                            target.Position, Program.Config.Item("catchRadius").GetValue<Slider>().Value, Color.White);
                        break;
                    default:
                        Render.Circle.DrawCircle(
                            Game.CursorPos, Program.Config.Item("catchRadius").GetValue<Slider>().Value, Color.White);
                        break;

                }
            }

            if (Program.Config.Item("DAR").GetValue<Circle>().Active)
            {
                foreach (var axe in AxeSpots)
                {
                    Render.Circle.DrawCircle(axe.AxeObj.Position, 120, Color.White);
                }
            }

            if (Program.Config.Item("DCA").GetValue<Circle>().Active)
            {
                Drawing.DrawText(
                    Drawing.WorldToScreen(Player.Position).X - 70, Drawing.WorldToScreen(Player.Position).Y + 60,
                    Color.White, "Current Axes: " + CurrentAxes);
            }

            if (Program.Config.Item("DCS").GetValue<Circle>().Active)
            {
                Drawing.DrawText(
                    Drawing.WorldToScreen(Player.Position).X - 70, Drawing.WorldToScreen(Player.Position).Y + 40,
                    Color.White, "Catching Active:  " + Program.Config.Item("catching").GetValue<KeyBind>().Active);
            }
        }

        public static bool hasQBuff {get{ return Player.Buffs.Any(a => a.DisplayName.ToLower().Contains("spinning"));}}

        public static bool canAA { get { return Environment.TickCount >= LastAA + Player.AttackDelay * 1000 + + (Game.Ping / 2); } }

        public static bool isCatching { get { return AxeSpots.Count > 0; } }

        public static bool inCatchRadius(Axe a)
        {
            var mode = Program.Config.Item("catchRadiusMode").GetValue<StringList>().SelectedIndex;
            var target = TargetSelector.GetTarget(1000, TargetSelector.DamageType.Physical);
            switch (mode)
            {
                case 1:
                    if (!target.IsValidTarget()) break;
                    return a.AxeObj.Position.Distance(target.Position) <
                           Program.Config.Item("catchRadius").GetValue<Slider>().Value;
                default:
                    return a.AxeObj.Position.Distance(Game.CursorPos) <
                           Program.Config.Item("catchRadius").GetValue<Slider>().Value;
            }
            return a.AxeObj.Position.Distance(Game.CursorPos) <
                           Program.Config.Item("catchRadius").GetValue<Slider>().Value;
        }

        public static void catchAxes()
        {
            var axeMinValue = int.MaxValue;
            Axe selectedAxe = null;

            foreach (var axe in AxeSpots.Where(a => a.AxeObj.IsValid))
            {
                if (axeMinValue > axe.EndTick)
                {
                    axeMinValue = axe.EndTick;
                    selectedAxe = axe;
                }
            }

            for (var i = 0; i < AxeSpots.Count; i++)
            {
                if (AxeSpots[i].EndTick < Environment.TickCount)
                {
                    AxeSpots.RemoveAt(i);
                    return;
                }
            }

            if (selectedAxe == null || AxeSpots.Count == 0 || Player.Distance(selectedAxe.AxeObj.Position) < 110 || GetTarget().IsValid<Obj_AI_Hero>() && Player.GetAutoAttackDamage(GetTarget() as Obj_AI_Base) * 2 > GetTarget().Health || !Program.Config.Item("catching").GetValue<KeyBind>().Active || !inCatchRadius(selectedAxe))
            {
                if (canAA && GetTarget() != null)
                {
                    Player.IssueOrder(GameObjectOrder.AttackUnit, GetTarget());
                    return;
                }
                if ((LastAA + (Player.AttackCastDelay*1000) + (Game.Ping*0.5) + Program.Config.Item("ExtraWindup").GetValue<Slider>().Value < Environment.TickCount) && Game.CursorPos.Distance(Player.Position) > Program.Config.Item("HoldPosRadius").GetValue<Slider>().Value && Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.None)
                {
                    Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                }
                return;
            }
            if ((Player.AttackCastDelay + ((Player.Distance(selectedAxe.AxeObj.Position.Extend(Game.CursorPos, 100))/Player.MoveSpeed)*1000) +
                Environment.TickCount < selectedAxe.EndTick && GetTarget().IsValidTarget(Orbwalking.GetRealAutoAttackRange(Player)) && canAA || Player.Distance(selectedAxe.AxeObj.Position) <= 120))
            {
                if (GetTarget() == null) return;
                Player.IssueOrder(GameObjectOrder.AttackUnit, GetTarget());
            }
            else if ((LastAA + Player.AttackCastDelay*1000 + (Game.Ping*0.5) + Program.Config.Item("ExtraWindup").GetValue<Slider>().Value < Environment.TickCount))
            {
                if (Program.Config.Item("useWCatch").GetValue<bool>() && Program.spells[Spells.W].IsReady() && selectedAxe.AxeObj.Position.Distance(Player.Position) > ((selectedAxe.EndTick / 1000 - Environment.TickCount / 1000) * (Player.MoveSpeed)) &&
                    (selectedAxe.AxeObj.Position.Distance(Player.Position) < ((selectedAxe.EndTick / 1000 - Environment.TickCount / 1000) * (Player.MoveSpeed * new[] { 1.40f, 1.45f, 1.50f, 1.55f, 1.60f }[Program.spells[Spells.W].Level - 1]))))
                {
                    Program.spells[Spells.W].Cast();
                    Player.IssueOrder(GameObjectOrder.MoveTo, AxeSpots[0].AxeObj.Position.Extend(AxeSpots[1].AxeObj.Position, 100));
                }

                if (AxeSpots.Count == 2)
                {
                    Player.IssueOrder(GameObjectOrder.MoveTo, AxeSpots[0].AxeObj.Position.Extend(AxeSpots[1].AxeObj.Position, 100));
                    return;
                }

                Player.IssueOrder(GameObjectOrder.MoveTo, selectedAxe.AxeObj.Position.Extend(Game.CursorPos, 100));
            }
        }


        public static bool CanMakeIt(int time)
        {
            var axeMinValue = int.MaxValue;
            Axe selectedAxe = null;

            foreach (var axe in AxeSpots.Where(a => a.AxeObj.IsValid))
            {
                if (axeMinValue > axe.EndTick)
                {
                    axeMinValue = axe.EndTick;
                    selectedAxe = axe;
                }
            }

            if (selectedAxe == null) return true;

            return time +
                   ((Player.Distance(selectedAxe.AxeObj.Position.Extend(Game.CursorPos, 100))/Player.MoveSpeed)*1000) +
                   Environment.TickCount < selectedAxe.EndTick;
        }


        //Events

        public static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;
            if (args.SData.IsAutoAttack())
            {
                LastAA = Environment.TickCount;
            }
            if (args.SData.Name == "dravenspinning")
            {
                LastQ = Environment.TickCount;
            }
        }

        public static void OnCreate(GameObject sender, EventArgs args)
        {
            var Name = sender.Name;

            if ((AxesList.Contains(Name)) &&
                sender.Position.Distance(ObjectManager.Player.Position) / ObjectManager.Player.MoveSpeed <= 2)
            {
                AxeSpots.Add(new Axe(sender));
            }

            if ((QBuffList.Contains(Name)) &&
                sender.Position.Distance(ObjectManager.Player.Position) < 100)
            {
                CurrentAxes += 1;
            }
        }

        public static void OnDelete(GameObject sender, EventArgs args)
        {
            for (var i = 0; i < AxeSpots.Count; i++)
            {
                if (AxeSpots[i].AxeObj.NetworkId == sender.NetworkId)
                {
                    AxeSpots.RemoveAt(i);
                    return;
                }
            }

            if ((QBuffList.Contains(sender.Name)) &&
                sender.Position.Distance(ObjectManager.Player.Position) < 100)
            {
                if (CurrentAxes == 0)
                    return;
                if (CurrentAxes <= 2)
                    CurrentAxes = CurrentAxes - 1;
                else CurrentAxes = CurrentAxes - 1;
            }
        }


        // Orbwalker stuff

        private static bool ShouldWait()
        {
            return
                ObjectManager.Get<Obj_AI_Minion>()
                    .Any(
                        minion =>
                            minion.IsValidTarget() && minion.Team != GameObjectTeam.Neutral &&
                            Orbwalking.InAutoAttackRange(minion) &&
                            HealthPrediction.LaneClearHealthPrediction(
                                minion, (int)((Player.AttackDelay * 1000) * 2), Program.Config.Item("FarmDelay").GetValue<Slider>().Value) <= RealAutoAttack(minion));
        }

        public static AttackableUnit GetTarget()
        {
            AttackableUnit result = null;

            /*Killable Minion*/
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed ||
                Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
            {
                foreach (var minion in
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Where(
                            minion =>
                                minion.IsValidTarget() && Orbwalker.InAutoAttackRange(minion) &&
                                minion.Health <
                                2 *
                                (RealAutoAttack(minion)))
                    )
                {
                    var t = (int)(Player.AttackCastDelay * 1000) - 100 + Game.Ping / 2 +
                            1000 * (int)Player.Distance(minion) / (int)Orbwalking.GetMyProjectileSpeed();
                    var predHealth = HealthPrediction.GetHealthPrediction(minion, t, Program.Config.Item("FarmDelay").GetValue<Slider>().Value);

                    if (minion.Team != GameObjectTeam.Neutral && MinionManager.IsMinion(minion, true))
                    {
                        if (predHealth > 0 && predHealth <= RealAutoAttack(minion))
                        {
                            return minion;
                        }
                    }
                }
            }

            /* turrets / inhibitors / nexus */
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                /* turrets */
                foreach (var turret in
                    ObjectManager.Get<Obj_AI_Turret>().Where(t => t.IsValidTarget() && Orbwalker.InAutoAttackRange(t)))
                {
                    return turret;
                }

                /* inhibitor */
                foreach (var turret in
                    ObjectManager.Get<Obj_BarracksDampener>().Where(t => t.IsValidTarget() && Orbwalker.InAutoAttackRange(t)))
                {
                    return turret;
                }

                /* nexus */
                foreach (var nexus in
                    ObjectManager.Get<Obj_HQ>().Where(t => t.IsValidTarget() && Orbwalker.InAutoAttackRange(t)))
                {
                    return nexus;
                }
            }

            /*Champions*/
            if (Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.LastHit)
            {
                var target = TargetSelector.GetTarget(-1, TargetSelector.DamageType.Physical);
                if (target.IsValidTarget())
                {
                    return target;
                }
            }

            /*Jungle minions*/
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                result =
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Where(
                            mob =>
                                mob.IsValidTarget() && Orbwalker.InAutoAttackRange(mob) && mob.Team == GameObjectTeam.Neutral)
                        .MaxOrDefault(mob => mob.MaxHealth);
                if (result != null)
                {
                    return result;
                }
            }

            /*Lane Clear minions*/
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                if (!ShouldWait())
                {
                    if (_prevMinion.IsValidTarget() && Orbwalker.InAutoAttackRange(_prevMinion))
                    {
                        var predHealth = HealthPrediction.LaneClearHealthPrediction(
                            _prevMinion, (int)((Player.AttackDelay * 1000) * 2f), Program.Config.Item("FarmDelay").GetValue<Slider>().Value);
                        if (predHealth >= 2 * RealAutoAttack(_prevMinion) ||
                            Math.Abs(predHealth - _prevMinion.Health) < float.Epsilon)
                        {
                            return _prevMinion;
                        }
                    }

                    result = (from minion in
                                  ObjectManager.Get<Obj_AI_Minion>()
                                      .Where(minion => minion.IsValidTarget() && Orbwalker.InAutoAttackRange(minion))
                              let predHealth =
                                  HealthPrediction.LaneClearHealthPrediction(
                                      minion, (int)((Player.AttackDelay * 1000) * 2f), Program.Config.Item("FarmDelay").GetValue<Slider>().Value)
                              where
                                  predHealth >= 2 * RealAutoAttack(minion) ||
                                  Math.Abs(predHealth - minion.Health) < float.Epsilon
                              select minion).MaxOrDefault(m => m.Health);

                    if (result != null)
                    {
                        _prevMinion = (Obj_AI_Minion)result;
                    }
                }
            }
            return null;
        }

    }
}