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

    // replaces the vanilla wizard cantrip choice with a fixed list, the same way Dark Elf Magic works
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

        // only High Elf itself changes; Half-Elf (High) and other races that reuse CastSpellElfHigh keep it
        var featureUnlocks = ElfHigh.FeatureUnlocks;

        foreach (var index in Enumerable.Range(0, featureUnlocks.Count)
                     .Where(i => featureUnlocks[i].FeatureDefinition == CastSpellElfHigh))
        {
            featureUnlocks[index] = new FeatureUnlockByLevel(CastSpellElfHighMagic, featureUnlocks[index].Level);
        }
    }
}
