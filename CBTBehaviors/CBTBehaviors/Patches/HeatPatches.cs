
using BattleTech;
using BattleTech.UI;
using Harmony;
using HBS;
using Localize;
using SVGImporter;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CBTBehaviors {

    public static class HeatPatches {

        [HarmonyPatch(typeof(Mech), "Init")]
        public static class Mech_Init {
            public static void Postfix(Mech __instance) {
                Mod.Log.Trace("M:I entered.");
                __instance.StatCollection.AddStatistic<int>(ModStats.TurnsOverheated, 0);
            }
        }

        [HarmonyPatch(typeof(Mech), "OnActivationEnd")]
        public static class Mech_OnActivationEnd {
            private static void Prefix(Mech __instance, string sourceID, int stackItemID) {

                if (__instance.IsOverheated) {
                    CBTPilotingRules rules = new CBTPilotingRules(__instance.Combat);
                    float gutsTestChance = rules.GetGutsModifier(__instance);
                    float skillRoll = __instance.Combat.NetworkRandom.Float();
                    float ammoRoll = __instance.Combat.NetworkRandom.Float();

                    int turnsOverheated = __instance.StatCollection.ContainsStatistic(ModStats.TurnsOverheated) ? __instance.StatCollection.GetValue<int>("TurnsOverheated") : 0;
                    float shutdownPercentage = HeatHelper.GetShutdownPercentageForTurn(turnsOverheated);
                    float ammoExplosionPercentage = HeatHelper.GetAmmoExplosionPercentageForTurn(turnsOverheated);

                    Mod.Log.Debug($"Mech:{CombatantHelper.LogLabel(__instance)} is overheated for {turnsOverheated} turns. Checking shutdown override.");
                    Mod.Log.Debug($"  Guts -> skill: {__instance.SkillGuts}  divisor: {Mod.Config.GutsDivisor}  bonus: {gutsTestChance}");
                    Mod.Log.Debug($"  Skill roll: {skillRoll} plus guts roll: {skillRoll + gutsTestChance}  target: {shutdownPercentage}");
                    Mod.Log.Debug($"  Ammo roll: {ammoRoll} plus guts roll: {ammoRoll + gutsTestChance}  target: {ammoExplosionPercentage}");

                    if (Mod.Config.UseGuts) {
                        ammoRoll = ammoRoll + gutsTestChance;
                        skillRoll = skillRoll + gutsTestChance;
                    }

                    MultiSequence sequence = new MultiSequence(__instance.Combat);
                    sequence.SetCamera(CameraControl.Instance.ShowDeathCam(__instance, false, -1f), 0);

                    if (HeatHelper.CanAmmoExplode(__instance)) {
                        if (ammoRoll < ammoExplosionPercentage) {
                            __instance.Combat.MessageCenter.PublishMessage(new FloatieMessage(__instance.GUID, __instance.GUID, "Ammo Overheated!", FloatieMessage.MessageNature.CriticalHit));

                            var ammoBox = __instance.ammoBoxes.Where(box => box.CurrentAmmo > 0).OrderByDescending(box => box.CurrentAmmo / box.AmmoCapacity).FirstOrDefault();
                            if (ammoBox != null) {
                                WeaponHitInfo fakeHit = new WeaponHitInfo(stackItemID, -1, -1, -1, string.Empty, string.Empty, -1, null, null, null, null, null, null, null, new AttackDirection[] { AttackDirection.None }, null, null, null);
                                ammoBox.DamageComponent(fakeHit, ComponentDamageLevel.Destroyed, true);
                            }

                            return;
                        }

                        sequence.AddChildSequence(new ShowActorInfoSequence(__instance, "Ammo Explosion Avoided!", FloatieMessage.MessageNature.Debuff, true), sequence.ChildSequenceCount - 1);
                    }

                    if (!__instance.IsPastMaxHeat) {
                        if (skillRoll < shutdownPercentage) {
                            Mod.Log.Debug(string.Format("Skill Check Failed! Initiating Shutdown"));

                            MechEmergencyShutdownSequence mechShutdownSequence = new MechEmergencyShutdownSequence(__instance);
                            sequence.AddChildSequence(mechShutdownSequence, sequence.ChildSequenceCount - 1);

                            __instance.StatCollection.Set<int>("TurnsOverheated", 0);
                        } else {
                            Mod.Log.Debug(string.Format("Skill Check Succeeded!"));

                            sequence.AddChildSequence(new ShowActorInfoSequence(__instance, "Shutdown Override Successful!", FloatieMessage.MessageNature.Buff, true), sequence.ChildSequenceCount - 1);

                            turnsOverheated += 1;
                            __instance.StatCollection.Set<int>("TurnsOverheated", turnsOverheated);
                        }
                    }

                    sequence.AddChildSequence(new DelaySequence(__instance.Combat, 2f), sequence.ChildSequenceCount - 1);

                    __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(sequence));
                } else {
                    int turnsOverheated = __instance.StatCollection.GetValue<int>("TurnsOverheated");
                    if (turnsOverheated > 0) {
                        __instance.StatCollection.Set<int>("TurnsOverheated", 0);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Mech))]
        [HarmonyPatch("MoveMultiplier", MethodType.Getter)]
        public static class Mech_MoveMultiplier_Get {
            public static void Postfix(Mech __instance, ref float __result) {
                Mod.Log.Trace("M:MM:GET entered.");
                int turnsOverheated = __instance.StatCollection.GetValue<int>("TurnsOverheated");

                if (__instance.IsOverheated && turnsOverheated > 0) {
                    float movePenalty = HeatHelper.GetOverheatedMovePenaltyForTurn(turnsOverheated);
                    Mod.Log.Debug($"Mech {CombatantHelper.LogLabel(__instance)} has overheated, applying movement penalty:{movePenalty}");
                    __result -= movePenalty;
                }
            }
        }

        [HarmonyPatch(typeof(ToHit), "GetHeatModifier")]
        public static class ToHit_GetHeatModifier {
            public static void Postfix(ToHit __instance, ref float __result, AbstractActor attacker) {
                Mod.Log.Trace("TH:GHM entered.");
                if (attacker is Mech mech && mech.StatCollection.ContainsStatistic(ModStats.TurnsOverheated)) {

                    int turnsOverheated = mech.StatCollection.GetValue<int>(ModStats.TurnsOverheated);
                    if (turnsOverheated > 0) {
                        float modifier = HeatHelper.GetHeatToHitModifierForTurn(turnsOverheated);
                        __result = modifier;
                        Mod.Log.Debug($"Mech {CombatantHelper.LogLabel(mech)} has overheat ToHit modifier:{modifier}");
                    } else {
                        __result = 0f;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CombatHUDStatusPanel), "ShowShutDownIndicator", null)]
        public static class CombatHUDStatusPanel_ShowShutDownIndicator {
            public static bool Prefix(CombatHUDStatusPanel __instance) {
                Mod.Log.Trace("CHUBSP:SSDI:PRE entered.");
                return false;
            }

            public static void Postfix(CombatHUDStatusPanel __instance, Mech mech) {
                Mod.Log.Trace("CHUBSP:SSDI:POST entered.");

                var type = __instance.GetType();
                MethodInfo methodInfo = type.GetMethod("ShowDebuff", (BindingFlags.NonPublic | BindingFlags.Instance), null, 
                    new Type[] { typeof(SVGAsset), typeof(Text), typeof(Text), typeof(Vector3), typeof(bool) }, new ParameterModifier[5]);

                int turnsOverheated = mech.StatCollection.ContainsStatistic(ModStats.TurnsOverheated) ? mech.StatCollection.GetValue<int>("TurnsOverheated") : 0;

                if (mech.IsShutDown) {
                    Mod.Log.Debug($"Mech:{CombatantHelper.LogLabel(mech)} is shutdown.");
                    methodInfo.Invoke(__instance, new object[] { LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.StatusShutDownIcon,
                        new Text("SHUT DOWN", new object[0]), new Text("This target is easier to hit, and Called Shots can be made against this target.", new object[0]),
                        __instance.defaultIconScale, false });
                } else if (mech.IsOverheated) {
                    float shutdownChance = HeatHelper.GetShutdownPercentageForTurn(turnsOverheated);
                    float ammoExplosionChance = HeatHelper.GetAmmoExplosionPercentageForTurn(turnsOverheated);
                    Mod.Log.Debug($"Mech:{CombatantHelper.LogLabel(mech)} is overheated, shutdownChance:{shutdownChance}% ammoExplosionChance:{ammoExplosionChance}%");

                    string descr = string.Format("This unit may trigger a Shutdown at the end of the turn unless heat falls below critical levels.\nShutdown Chance: {0:P2}\nAmmo Explosion Chance: {1:P2}", 
                        shutdownChance, ammoExplosionChance);
                    methodInfo.Invoke(__instance, new object[] { LazySingletonBehavior<UIManager>.Instance.UILookAndColorConstants.StatusOverheatingIcon,
                        new Text("OVERHEATING", new object[0]), new Text(descr, new object[0]), __instance.defaultIconScale, false });
                }
            }
        }
        [HarmonyPatch(typeof(Mech))]
        [HarmonyPatch("ApplyHeatDamage")]
        public static class Heat_Damage_Chance
        {
            private static void Prefix(Mech __instance, ref float damageAmount)
            {
                int turnsOverheated = __instance.StatCollection.GetValue<int>("TurnsOverheated");
                float heatDamageRoll = __instance.Combat.NetworkRandom.Float();
                float heatDamagePercentage =  HeatHelper.GetHeatDamagePercentageForTurn(turnsOverheated);
                var rng = new System.Random();
                damageAmount = (float)(damageAmount * rng.NextDouble());

                if (heatDamageRoll > heatDamagePercentage)
                    damageAmount = 0;
            }
        }


    }
}
