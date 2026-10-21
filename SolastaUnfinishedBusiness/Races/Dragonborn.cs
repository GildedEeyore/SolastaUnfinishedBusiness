using System.Linq;
using SolastaUnfinishedBusiness.Builders;
using SolastaUnfinishedBusiness.Builders.Features;
using SolastaUnfinishedBusiness.CustomUI;
using SolastaUnfinishedBusiness.Properties;
using SolastaUnfinishedBusiness.Validators;
using static RuleDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.CharacterRaceDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.FeatureDefinitionSenses;

namespace SolastaUnfinishedBusiness.Races;

// adds the 2024 Darkvision and Draconic Flight traits to the official Dragonborn race
internal static class RaceDragonbornBuilder
{
    private const string Name = "Dragonborn";

    internal static void Load()
    {
        Dragonborn.FeatureUnlocks.Add(new FeatureUnlockByLevel(SenseDarkvision, 1));
        Dragonborn.FeatureUnlocks.Add(new FeatureUnlockByLevel(BuildDraconicFlight(), 5));
    }

    private static FeatureDefinitionFeatureSet BuildDraconicFlight()
    {
        var conditionDraconicFlight = ConditionDefinitionBuilder
            .Create(ConditionDefinitions.ConditionFlyingAdaptive, $"Condition{Name}DraconicFlight")
            .SetGuiPresentation(Category.Condition, ConditionDefinitions.ConditionFlying)
            .SetPossessive()
            .SetCancellingConditions(
                DatabaseRepository.GetDatabase<ConditionDefinition>().Where(x =>
                    x.IsSubtypeOf(ConditionIncapacitated)).ToArray())
            .AddCancellingConditions(ConditionDefinitions.ConditionPetrified)
            .AddToDB();

        var powerDraconicFlight = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}DraconicFlight")
            .SetGuiPresentation(Category.Feature,
                Sprites.GetSprite("PowerDragonWings", Resources.PowerDragonWings, 256, 128))
            .SetUsesFixed(ActivationTime.BonusAction, RechargeRate.LongRest)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Minute, 10)
                    .SetTargetingData(Side.Ally, RangeType.Self, 0, TargetType.Self)
                    .SetEffectForms(EffectFormBuilder.ConditionForm(conditionDraconicFlight))
                    .Build())
            .AddCustomSubFeatures(new ValidatorsValidatePowerUse(
                ValidatorsCharacter.HasNoneOfConditions(conditionDraconicFlight.Name)))
            .AddToDB();

        var powerDraconicFlightDismiss = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}DraconicFlightDismiss")
            .SetGuiPresentation(Category.Feature,
                Sprites.GetSprite("FlightDismiss", Resources.PowerAngelicFormDismiss, 256, 128))
            .SetUsesFixed(ActivationTime.NoCost)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Ally, RangeType.Self, 0, TargetType.Self)
                    .SetEffectForms(
                        EffectFormBuilder.ConditionForm(conditionDraconicFlight,
                            ConditionForm.ConditionOperation.Remove))
                    .Build())
            .AddCustomSubFeatures(new ValidatorsValidatePowerUse(
                ValidatorsCharacter.HasAnyOfConditions(conditionDraconicFlight.Name)))
            .AddToDB();

        return FeatureDefinitionFeatureSetBuilder
            .Create($"FeatureSet{Name}DraconicFlight")
            .SetGuiPresentation(Category.Feature)
            .AddFeatureSet(powerDraconicFlight, powerDraconicFlightDismiss)
            .AddToDB();
    }
}
