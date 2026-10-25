using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Api.GameExtensions;
using SolastaUnfinishedBusiness.Api.Helpers;
using SolastaUnfinishedBusiness.Behaviors;
using TA;
using UnityEngine;
using static RuleDefinitions;

namespace SolastaUnfinishedBusiness.Patches;

[UsedImplicitly]
public static class GameLocationEnvironmentManagerPatcher
{
    [HarmonyPatch(typeof(GameLocationEnvironmentManager),
        nameof(GameLocationEnvironmentManager.UpdateAffectedCharactersOfGlobalEffects))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class UpdateAffectedCharactersOfGlobalEffects_Patch
    {
        [NotNull]
        [UsedImplicitly]
        public static IEnumerable<CodeInstruction> Transpiler([NotNull] IEnumerable<CodeInstruction> instructions)
        {
            //PATCH: Support for marker to make recurring effect that happen on turn start happened only on caster's turn

            var validate = new Func<
                RulesetEffect,
                RecurrentEffect,
                ulong
            >(ValidateApplicationOfEffect).Method;

            var method = typeof(RulesetEffect).GetProperty(nameof(RulesetEffect.SourceGuid))!.GetGetMethod();

            return instructions.ReplaceCode(instruction => instruction.Calls(method),
                -1, "GameLocationEnvironmentManagerPatcher.UpdateAffectedCharactersOfGlobalEffects",
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Call, validate));
        }

        private static ulong ValidateApplicationOfEffect(RulesetEffect effect, RecurrentEffect context)
        {
            var guid = effect.SourceGuid;

            if (context != RecurrentEffect.OnTurnStart)
            {
                return guid;
            }

            if (!effect.SourceDefinition.HasSubFeatureOfType<RestrictRecurrentEffectsOnSelfTurnOnly>())
            {
                return guid;
            }

            var active = Gui.Battle?.activeContender?.RulesetCharacter?.Guid ?? 0;
            if (guid > 0 && guid != active)
            {
                return 0;
            }

            return guid;
        }
    }

