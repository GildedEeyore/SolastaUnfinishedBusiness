using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Spells;

namespace SolastaUnfinishedBusiness.Patches;

[UsedImplicitly]
public static class DeathTimerGroupPatcher
{
    [HarmonyPatch(typeof(DeathTimerGroup), nameof(DeathTimerGroup.UpdateDeathTimer))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class UpdateDeathTimer_Patch
    {
        [UsedImplicitly]
        public static void Prefix(DeathTimerGroup __instance)
        {
            //PATCH: supports Gentle Repose, whose days don't count against the time limit of revive spells
            SpellBuilders.PauseDeathClockUnderGentleRepose(__instance.character?.RulesetCharacter);
        }
    }
}
