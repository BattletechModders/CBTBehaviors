
using BattleTech;
using BattleTech.UI;
using Harmony;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CBTBehaviors {

    public static class MovementPatches {
        [HarmonyPatch(typeof(EncounterLayerData))]
        [HarmonyPatch("ContractInitialize")]
        public static class EncounterLayerData_ContractInitialize {
            static void Prefix(EncounterLayerData __instance) {
                Mod.Log.Trace("ELD:CI entered");
                try {
                    __instance.turnDirectorBehavior = TurnDirectorBehaviorType.AlwaysInterleaved;
                } catch (Exception e) {
                    Mod.Log.Info($"Failed to set behavior to interleaved due to:{e.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(ToHit), "GetAllModifiers")]
        public static class ToHit_GetAllModifiers {
            private static void Postfix(ToHit __instance, ref float __result, AbstractActor attacker, Weapon weapon, ICombatant target,
                Vector3 attackPosition, Vector3 targetPosition, LineOfFireLevel lofLevel, bool isCalledShot) {
                Mod.Log.Trace("TH:GAM entered");

                if (attacker.HasMovedThisRound && attacker.JumpedLastRound && attacker.SkillTactics != 10) {
                    __result = __result + (float)Mod.Config.ToHitSelfJumped;
                }
            }
        }

        [HarmonyPatch(typeof(ToHit), "GetAllModifiersDescription")]
        public static class ToHit_GetAllModifiersDescription {
            private static void Postfix(ToHit __instance, ref string __result, AbstractActor attacker, Weapon weapon, ICombatant target,
                Vector3 attackPosition, Vector3 targetPosition, LineOfFireLevel lofLevel, bool isCalledShot) {
                Mod.Log.Trace("TH:GAMD entered");

                if (attacker.HasMovedThisRound && attacker.JumpedLastRound) {
                    __result = string.Format("{0}JUMPED {1:+#;-#}; ", __result, Mod.Config.ToHitSelfJumped);
                }
            }
        }

        [HarmonyPatch(typeof(CombatHUDWeaponSlot), "SetHitChance", new Type[] { typeof(ICombatant) })]
        public static class CombatHUDWeaponSlot_SetHitChance {

            private static void Postfix(CombatHUDWeaponSlot __instance, ICombatant target) {
                Mod.Log.Trace("CHUDWS:SHC entered");

                AbstractActor actor = __instance.DisplayedWeapon.parent;
                var _this = Traverse.Create(__instance);

                if (actor.HasMovedThisRound && actor.JumpedLastRound && actor.SkillTactics != 10) {
                    Traverse addToolTipDetailT = Traverse.Create(__instance).Method("AddToolTipDetail", "JUMPED SELF", Mod.Config.ToHitSelfJumped);
                    Mod.Log.Debug($"Invoking addToolTipDetail for: JUMPED SELF = {Mod.Config.ToHitSelfJumped}");
                    addToolTipDetailT.GetValue();
                }
            }
        }

        //[HarmonyPatch(typeof(AbstractActor), "InitEffectStats")]
        //public static class AbstractActor_InitEffectStats {
        //    private static void Postfix(AbstractActor __instance) {
        //        Mod.Log.Trace("AA:IES entered");
        //        __instance.StatCollection.Set(ModStats.CanShootAfterSpring, true);
        //    }
        //}

        [HarmonyPatch(typeof(AbstractActor), "ResolveAttackSequence", null)]
        public static class AbstractActor_ResolveAttackSequence_Patch {
            
            private static bool Prefix(AbstractActor __instance) {
                Mod.Log.Trace("AA:RAS:PRE entered");
                return false;
            }

            private static void Postfix(AbstractActor __instance, string sourceID, int sequenceID, int stackItemID, AttackDirection attackDirection) {
                Mod.Log.Trace("AA:RAS:POST entered");

                //int evasivePipsCurrent = __instance.EvasivePipsCurrent;
                //__instance.ConsumeEvasivePip(true);
                //int evasivePipsCurrent2 = __instance.EvasivePipsCurrent;
                //if (evasivePipsCurrent2 < evasivePipsCurrent && !__instance.IsDead && !__instance.IsFlaggedForDeath) {
                //    __instance.Combat.MessageCenter.PublishMessage(new FloatieMessage(__instance.GUID, __instance.GUID, "-1 EVASION", FloatieMessage.MessageNature.Debuff));
                //}

                AttackDirector.AttackSequence attackSequence = __instance.Combat.AttackDirector.GetAttackSequence(sequenceID);
                if (attackSequence != null) {
                    if (!attackSequence.GetAttackDidDamage(__instance.GUID)) {
                        return;
                    }
                    List<Effect> list = __instance.Combat.EffectManager.GetAllEffectsTargeting(__instance).FindAll((Effect x) => x.EffectData.targetingData.effectTriggerType == EffectTriggerType.OnDamaged);
                    for (int i = 0; i < list.Count; i++) {
                        list[i].OnEffectTakeDamage(attackSequence.attacker, __instance);
                    }
                    if (attackSequence.isMelee) {
                        int value = attackSequence.attacker.StatCollection.GetValue<int>(ModStats.MeleeHitPushBackPhases);
                        if (value > 0) {
                            for (int j = 0; j < value; j++) {
                                __instance.ForceUnitOnePhaseDown(sourceID, stackItemID, false);
                            }
                        }
                    }
                }

            }

        }
    }
}
