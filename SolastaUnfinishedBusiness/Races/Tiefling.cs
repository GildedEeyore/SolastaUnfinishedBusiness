using static SolastaUnfinishedBusiness.Api.DatabaseHelper;

namespace SolastaUnfinishedBusiness.Races;

internal static class RaceTieflingInfernalLegacyBuilder
{
    // Infernal Legacy grants Fire Bolt instead of Produce Flame
    internal static void Load()
    {
        FeatureDefinitionCastSpells.CastSpellTiefling.SpellListDefinition.SpellsByLevel[0].Spells = [SpellDefinitions.FireBolt];
    }
}
