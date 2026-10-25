using System.Linq;
using SolastaUnfinishedBusiness.Builders;
using SolastaUnfinishedBusiness.Builders.Features;
using SolastaUnfinishedBusiness.Models;
using static RuleDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.CharacterRaceDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.FeatureDefinitionCastSpells;

namespace SolastaUnfinishedBusiness.Races;

internal static class SubraceHighElfBuilder
{
    internal static FeatureDefinitionCastSpell CastSpellElfHighMagic { get; private set; }

    internal static FeatureDefinitionCastSpell CastSpellHalfElfHighMagic { get; private set; }

    // replaces the vanilla wizard cantrip choice with fixed lists, the same way Dark Elf Magic works
    internal static void Load()
    {
        CastSpellElfHighMagic = FeatureDefinitionCastSpellBuilder
            .Create("CastSpellElfHighMagic")
            .SetGuiPresentation(Category.Feature)
            .SetSpellCastingOrigin(FeatureDefinitionCastSpell.CastingOrigin.Race)
            .SetSpellCastingAbility(AttributeDefinitions.Intelligence)
            .SetSpellKnowledge(SpellKnowledge.FixedList)
            .SetSpellReadyness(SpellReadyness.AllKnown)
            .SetSlotsRecharge(RechargeRate.LongRest)
            .SetSlotsPerLevel(SharedSpellsContext.RaceCastingSlots)
            .SetReplacedSpells(1, 0)
            .SetKnownCantrips(1, 1, FeatureDefinitionCastSpellBuilder.CasterProgression.Flat)
            .SetSpellList(
                SpellListDefinitionBuilder
                    .Create("SpellListElfHighMagic")
                    .SetGuiPresentationNoContent(true)
                    .ClearSpells()
                    .SetSpellsAtLevel(0, SpellDefinitions.Sparkle)
                    .SetSpellsAtLevel(1, SpellDefinitions.DetectMagic)
                    .SetSpellsAtLevel(2, SpellDefinitions.MistyStep)
                    .FinalizeSpells()
                    .AddToDB())
            .AddToDB();

        // Half-Elf (High) only gets the cantrip
        CastSpellHalfElfHighMagic = FeatureDefinitionCastSpellBuilder
            .Create("CastSpellHalfElfHighMagic")
            .SetGuiPresentation(Category.Feature)
            .SetSpellCastingOrigin(FeatureDefinitionCastSpell.CastingOrigin.Race)
            .SetSpellCastingAbility(AttributeDefinitions.Intelligence)
            .SetSpellKnowledge(SpellKnowledge.FixedList)
            .SetSpellReadyness(SpellReadyness.AllKnown)
            .SetSlotsRecharge(RechargeRate.LongRest)
            .SetSlotsPerLevel(SharedSpellsContext.RaceEmptyCastingSlots)
            .SetKnownCantrips(1, 1, FeatureDefinitionCastSpellBuilder.CasterProgression.Flat)
            .SetSpellList(
                SpellListDefinitionBuilder
                    .Create("SpellListHalfElfHighMagic")
                    .SetGuiPresentationNoContent(true)
                    .ClearSpells()
                    .SetSpellsAtLevel(0, SpellDefinitions.Sparkle)
                    .FinalizeSpells()
                    .AddToDB())
            .AddToDB();

        // Bolgrif and Arcane Archer copy CastSpellElfHigh, so the vanilla feature itself stays untouched
        ReplaceCastSpellElfHigh(ElfHigh, CastSpellElfHighMagic);
        ReplaceCastSpellElfHigh(RaceHalfElfBuilder.RaceHalfElfHighVariant, CastSpellHalfElfHighMagic);
    }

    private static void ReplaceCastSpellElfHigh(
        CharacterRaceDefinition characterRaceDefinition, FeatureDefinitionCastSpell castSpell)
    {
        var featureUnlocks = characterRaceDefinition.FeatureUnlocks;

        foreach (var index in Enumerable.Range(0, featureUnlocks.Count)
                     .Where(i => featureUnlocks[i].FeatureDefinition == CastSpellElfHigh))
        {
            featureUnlocks[index] = new FeatureUnlockByLevel(castSpell, featureUnlocks[index].Level);
        }
    }
}
