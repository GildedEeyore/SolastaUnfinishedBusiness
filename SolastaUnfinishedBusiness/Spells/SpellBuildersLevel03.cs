using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SolastaUnfinishedBusiness.Api.GameExtensions;
using SolastaUnfinishedBusiness.Api.Helpers;
using SolastaUnfinishedBusiness.Api.LanguageExtensions;
using SolastaUnfinishedBusiness.Behaviors;
using SolastaUnfinishedBusiness.Behaviors.Specific;
using SolastaUnfinishedBusiness.Builders;
using SolastaUnfinishedBusiness.Builders.Features;
using SolastaUnfinishedBusiness.CustomUI;
using SolastaUnfinishedBusiness.Interfaces;
using SolastaUnfinishedBusiness.Models;
using SolastaUnfinishedBusiness.Properties;
using TA;
using UnityEngine.AddressableAssets;
using static RuleDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.ConditionDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.FeatureDefinitionPowers;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.SpellDefinitions;

namespace SolastaUnfinishedBusiness.Spells;

internal static partial class SpellBuilders
{
    #region Adder's Fangs

    internal static SpellDefinition BuildAdderFangs()
    {
        const string Name = "AdderFangs";

        var movementAffinityAdderFangs = FeatureDefinitionMovementAffinityBuilder
            .Create($"MovementAffinity{Name}")
            .SetGuiPresentationNoContent(true)
            .SetBaseSpeedMultiplicativeModifier(0.5f)
            .AddToDB();

        var conditionAdderFangs = ConditionDefinitionBuilder
            .Create(ConditionDefinitions.ConditionPoisoned, $"Condition{Name}")
            .SetPossessive()
            .AddFeatures(movementAffinityAdderFangs)
            .AddToDB();

        conditionAdderFangs.GuiPresentation.description = Gui.EmptyContent;

        var spell = SpellDefinitionBuilder
            .Create(Name)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(Name, Resources.AdderFangs, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolConjuration)
            .SetSpellLevel(3)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.Mundane)
            .SetVerboseComponent(true)
            .SetSomaticComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Attack)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Minute, 1)
                    .SetTargetingData(Side.Enemy, RangeType.Distance, 24, TargetType.IndividualsUnique)
                    .SetEffectAdvancement(EffectIncrementMethod.PerAdditionalSlotLevel,
                        additionalTargetsPerIncrement: 1)
                    .SetSavingThrowData(false, AttributeDefinitions.Constitution, false,
                        EffectDifficultyClassComputation.SpellCastingFeature)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .HasSavingThrow(EffectSavingThrowType.HalfDamage)
                            .SetDamageForm(DamageTypePoison, 4, DieType.D10)
                            .Build(),
                        EffectFormBuilder
                            .Create()
                            .HasSavingThrow(EffectSavingThrowType.Negates, TurnOccurenceType.EndOfTurn, true)
                            .SetConditionForm(conditionAdderFangs, ConditionForm.ConditionOperation.Add)
                            .Build())
                    .SetParticleEffectParameters(VenomousSpike)
                    .SetEffectEffectParameters(InflictWounds)
                    .Build())
            .AddToDB();

        return spell;
    }

    #endregion

    #region Blinding Smite

    internal static SpellDefinition BuildBlindingSmite()
    {
        const string NAME = "BlindingSmite";

        var conditionBlindedByBlindingSmite = ConditionDefinitionBuilder
            .Create(ConditionDefinitions.ConditionBlinded, $"ConditionBlindedBy{NAME}")
            .SetOrUpdateGuiPresentation(Category.Condition)
            .SetParentCondition(ConditionDefinitions.ConditionBlinded)
            .SetSpecialDuration(DurationType.Minute, 1)
            .SetFeatures()
            .AddToDB();

        conditionBlindedByBlindingSmite.GuiPresentation.description = "Rules/&ConditionBlindedDescription";

        var additionalDamageBlindingSmite = FeatureDefinitionAdditionalDamageBuilder
            .Create($"AdditionalDamage{NAME}")
            .SetGuiPresentation(NAME, Category.Spell)
            .SetNotificationTag(NAME)
            .SetAttackModeOnly()
            .SetRequiredProperty(RestrictedContextRequiredProperty.MeleeWeapon)
            .SetDamageDice(DieType.D8, 3)
            .SetSpecificDamageType(DamageTypeRadiant)
            .SetSavingThrowData(EffectDifficultyClassComputation.SpellCastingFeature, EffectSavingThrowType.None)
            .AddConditionOperation(
                new ConditionOperationDescription
                {
                    operation = ConditionOperationDescription.ConditionOperation.Add,
                    conditionDefinition = conditionBlindedByBlindingSmite,
                    hasSavingThrow = true,
                    canSaveToCancel = true,
                    saveAffinity = EffectSavingThrowType.Negates,
                    saveOccurence = TurnOccurenceType.EndOfTurn
                })
            // doesn't follow the standard impact particle reference
            .SetImpactParticleReference(DivineFavor.EffectDescription.EffectParticleParameters.casterParticleReference)
            .AddToDB();

        var conditionBlindingSmite = ConditionDefinitionBuilder
            .Create($"Condition{NAME}")
            .SetGuiPresentation(NAME, Category.Spell, ConditionBrandingSmite)
            .SetPossessive()
            .SetFeatures(additionalDamageBlindingSmite)
            .SetSpecialInterruptions(ExtraConditionInterruption.AttacksWithMeleeAndDamages)
            .AddToDB();

        var spell = SpellDefinitionBuilder
            .Create(BrandingSmite, NAME)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(NAME, Resources.BlindingSmite, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolEvocation)
            .SetSpellLevel(3)
            .SetCastingTime(ActivationTime.BonusAction)
            .SetMaterialComponent(MaterialComponentType.None)
            .SetSomaticComponent(false)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Buff)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Minute, 1)
                    .SetTargetingData(Side.Ally, RangeType.Self, 0, TargetType.Self)
                    // .SetEffectAdvancement(EffectIncrementMethod.PerAdditionalSlotLevel, additionalDicePerIncrement: 1)
                    .SetEffectForms(EffectFormBuilder.ConditionForm(conditionBlindingSmite))
                    .SetParticleEffectParameters(DivineFavor)
                    .Build())
            .AddToDB();

        SmiteSpells2024Context.SmiteSpells.Add(spell);
        SmiteSpells2024Context.SmiteDamages.Add(additionalDamageBlindingSmite);
        SmiteSpells2024Context.SmiteConditions.Add(conditionBlindingSmite);

        return spell;
    }

    #endregion

    #region Crusaders Mantle

    internal static SpellDefinition BuildCrusadersMantle()
    {
        const string NAME = "CrusadersMantle";

        var additionalDamageCrusadersMantle = FeatureDefinitionAdditionalDamageBuilder
            .Create(FeatureDefinitionAdditionalDamages.AdditionalDamageDivineFavor, $"AdditionalDamage{NAME}")
            .SetGuiPresentationNoContent(true)
            .SetNotificationTag(NAME)
            .SetAttackOnly()
            .SetDamageDice(DieType.D4, 1)
            .AddToDB();

        var conditionCrusadersMantle = ConditionDefinitionBuilder
            .Create($"Condition{NAME}")
            .SetGuiPresentation(Category.Condition, ConditionDivineFavor)
            .SetSilent(Silent.WhenAddedOrRemoved)
            .AddFeatures(additionalDamageCrusadersMantle)
            .AddToDB();

        var spell = SpellDefinitionBuilder
            .Create(NAME)
            .SetGuiPresentation(Category.Spell,
                Sprites.GetSprite(NAME, Resources.CrusadersMantle, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolEvocation)
            .SetSpellLevel(3)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.None)
            .SetSomaticComponent(false)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Buff)
            .SetRequiresConcentration(true)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Minute, 1)
                    .SetTargetingData(Side.Ally, RangeType.Self, 0, TargetType.Sphere, 6)
                    .SetRecurrentEffect(
                        RecurrentEffect.OnActivation | RecurrentEffect.OnTurnStart | RecurrentEffect.OnEnter)
                    .SetEffectForms(EffectFormBuilder.ConditionForm(conditionCrusadersMantle))
                    .SetCasterEffectParameters(HolyAura)
                    .Build())
            .AddToDB();

        return spell;
    }

    #endregion

    #region Intellect Fortress

    internal static SpellDefinition BuildIntellectFortress()
    {
        const string NAME = "IntellectFortress";

        var condition = ConditionDefinitionBuilder
            .Create($"Condition{NAME}")
            .SetGuiPresentation(NAME, Category.Spell, ConditionProtectedInsideMagicCircle)
            .SetPossessive()
            .SetFeatures(
                FeatureDefinitionSavingThrowAffinityBuilder
                    .Create($"SavingThrowAffinity{NAME}")
                    .SetGuiPresentation(NAME, Category.Spell, Gui.NoLocalization)
                    .SetAffinities(CharacterSavingThrowAffinity.Advantage, false,
                        AttributeDefinitions.Intelligence,
                        AttributeDefinitions.Wisdom,
                        AttributeDefinitions.Charisma)
                    .AddToDB(),
                FeatureDefinitionDamageAffinitys.DamageAffinityPsychicResistance)
            .SetConditionParticleReference(ConditionFeebleMinded)
            .AddToDB();

        condition.GuiPresentation.description = Gui.EmptyContent;

        var spell = SpellDefinitionBuilder
            .Create(NAME)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(NAME, Resources.IntellectFortress, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolAbjuration)
            .SetSpellLevel(3)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.None)
            .SetSomaticComponent(false)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Buff)
            .SetRequiresConcentration(true)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Hour, 1)
                    .SetTargetingData(Side.Ally, RangeType.Distance, 6, TargetType.IndividualsUnique)
                    .SetEffectAdvancement(EffectIncrementMethod.PerAdditionalSlotLevel,
                        additionalTargetsPerIncrement: 1)
                    .SetEffectForms(EffectFormBuilder.ConditionForm(condition))
                    .SetCasterEffectParameters(Confusion)
                    .SetImpactEffectParameters(
                        PowerMagebaneSpellCrusher.EffectDescription.EffectParticleParameters.effectParticleReference)
                    .Build())
            .AddToDB();

        return spell;
    }

    #endregion

    #region Winter Breath

    internal static SpellDefinition BuildWinterBreath()
    {
        const string NAME = "WinterBreath";

        var spriteReference = Sprites.GetSprite(NAME, Resources.WinterBreath, 128);

        var effectDescription = EffectDescriptionBuilder
            .Create()
            .SetTargetingData(Side.All, RangeType.Self, 0, TargetType.Cone, 3)
            .SetEffectAdvancement(EffectIncrementMethod.PerAdditionalSlotLevel, additionalDicePerIncrement: 1)
            .SetSavingThrowData(
                false,
                AttributeDefinitions.Dexterity,
                false,
                EffectDifficultyClassComputation.SpellCastingFeature,
                AttributeDefinitions.Wisdom,
                12)
            .SetParticleEffectParameters(ConeOfCold)
            .ExcludeCaster()
            .SetEffectForms(
                EffectFormBuilder
                    .Create()
                    .SetMotionForm(MotionForm.MotionType.FallProne, 1)
                    .HasSavingThrow(EffectSavingThrowType.Negates)
                    .Build(),
                EffectFormBuilder
                    .Create()
                    .SetDamageForm(DamageTypeCold, 4, DieType.D8)
                    .HasSavingThrow(EffectSavingThrowType.HalfDamage)
                    .Build())
            .Build();

        var spell = SpellDefinitionBuilder
            .Create(NAME)
            .SetGuiPresentation(Category.Spell, spriteReference)
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolConjuration)
            .SetSpellLevel(3)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.None)
            .SetSomaticComponent(true)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Attack)
            .SetEffectDescription(effectDescription)
            .AddToDB();

        return spell;
    }

    #endregion

    #region Corrupting Bolt

    internal static SpellDefinition BuildCorruptingBolt()
    {
        const string Name = "CorruptingBolt";

        var conditionCorruptingBolt = ConditionDefinitionBuilder
            .Create(ConditionEyebiteSickened, $"Condition{Name}")
            .SetGuiPresentation(Category.Condition, ConditionDoomLaughter)
            .SetConditionType(ConditionType.Detrimental)
            .SetFeatures() // remove eyebite features
            .AddCustomSubFeatures(new CustomBehaviorCorruptingBolt())
            .AddToDB();

        var spell = SpellDefinitionBuilder
            .Create(Name)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(Name, Resources.CorruptingBolt, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolNecromancy)
            .SetSpellLevel(3)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.Mundane)
            .SetVerboseComponent(true)
            .SetSomaticComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Attack)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Round, 1, TurnOccurenceType.EndOfSourceTurn)
                    .SetTargetingData(Side.Enemy, RangeType.RangeHit, 24, TargetType.IndividualsUnique)
                    .SetEffectAdvancement(EffectIncrementMethod.PerAdditionalSlotLevel, additionalDicePerIncrement: 1)
                    .SetSavingThrowData(false, AttributeDefinitions.Constitution, false,
                        EffectDifficultyClassComputation.SpellCastingFeature)
                    .SetEffectForms(
                        EffectFormBuilder.DamageForm(DamageTypeNecrotic, 4, DieType.D8),
                        EffectFormBuilder
                            .Create()
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .SetConditionForm(conditionCorruptingBolt, ConditionForm.ConditionOperation.Add)
                            .Build())
                    .SetParticleEffectParameters(FingerOfDeath)
                    .SetImpactEffectParameters(Disintegrate)
                    .SetEffectEffectParameters(Disintegrate)
                    .Build())
            .AddToDB();

        return spell;
    }

    private sealed class CustomBehaviorCorruptingBolt :
        ITryAlterOutcomeAttack,
        IPhysicalAttackFinishedOnMe,
        IMagicEffectFinishedOnMe
    {
        private const string Name = "CorruptingBolt";
        private const string ConditionCorruptingBoltName = $"Condition{Name}";

        private static readonly FeatureDefinitionDamageAffinity[] VulnerabilityFeatures =
            DatabaseRepository.GetDatabase<DamageDefinition>()
                .Select(damageDefinition =>
                    FeatureDefinitionDamageAffinityBuilder
                        .Create($"DamageAffinity{Name}{damageDefinition.Name}")
                        .SetGuiPresentationNoContent(true)
                        .SetDamageAffinityType(DamageAffinityType.Vulnerability)
                        .SetDamageType(damageDefinition.Name)
                        .AddToDB()
                ).ToArray();

        public IEnumerator OnMagicEffectFinishedOnMe(
            CharacterAction action,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            List<GameLocationCharacter> targets)
        {
            var rulesetEffect = action.ActionParams.RulesetEffect;
            var rulesetAttacker = action.ActingCharacter.RulesetCharacter;
            var rulesetDefender = defender.RulesetCharacter;

            if (!rulesetEffect.EffectDescription.NeedsToRollDie() ||
                !rulesetDefender.TryGetConditionOfCategoryAndType(
                    AttributeDefinitions.TagEffect, ConditionCorruptingBoltName, out var activeCondition))
            {
                yield break;
            }

            if (action.AttackRollOutcome is RollOutcome.Success or RollOutcome.CriticalSuccess &&
                !rulesetAttacker.SpellsCastByMe.Any(x => x.TrackedConditionGuids.Contains(activeCondition.Guid)))
            {
                rulesetDefender.RemoveAllConditionsOfCategoryAndType(
                    AttributeDefinitions.TagEffect, ConditionCorruptingBoltName);
            }
            else
            {
                activeCondition.ConditionDefinition.features.Clear();
            }
        }

        public IEnumerator OnPhysicalAttackFinishedOnMe(
            GameLocationBattleManager battleManager,
            CharacterAction action,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            RulesetAttackMode attackMode,
            RollOutcome rollOutcome,
            int damageAmount)
        {
            var rulesetDefender = defender.RulesetCharacter;

            if (!rulesetDefender.TryGetConditionOfCategoryAndType(
                    AttributeDefinitions.TagEffect, ConditionCorruptingBoltName, out var activeCondition))
            {
                yield break;
            }

            if (action.AttackRollOutcome is RollOutcome.Success or RollOutcome.CriticalSuccess)
            {
                rulesetDefender.RemoveAllConditionsOfCategoryAndType(
                    AttributeDefinitions.TagEffect, ConditionCorruptingBoltName);
            }
            else
            {
                activeCondition.ConditionDefinition.features.Clear();
            }
        }

        public int HandlerPriority => 10;

        public IEnumerator OnTryAlterOutcomeAttack(
            GameLocationBattleManager instance,
            CharacterAction action,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            GameLocationCharacter helper,
            ActionModifier actionModifier,
            RulesetAttackMode attackMode,
            RulesetEffect rulesetEffect)
        {
            var rulesetDefender = defender.RulesetCharacter;

            if ((attackMode is null &&
                 !rulesetEffect.EffectDescription.NeedsToRollDie()) ||
                !rulesetDefender.TryGetConditionOfCategoryAndType(
                    AttributeDefinitions.TagEffect, ConditionCorruptingBoltName, out var activeCondition))
            {
                yield break;
            }

            activeCondition.ConditionDefinition.Features.Clear();

            foreach (var feature in VulnerabilityFeatures)
            {
                activeCondition.ConditionDefinition.Features.Add(feature);
            }
        }
    }

    #endregion

    #region Ashardalon's Stride

    internal static SpellDefinition BuildAshardalonStride()
    {
        const string Name = "AshardalonStride";

        var combatAffinity = FeatureDefinitionCombatAffinityBuilder
            .Create($"CombatAffinity{Name}")
            .SetGuiPresentationNoContent(true)
            .SetAttackOfOpportunityImmunity(true)
            .AddToDB();

        var conditionMark = ConditionDefinitionBuilder
            .Create($"Condition{Name}Mark")
            .SetGuiPresentationNoContent(true)
            .SetSilent(Silent.WhenAddedOrRemoved)
            .AddToDB();

        var powerDamage = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}")
            .SetGuiPresentation(Name, Category.Spell, hidden: true)
            .SetUsesFixed(ActivationTime.NoCost)
            .SetShowCasting(false)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Round, 0, TurnOccurenceType.EndOfSourceTurn)
                    .SetTargetingData(Side.Enemy, RangeType.Distance, 6, TargetType.IndividualsUnique)
                    .SetEffectForms(
                        EffectFormBuilder.DamageForm(DamageTypeFire, 1, DieType.D6),
                        EffectFormBuilder.ConditionForm(conditionMark))
                    .SetImpactEffectParameters(Fireball)
                    .Build())
            .AddToDB();

        var conditions = new List<ConditionDefinition>();

        for (var effectLevel = 3; effectLevel <= 9; effectLevel++)
        {
            var movementAffinity = FeatureDefinitionMovementAffinityBuilder
                .Create($"MovementAffinity{Name}{effectLevel}")
                .SetGuiPresentationNoContent(true)
                .SetBaseSpeedAdditiveModifier(effectLevel + 1)
                .AddToDB();

            var conditionAshardalonStride = ConditionDefinitionBuilder
                .Create($"Condition{Name}{effectLevel}")
                .SetGuiPresentation(Name, Category.Spell, ConditionFreedomOfMovement)
                .SetPossessive()
                .AddFeatures(movementAffinity, combatAffinity)
                .AddCustomSubFeatures(new MoveStepFinishedAshardalonStride(powerDamage, conditionMark))
                .SetConditionParticleReference(ConditionOnFire)
                .AddToDB();

            conditionAshardalonStride.GuiPresentation.description = Gui.EmptyContent;

            conditions.Add(conditionAshardalonStride);
        }

        var spell = SpellDefinitionBuilder
            .Create(Name)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(Name, Resources.AshardalonStride, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolTransmutation)
            .SetSpellLevel(3)
            .SetCastingTime(ActivationTime.BonusAction)
            .SetMaterialComponent(MaterialComponentType.None)
            .SetVerboseComponent(true)
            .SetSomaticComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Buff)
            .SetRequiresConcentration(true)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Minute, 1)
                    .SetTargetingData(Side.Ally, RangeType.Self, 0, TargetType.Self)
                    .SetEffectAdvancement(EffectIncrementMethod.PerAdditionalSlotLevel, additionalDicePerIncrement: 1,
                        additionalTargetCellsPerIncrement: 1)
                    .SetEffectForms(EffectFormBuilder.ConditionForm(conditions[0]))
                    .SetCasterEffectParameters(FireBolt)
                    .SetImpactEffectParameters(PowerOathOfMotherlandFieryWrath
                        .EffectDescription.EffectParticleParameters.effectParticleReference)
                    .Build())
            .AddToDB();

        powerDamage.AddCustomSubFeatures(new ModifyEffectDescriptionAshardalonStride(powerDamage, spell));
        spell.AddCustomSubFeatures(new CustomBehaviorAshardalonStride([.. conditions]));

        return spell;
    }

    private sealed class ModifyEffectDescriptionAshardalonStride(
        FeatureDefinitionPower powerDamage,
        SpellDefinition spell)
        : IModifyEffectDescription
    {
        public bool IsValid(BaseDefinition definition, RulesetCharacter character, EffectDescription effectDescription)
        {
            return definition == powerDamage;
        }

        public EffectDescription GetEffectDescription(
            BaseDefinition definition,
            EffectDescription effectDescription,
            RulesetCharacter character,
            RulesetEffect rulesetEffect)
        {
            if (character.ConcentratedSpell != null &&
                character.ConcentratedSpell.SpellDefinition == spell)
            {
                effectDescription.EffectForms[0].DamageForm.DiceNumber =
                    1 + (character.ConcentratedSpell.EffectLevel - 3);
            }

            return effectDescription;
        }
    }

    private sealed class MoveStepFinishedAshardalonStride(
        FeatureDefinitionPower powerDamage,
        ConditionDefinition conditionMark) : IMoveStepStarted
    {
        public void MoveStepStarted(GameLocationCharacter mover, int3 source, int3 destination)
        {
            var locationCharacterService = ServiceRepository.GetService<IGameLocationCharacterService>();
            var targets =
                (Gui.Battle?.AllContenders ??
                 locationCharacterService.PartyCharacters.Union(locationCharacterService.GuestCharacters))
                .Where(x =>
                    x != mover &&
                    DistanceCalculation.GetDistanceFromCharacter(x, destination) <= 1 &&
                    !x.RulesetCharacter.HasConditionOfCategoryAndType(
                        AttributeDefinitions.TagEffect, conditionMark.Name))
                .ToArray();

            var rulesetAttacker = mover.RulesetCharacter;
            var usablePower = PowerProvider.Get(powerDamage, rulesetAttacker);

            mover.MyExecuteActionSpendPower(usablePower, targets);
        }
    }

    private sealed class CustomBehaviorAshardalonStride(params ConditionDefinition[] conditions)
        : IPowerOrSpellInitiatedByMe, IPowerOrSpellFinishedByMe
    {
        public IEnumerator OnPowerOrSpellFinishedByMe(CharacterActionMagicEffect action, BaseDefinition baseDefinition)
        {
            action.ActionParams.RulesetEffect.EffectDescription.EffectForms[0].ConditionForm.conditionDefinition =
                conditions[0];

            yield break;
        }

        public IEnumerator OnPowerOrSpellInitiatedByMe(CharacterActionMagicEffect action, BaseDefinition baseDefinition)
        {
            action.ActionParams.RulesetEffect.EffectDescription.EffectForms[0].ConditionForm.conditionDefinition =
                conditions[action.ActionParams.RulesetEffect.EffectLevel - 3];

            yield break;
        }
    }

    #endregion

    #region Aura of Vitality

    internal static SpellDefinition BuildAuraOfVitality()
    {
        // kept name for backward compatibility
        const string NAME = "AuraOfLife";

        var sprite = Sprites.GetSprite(NAME, Resources.AuraOfVitality, 128);

        var powerAuraOfLife = FeatureDefinitionPowerBuilder
            .Create($"Power{NAME}")
            .SetGuiPresentation(NAME, Category.Spell, sprite)
            .SetUsesFixed(ActivationTime.NoCost, RechargeRate.TurnStart)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Ally, RangeType.Distance, 6, TargetType.IndividualsUnique)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetHealingForm(HealingComputation.Dice, 0, DieType.D6, 2, false,
                                HealingCap.MaximumHitPoints)
                            .Build())
                    .SetParticleEffectParameters(HealingWord)
                    .Build())
            .AddToDB();

        var conditionAuraOfLifeSelf = ConditionDefinitionBuilder
            .Create($"Condition{NAME}Self")
            .SetGuiPresentationNoContent(true)
            .SetSilent(Silent.WhenAddedOrRemoved)
            .SetFeatures(powerAuraOfLife)
            .AddCustomSubFeatures(AddUsablePowersFromCondition.Marker)
            .AddToDB();

        var conditionAuraOfLife = ConditionDefinitionBuilder
            .Create($"Condition{NAME}")
            .SetGuiPresentation(Category.Condition, ConditionHeroism)
            .SetSilent(Silent.WhenAddedOrRemoved)
            .AddToDB();

        var spell = SpellDefinitionBuilder
            .Create(NAME)
            .SetGuiPresentation(Category.Spell, sprite)
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolEvocation)
            .SetSpellLevel(3)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.None)
            .SetSomaticComponent(false)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Buff)
            .SetRequiresConcentration(true)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Minute, 1)
                    .SetTargetingData(Side.Ally, RangeType.Self, 0, TargetType.Sphere, 6)
                    .SetRecurrentEffect(
                        RecurrentEffect.OnActivation | RecurrentEffect.OnEnter | RecurrentEffect.OnTurnStart)
                    .SetEffectForms(
                        EffectFormBuilder.ConditionForm(conditionAuraOfLife),
                        EffectFormBuilder.ConditionForm(
                            conditionAuraOfLifeSelf,
                            ConditionForm.ConditionOperation.Add, true, true))
                    .SetParticleEffectParameters(DivineWord)
                    .Build())
            .AddCustomSubFeatures(new FilterTargetingCharacterAuraOfVitality(conditionAuraOfLife))
            .AddToDB();

        return spell;
    }

    private sealed class FilterTargetingCharacterAuraOfVitality(
        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        ConditionDefinition conditionAuraOfVitality) : IFilterTargetingCharacter
    {
        public bool EnforceFullSelection => false;

        public bool IsValid(CursorLocationSelectTarget __instance, GameLocationCharacter target)
        {
            if (target.RulesetCharacter == null)
            {
                return false;
            }

            var isValid = target.RulesetCharacter.HasConditionOfCategoryAndType(
                AttributeDefinitions.TagEffect, conditionAuraOfVitality.Name);

            if (!isValid)
            {
                __instance.actionModifier.FailureFlags.Add("Failure/&MustBeAuraOfLife");
            }

            return isValid;
        }
    }

    #endregion

    #region Booming Step

    internal static SpellDefinition BuildBoomingStep()
    {
        const string Name = "BoomingStep";

        var powerExplode = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}Explode")
            .SetGuiPresentation(Name, Category.Spell, hidden: true)
            .SetUsesFixed(ActivationTime.NoCost)
            .SetShowCasting(false)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Round)
                    .SetTargetingData(Side.All, RangeType.Distance, 18, TargetType.IndividualsUnique)
                    .SetSavingThrowData(false, AttributeDefinitions.Constitution, false,
                        EffectDifficultyClassComputation.SpellCastingFeature)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetDamageForm(DamageTypeThunder, 3, DieType.D10)
                            .HasSavingThrow(EffectSavingThrowType.HalfDamage)
                            .Build())
                    .SetParticleEffectParameters(Thunderwave)
                    .Build())
            .AddToDB();

        var conditionExplode = ConditionDefinitionBuilder
            .Create($"Condition{Name}Explode")
            .SetGuiPresentationNoContent(true)
            .SetSilent(Silent.WhenAddedOrRemoved)
            .SetFeatures(powerExplode)
            .AddCustomSubFeatures(AddUsablePowersFromCondition.Marker)
            .AddToDB();

        powerExplode.AddCustomSubFeatures(
            new ModifyEffectDescriptionBoomingStepExplode(powerExplode, conditionExplode));

        var spell = SpellDefinitionBuilder
            .Create(Name)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(Name, Resources.ThunderStep, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolConjuration)
            .SetSpellLevel(3)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.None)
            .SetVerboseComponent(true)
            .SetSomaticComponent(false)
            .SetVocalSpellSameType(VocalSpellSemeType.Buff)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Round)
                    .SetTargetingData(Side.Ally, RangeType.Distance, 18, TargetType.Position)
                    .SetEffectAdvancement(EffectIncrementMethod.PerAdditionalSlotLevel, additionalDicePerIncrement: 1)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetMotionForm(MotionForm.MotionType.TeleportToDestination)
                            .Build(),
                        EffectFormBuilder.ConditionForm(
                            conditionExplode,
                            ConditionForm.ConditionOperation.Add, true, true))
                    .InviteOptionalAlly()
                    .ExcludeCaster()
                    .SetParticleEffectParameters(Thunderwave)
                    .Build())
            .AddCustomSubFeatures(new CustomBehaviorBoomingStep(powerExplode))
            .AddToDB();

        return spell;
    }

    private sealed class CustomBehaviorBoomingStep(FeatureDefinitionPower powerExplode)
        : IPowerOrSpellInitiatedByMe, IPowerOrSpellFinishedByMe, IFilterTargetingCharacter
    {
        private readonly List<GameLocationCharacter> _targets = [];

        public bool EnforceFullSelection => false;

        public bool IsValid(CursorLocationSelectTarget __instance, GameLocationCharacter target)
        {
            if (target.RulesetCharacter == null)
            {
                return false;
            }

            var isValid =
                target.RulesetCharacter is not RulesetCharacterEffectProxy &&
                __instance.ActionParams.ActingCharacter.IsWithinRange(target, 1);

            if (!isValid)
            {
                __instance.actionModifier.FailureFlags.Add("Failure/&MustBeWithin5ft");
            }

            return isValid;
        }

        public IEnumerator OnPowerOrSpellFinishedByMe(CharacterActionMagicEffect action, BaseDefinition baseDefinition)
        {
            if (action.Countered || action.ExecutionFailed)
            {
                yield break;
            }

            var attacker = action.ActingCharacter;
            var rulesetAttacker = attacker.RulesetCharacter;
            var usablePower = PowerProvider.Get(powerExplode, rulesetAttacker);

            attacker.MyExecuteActionSpendPower(usablePower, [.. _targets]);
        }

        public IEnumerator OnPowerOrSpellInitiatedByMe(CharacterActionMagicEffect action, BaseDefinition baseDefinition)
        {
            var attacker = action.ActingCharacter;
            var locationCharacterService = ServiceRepository.GetService<IGameLocationCharacterService>();
            var contenders =
                Gui.Battle?.AllContenders ??
                locationCharacterService.PartyCharacters.Union(locationCharacterService.GuestCharacters);

            _targets.SetRange(contenders
                .Where(x =>
                    x.RulesetCharacter is { IsDeadOrDyingOrUnconscious: false } &&
                    x != attacker &&
                    !action.ActionParams.TargetCharacters.Contains(x) &&
                    attacker.IsWithinRange(x, 2)));

            yield break;
        }
    }

    private sealed class ModifyEffectDescriptionBoomingStepExplode(
        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        FeatureDefinitionPower powerExplode,
        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        ConditionDefinition conditionExplode)
        : IModifyEffectDescription
    {
        public bool IsValid(BaseDefinition definition, RulesetCharacter character, EffectDescription effectDescription)
        {
            return definition == powerExplode;
        }

        public EffectDescription GetEffectDescription(
            BaseDefinition definition,
            EffectDescription effectDescription,
            RulesetCharacter character,
            RulesetEffect rulesetEffect)
        {
            if (character.TryGetConditionOfCategoryAndType(
                    AttributeDefinitions.TagEffect, conditionExplode.Name, out var activeCondition))
            {
                effectDescription.EffectForms[0].DamageForm.DiceNumber = 3 + (activeCondition.EffectLevel - 3);
            }

            return effectDescription;
        }
    }

    #endregion

    #region Elemental Weapon

    internal static SpellDefinition BuildElementalWeapon()
    {
        const string NAME = "ElementalWeapon";

        var spell = SpellDefinitionBuilder
            .Create(NAME)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(NAME, Resources.ElementalWeapon, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolTransmutation)
            .SetSpellLevel(3)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.None)
            .SetSomaticComponent(true)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Buff)
            .SetRequiresConcentration(true)
            .SetUniqueInstance()
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Minute, 1)
                    .SetEffectAdvancement(EffectIncrementMethod.PerAdditionalSlotLevel, additionalDicePerIncrement: 1)
                    .Build())
            .SetSubSpells(DamagesAndEffects
                .Where(x => x.Item1 != DamageTypePoison)
                .Select(x => BuildElementalWeaponSubspell(x.Item1, x.Item2)).ToArray())
            .AddToDB();

        return spell;
    }

    private static SpellDefinition BuildElementalWeaponSubspell(string damageType, IMagicEffect magicEffect)
    {
        var effectParticleParameters = magicEffect.EffectDescription.EffectParticleParameters;

        const string NOTIFICATION_TAG = "ElementalWeapon";

        const string ELEMENTAL_WEAPON_ADDITIONAL_DESCRIPTION = "Feature/&AdditionalDamageElementalWeaponDescription";

        const string ELEMENTAL_WEAPON_ADDITIONAL_DESCRIPTION1 = "Feature/&AdditionalDamageElementalWeapon1Description";

        const string ELEMENTAL_WEAPON_ADDITIONAL_DESCRIPTION2 = "Feature/&AdditionalDamageElementalWeapon2Description";

        const string ELEMENTAL_WEAPON_MODIFIER_DESCRIPTION = "Feature/&AttackModifierElementalWeaponDescription";

        var additionalDamageElementalWeapon = FeatureDefinitionAdditionalDamageBuilder
            .Create($"AdditionalDamage{damageType}ElementalWeapon")
            .SetGuiPresentation("AdditionalDamageElementalWeapon", Category.Feature,
                AdditionalDamageElementalWeaponDescription(damageType), MagicWeapon.guiPresentation.SpriteReference)
            .SetAttackModeOnly()
            .SetDamageDice(DieType.D4, 1)
            .SetNotificationTag(NOTIFICATION_TAG)
            .SetImpactParticleReference(effectParticleParameters.impactParticleReference)
            .AddToDB();

        var additionalDamageElementalWeapon1 = FeatureDefinitionAdditionalDamageBuilder
            .Create($"AdditionalDamage{damageType}ElementalWeapon1")
            .SetGuiPresentation("AdditionalDamageElementalWeapon", Category.Feature,
                AdditionalDamageElementalWeaponDescription1(damageType), MagicWeapon.guiPresentation.SpriteReference)
            .SetAttackModeOnly()
            .SetDamageDice(DieType.D4, 2)
            .SetNotificationTag(NOTIFICATION_TAG)
            .SetImpactParticleReference(effectParticleParameters.impactParticleReference)
            .AddToDB();

        var additionalDamageElementalWeapon2 = FeatureDefinitionAdditionalDamageBuilder
            .Create($"AdditionalDamage{damageType}ElementalWeapon2")
            .SetGuiPresentation("AdditionalDamageElementalWeapon", Category.Feature,
                AdditionalDamageElementalWeaponDescription2(damageType), MagicWeapon.guiPresentation.SpriteReference)
            .SetSpecificDamageType(damageType)
            .SetAttackModeOnly()
            .SetDamageDice(DieType.D4, 3)
            .SetNotificationTag(NOTIFICATION_TAG)
            .SetImpactParticleReference(effectParticleParameters.impactParticleReference)
            .AddToDB();

        var attackModifierElementalWeapon = FeatureDefinitionAttackModifierBuilder
            .Create(FeatureDefinitionAttackModifiers.AttackModifierMagicWeapon,
                $"AttackModifier{damageType}ElementalWeapon")
            .SetGuiPresentation("AttackModifierElementalWeapon", Category.Feature,
                AttackModifierElementalWeaponDescription(1), MagicWeapon.guiPresentation.SpriteReference)
            .AddToDB();

        var attackModifierElementalWeapon1 = FeatureDefinitionAttackModifierBuilder
            .Create(FeatureDefinitionAttackModifiers.AttackModifierMagicWeapon2,
                $"AttackModifier{damageType}ElementalWeapon1")
            .SetGuiPresentation("AttackModifierElementalWeapon", Category.Feature,
                AttackModifierElementalWeaponDescription(2), MagicWeapon.guiPresentation.SpriteReference)
            .AddToDB();

        var attackModifierElementalWeapon2 = FeatureDefinitionAttackModifierBuilder
            .Create(FeatureDefinitionAttackModifiers.AttackModifierMagicWeapon3,
                $"AttackModifier{damageType}ElementalWeapon2")
            .SetGuiPresentation("AttackModifierElementalWeapon", Category.Feature,
                AttackModifierElementalWeaponDescription(3), MagicWeapon.guiPresentation.SpriteReference)
            .AddToDB();

        var spell = SpellDefinitionBuilder
            .Create($"ElementalWeapon{damageType}")
            .SetGuiPresentation(Category.Spell, MagicWeapon)
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolTransmutation)
            .SetSpellLevel(3)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.None)
            .SetSomaticComponent(true)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Buff)
            .SetRequiresConcentration(true)
            .SetUniqueInstance()
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create(MagicWeapon)
                    .SetDurationData(DurationType.Minute, 1)
                    .SetEffectAdvancement(EffectIncrementMethod.PerAdditionalSlotLevel, additionalDicePerIncrement: 1)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetDiceAdvancement(LevelSourceType.EffectLevel)
                            .SetLevelAdvancement(EffectForm.LevelApplianceType.MultiplyDice,
                                LevelSourceType.EffectLevel)
                            .SetItemPropertyForm(ItemPropertyUsage.Unlimited, 1,
                                new FeatureUnlockByLevel(attackModifierElementalWeapon, 3),
                                new FeatureUnlockByLevel(attackModifierElementalWeapon, 4),
                                new FeatureUnlockByLevel(attackModifierElementalWeapon1, 5),
                                new FeatureUnlockByLevel(attackModifierElementalWeapon1, 6),
                                new FeatureUnlockByLevel(attackModifierElementalWeapon2, 7),
                                new FeatureUnlockByLevel(attackModifierElementalWeapon2, 8),
                                new FeatureUnlockByLevel(attackModifierElementalWeapon2, 9))
                            .Build(),
                        EffectFormBuilder
                            .Create()
                            .SetDiceAdvancement(LevelSourceType.EffectLevel)
                            .SetLevelAdvancement(EffectForm.LevelApplianceType.MultiplyDice,
                                LevelSourceType.EffectLevel)
                            .SetItemPropertyForm(ItemPropertyUsage.Unlimited, 1,
                                new FeatureUnlockByLevel(additionalDamageElementalWeapon, 3),
                                new FeatureUnlockByLevel(additionalDamageElementalWeapon, 4),
                                new FeatureUnlockByLevel(additionalDamageElementalWeapon1, 5),
                                new FeatureUnlockByLevel(additionalDamageElementalWeapon1, 6),
                                new FeatureUnlockByLevel(additionalDamageElementalWeapon2, 7),
                                new FeatureUnlockByLevel(additionalDamageElementalWeapon2, 8),
                                new FeatureUnlockByLevel(additionalDamageElementalWeapon2, 9))
                            .Build())
                    .SetParticleEffectParameters(effectParticleParameters)
                    .Build())
            .AddCustomSubFeatures(TrackItemsCarefully.Marker)
            .AddToDB();

        return spell;

        static string AdditionalDamageElementalWeaponDescription(string x)
        {
            return Gui.Format(ELEMENTAL_WEAPON_ADDITIONAL_DESCRIPTION, x);
        }

        static string AdditionalDamageElementalWeaponDescription1(string x)
        {
            return Gui.Format(ELEMENTAL_WEAPON_ADDITIONAL_DESCRIPTION1, x);
        }

        static string AdditionalDamageElementalWeaponDescription2(string x)
        {
            return Gui.Format(ELEMENTAL_WEAPON_ADDITIONAL_DESCRIPTION2, x);
        }

        static string AttackModifierElementalWeaponDescription(int x)
        {
            return Gui.Format(ELEMENTAL_WEAPON_MODIFIER_DESCRIPTION, x.ToString());
        }
    }

    #endregion

    #region Flame Arrows

    internal static SpellDefinition BuildFlameArrows()
    {
        const string Name = "FlameArrows";

        var conditionFlameArrows = ConditionDefinitionBuilder
            .Create($"Condition{Name}")
            .SetGuiPresentation(Category.Condition, ConditionFeatTakeAim)
            .SetPossessive()
            .SetFeatures(
                FeatureDefinitionAdditionalDamageBuilder
                    .Create($"AdditionalDamage{Name}")
                    .SetGuiPresentationNoContent(true)
                    .SetNotificationTag(Name)
                    .SetRequiredProperty(RestrictedContextRequiredProperty.RangeWeapon)
                    .SetAttackModeOnly()
                    .SetDamageDice(DieType.D6, 1)
                    .SetSpecificDamageType(DamageTypeFire)
                    .SetImpactParticleReference(FireBolt)
                    .AddToDB())
            .AddCustomSubFeatures(new CustomBehaviorFlameArrows())
            .AddToDB();

        var spell = SpellDefinitionBuilder
            .Create(Name)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(Name, Resources.FlameArrows, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolTransmutation)
            .SetSpellLevel(3)
            .SetCastingTime(ActivationTime.BonusAction)
            .SetMaterialComponent(MaterialComponentType.Specific)
            .SetSpecificMaterialComponent(TagsDefinitions.WeaponTagRange, 0, false)
            .SetVerboseComponent(true)
            .SetSomaticComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Buff)
            .SetRequiresConcentration(true)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Hour, 1)
                    .SetTargetingData(Side.Ally, RangeType.Self, 0, TargetType.Self)
                    .SetEffectAdvancement(EffectIncrementMethod.PerAdditionalSlotLevel, additionalItemBonus: 2)
                    .SetEffectForms(EffectFormBuilder.ConditionForm(conditionFlameArrows))
                    .SetParticleEffectParameters(FireBolt)
                    .Build())
            .AddToDB();

        return spell;
    }

    private sealed class CustomBehaviorFlameArrows : IOnConditionAddedOrRemoved, IPhysicalAttackFinishedByMe
    {
        public void OnConditionAdded(RulesetCharacter target, RulesetCondition rulesetCondition)
        {
            rulesetCondition.Amount = 12 + (2 * (rulesetCondition.EffectLevel - 3));
        }

        public void OnConditionRemoved(RulesetCharacter target, RulesetCondition rulesetCondition)
        {
            // empty
        }

        public IEnumerator OnPhysicalAttackFinishedByMe(
            GameLocationBattleManager battleManager,
            CharacterAction action,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            RulesetAttackMode attackMode,
            RollOutcome rollOutcome,
            int damageAmount)
        {
            if (attackMode is not { Ranged: true } or { Thrown: true })
            {
                yield break;
            }

            var rulesetAttacker = attacker.RulesetCharacter;

            if (!rulesetAttacker.TryGetConditionOfCategoryAndType(
                    AttributeDefinitions.TagEffect,
                    "ConditionFlameArrows",
                    out var activeCondition))
            {
                yield break;
            }

            activeCondition.Amount--;

            if (activeCondition.Amount <= 0)
            {
                rulesetAttacker.BreakConcentration();
            }
        }
    }

    #endregion

    #region Hunger of the Void

    internal static SpellDefinition BuildHungerOfTheVoid()
    {
        const string Name = "HungerOfTheVoid";

        var powerHungerOfTheVoidDamageAcid = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}Acid")
            .SetGuiPresentation(Name, Category.Spell, hidden: true)
            .SetUsesFixed(ActivationTime.NoCost)
            .SetShowCasting(false)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Enemy, RangeType.Distance, 6, TargetType.IndividualsUnique)
                    .SetSavingThrowData(false, AttributeDefinitions.Dexterity, false,
                        EffectDifficultyClassComputation.SpellCastingFeature)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .SetDamageForm(DamageTypeAcid, 2, DieType.D6)
                            .Build())
                    .SetImpactEffectParameters(VenomousSpike)
                    .Build())
            .AddToDB();

        var powerHungerOfTheVoidDamageCold = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}Cold")
            .SetGuiPresentation(Name, Category.Spell, hidden: true)
            .SetUsesFixed(ActivationTime.NoCost)
            .SetShowCasting(false)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Enemy, RangeType.Distance, 6, TargetType.IndividualsUnique)
                    .SetEffectForms(EffectFormBuilder.DamageForm(DamageTypeCold, 2, DieType.D6))
                    .SetImpactEffectParameters(ConeOfCold)
                    .Build())
            .AddToDB();

        var conditionHungerOfTheVoid = ConditionDefinitionBuilder
            .Create(ConditionDefinitions.ConditionBlinded, $"ConditionBlindedBy{Name}")
            .SetOrUpdateGuiPresentation(Category.Condition)
            .SetParentCondition(ConditionDefinitions.ConditionBlinded)
            .SetFeatures()
            .AddToDB();

        conditionHungerOfTheVoid.GuiPresentation.description = "Rules/&ConditionBlindedDescription";
        conditionHungerOfTheVoid.AddCustomSubFeatures(
            new CustomBehaviorHungerOfTheVoid(
                powerHungerOfTheVoidDamageAcid, powerHungerOfTheVoidDamageCold, conditionHungerOfTheVoid));

        var spell = SpellDefinitionBuilder
            .Create(Name)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(Name, Resources.HungerOfTheVoid, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolTransmutation)
            .SetSpellLevel(3)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.Mundane)
            .SetVerboseComponent(true)
            .SetSomaticComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Buff)
            .SetRequiresConcentration(true)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Minute, 1)
                    .SetTargetingData(Side.All, RangeType.Distance, 24, TargetType.Sphere, 4)
                    .SetRecurrentEffect(
                        RecurrentEffect.OnActivation | RecurrentEffect.OnEnter | RecurrentEffect.OnTurnStart)
                    .SetEffectForms(
                        EffectFormBuilder.ConditionForm(conditionHungerOfTheVoid),
                        EffectFormBuilder.TopologyForm(TopologyForm.Type.DangerousZone, true),
                        Darkness.EffectDescription.EffectForms[2],
                        Darkness.EffectDescription.EffectForms[3])
                    .SetParticleEffectParameters(Darkness)
                    .Build())
            .AddToDB();

        return spell;
    }

    private sealed class CustomBehaviorHungerOfTheVoid(
        FeatureDefinitionPower powerHungerOfTheVoidDamageAcid,
        FeatureDefinitionPower powerHungerOfTheVoidDamageCold,
        ConditionDefinition conditionHungerOfTheVoid) : ICharacterTurnStartListener, ICharacterBeforeTurnEndListener
    {
        public void OnCharacterBeforeTurnEnded(GameLocationCharacter character)
        {
            var rulesetCharacter = character.RulesetCharacter;

            if (!rulesetCharacter.TryGetConditionOfCategoryAndType(
                    AttributeDefinitions.TagEffect, conditionHungerOfTheVoid.Name, out var activeCondition))
            {
                return;
            }

            var rulesetCaster = EffectHelpers.GetCharacterByGuid(activeCondition.SourceGuid);
            var caster = GameLocationCharacter.GetFromActor(rulesetCaster);
            var usablePower = PowerProvider.Get(powerHungerOfTheVoidDamageAcid, rulesetCaster);

            caster.MyExecuteActionSpendPower(usablePower, character);
        }

        public void OnCharacterTurnStarted(GameLocationCharacter character)
        {
            var rulesetCharacter = character.RulesetCharacter;

            if (!rulesetCharacter.TryGetConditionOfCategoryAndType(
                    AttributeDefinitions.TagEffect, conditionHungerOfTheVoid.Name, out var activeCondition))
            {
                return;
            }

            var rulesetCaster = EffectHelpers.GetCharacterByGuid(activeCondition.SourceGuid);
            var caster = GameLocationCharacter.GetFromActor(rulesetCaster);
            var usablePower = PowerProvider.Get(powerHungerOfTheVoidDamageCold, rulesetCaster);

            caster.MyExecuteActionSpendPower(usablePower, character);
        }
    }

    #endregion

    #region Lightning Arrow

    internal static SpellDefinition BuildLightningArrow()
    {
        const string Name = "LightningArrow";

        var powerLightningArrowLeap = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}Leap")
            .SetGuiPresentation(Name, Category.Spell, hidden: true)
            .SetUsesFixed(ActivationTime.NoCost)
            .SetShowCasting(false)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Enemy, RangeType.Distance, 12, TargetType.IndividualsUnique)
                    .SetSavingThrowData(false, AttributeDefinitions.Dexterity, false,
                        EffectDifficultyClassComputation.SpellCastingFeature)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .HasSavingThrow(EffectSavingThrowType.HalfDamage)
                            .SetDamageForm(DamageTypeLightning, 2, DieType.D8)
                            .Build())
                    .SetParticleEffectParameters(LightningBolt)
                    .Build())
            .AddToDB();

        var conditionLightningArrow = ConditionDefinitionBuilder
            .Create($"Condition{Name}")
            .SetGuiPresentation(Category.Condition, ConditionFeatTakeAim)
            .SetPossessive()
            .SetFeatures(powerLightningArrowLeap)
            .AddToDB();

        conditionLightningArrow.AddCustomSubFeatures(
            AddUsablePowersFromCondition.Marker,
            new CustomBehaviorLightningArrow(powerLightningArrowLeap, conditionLightningArrow));

        powerLightningArrowLeap.AddCustomSubFeatures(
            new ModifyEffectDescriptionLightningArrowLeap(powerLightningArrowLeap, conditionLightningArrow));

        var spell = SpellDefinitionBuilder
            .Create(Name)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(Name, Resources.LightningArrow, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolTransmutation)
            .SetSpellLevel(3)
            .SetCastingTime(ActivationTime.BonusAction)
            .SetMaterialComponent(MaterialComponentType.Mundane)
            .SetVerboseComponent(true)
            .SetSomaticComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Buff)
            .SetRequiresConcentration(true)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Minute, 1)
                    .SetTargetingData(Side.Ally, RangeType.Self, 0, TargetType.Self)
                    .SetEffectAdvancement(EffectIncrementMethod.PerAdditionalSlotLevel, additionalDicePerIncrement: 1)
                    .SetEffectForms(EffectFormBuilder.ConditionForm(conditionLightningArrow))
                    .SetParticleEffectParameters(LightningBolt)
                    .Build())
            .AddToDB();

        return spell;
    }

    private sealed class ModifyEffectDescriptionLightningArrowLeap(
        FeatureDefinitionPower featureDefinitionPower,
        ConditionDefinition conditionLightningArrow)
        : IModifyEffectDescription
    {
        public bool IsValid(BaseDefinition definition, RulesetCharacter character, EffectDescription effectDescription)
        {
            return definition == featureDefinitionPower;
        }

        public EffectDescription GetEffectDescription(
            BaseDefinition definition,
            EffectDescription effectDescription,
            RulesetCharacter character,
            RulesetEffect rulesetEffect)
        {
            if (character.TryGetConditionOfCategoryAndType(
                    AttributeDefinitions.TagEffect, conditionLightningArrow.Name, out var activeCondition))
            {
                effectDescription.EffectForms[0].DamageForm.DiceNumber = 2 + (activeCondition.EffectLevel - 3);
            }

            return effectDescription;
        }
    }

    private sealed class CustomBehaviorLightningArrow(
        FeatureDefinitionPower powerLightningArrowLeap,
        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        ConditionDefinition conditionLightningArrow)
        : IPhysicalAttackBeforeHitConfirmedOnEnemy, IPhysicalAttackFinishedByMe
    {
        private const int MainTargetDiceNumber = 3;

        public IEnumerator OnPhysicalAttackBeforeHitConfirmedOnEnemy(
            GameLocationBattleManager battleManager,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            ActionModifier actionModifier,
            RulesetAttackMode attackMode,
            bool rangedAttack,
            AdvantageType advantageType,
            List<EffectForm> actualEffectForms,
            bool firstTarget,
            bool criticalHit)
        {
            if (attackMode is not { Ranged: true } && attackMode is not { Thrown: true })
            {
                yield break;
            }

            var rulesetAttacker = attacker.RulesetCharacter;

            if (!rulesetAttacker.TryGetConditionOfCategoryAndType(
                    AttributeDefinitions.TagEffect,
                    conditionLightningArrow.Name,
                    out var activeCondition))
            {
                yield break;
            }

            var index = actualEffectForms.FindIndex(x => x.FormType == EffectForm.EffectFormType.Damage);

            if (index < 0)
            {
                yield break;
            }

            var diceNumber = MainTargetDiceNumber + (activeCondition.EffectLevel - 3);
            var effectForm = EffectFormBuilder.DamageForm(DamageTypeLightning, diceNumber, DieType.D8);

            effectForm.DamageForm.IgnoreCriticalDoubleDice = true;

            actualEffectForms.Insert(index + 1, effectForm);
        }

        public IEnumerator OnPhysicalAttackFinishedByMe(
            GameLocationBattleManager battleManager,
            CharacterAction action,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            RulesetAttackMode attackMode,
            RollOutcome rollOutcome,
            int damageAmount)
        {
            if (attackMode is not { Ranged: true } && attackMode is not { Thrown: true })
            {
                yield break;
            }

            var rulesetAttacker = attacker.RulesetCharacter;

            if (!rulesetAttacker.TryGetConditionOfCategoryAndType(
                    AttributeDefinitions.TagEffect,
                    conditionLightningArrow.Name,
                    out var activeCondition))
            {
                yield break;
            }

            // keep a tab on additionalDice for leap power later on
            var additionalDice = activeCondition.EffectLevel - 3;

            rulesetAttacker.RemoveCondition(activeCondition);

            // half damage on target on a miss
            if (rollOutcome is not (RollOutcome.Success or RollOutcome.CriticalSuccess))
            {
                var rolls = new List<int>();
                var damageForm = new DamageForm
                {
                    DamageType = DamageTypeLightning,
                    DieType = DieType.D8,
                    DiceNumber = MainTargetDiceNumber + additionalDice
                };
                var damageRoll = rulesetAttacker.RollDamage(damageForm, 0, false, 0, 0, 1, false, false, false, rolls);
                var rulesetDefender = defender.RulesetActor;
                var applyFormsParams = new RulesetImplementationDefinitions.ApplyFormsParams
                {
                    sourceCharacter = rulesetAttacker,
                    targetCharacter = rulesetDefender,
                    position = defender.LocationPosition
                };

                RulesetActor.InflictDamage(
                    damageRoll,
                    damageForm,
                    damageForm.DamageType,
                    applyFormsParams,
                    rulesetDefender,
                    false,
                    rulesetAttacker.Guid,
                    true,
                    attackMode.AttackTags,
                    new RollInfo(damageForm.DieType, rolls, 0),
                    false,
                    out _);
            }

            // leap damage on enemies within 10 ft from target
            var usablePower = PowerProvider.Get(powerLightningArrowLeap, rulesetAttacker);
            var targets = battleManager.Battle
                .GetContenders(defender, isOppositeSide: false, withinRange: 2)
                .ToArray();

            attacker.MyExecuteActionSpendPower(usablePower, targets);
        }
    }

    #endregion

    #region Pulse Wave

    internal static SpellDefinition BuildPulseWave()
    {
        const string NAME = "PulseWave";

        var spellPush = SpellDefinitionBuilder
            .Create($"{NAME}Push")
            .SetGuiPresentation(Category.Spell)
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolEvocation)
            .SetSpellLevel(3)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.None)
            .SetSomaticComponent(true)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Attack)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.All, RangeType.Self, 0, TargetType.Cone, 6)
                    .ExcludeCaster()
                    .SetEffectAdvancement(EffectIncrementMethod.PerAdditionalSlotLevel, additionalDicePerIncrement: 1)
                    .SetSavingThrowData(
                        false,
                        AttributeDefinitions.Constitution,
                        true,
                        EffectDifficultyClassComputation.SpellCastingFeature,
                        AttributeDefinitions.Wisdom,
                        12)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetDamageForm(DamageTypeForce, dieType: DieType.D6, diceNumber: 6)
                            .HasSavingThrow(EffectSavingThrowType.HalfDamage)
                            .Build(),
                        EffectFormBuilder
                            .Create()
                            .SetMotionForm(MotionForm.MotionType.PushFromOrigin, 3)
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .Build())
                    .SetParticleEffectParameters(PowerFunctionWandFearCone)
                    .SetCasterEffectParameters(Darkness)
                    .SetImpactEffectParameters(MindTwist)
                    .Build())
            .AddToDB();

        spellPush.AddCustomSubFeatures(new ModifyEffectDescriptionPulseWave(spellPush));

        var spellPull = SpellDefinitionBuilder
            .Create($"{NAME}Pull")
            .SetGuiPresentation(Category.Spell)
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolEvocation)
            .SetSpellLevel(3)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.None)
            .SetSomaticComponent(true)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Attack)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.All, RangeType.Self, 0, TargetType.Cone, 6)
                    .ExcludeCaster()
                    .SetEffectAdvancement(EffectIncrementMethod.PerAdditionalSlotLevel, additionalDicePerIncrement: 1)
                    .SetSavingThrowData(
                        false,
                        AttributeDefinitions.Constitution,
                        true,
                        EffectDifficultyClassComputation.SpellCastingFeature,
                        AttributeDefinitions.Wisdom,
                        12)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetDamageForm(DamageTypeForce, dieType: DieType.D6, diceNumber: 6)
                            .HasSavingThrow(EffectSavingThrowType.HalfDamage)
                            .Build(),
                        EffectFormBuilder
                            .Create()
                            .SetMotionForm(MotionForm.MotionType.DragToOrigin, 3)
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .Build())
                    .SetParticleEffectParameters(PowerFunctionWandFearCone)
                    .SetCasterEffectParameters(Darkness)
                    .SetImpactEffectParameters(MindTwist)
                    .Build())
            .AddToDB();

        spellPull.AddCustomSubFeatures(new ModifyEffectDescriptionPulseWave(spellPull));

        var spell = SpellDefinitionBuilder
            .Create(NAME)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(NAME, Resources.PulseWave, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolEvocation)
            .SetSpellLevel(3)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.None)
            .SetSomaticComponent(true)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Attack)
            .SetEffectDescription(
                // UI Only
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.All, RangeType.Self, 0, TargetType.Cone, 6)
                    .ExcludeCaster()
                    .SetEffectAdvancement(EffectIncrementMethod.PerAdditionalSlotLevel, additionalDicePerIncrement: 1)
                    .SetSavingThrowData(
                        false,
                        AttributeDefinitions.Constitution,
                        true,
                        EffectDifficultyClassComputation.SpellCastingFeature,
                        AttributeDefinitions.Wisdom,
                        12)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetDamageForm(DamageTypeForce, dieType: DieType.D6, diceNumber: 6)
                            .HasSavingThrow(EffectSavingThrowType.HalfDamage)
                            .Build(),
                        EffectFormBuilder
                            .Create()
                            .SetMotionForm(MotionForm.MotionType.PushFromOrigin, 3)
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .Build(),
                        EffectFormBuilder
                            .Create()
                            .SetMotionForm(MotionForm.MotionType.DragToOrigin, 3)
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .Build())
                    .Build())
            .SetSubSpells(spellPush, spellPull)
            .AddToDB();

        return spell;
    }

    private sealed class ModifyEffectDescriptionPulseWave(SpellDefinition spellDefinition)
        : IMagicEffectBeforeHitConfirmedOnEnemy
    {
        public IEnumerator OnMagicEffectBeforeHitConfirmedOnEnemy(
            GameLocationBattleManager battleManager,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            ActionModifier actionModifier,
            RulesetEffect rulesetEffect,
            List<EffectForm> actualEffectForms,
            bool firstTarget,
            bool criticalHit)
        {
            if (rulesetEffect.SourceDefinition != spellDefinition)
            {
                yield break;
            }

            var motionEffect = actualEffectForms.FirstOrDefault(x => x.FormType == EffectForm.EffectFormType.Motion);

            if (motionEffect == null)
            {
                yield break;
            }

            motionEffect.MotionForm.distance = rulesetEffect.EffectLevel;
        }
    }

    #endregion

    #region Cacophonic Shield

    internal static SpellDefinition BuildCacophonicShield()
    {
        const string Name = "CacophonicShield";

        var conditionDeafened = ConditionDefinitionBuilder
            .Create(ConditionDefinitions.ConditionDeafened, $"Condition{Name}Deafened")
            .SetParentCondition(ConditionDefinitions.ConditionDeafened)
            .SetConditionType(ConditionType.Detrimental)
            .SetSpecialDuration(DurationType.Round, 1, (TurnOccurenceType)ExtraTurnOccurenceType.StartOfSourceTurn)
            .AddToDB();

        var combatAffinity = FeatureDefinitionCombatAffinityBuilder
            .Create($"CombatAffinity{Name}")
            .SetGuiPresentationNoContent(true)
            .SetSituationalContext(ExtraSituationalContext.IsRangedAttack)
            .SetAttackOnMeAdvantage(AdvantageType.Disadvantage)
            .AddToDB();

        var conditionSelf = ConditionDefinitionBuilder
            .Create($"Condition{Name}")
            .SetGuiPresentation(Category.Condition, ConditionShielded)
            .SetPossessive()
            .SetFeatures(FeatureDefinitionDamageAffinitys.DamageAffinityThunderResistance, combatAffinity)
            .AddToDB();

        var spell = SpellDefinitionBuilder
            .Create(Name)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(Name, Resources.CacophonicShield, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolEvocation)
            .SetSpellLevel(3)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.None)
            .SetSomaticComponent(true)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Defense)
            .SetRequiresConcentration(true)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Minute, 10)
                    // RAW the caster picks who is unaffected; here allies are unaffected and enemies are affected
                    .SetTargetingData(Side.Enemy, RangeType.Self, 0, TargetType.Sphere, 2)
                    .SetEffectAdvancement(EffectIncrementMethod.PerAdditionalSlotLevel, additionalDicePerIncrement: 1)
                    .SetSavingThrowData(false, AttributeDefinitions.Constitution, false,
                        EffectDifficultyClassComputation.SpellCastingFeature)
                    .SetRecurrentEffect(
                        RecurrentEffect.OnActivation | RecurrentEffect.OnEnter | RecurrentEffect.OnTurnEnd)
                    .SetParticleEffectParameters(SpiritGuardians)
                    .SetCasterEffectParameters(Thunderwave)
                    .SetImpactEffectParameters(Shatter)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetDamageForm(DamageTypeThunder, 3, DieType.D6)
                            .HasSavingThrow(EffectSavingThrowType.HalfDamage)
                            .Build(),
                        EffectFormBuilder
                            .Create()
                            .SetConditionForm(conditionDeafened, ConditionForm.ConditionOperation.Add)
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .Build(),
                        EffectFormBuilder.ConditionForm(
                            conditionSelf, ConditionForm.ConditionOperation.Add, true, true))
                    .Build())
            .AddCustomSubFeatures(
                ApplyRecurrentEffectWhenSourceMoves.Mark,
                RecurrentEffectOncePerTurn.Mark)
            .AddToDB();

        return spell;
    }

    #endregion

    #region Laeral's Silver Lance

    internal static SpellDefinition BuildLaeralsSilverLance()
    {
        const string Name = "LaeralsSilverLance";

        var spell = SpellDefinitionBuilder
            .Create(Name)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(Name, Resources.LaeralsSilverLance, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolEvocation)
            .SetSpellLevel(3)
            .SetCastingTime(ActivationTime.Action)
            // RAW requires a silver pin worth 250+ GP; no buyable item carries the Silver tag at that cost
            .SetMaterialComponent(MaterialComponentType.Mundane)
            .SetSomaticComponent(true)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Attack)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    // RAW the caster picks the creatures in the line; here only enemies are affected
                    .SetTargetingData(Side.Enemy, RangeType.Self, 0, TargetType.Line, 24)
                    .ExcludeCaster()
                    .SetEffectAdvancement(EffectIncrementMethod.PerAdditionalSlotLevel, additionalDicePerIncrement: 1)
                    .SetSavingThrowData(false, AttributeDefinitions.Strength, false,
                        EffectDifficultyClassComputation.SpellCastingFeature)
                    .SetParticleEffectParameters(LightningBolt)
                    .SetImpactEffectParameters(EldritchBlast)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetDamageForm(DamageTypeForce, 3, DieType.D10)
                            .HasSavingThrow(EffectSavingThrowType.HalfDamage)
                            .Build(),
                        EffectFormBuilder
                            .Create()
                            .SetMotionForm(MotionForm.MotionType.FallProne)
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .Build())
                    .Build())
            .AddToDB();

        return spell;
    }

    #endregion

    #region Spirit Shroud

    private const string SpiritShroudName = "SpiritShroud";

    internal static SpellDefinition BuildSpiritShroud()
    {
        var sprite = Sprites.GetSprite(SpiritShroudName, Resources.SpiritShroud, 128);

        var hinder = ConditionDefinitionBuilder
            .Create(ConditionHindered_By_Frost, $"Condition{SpiritShroudName}Hinder")
            .SetSilent(Silent.None)
            .SetConditionType(ConditionType.Detrimental)
            .SetParentCondition(ConditionHindered)
            .CopyParticleReferences(ConditionSpiritGuardians)
            .AddToDB();

        var noHeal = ConditionDefinitionBuilder
            .Create($"Condition{SpiritShroudName}NoHeal")
            .SetGuiPresentation(Category.Condition, ConditionChilledByTouch.GuiPresentation.SpriteReference)
            .SetConditionType(ConditionType.Detrimental)
            .SetFeatures(FeatureDefinitionHealingModifiers.HealingModifierChilledByTouch)
            .SetSpecialDuration(DurationType.Round, 1, TurnOccurenceType.StartOfTurn)
            .AddToDB();

        return SpellDefinitionBuilder
            .Create(SpiritShroudName)
            .SetGuiPresentation(Category.Spell, sprite)
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolNecromancy)
            .SetSpellLevel(3)
            .SetCastingTime(ActivationTime.BonusAction)
            .SetMaterialComponent(MaterialComponentType.None)
            .SetSomaticComponent(true)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Defense)
            .SetRequiresConcentration(true)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Minute, 1)
                    .SetEffectAdvancement(
                        EffectIncrementMethod.PerAdditionalSlotLevel, 2, additionalDicePerIncrement: 1)
                    .Build())
            .SetSubSpells(
                BuildSpiritShroudSubSpell(DamageTypeRadiant, hinder, noHeal, sprite),
                BuildSpiritShroudSubSpell(DamageTypeNecrotic, hinder, noHeal, sprite),
                BuildSpiritShroudSubSpell(DamageTypeCold, hinder, noHeal, sprite))
            .AddToDB();
    }

    private static SpellDefinition BuildSpiritShroudSubSpell(
        string damage,
        ConditionDefinition hinder,
        ConditionDefinition noHeal,
        AssetReferenceSprite sprite)
    {
        return SpellDefinitionBuilder
            .Create($"{SpiritShroudName}{damage}")
            .SetGuiPresentation(Category.Spell, sprite)
            .SetSpellLevel(3)
            .SetVocalSpellSameType(VocalSpellSemeType.Defense)
            .SetCastingTime(ActivationTime.BonusAction)
            .SetRequiresConcentration(true)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Enemy, RangeType.Self, 1, TargetType.Sphere, 2)
                    .SetDurationData(DurationType.Minute, 1)
                    .SetEffectAdvancement(EffectIncrementMethod.PerAdditionalSlotLevel, 2,
                        additionalDicePerIncrement: 1)
                    //RAW it should only trigger if target starts turn in the area, but game doesn't trigger on turn start for some reason without OnEnter
                    .SetRecurrentEffect(
                        RecurrentEffect.OnActivation | RecurrentEffect.OnTurnStart | RecurrentEffect.OnEnter)
                    .SetParticleEffectParameters(SpiritGuardians)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetConditionForm(hinder, ConditionForm.ConditionOperation.Add)
                            .Build(),
                        EffectFormBuilder.ConditionForm(
                            ConditionDefinitionBuilder
                                .Create($"Condition{SpiritShroudName}{damage}")
                                .SetGuiPresentationNoContent(true)
                                .SetSilent(Silent.WhenAddedOrRemoved)
                                .CopyParticleReferences(ConditionSpiritGuardiansSelf)
                                .SetFeatures(
                                    FeatureDefinitionAdditionalDamageBuilder
                                        .Create($"AdditionalDamage{SpiritShroudName}{damage}")
                                        .SetGuiPresentationNoContent(true)
                                        .SetNotificationTag($"{SpiritShroudName}{damage}")
                                        .SetTriggerCondition(ExtraAdditionalDamageTriggerCondition.TargetIsWithin10Ft)
                                        .SetAttackOnly()
                                        .SetDamageDice(DieType.D8, 1)
                                        .SetSpecificDamageType(damage)
                                        .SetAdvancement(AdditionalDamageAdvancement.SlotLevel, 0, 1, 2)
                                        .AddConditionOperation(
                                            ConditionOperationDescription.ConditionOperation.Add, noHeal)
                                        .AddToDB())
                                .AddToDB(),
                            ConditionForm.ConditionOperation.Add, true, true),
                        EffectFormBuilder
                            .Create()
                            .SetTopologyForm(TopologyForm.Type.DangerousZone, true)
                            .Build())
                    .Build())
            .AddToDB();
    }

    #endregion

    #region Summon Spirits

    // shared by Summon Elemental and Summon Undead

    private static ConditionDefinition BuildSummonSpiritConditionAttackRoll(string name)
    {
        var attackModifier = FeatureDefinitionAttackModifierBuilder
            .Create($"AttackModifier{name}AttackRoll")
            .SetGuiPresentation("Feedback/&SummonSpiritBonusTitle", Gui.NoLocalization)
            .SetAttackRollModifier(1, AttackModifierMethod.SourceConditionAmount)
            .AddToDB();

        // amount is the caster's spell attack modifier, set when the spirit is summoned
        return ConditionDefinitionBuilder
            .Create($"Condition{name}AttackRoll")
            .SetGuiPresentationNoContent(true)
            .SetSilent(Silent.WhenAddedOrRemoved)
            .SetFeatures(attackModifier)
            .AddToDB();
    }

    private static FeatureDefinition BuildSummonSpiritInitiative(string name)
    {
        return FeatureDefinitionBuilder
            .Create($"Feature{name}Initiative")
            .SetGuiPresentationNoContent(true)
            .AddCustomSubFeatures(ForceInitiativeToSummoner.Mark)
            .AddToDB();
    }

    private static FeatureDefinitionConditionAffinity GetSummonSpiritConditionImmunity(ConditionDefinition condition)
    {
        var name = $"ConditionAffinitySummonSpirit{condition.Name}Immunity";

        if (TryGetDefinition<FeatureDefinitionConditionAffinity>(name, out var conditionAffinity))
        {
            return conditionAffinity;
        }

        return FeatureDefinitionConditionAffinityBuilder
            .Create(name)
            .SetGuiPresentation(Category.Feature)
            .SetConditionAffinityType(ConditionAffinityType.Immunity)
            .SetConditionType(condition)
            .AddToDB();
    }

    private static MonsterAttackDefinition GetSummonSpiritBaseAttack(MonsterDefinition monster, string attackName)
    {
        return TryGetDefinition<MonsterAttackDefinition>(attackName, out var attack)
            ? attack
            : monster.AttackIterations[0].MonsterAttackDefinition;
    }

    private static MonsterAttackDefinition BuildSummonSpiritAttack(
        MonsterAttackDefinition baseAttack,
        string name,
        string title,
        params EffectForm[] effectForms)
    {
        var attack = MonsterAttackDefinitionBuilder
            .Create(baseAttack, name)
            .SetGuiPresentation(title, Category.Monster, baseAttack)
            // the caster's spell attack modifier is added by the AttackRoll condition
            .SetToHitBonus(0)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create(baseAttack.EffectDescription)
                    .SetEffectForms(effectForms)
                    .Build())
            .AddToDB();

        attack.limitedUse = false;
        attack.maxUses = -1;

        return attack;
    }

    private sealed class PowerOrSpellFinishedByMeSummonSpirit(
        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        ConditionDefinition conditionAttackRoll,
        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        ConditionDefinition conditionSaveDC = null) : IPowerOrSpellFinishedByMe
    {
        public IEnumerator OnPowerOrSpellFinishedByMe(CharacterActionMagicEffect action, BaseDefinition baseDefinition)
        {
            if (action is not CharacterActionCastSpell actionCastSpell)
            {
                yield break;
            }

            var activeSpell = actionCastSpell.ActiveSpell;
            var rulesetCaster = action.ActingCharacter.RulesetCharacter;
            var saveDC = activeSpell.SpellRepertoire?.SaveDC ??
                         rulesetCaster.SpellRepertoires.Select(x => x.SaveDC).DefaultIfEmpty(10).Max();

            foreach (var rulesetSpirit in EffectHelpers.GetSummonedCreatures(activeSpell))
            {
                rulesetSpirit.InflictCondition(
                    conditionAttackRoll.Name,
                    DurationType.Permanent,
                    0,
                    TurnOccurenceType.EndOfTurn,
                    AttributeDefinitions.TagEffect,
                    rulesetCaster.Guid,
                    rulesetCaster.CurrentFaction.Name,
                    1,
                    conditionAttackRoll.Name,
                    activeSpell.MagicAttackBonus,
                    0,
                    0);

                if (!conditionSaveDC ||
                    rulesetSpirit is not RulesetCharacterMonster rulesetMonster ||
                    !rulesetMonster.MonsterDefinition.Features.Contains(FeatureSummonUndeadFesteringAura))
                {
                    continue;
                }

                rulesetSpirit.InflictCondition(
                    conditionSaveDC.Name,
                    DurationType.Permanent,
                    0,
                    TurnOccurenceType.EndOfTurn,
                    AttributeDefinitions.TagEffect,
                    rulesetCaster.Guid,
                    rulesetCaster.CurrentFaction.Name,
                    1,
                    conditionSaveDC.Name,
                    saveDC,
                    0,
                    0);
            }
        }
    }

    #endregion

    #region Summon Undead

    private const string SummonUndeadName = "SummonUndead";
    private const string ConditionSummonUndeadSaveDCName = $"Condition{SummonUndeadName}SaveDC";

    private static FeatureDefinition FeatureSummonUndeadFesteringAura { get; set; }

    internal static SpellDefinition BuildSummonUndead()
    {
        const string NAME = SummonUndeadName;

        var conditionAttackRoll = BuildSummonSpiritConditionAttackRoll(NAME);

        // amount is the caster's spell save DC, used by Festering Aura
        var conditionSaveDC = ConditionDefinitionBuilder
            .Create(ConditionSummonUndeadSaveDCName)
            .SetGuiPresentationNoContent(true)
            .SetSilent(Silent.WhenAddedOrRemoved)
            .AddToDB();

        FeatureSummonUndeadFesteringAura = FeatureDefinitionBuilder
            .Create($"Feature{NAME}FesteringAura")
            .SetGuiPresentation(Category.Feature)
            .AddToDB();

        var featureRottingClaw = FeatureDefinitionBuilder
            .Create($"Feature{NAME}RottingClaw")
            .SetGuiPresentation(Category.Feature)
            .AddToDB();

        featureRottingClaw.AddCustomSubFeatures(new PhysicalAttackFinishedByMeRottingClaw());

        var commonFeatures = new[]
        {
            BuildSummonSpiritInitiative(NAME), FeatureDefinitionMoveModes.MoveModeMove6,
            FeatureDefinitionSenses.SenseNormalVision, FeatureDefinitionSenses.SenseDarkvision12,
            FeatureDefinitionDamageAffinitys.DamageAffinityNecroticImmunity,
            FeatureDefinitionDamageAffinitys.DamageAffinityPoisonImmunity,
            FeatureDefinitionConditionAffinitys.ConditionAffinityExhaustionImmunity,
            FeatureDefinitionConditionAffinitys.ConditionAffinityFrightenedImmunity,
            FeatureDefinitionConditionAffinitys.ConditionAffinityFrightenedFearImmunity,
            GetSummonSpiritConditionImmunity(ConditionDefinitions.ConditionParalyzed),
            FeatureDefinitionConditionAffinitys.ConditionAffinityPoisonImmunity
        };

        var attackGhostly = GetSummonSpiritBaseAttack(MonsterDefinitions.Ghost, "Attack_Ghost_Withering_Touch");
        var attackPutrid = GetSummonSpiritBaseAttack(MonsterDefinitions.Ghoul, "Attack_Ghoul_Claw");
        var attackSkeletal = GetSummonSpiritBaseAttack(MonsterDefinitions.Skeleton_Archer, "Attack_Skeleton_ShortBow");

        var subSpells = new List<SpellDefinition>();

        // RAW the spell's level sets the spirit's stats, so build one spirit per form and slot level
        for (var level = 3; level <= 9; level++)
        {
            var bonus = 3 + level;

            var ghostlyAttack = BuildSummonSpiritAttack(
                attackGhostly, $"Attack{NAME}Ghostly{level}", $"Attack{NAME}DeathlyTouch",
                EffectFormBuilder.DamageForm(DamageTypeNecrotic, 1, DieType.D8, bonus),
                EffectFormBuilder.ConditionForm(ConditionDefinitions.ConditionFrightened));

            // frightened until the end of the target's next turn
            ghostlyAttack.EffectDescription.durationType = DurationType.Round;
            ghostlyAttack.EffectDescription.durationParameter = 1;
            ghostlyAttack.EffectDescription.endOfEffect = TurnOccurenceType.EndOfTurn;

            var ghostly = BuildSummonUndeadSpirit(
                "Ghostly", level, MonsterDefinitions.Ghost, 30, ghostlyAttack,
                [.. commonFeatures, FeatureDefinitionMoveModes.MoveModeFly8]);

            var putrid = BuildSummonUndeadSpirit(
                "Putrid", level, MonsterDefinitions.Ghoul, 30,
                BuildSummonSpiritAttack(
                    attackPutrid, $"Attack{NAME}Putrid{level}", $"Attack{NAME}RottingClaw",
                    EffectFormBuilder.DamageForm(DamageTypeSlashing, 1, DieType.D6, bonus)),
                [.. commonFeatures, FeatureSummonUndeadFesteringAura, featureRottingClaw]);

            var skeletalAttack = BuildSummonSpiritAttack(
                attackSkeletal, $"Attack{NAME}Skeletal{level}", $"Attack{NAME}GraveBolt",
                EffectFormBuilder.DamageForm(DamageTypeNecrotic, 2, DieType.D4, bonus));

            // range 150 ft
            skeletalAttack.closeRange = 30;
            skeletalAttack.maxRange = 30;

            var skeletal = BuildSummonUndeadSpirit(
                "Skeletal", level, MonsterDefinitions.Skeleton_Archer, 20, skeletalAttack,
                commonFeatures);

            subSpells.Add(BuildSummonUndeadSubSpell("Ghostly", level, ghostly,
                Sprites.GetSprite("SpellRaiseGhost", Resources.SpellRaiseGhost, 128)));
            subSpells.Add(BuildSummonUndeadSubSpell("Putrid", level, putrid,
                Sprites.GetSprite("SpellRaiseGhoul", Resources.SpellRaiseGhoul, 128)));
            subSpells.Add(BuildSummonUndeadSubSpell("Skeletal", level, skeletal,
                Sprites.GetSprite("SpellRaiseSkeletonArcher", Resources.SpellRaiseSkeletonArcher, 128)));
        }

        foreach (var subSpell in subSpells)
        {
            subSpell.AddCustomSubFeatures(new PowerOrSpellFinishedByMeSummonSpirit(conditionAttackRoll, conditionSaveDC));
        }

        var spell = SpellDefinitionBuilder
            .Create(NAME)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(NAME, Resources.SummonUndead, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolNecromancy)
            .SetSpellLevel(3)
            .SetCastingTime(ActivationTime.Action)
            // RAW requires a gilded skull worth 300+ GP; no buyable item matches, so a pouch or focus is enough
            .SetMaterialComponent(MaterialComponentType.Mundane)
            .SetSomaticComponent(true)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Buff)
            .SetRequiresConcentration(true)
            .SetEffectDescription(
                // UI Only
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Hour, 1)
                    .SetTargetingData(Side.Ally, RangeType.Distance, 18, TargetType.Position)
                    .Build())
            .SetSubSpells([.. subSpells])
            .AddToDB();

        return spell;
    }

    private static MonsterDefinition BuildSummonUndeadSpirit(
        string form,
        int level,
        MonsterDefinition baseMonster,
        int baseHitPoints,
        MonsterAttackDefinition attack,
        FeatureDefinition[] features)
    {
        return MonsterDefinitionBuilder
            .Create(baseMonster, $"{SummonUndeadName}{form}Spirit{level}")
            .SetOrUpdateGuiPresentation($"{SummonUndeadName}{form}Spirit", Category.Monster)
            .SetFeatures(features)
            .SetAttackIterations(new MonsterAttackIteration(attack, level / 2))
            .SetArmorClass(11 + level)
            .SetAbilityScores(12, 16, 15, 4, 10, 9)
            .SetSavingThrowScores()
            .SetSkillScores()
            .SetStandardHitPoints(baseHitPoints + (10 * (level - 3)))
            .SetSizeDefinition(CharacterSizeDefinitions.Medium)
            .SetAlignment(MonsterDefinitionBuilder.NeutralAlignment)
            .SetCharacterFamily(CharacterFamilyDefinitions.Undead.Name)
            .SetCreatureTags(SummonUndeadName)
            .SetChallengeRating(0)
            .NoExperienceGain()
            .SetDroppedLootDefinition(null)
            .SetFullyControlledWhenAllied(true)
            .SetDefaultFaction(FactionDefinitions.Party)
            .SetBestiaryEntry(BestiaryDefinitions.BestiaryEntry.None)
            .SetDungeonMakerPresence(MonsterDefinition.DungeonMaker.None)
            .AddToDB();
    }

    private static SpellDefinition BuildSummonUndeadSubSpell(
        string form,
        int level,
        MonsterDefinition spirit,
        AssetReferenceSprite sprite)
    {
        var subSpell = SpellDefinitionBuilder
            .Create($"{SummonUndeadName}{form}{level}")
            .SetGuiPresentation($"{SummonUndeadName}{form}", Category.Spell, sprite)
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolNecromancy)
            .SetSpellLevel(3)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.Mundane)
            .SetSomaticComponent(true)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Buff)
            .SetRequiresConcentration(true)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Hour, 1)
                    .SetTargetingData(Side.Ally, RangeType.Distance, 18, TargetType.Position)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetSummonCreatureForm(1, spirit.Name)
                            .Build())
                    .SetParticleEffectParameters(VampiricTouch)
                    .Build())
            .AddToDB();

        UpcastConjureElementalAndFey.RegisterSlotLevelSubspell(subSpell, level);

        return subSpell;
    }

    // called at the start of each creature's turn in battle
    internal static void HandleSummonUndeadFesteringAura(GameLocationCharacter character)
    {
        var battle = Gui.Battle;

        if (battle == null)
        {
            return;
        }

        var rulesetCharacter = character.RulesetCharacter;

        foreach (var spirit in battle.AllContenders
                     .Where(x =>
                         x != character &&
                         x.RulesetCharacter is { IsDeadOrDyingOrUnconscious: false } &&
                         x.IsWithinRange(character, 1))
                     .ToArray())
        {
            var rulesetSpirit = spirit.RulesetCharacter;

            if (!rulesetSpirit.TryGetConditionOfCategoryAndType(
                    AttributeDefinitions.TagEffect, ConditionSummonUndeadSaveDCName, out var activeCondition) ||
                // RAW the aura affects any creature other than the caster
                activeCondition.SourceGuid == rulesetCharacter.Guid ||
                rulesetCharacter.IsImmuneToCondition(RuleDefinitions.ConditionPoisoned, rulesetSpirit.Guid, out _))
            {
                continue;
            }

            var outcome = rulesetCharacter.MakeSimpleSavingThrow(
                AttributeDefinitions.Constitution, activeCondition.Amount, FeatureSummonUndeadFesteringAura,
                conditionType: RuleDefinitions.ConditionPoisoned);

            if (outcome is RollOutcome.Success or RollOutcome.CriticalSuccess)
            {
                continue;
            }

            // until the start of its next turn
            rulesetCharacter.InflictCondition(
                RuleDefinitions.ConditionPoisoned,
                DurationType.Round,
                1,
                TurnOccurenceType.StartOfTurn,
                AttributeDefinitions.TagEffect,
                rulesetSpirit.Guid,
                rulesetSpirit.CurrentFaction.Name,
                1,
                RuleDefinitions.ConditionPoisoned,
                0,
                0,
                0);
        }
    }

    private sealed class PhysicalAttackFinishedByMeRottingClaw : IPhysicalAttackFinishedByMe
    {
        public IEnumerator OnPhysicalAttackFinishedByMe(
            GameLocationBattleManager battleManager,
            CharacterAction action,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            RulesetAttackMode attackMode,
            RollOutcome rollOutcome,
            int damageAmount)
        {
            var rulesetDefender = defender.RulesetCharacter;

            if (rollOutcome is not (RollOutcome.Success or RollOutcome.CriticalSuccess) ||
                attackMode?.SourceDefinition is not MonsterAttackDefinition monsterAttack ||
                !monsterAttack.Name.StartsWith($"Attack{SummonUndeadName}Putrid") ||
                rulesetDefender is not { IsDeadOrDyingOrUnconscious: false } ||
                !rulesetDefender.HasConditionOfTypeOrSubType(RuleDefinitions.ConditionPoisoned))
            {
                yield break;
            }

            var rulesetAttacker = attacker.RulesetCharacter;

            // until the end of its next turn
            rulesetDefender.InflictCondition(
                RuleDefinitions.ConditionParalyzed,
                DurationType.Round,
                1,
                TurnOccurenceType.EndOfTurn,
                AttributeDefinitions.TagEffect,
                rulesetAttacker.Guid,
                rulesetAttacker.CurrentFaction.Name,
                1,
                RuleDefinitions.ConditionParalyzed,
                0,
                0,
                0);
        }
    }

    #endregion

    #region Vitality Transfer

    internal static SpellDefinition BuildVitalityTransfer()
    {
        const string Name = "VitalityTransfer";

        var spell = SpellDefinitionBuilder
            .Create(Name)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(Name, Resources.VitalityTransfer, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolNecromancy)
            .SetSpellLevel(3)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.Mundane)
            .SetVerboseComponent(true)
            .SetSomaticComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Buff)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Ally, RangeType.Distance, 12, TargetType.IndividualsUnique)
                    .SetEffectAdvancement(EffectIncrementMethod.PerAdditionalSlotLevel, additionalDicePerIncrement: 1)
                    .ExcludeCaster()
                    .SetParticleEffectParameters(FalseLife)
                    .SetEffectEffectParameters(CureWounds)
                    .Build())
            .AddCustomSubFeatures(new ModifyDiceRollVitalityTransfer())
            .AddToDB();

        return spell;
    }

    private sealed class ModifyDiceRollVitalityTransfer : IPowerOrSpellFinishedByMe
    {
        public IEnumerator OnPowerOrSpellFinishedByMe(CharacterActionMagicEffect action, BaseDefinition baseDefinition)
        {
            if (action is not CharacterActionCastSpell actionCastSpell || action.Countered || action.ExecutionFailed)
            {
                yield break;
            }

            var caster = action.ActingCharacter;
            var rulesetCaster = caster.RulesetCharacter;
            var diceNumber = 4 + (actionCastSpell.activeSpell.EffectLevel - 3);
            var damageForm = new DamageForm
            {
                DamageType = DamageTypeNecrotic, DiceNumber = diceNumber, DieType = DieType.D8
            };

            // need to loop over target characters to support twinned metamagic scenarios
            foreach (var target in action.ActionParams.TargetCharacters)
            {
                var rulesetTarget = target.RulesetCharacter;
                var rolls = new List<int>();
                var totalDamage = rulesetCaster.RollDamage(damageForm, 0, false, 0, 0, 1, false, false, false, rolls);
                var totalHealing = totalDamage * 2;
                var currentHitPoints = rulesetCaster.CurrentHitPoints;

                rulesetCaster.SustainDamage(totalDamage, damageForm.DamageType, false, rulesetCaster.Guid,
                    new RollInfo(damageForm.DieType, rolls, 0), out _);

                EffectHelpers.StartVisualEffect(caster, caster, PowerSorcererChildRiftOffering);

                rulesetCaster.DamageSustained?.Invoke(rulesetCaster, totalDamage, damageForm.DamageType, true,
                    currentHitPoints > totalDamage, false);

                rulesetTarget.ReceiveHealing(totalHealing, true, rulesetCaster.Guid);
            }
        }
    }

    #endregion

    #region Sylune's Viper

    internal static SpellDefinition BuildSylunesViper()
    {
        const string NAME = "SylunesViper";
        const int TempHitPoints = 15;
        const int TempHitPointsPerSlotLevel = 5;

        var sprite = Sprites.GetSprite(NAME, Resources.SylunesViper, 128);

        var conditionPoisoned = ConditionDefinitionBuilder
            .Create(ConditionDefinitions.ConditionPoisoned, $"Condition{NAME}Poisoned")
            .SetParentCondition(ConditionDefinitions.ConditionPoisoned)
            .SetConditionType(ConditionType.Detrimental)
            .SetSpecialDuration(DurationType.Round, 1, (TurnOccurenceType)ExtraTurnOccurenceType.StartOfSourceTurn)
            .AddToDB();

        // while poisoned by the bite, the target is also incapacitated
        var conditionIncapacitated = ConditionDefinitionBuilder
            .Create(ConditionDefinitions.ConditionIncapacitated, $"Condition{NAME}Incapacitated")
            .SetParentCondition(ConditionDefinitions.ConditionIncapacitated)
            .SetConditionType(ConditionType.Detrimental)
            .SetSpecialDuration(DurationType.Round, 1, (TurnOccurenceType)ExtraTurnOccurenceType.StartOfSourceTurn)
            .AddToDB();

        var powerVenomousBite = FeatureDefinitionPowerBuilder
            .Create($"Power{NAME}VenomousBite")
            .SetGuiPresentation(Category.Feature, sprite)
            .SetUsesFixed(ActivationTime.Action)
            .SetUseSpellAttack()
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Enemy, RangeType.RangeHit, 10, TargetType.IndividualsUnique)
                    .SetEffectForms(
                        EffectFormBuilder.DamageForm(DamageTypeForce, 1, DieType.D6),
                        EffectFormBuilder.ConditionForm(conditionPoisoned),
                        EffectFormBuilder.ConditionForm(conditionIncapacitated))
                    .SetParticleEffectParameters(PoisonSpray)
                    .SetImpactEffectParameters(PoisonSpray)
                    .Build())
            .AddToDB();

        var movementAffinityClimb = FeatureDefinitionMovementAffinityBuilder
            .Create($"MovementAffinity{NAME}")
            .SetGuiPresentationNoContent(true)
            // a climb speed equal to your speed: climbing costs no extra movement
            .SetClimbing(true, true)
            .AddToDB();

        var condition = ConditionDefinitionBuilder
            .Create($"Condition{NAME}")
            .SetGuiPresentation(NAME, Category.Spell, ConditionDefinitions.ConditionProtectedFromPoison)
            .SetPossessive()
            .SetConditionType(ConditionType.Beneficial)
            .SetFeatures(movementAffinityClimb, powerVenomousBite)
            .AddCustomSubFeatures(AddUsablePowersFromCondition.Marker)
            .AddToDB();

        condition.GuiPresentation.description = Gui.EmptyContent;
        condition.AddCustomSubFeatures(new CustomBehaviorSylunesViper(condition));
        powerVenomousBite.AddCustomSubFeatures(new ModifyEffectDescriptionSylunesViper(condition));

        var spell = SpellDefinitionBuilder
            .Create(NAME)
            .SetGuiPresentation(Category.Spell, sprite)
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolConjuration)
            .SetSpellLevel(3)
            .SetCastingTime(ActivationTime.BonusAction)
            // RAW a snake fang, covered by a pouch or focus
            .SetMaterialComponent(MaterialComponentType.Mundane)
            .SetSomaticComponent(true)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Buff)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Hour, 1)
                    .SetTargetingData(Side.Ally, RangeType.Self, 0, TargetType.Self)
                    .SetEffectAdvancement(EffectIncrementMethod.PerAdditionalSlotLevel,
                        additionalTempHpPerIncrement: TempHitPointsPerSlotLevel)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetTempHpForm(TempHitPoints)
                            .Build(),
                        EffectFormBuilder.ConditionForm(condition))
                    .SetParticleEffectParameters(FalseLife)
                    .Build())
            .AddToDB();

        return spell;
    }

    // Venomous Bite deals 1d6 more force damage for each slot level above 3rd
    private sealed class ModifyEffectDescriptionSylunesViper(ConditionDefinition conditionSylunesViper)
        : IModifyEffectDescription
    {
        public bool IsValid(BaseDefinition definition, RulesetCharacter character, EffectDescription effectDescription)
        {
            return character.TryGetConditionOfCategoryAndType(
                AttributeDefinitions.TagEffect, conditionSylunesViper.Name, out _);
        }

        public EffectDescription GetEffectDescription(
            BaseDefinition definition,
            EffectDescription effectDescription,
            RulesetCharacter character,
            RulesetEffect rulesetEffect)
        {
            if (character.TryGetConditionOfCategoryAndType(
                    AttributeDefinitions.TagEffect, conditionSylunesViper.Name, out var activeCondition))
            {
                effectDescription.FindFirstDamageForm().DiceNumber =
                    1 + System.Math.Max(0, activeCondition.EffectLevel - 3);
            }

            return effectDescription;
        }
    }

    // the spell ends early once the caster has no temporary hit points left
    private sealed class CustomBehaviorSylunesViper(ConditionDefinition conditionSylunesViper)
        : IMagicEffectFinishedOnMe, IPhysicalAttackFinishedOnMe
    {
        public IEnumerator OnMagicEffectFinishedOnMe(
            CharacterAction action,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            List<GameLocationCharacter> targets)
        {
            EndIfNoTemporaryHitPoints(defender.RulesetCharacter);

            yield break;
        }

        public IEnumerator OnPhysicalAttackFinishedOnMe(
            GameLocationBattleManager battleManager,
            CharacterAction action,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            RulesetAttackMode attackMode,
            RollOutcome rollOutcome,
            int damageAmount)
        {
            EndIfNoTemporaryHitPoints(defender.RulesetCharacter);

            yield break;
        }

        private void EndIfNoTemporaryHitPoints(RulesetCharacter rulesetCharacter)
        {
            if (rulesetCharacter.TemporaryHitPoints == 0 &&
                rulesetCharacter.TryGetConditionOfCategoryAndType(
                    AttributeDefinitions.TagEffect, conditionSylunesViper.Name, out var activeCondition))
            {
                rulesetCharacter.RemoveCondition(activeCondition);
            }
        }
    }

    #endregion
}
