namespace SolastaUnfinishedBusiness.Behaviors;

// adds the spells known by a feat repertoire as auto prepared spells so they can also be cast with spell slots
public class AutoPreparedSpellsFromFeatRepertoire(FeatureDefinitionCastSpell castSpell)
{
    public FeatureDefinitionCastSpell CastSpell { get; } = castSpell;
}
