using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Api;

namespace SolastaUnfinishedBusiness.Behaviors.Specific;

internal static class UpcastConjureElementalAndFey
{
    // subspells that only show when cast with the slot level they were built for (Summon Elemental, Summon Undead)
    private static readonly Dictionary<SpellDefinition, int> SlotLevelBySubspell = [];

    private static List<SpellDefinition> _filteredSubspells;

    internal static void RegisterSlotLevelSubspell(SpellDefinition subspell, int slotLevel)
    {
        SlotLevelBySubspell[subspell] = slotLevel;
    }

    /**
     * Patch implementation
     * Replaces subspell activation with custom code for upcasted elementals/fey
     */
    internal static bool CheckSubSpellActivated(SubspellSelectionModal __instance, int index)
    {
        if (_filteredSubspells is not { Count: > 0 })
        {
            return true;
        }

        if (_filteredSubspells.Count <= index)
        {
            return true;
        }

        if (__instance.deviceFunctionEngaged != null)
        {
            // the game expects an index into the full subspell list when using a scroll or other device
            __instance.deviceFunctionEngaged(
                __instance.guiCharacter, __instance.rulesetItemDevice, __instance.rulesetDeviceFunction, 0,
                __instance.masterSpell.SubspellsList.IndexOf(_filteredSubspells[index]));
        }
        else
        {
            __instance.spellCastEngaged?.Invoke(
                __instance.spellRepertoire, _filteredSubspells[index], __instance.slotLevel);
        }

        __instance.Hide();

        _filteredSubspells.Clear();

        return false;
    }

    /**
     * Patch implementation
     * Replaces calls to masterSpell.SubspellsList getter when using a scroll or other device, so spells with one
     * subspell per slot level only show the ones for the level the device casts at
     */
    [CanBeNull]
    internal static List<SpellDefinition> DeviceSubspellsList([NotNull] SpellDefinition masterSpell)
    {
        var subspellsList = masterSpell.SubspellsList;

        _filteredSubspells = null;

        if (subspellsList == null || !subspellsList.Any(SlotLevelBySubspell.ContainsKey))
        {
            return subspellsList;
        }

        // devices cast at the spell's own level (RulesetEffectSpell uses SpellLevel + added charges)
        _filteredSubspells = subspellsList
            .Where(x => SlotLevelBySubspell.TryGetValue(x, out var level) && level == masterSpell.SpellLevel)
            .ToList();

        return _filteredSubspells;
    }

    /**
     * Patch implementation
     * Replaces calls to masterSpell.SubspellsList getter with custom method that adds extra options for upcasted elementals/fey
     */
    [CanBeNull]
    internal static List<SpellDefinition> SubspellsList([NotNull] SpellDefinition masterSpell, int slotLevel)
    {
        var subspellsList = masterSpell.SubspellsList;

        _filteredSubspells = null;

        if (subspellsList != null && subspellsList.Any(SlotLevelBySubspell.ContainsKey))
        {
            _filteredSubspells = subspellsList
                .Where(x => SlotLevelBySubspell.TryGetValue(x, out var level) && level == slotLevel)
                .ToList();

            return _filteredSubspells;
        }

        var mySlotLevel =
            masterSpell.Name == DatabaseHelper.SpellDefinitions.ConjureElemental.Name ||
            masterSpell.Name == DatabaseHelper.SpellDefinitions.ConjureFey.Name
                ? slotLevel
                : -1;

        if (!Main.Settings.EnableUpcastConjureElementalAndFey || mySlotLevel < 0 || subspellsList == null ||
            subspellsList.Count == 0)
        {
            return subspellsList;
        }

        var subspellsGroupedAndFilteredByCR = subspellsList
            .Select(s =>
                new
                {
                    SpellDefinition = s,
                    s.EffectDescription
                        .GetFirstFormOfType(EffectForm.EffectFormType.Summon)
                        .SummonForm
                        .MonsterDefinitionName
                })
            .Select(s => new
            {
                s.SpellDefinition,
                s.MonsterDefinitionName,
                ChallengeRating =
                    DatabaseHelper.TryGetDefinition<MonsterDefinition>(s.MonsterDefinitionName,
                        out var monsterDefinition)
                        ? monsterDefinition.ChallengeRating
                        : int.MaxValue
            })
            .GroupBy(s => s.ChallengeRating)
            .Select(g => new
            {
                ChallengeRating = g.Key,
                SpellDefinitions = g.Select(s => s.SpellDefinition)
                    .OrderBy(s => Gui.Localize(s.GuiPresentation.Title))
            })
            .Where(s => s.ChallengeRating <= mySlotLevel)
            .OrderByDescending(s => s.ChallengeRating);

        var allOrMostPowerful = Main.Settings.OnlyShowMostPowerfulUpcastConjuredElementalOrFey
            ? subspellsGroupedAndFilteredByCR.Take(1)
            : subspellsGroupedAndFilteredByCR;

        _filteredSubspells = allOrMostPowerful.SelectMany(s => s.SpellDefinitions).ToList();

        return _filteredSubspells;
    }
}
