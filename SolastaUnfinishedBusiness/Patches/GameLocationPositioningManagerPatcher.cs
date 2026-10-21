using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Api.Helpers;
using TA;

namespace SolastaUnfinishedBusiness.Patches;

[UsedImplicitly]
public static class GameLocationPositioningManagerPatcher
{
    //PATCH: avoids a trace message when party greater than 4 (PARTYSIZE)
    [HarmonyPatch(typeof(GameLocationPositioningManager), nameof(GameLocationPositioningManager.CharacterMoved),
        typeof(GameLocationCharacter),
        typeof(int3), typeof(int3), typeof(RulesetActor.SizeParameters), typeof(RulesetActor.SizeParameters))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class CharacterMoved_Patch
    {
        [UsedImplicitly]
        public static IEnumerable<CodeInstruction> Transpiler([NotNull] IEnumerable<CodeInstruction> instructions)
        {
            var logErrorMethod = typeof(Trace).GetMethod("LogError", BindingFlags.Public | BindingFlags.Static,
                Type.DefaultBinder, [typeof(string)], null);

            return instructions.ReplaceCalls(logErrorMethod, "GameLocationPositioningManager.CharacterMoved",
                new CodeInstruction(OpCodes.Pop));
        }

        [UsedImplicitly]
        public static bool Prefix(GameLocationCharacter character,
            int3 oldPosition,
            int3 newPosition,
            RulesetActor.SizeParameters oldSize,
            RulesetActor.SizeParameters newSize)
        {
            //PATCH: process location change only if position or size has changed
            //FIX: fixes Spike Growth and similar spells triggering twice on last tile of movement
            return oldPosition != newPosition
                   || oldSize.minExtent != newSize.minExtent
                   || oldSize.maxExtent != newSize.maxExtent;
        }
    }

    //PATCH: cells flagged by a MoveBlocker topology form (Wall of Force) can't be entered
    [HarmonyPatch(typeof(GameLocationPositioningManager),
        nameof(GameLocationPositioningManager.CanPlaceCharacterImpl))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class CanPlaceCharacterImpl_Patch
    {
        [UsedImplicitly]
        public static void Postfix(
            GameLocationPositioningManager __instance,
            ref bool __result,
            GameLocationCharacter character,
            RulesetActor.SizeParameters sizeParams,
            int3 position)
        {
            if (__result &&
                IsMoveBlocked(__instance, character,
                    new BoxInt(position + sizeParams.minExtent, position + sizeParams.maxExtent)))
            {
                __result = false;
            }
        }
    }

    //PATCH: cells flagged by a MoveBlocker topology form (Wall of Force) can't be entered
    [HarmonyPatch(typeof(GameLocationPositioningManager),
        nameof(GameLocationPositioningManager.CanPlaceSmallCharacterImpl))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class CanPlaceSmallCharacterImpl_Patch
    {
        [UsedImplicitly]
        public static void Postfix(
            GameLocationPositioningManager __instance,
            ref bool __result,
            GameLocationCharacter character,
            int3 position)
        {
            if (__result && IsMoveBlocked(__instance, character, position))
            {
                __result = false;
            }
        }
    }

    private static bool IsMoveBlocked(
        GameLocationPositioningManager positioningManager,
        GameLocationCharacter character,
        BoxInt boxInt)
    {
        foreach (var position in boxInt.EnumerateAllPositionsWithin())
        {
            if (IsMoveBlocked(positioningManager, character, position))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsMoveBlocked(
        GameLocationPositioningManager positioningManager,
        GameLocationCharacter character,
        int3 position)
    {
        var gridAccessor =
            new GridAccessor(positioningManager.GameLocationService, positioningManager.cachedSectorForHelpers);

        return (gridAccessor.RuntimeFlags(position) & CellFlags.Runtime.DynamicMoveBlocker) != 0 &&
               // effect proxies, including the wall's own proxy, are never blocked
               character?.RulesetActor is not RulesetCharacterEffectProxy &&
               // a creature that couldn't be pushed out of the wall can still leave the cells it stands on
               (character == null || !character.LocationBoundingBox.Contains(position));
    }
}
