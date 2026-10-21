using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using JetBrains.Annotations;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.SpellDefinitions;

namespace SolastaUnfinishedBusiness.Patches;

[UsedImplicitly]
public static class RulesetCharacterEffectProxyPatcher
{
    //PATCH: Wall of Force can't be dispelled by Dispel Magic
    [HarmonyPatch(typeof(RulesetCharacterEffectProxy), nameof(RulesetCharacterEffectProxy.DissipateSpell))]
    [SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
    [UsedImplicitly]
    public static class DissipateSpell_Patch
    {
        [UsedImplicitly]
        public static bool Prefix(RulesetActor sourceEntity, RulesetEffectSpell activeSpell)
        {
            if (activeSpell.SpellDefinition != WallOfForce)
            {
                return true;
            }

            // same feedback vanilla gives when the dispel roll fails
            sourceEntity.SpellDissipated?.Invoke(sourceEntity, null, activeSpell.SpellDefinition, false);

            return false;
        }
    }
}
