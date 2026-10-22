using System.Collections;
using System.Linq;
using SolastaUnfinishedBusiness.Api.GameExtensions;
using SolastaUnfinishedBusiness.Builders;
using SolastaUnfinishedBusiness.Builders.Features;
using SolastaUnfinishedBusiness.Interfaces;
using static RuleDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.CharacterRaceDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.FeatureDefinitionAdditionalDamages;

namespace SolastaUnfinishedBusiness.Races;

// replaces the official Half-Orc Savage Attacks trait with the 2024 Adrenaline Rush trait
internal static class RaceHalfOrcBuilder
{
    private const string Name = "HalfOrc";

    internal static void Load()
    {
        var powerAdrenalineRush = BuildAdrenalineRush();
        var featureUnlocks = HalfOrc.FeatureUnlocks;

        foreach (var index in Enumerable.Range(0, featureUnlocks.Count)
                     .Where(i => featureUnlocks[i].FeatureDefinition == AdditionalDamageHalfOrcSavageAttacks))
        {
            featureUnlocks[index] = new FeatureUnlockByLevel(powerAdrenalineRush, featureUnlocks[index].Level);
        }
    }

    private static FeatureDefinitionPower BuildAdrenalineRush()
    {
        var powerAdrenalineRush = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}AdrenalineRush")
            .SetGuiPresentation(Category.Feature, FeatureDefinitionPowers.PowerMonkStepOfTheWindDash)
            .SetUsesProficiencyBonus(ActivationTime.BonusAction, RechargeRate.ShortRest)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Round, 0, TurnOccurenceType.EndOfTurn)
                    .SetTargetingData(Side.Ally, RangeType.Self, 0, TargetType.Self)
                    .SetEffectForms(EffectFormBuilder.ConditionForm(ConditionDefinitions.ConditionDashingBonus))
                    .Build())
            .AddToDB();

        powerAdrenalineRush.AddCustomSubFeatures(new PowerOrSpellFinishedByMeAdrenalineRush(powerAdrenalineRush));

        return powerAdrenalineRush;
    }

    private sealed class PowerOrSpellFinishedByMeAdrenalineRush(FeatureDefinitionPower powerAdrenalineRush)
        : IPowerOrSpellFinishedByMe
    {
        public IEnumerator OnPowerOrSpellFinishedByMe(CharacterActionMagicEffect action, BaseDefinition baseDefinition)
        {
            if (baseDefinition != powerAdrenalineRush)
            {
                yield break;
            }

            var rulesetCharacter = action.ActingCharacter.RulesetCharacter;
            var proficiencyBonus = rulesetCharacter.TryGetAttributeValue(AttributeDefinitions.ProficiencyBonus);

            rulesetCharacter.ReceiveTemporaryHitPoints(
                proficiencyBonus, DurationType.UntilLongRest, 0, TurnOccurenceType.StartOfTurn,
                rulesetCharacter.Guid);
        }
    }
}