    [HarmonyPatch(typeof(GameLocationEnvironmentManager),
        nameof(GameLocationEnvironmentManager.ApplyEffectToCharacterList))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class ApplyEffectToCharacterList_Patch
    {
        [UsedImplicitly]
        public static void Prefix(
            GameLocationEnvironmentManager __instance,
            List<GameLocationCharacter> characterList,
            RulesetEffect effect,
            GameLocationCharacter exclusiveCharacter,
            bool applyEffect)
        {
            //PATCH: support for `ApplyRecurrentEffectWhenSourceMoves` (Cacophonic Shield)
            //vanilla only applies OnEnter to creatures that move into the area, not when the area moves onto them
            if (!applyEffect ||
                exclusiveCharacter == null ||
                exclusiveCharacter.Guid != effect.SourceGuid ||
                (!exclusiveCharacter.MovingToDestination && !exclusiveCharacter.Pushed) ||
                (effect.EffectDescription.RecurrentEffect & RecurrentEffect.OnEnter) == 0 ||
                !effect.SourceDefinition.HasSubFeatureOfType<ApplyRecurrentEffectWhenSourceMoves>())
            {
                return;
            }

            foreach (var character in characterList.ToArray())
            {
                if (character == exclusiveCharacter ||
                    character.MovingToDestination ||
                    character.Pushed ||
                    character.AffectingGlobalEffects.Contains(effect) ||
                    !__instance.affectedCharacters.Contains(character))
                {
                    continue;
                }

                effect.ApplyEffectOnCharacter(character.RulesetActor, true, character.LocationPosition);

                if (effect.Terminated)
                {
                    break;
                }
            }
        }
    }

    //BUGFIX: removes LOG and TRACE message
    [HarmonyPatch(typeof(GameLocationEnvironmentManager),
        nameof(GameLocationEnvironmentManager.RegisterGlobalActiveEffect))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class RegisterGlobalActiveEffect_Patch
    {
        [NotNull]
        [UsedImplicitly]
        public static IEnumerable<CodeInstruction> Transpiler([NotNull] IEnumerable<CodeInstruction> instructions)
        {
            var logErrorMethod = typeof(Trace).GetMethod("LogError", BindingFlags.Public | BindingFlags.Static,
                Type.DefaultBinder, [typeof(string)], null);
            var logExceptionMethod = typeof(Trace).GetMethod("LogException", BindingFlags.Public | BindingFlags.Static);

            return instructions
                .ReplaceCalls(
                    logErrorMethod, "GameLocationEnvironmentManager.RegisterGlobalActiveEffect.LogError",
                    new CodeInstruction(OpCodes.Pop))
                .ReplaceCalls(
                    logExceptionMethod, "GameLocationEnvironmentManager.RegisterGlobalActiveEffect.LogException",
                    new CodeInstruction(OpCodes.Pop));
        }
    }

    [HarmonyPatch(typeof(GameLocationEnvironmentManager),
        nameof(GameLocationEnvironmentManager.ComputePushDestination))]
    [HarmonyPatch([
        typeof(Vector3), //sourceCenter
        typeof(GameLocationCharacter), //target
        typeof(int), //distance
        typeof(bool), //reverse
        typeof(IGameLocationPositioningService), //positioningService
        typeof(int3), //destination
        typeof(Vector3) //direction
    ], [
        ArgumentType.Normal, //sourceCenter
        ArgumentType.Normal, //target
        ArgumentType.Normal, //distance
        ArgumentType.Normal, //reverse
        ArgumentType.Normal, //positioningService
        ArgumentType.Out, //destination
        ArgumentType.Out //direction
    ])]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class ComputePushDestination_Patch
    {
        [UsedImplicitly]
        public static bool Prefix(
            ref bool __result,
            Vector3 sourceCenter,
            GameLocationCharacter target,
            int distance,
            bool reverse,
            IGameLocationPositioningService positioningService,
            ref int3 destination,
            ref Vector3 direction)
        {
            if (!Main.Settings.EnablePullPushOnVerticalDirection)
            {
                return true;
            }

            //PATCH: allow push and pull motion effects to change vertical position of a target
            __result = MotionContext.ComputePushDestination(
                sourceCenter, target, distance, reverse, positioningService, ref destination, ref direction);

            return false;
        }
    }

    //PATCH: vanilla never applies MoveBlocker topology forms so this adds it to the list (Wall of Force)
    [HarmonyPatch(typeof(GameLocationEnvironmentManager),
        nameof(GameLocationEnvironmentManager.TopologyChangeInProgress))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class TopologyChangeInProgress_Patch
    {
        [UsedImplicitly]
        public static bool Prefix(
            GameLocationEnvironmentManager __instance,
            List<GameSector> gameSectors,
            ref BoxInt boxInt,
            List<TopologyForm.Type> impacterTopologyFlags)
        {
            // same as vanilla with the MoveBlocker step added before the trackers are cleaned up
            __instance.RegisterPreviousCoveredCells();
            __instance.ApplyTopologyChangeOfGlobalEffects(
                gameSectors, ref boxInt, impacterTopologyFlags, TopologyForm.Type.SightBlocker);
            __instance.ApplyTopologyChangeOfGlobalEffects(
                gameSectors, ref boxInt, impacterTopologyFlags, TopologyForm.Type.ProjectileBlocker);
            __instance.ApplyTopologyChangeOfGlobalEffects(
                gameSectors, ref boxInt, impacterTopologyFlags, TopologyForm.Type.DifficultThrough);
            __instance.ApplyTopologyChangeOfGlobalEffects(
                gameSectors, ref boxInt, impacterTopologyFlags, TopologyForm.Type.SightImpaired);
            __instance.ApplyTopologyChangeOfGlobalEffects(
                gameSectors, ref boxInt, impacterTopologyFlags, TopologyForm.Type.DangerousZone);
            __instance.ApplyTopologyChangeOfGlobalEffects(
                gameSectors, ref boxInt, impacterTopologyFlags, TopologyForm.Type.MoveBlocker);
            __instance.ApplyTopologyChangeOfGlobalEffects(
                gameSectors, ref boxInt, impacterTopologyFlags, TopologyForm.Type.None);
            __instance.FinishTopologyChange();

            return false;
        }
    }
}
