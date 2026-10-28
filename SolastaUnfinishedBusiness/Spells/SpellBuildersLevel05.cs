using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
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
using SolastaUnfinishedBusiness.Validators;
using TA;
using UnityEngine.AddressableAssets;
using static RuleDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.ConditionDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.FeatureDefinitionPowers;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.SpellDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.WeaponTypeDefinitions;
using static SolastaUnfinishedBusiness.Api.GameExtensions.GameLocationCharacterExtensions;

namespace SolastaUnfinishedBusiness.Spells;

internal static partial class SpellBuilders
{
    #region Dawn

    internal static SpellDefinition BuildDawn()
    {
        const string NAME = "Dawn";

        var effectProxy = EffectProxyDefinitionBuilder
            .Create(EffectProxyDefinitions.ProxyDaylight, $"Proxy{NAME}")
            .SetOrUpdateGuiPresentation(Category.Proxy)
            .SetActionId(ExtraActionId.ProxyDawn)
            .SetCanMove()
            .SetAdditionalFeatures(FeatureDefinitionMoveModes.MoveModeMove12, FeatureDefinitionMoveModes.MoveModeFly12)
            .AddToDB();

        var spell = SpellDefinitionBuilder
            .Create(NAME)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(NAME, Resources.Dawn, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolEvocation)
            .SetSpellLevel(5)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.Mundane)
            .SetVerboseComponent(true)
            .SetSomaticComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Buff)
            .SetRequiresConcentration(true)
            .SetEffectDescription(EffectDescriptionBuilder.Create()
                .SetDurationData(DurationType.Minute, 1)
                .SetTargetingData(Side.All, RangeType.Distance, 24, TargetType.Cylinder, 6, 8)
                .SetSavingThrowData(false, AttributeDefinitions.Constitution, true,
                    EffectDifficultyClassComputation.SpellCastingFeature)
                .SetRecurrentEffect(RecurrentEffect.OnActivation | RecurrentEffect.OnTurnEnd)
                .SetEffectForms(
                    EffectFormBuilder.SummonEffectProxyForm(effectProxy),
                    EffectFormBuilder.WithSavingThrow(EffectSavingThrowType.HalfDamage)
                        .DamageForm(DamageTypeRadiant, 4, DieType.D10))
                .SetParticleEffectParameters(Daylight)
                .SetImpactEffectParameters(Sunburst)
                .Build())
            .AddToDB();

        spell.EffectDescription.effectParticleParameters.activeEffectImpactParticleReference =
            Sunburst.EffectDescription.EffectParticleParameters.impactParticleReference;

        return spell;
    }

    #endregion

    #region Divine Wrath

    internal static SpellDefinition BuildDivineWrath()
    {
        const string NAME = "DivineWrath";

        var spellRadiant = SpellDefinitionBuilder
            .Create($"{NAME}Radiant")
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(NAME, Resources.DivineWrath, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolEvocation)
            .SetSpellLevel(5)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.None)
            .SetVerboseComponent(true)
            .SetSomaticComponent(false)
            .SetVocalSpellSameType(VocalSpellSemeType.Attack)
            .SetEffectDescription(EffectDescriptionBuilder.Create()
                .SetTargetingData(Side.Enemy, RangeType.Self, 0, TargetType.Sphere, 6)
                .SetSavingThrowData(false, AttributeDefinitions.Constitution, true,
                    EffectDifficultyClassComputation.SpellCastingFeature)
                .SetEffectForms(
                    EffectFormBuilder.WithSavingThrow(EffectSavingThrowType.HalfDamage)
                        .DamageForm(DamageTypeThunder, 5, DieType.D6),
                    EffectFormBuilder.WithSavingThrow(EffectSavingThrowType.HalfDamage)
                        .DamageForm(DamageTypeRadiant, 5, DieType.D6),
                    EffectFormBuilder.WithSavingThrow(EffectSavingThrowType.Negates)
                        .SetMotionForm(MotionForm.MotionType.FallProne)
                        .Build())
                .SetParticleEffectParameters(HolyAura)
                .SetImpactEffectParameters(Sunburst)
                .Build())
            .AddToDB();

        var spellNecrotic = SpellDefinitionBuilder
            .Create($"{NAME}Necrotic")
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(NAME, Resources.DivineWrath, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolEvocation)
            .SetSpellLevel(5)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.None)
            .SetVerboseComponent(true)
            .SetSomaticComponent(false)
            .SetVocalSpellSameType(VocalSpellSemeType.Attack)
            .SetEffectDescription(EffectDescriptionBuilder.Create()
                .SetTargetingData(Side.Enemy, RangeType.Self, 0, TargetType.Sphere, 6)
                .SetSavingThrowData(false, AttributeDefinitions.Constitution, true,
                    EffectDifficultyClassComputation.SpellCastingFeature)
                .SetEffectForms(
                    EffectFormBuilder.WithSavingThrow(EffectSavingThrowType.HalfDamage)
                        .DamageForm(DamageTypeThunder, 5, DieType.D6),
                    EffectFormBuilder.WithSavingThrow(EffectSavingThrowType.HalfDamage)
                        .DamageForm(DamageTypeNecrotic, 5, DieType.D6),
                    EffectFormBuilder.WithSavingThrow(EffectSavingThrowType.Negates)
                        .SetMotionForm(MotionForm.MotionType.FallProne)
                        .Build())
                .SetParticleEffectParameters(HolyAura)
                .Build())
            .AddToDB();

        var spell = SpellDefinitionBuilder
            .Create(NAME)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(NAME, Resources.DivineWrath, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolEvocation)
            .SetSpellLevel(5)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.None)
            .SetVerboseComponent(true)
            .SetSomaticComponent(false)
            .SetVocalSpellSameType(VocalSpellSemeType.Attack)
            .SetSubSpells(spellNecrotic, spellRadiant)
            .SetEffectDescription(EffectDescriptionBuilder.Create()
                .SetTargetingData(Side.Enemy, RangeType.Self, 0, TargetType.Sphere, 6)
                .SetSavingThrowData(false, AttributeDefinitions.Constitution, true,
                    EffectDifficultyClassComputation.SpellCastingFeature)
                .SetEffectForms(
                    EffectFormBuilder
                        .WithSavingThrow(EffectSavingThrowType.HalfDamage)
                        .DamageForm(DamageTypeThunder, 5, DieType.D6),
                    EffectFormBuilder.WithSavingThrow(EffectSavingThrowType.Negates)
                        .SetMotionForm(MotionForm.MotionType.FallProne)
                        .Build())
                .SetParticleEffectParameters(HolyAura)
                .Build())
            .AddToDB();

        return spell;
    }

    #endregion

    #region Far Step

    internal static SpellDefinition BuildFarStep()
    {
        var condition = ConditionDefinitionBuilder
            .Create("ConditionFarStep")
            .SetGuiPresentation(Category.Condition, ConditionJump)
            .AddCustomSubFeatures(AddUsablePowersFromCondition.Marker)
            .SetSilent(Silent.None)
            .SetPossessive()
            .SetFeatures(CustomActionIdContext.FarStep)
            .AddToDB();

        return SpellDefinitionBuilder
            .Create("FarStep")
            .SetGuiPresentation(Category.Spell, Sprites.FarStep)
            .SetSpellLevel(5)
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolConjuration)
            .SetCastingTime(ActivationTime.BonusAction)
            .SetMaterialComponent(MaterialComponentType.None)
            .SetSomaticComponent(false)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Buff)
            .SetRequiresConcentration(true)
            .SetEffectDescription(EffectDescriptionBuilder.Create()
                .SetDurationData(DurationType.Minute, 1)
                .SetTargetingData(Side.Ally, RangeType.Distance, 12, TargetType.Position)
                .SetEffectForms(
                    EffectFormBuilder.MotionForm(MotionForm.MotionType.TeleportToDestination),
                    EffectFormBuilder.AddConditionForm(condition, true, true))
                .SetParticleEffectParameters(MistyStep)
                .UseQuickAnimations()
                .Build())
            .AddToDB();
    }

    #endregion

    #region Incineration

    internal static SpellDefinition BuildIncineration()
    {
        const string NAME = "Incineration";

        var lightSourceForm = Light.EffectDescription
            .GetFirstFormOfType(EffectForm.EffectFormType.LightSource).LightSourceForm;

        var conditionIncineration = ConditionDefinitionBuilder
            .Create(ConditionOnFire, $"Condition{NAME}")
            .SetParentCondition(ConditionOnFire)
            .SetFeatures()
            .SetRecurrentEffectForms(
                EffectFormBuilder
                    .Create()
                    .SetDamageForm(DamageTypeFire, 8, DieType.D6)
                    .SetCreatedBy()
                    .Build())
            .AddToDB();

        conditionIncineration.specialDuration = false;

        var spell = SpellDefinitionBuilder
            .Create(NAME)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(NAME, Resources.Immolation, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolEvocation)
            .SetSpellLevel(5)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.None)
            .SetVerboseComponent(true)
            .SetSomaticComponent(false)
            .SetVocalSpellSameType(VocalSpellSemeType.Buff)
            .SetRequiresConcentration(true)
            .SetEffectDescription(EffectDescriptionBuilder.Create(Light)
                .SetDurationData(DurationType.Minute, 1)
                .SetTargetingData(Side.Enemy, RangeType.Distance, 18, TargetType.IndividualsUnique)
                .SetSavingThrowData(false, AttributeDefinitions.Dexterity, true,
                    EffectDifficultyClassComputation.SpellCastingFeature)
                .SetEffectForms(
                    EffectFormBuilder.WithSavingThrow(EffectSavingThrowType.HalfDamage)
                        .DamageForm(DamageTypeFire, 8, DieType.D6),
                    EffectFormBuilder.WithSavingThrow(EffectSavingThrowType.Negates)
                        .SetConditionForm(conditionIncineration, ConditionForm.ConditionOperation.Add)
                        .Build(),
                    EffectFormBuilder.WithSavingThrow(EffectSavingThrowType.Negates)
                        .SetLightSourceForm(
                            LightSourceType.Basic, 6, 6, lightSourceForm.Color,
                            lightSourceForm.graphicsPrefabReference)
                        .Build())
                .SetParticleEffectParameters(Fireball)
                .Build())
            .AddToDB();

        return spell;
    }

    #endregion

    #region Mantle of Thorns

    internal static SpellDefinition BuildMantleOfThorns()
    {
        const string NAME = "MantleOfThorns";

        var spell = SpellDefinitionBuilder
            .Create(NAME)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(NAME, Resources.MantleOfThorns, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolTransmutation)
            .SetSpellLevel(5)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.None)
            .SetSomaticComponent(true)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Debuff)
            .SetRequiresConcentration(true)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetParticleEffectParameters(SpikeGrowth)
                    .SetTargetingData(Side.Enemy, RangeType.Self, 0, TargetType.Sphere, 3)
                    .SetDurationData(DurationType.Minute, 1)
                    .SetRecurrentEffect(RecurrentEffect.OnEnter | RecurrentEffect.OnMove | RecurrentEffect.OnTurnStart)
                    .SetEffectForms(
                        EffectFormBuilder.DamageForm(DamageTypePiercing, 2, DieType.D8),
                        EffectFormBuilder.TopologyForm(TopologyForm.Type.DangerousZone, false),
                        EffectFormBuilder.TopologyForm(TopologyForm.Type.DifficultThrough, false))
                    .Build())
            .AddToDB();

        return spell;
    }

    #endregion

    #region Sonic Boom

    internal static SpellDefinition BuildSonicBoom()
    {
        const string NAME = "SonicBoom";

        var effectDescription = EffectDescriptionBuilder
            .Create()
            .SetTargetingData(Side.All, RangeType.Distance, 30, TargetType.Sphere, 6)
            .SetDurationData(DurationType.Round, 1, TurnOccurenceType.EndOfSourceTurn)
            .SetEffectAdvancement(EffectIncrementMethod.PerAdditionalSlotLevel, 2, additionalDicePerIncrement: 1)
            .SetParticleEffectParameters(Thunderwave)
            .SetSavingThrowData(
                false, AttributeDefinitions.Strength, false, EffectDifficultyClassComputation.SpellCastingFeature)
            .SetEffectForms(
                EffectFormBuilder
                    .Create()
                    .SetDamageForm(DamageTypeThunder, 6, DieType.D8)
                    .HasSavingThrow(EffectSavingThrowType.HalfDamage)
                    .Build(),
                EffectFormBuilder
                    .Create()
                    .SetMotionForm(MotionForm.MotionType.PushFromOrigin, 6)
                    .HasSavingThrow(EffectSavingThrowType.Negates)
                    .Build())
            .Build();

        effectDescription.EffectParticleParameters.zoneParticleReference =
            Shatter.EffectDescription.EffectParticleParameters.zoneParticleReference;

        var spell = SpellDefinitionBuilder
            .Create(NAME)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(NAME, Resources.SonicBoom, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolEvocation)
            .SetSpellLevel(5)
            .SetMaterialComponent(MaterialComponentType.None)
            .SetSomaticComponent(true)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Attack)
            .SetCastingTime(ActivationTime.Action)
            .SetEffectDescription(effectDescription)
            .AddCustomSubFeatures(ForcePushOrDragFromEffectPoint.Marker)
            .AddToDB();

        return spell;
    }

    #endregion

    #region Synaptic Static

    internal static SpellDefinition BuildSynapticStatic()
    {
        const string NAME = "SynapticStatic";

        var conditionMuddled = ConditionDefinitionBuilder
            .Create($"Condition{NAME}")
            .SetGuiPresentation(Category.Condition, Gui.EmptyContent, ConditionDazzled)
            .SetPossessive()
            .SetConditionType(ConditionType.Detrimental)
            .SetFeatures(
                FeatureDefinitionCombatAffinityBuilder
                    .Create($"CombatAffinity{NAME}")
                    .SetGuiPresentation($"Condition{NAME}", Category.Condition, Gui.NoLocalization)
                    .SetMyAttackModifierSign(AttackModifierSign.Substract)
                    .SetMyAttackModifierDie(DieType.D6)
                    .AddToDB(),
                FeatureDefinitionAbilityCheckAffinityBuilder
                    .Create($"AbilityCheckAffinity{NAME}")
                    .SetGuiPresentation($"Condition{NAME}", Category.Condition, Gui.NoLocalization)
                    .BuildAndSetAffinityGroups(CharacterAbilityCheckAffinity.None, DieType.D6, 1,
                        AbilityCheckGroupOperation.SubstractDie,
                        (AttributeDefinitions.Strength, string.Empty),
                        (AttributeDefinitions.Dexterity, string.Empty),
                        (AttributeDefinitions.Constitution, string.Empty),
                        (AttributeDefinitions.Intelligence, string.Empty),
                        (AttributeDefinitions.Wisdom, string.Empty),
                        (AttributeDefinitions.Charisma, string.Empty))
                    .AddToDB())
            .SetConditionParticleReference(ConditionFeebleMinded)
            .AddToDB();

        var spell = SpellDefinitionBuilder
            .Create(NAME)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(NAME, Resources.SynapticStatic, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolEvocation)
            .SetSpellLevel(5)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.None)
            .SetSomaticComponent(false)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Buff)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Minute, 1)
                    .SetTargetingData(Side.All, RangeType.Distance, 24, TargetType.Sphere, 4)
                    .SetSavingThrowData(false, AttributeDefinitions.Intelligence, false,
                        EffectDifficultyClassComputation.SpellCastingFeature)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .HasSavingThrow(EffectSavingThrowType.HalfDamage)
                            .SetDamageForm(DamageTypePsychic, 8, DieType.D6)
                            .Build(),
                        EffectFormBuilder
                            .Create()
                            .HasSavingThrow(EffectSavingThrowType.Negates, TurnOccurenceType.EndOfTurn, true)
                            .SetConditionForm(conditionMuddled, ConditionForm.ConditionOperation.Add)
                            .Build())
                    .SetParticleEffectParameters(Feeblemind)
                    .SetImpactEffectParameters(PowerSorakDreadLaughter)
                    .Build())
            .AddToDB();

        return spell;
    }

    #endregion

    #region Banishing Smite

    internal static SpellDefinition BuildBanishingSmite()
    {
        const string NAME = "BanishingSmite";

        var conditionBanishingSmiteEnemy = ConditionDefinitionBuilder
            .Create(ConditionBanished, $"Condition{NAME}Enemy")
            .SetSpecialDuration(DurationType.Minute, 1)
            .AddToDB();

        conditionBanishingSmiteEnemy.permanentlyRemovedIfExtraPlanar = true;

        var additionalDamageBanishingSmite = FeatureDefinitionAdditionalDamageBuilder
            .Create($"AdditionalDamage{NAME}")
            .SetGuiPresentation(NAME, Category.Spell)
            .SetNotificationTag(NAME)
            .SetAttackModeOnly()
            .AddCustomSubFeatures(new PhysicalAttackFinishedByMeBanishingSmite(conditionBanishingSmiteEnemy))
            .SetDamageDice(DieType.D10, 5)
            .SetSpecificDamageType(DamageTypeForce)
            // doesn't follow the standard impact particle reference
            .SetImpactParticleReference(Banishment.EffectDescription.EffectParticleParameters.effectParticleReference)
            .AddToDB();

        var conditionBanishingSmite = ConditionDefinitionBuilder
            .Create($"Condition{NAME}")
            .SetGuiPresentation(NAME, Category.Spell, ConditionBrandingSmite)
            .SetPossessive()
            .SetFeatures(additionalDamageBanishingSmite)
            .SetSpecialInterruptions(ConditionInterruption.AttacksAndDamages)
            .AddToDB();

        var spell = SpellDefinitionBuilder
            .Create(BrandingSmite, NAME)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(NAME, Resources.ThunderousSmite, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolConjuration)
            .SetSpellLevel(5)
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
                    .SetEffectForms(EffectFormBuilder.ConditionForm(conditionBanishingSmite))
                    .SetParticleEffectParameters(Banishment)
                    .Build())
            .AddToDB();

        SmiteSpells2024Context.SmiteSpells.Add(spell);
        SmiteSpells2024Context.SmiteDamages.Add(additionalDamageBanishingSmite);
        SmiteSpells2024Context.SmiteConditions.Add(conditionBanishingSmite);

        return spell;
    }

    private sealed class PhysicalAttackFinishedByMeBanishingSmite(
        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        ConditionDefinition conditionDefinition)
        : IPhysicalAttackFinishedByMe
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
            var rulesetAttacker = attacker.RulesetCharacter;
            var rulesetDefender = defender.RulesetActor;

            if (rollOutcome is RollOutcome.Failure or RollOutcome.CriticalFailure ||
                rulesetAttacker is not { IsDeadOrDyingOrUnconscious: false } ||
                rulesetDefender is not { IsDeadOrDyingOrUnconscious: false } ||
                rulesetDefender.CurrentHitPoints > 50)
            {
                yield break;
            }

            rulesetDefender.InflictCondition(
                conditionDefinition.Name,
                DurationType.Minute,
                1,
                TurnOccurenceType.EndOfTurn,
                AttributeDefinitions.TagEffect,
                rulesetAttacker.guid,
                rulesetAttacker.CurrentFaction.Name,
                1,
                conditionDefinition.Name,
                0,
                0,
                0);
        }
    }

    #endregion

    #region Circle of Magical Negation

    internal static SpellDefinition BuildCircleOfMagicalNegation()
    {
        const string NAME = "CircleOfMagicalNegation";

        var savingThrowAffinityCircleOfMagicalNegation = FeatureDefinitionSavingThrowAffinityBuilder
            .Create($"SavingThrowAffinity{NAME}")
            .SetGuiPresentation(NAME, Category.Spell)
            // only against magic
            .SetAffinities(CharacterSavingThrowAffinity.Advantage, true, AttributeDefinitions.AbilityScoreNames)
            .AddToDB();

        var conditionCircleOfMagicalNegation = ConditionDefinitionBuilder
            .Create($"Condition{NAME}")
            .SetGuiPresentation(NAME, Category.Spell, ConditionShielded)
            .SetPossessive()
            .SetSilent(Silent.WhenAddedOrRemoved)
            .SetFeatures(savingThrowAffinityCircleOfMagicalNegation)
            .AddToDB();

        conditionCircleOfMagicalNegation.GuiPresentation.description = Gui.EmptyContent;

        conditionCircleOfMagicalNegation.AddCustomSubFeatures(
            new MagicEffectBeforeHitConfirmedOnMeCircleOfMagicalNegation(conditionCircleOfMagicalNegation));

        var spell = SpellDefinitionBuilder
            .Create(NAME)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(NAME, Resources.CircleOfMagicalNegation, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolAbjuration)
            .SetSpellLevel(5)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.None)
            .SetSomaticComponent(false)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Buff)
            .SetRequiresConcentration(true)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Minute, 10)
                    .SetTargetingData(Side.Ally, RangeType.Self, 0, TargetType.Sphere, 6)
                    .SetRecurrentEffect(
                        RecurrentEffect.OnActivation | RecurrentEffect.OnEnter | RecurrentEffect.OnTurnStart)
                    .SetEffectForms(EffectFormBuilder.ConditionForm(conditionCircleOfMagicalNegation))
                    .SetParticleEffectParameters(DivineWord)
                    .Build())
            .AddToDB();

        return spell;
    }

    private sealed class MagicEffectBeforeHitConfirmedOnMeCircleOfMagicalNegation(
        ConditionDefinition conditionCircleOfMagicalNegation)
        : IMagicEffectBeforeHitConfirmedOnMe, IRollSavingThrowFinished
    {
        private const string CircleOfMagicalNegationSavedTag = "CircleOfMagicalNegationSaved";

        public IEnumerator OnMagicEffectBeforeHitConfirmedOnMe(
            GameLocationBattleManager battleManager,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            ActionModifier actionModifier,
            RulesetEffect rulesetEffect,
            List<EffectForm> actualEffectForms,
            bool firstTarget,
            bool criticalHit)
        {
            if (!defender.UsedSpecialFeatures.Remove(CircleOfMagicalNegationSavedTag))
            {
                yield break;
            }

            var removed = actualEffectForms.RemoveAll(x =>
                x.HasSavingThrow
                && x.FormType == EffectForm.EffectFormType.Damage
                && x.SavingThrowAffinity == EffectSavingThrowType.HalfDamage);

            if (removed > 0)
            {
                defender.RulesetCharacter.LogCharacterAffectedByCondition(conditionCircleOfMagicalNegation);
            }
        }

        public void OnSavingThrowFinished(
            RulesetActor rulesetActorCaster,
            RulesetActor rulesetActorDefender,
            int saveBonus,
            string abilityScoreName,
            BaseDefinition sourceDefinition,
            List<TrendInfo> modifierTrends,
            List<TrendInfo> advantageTrends,
            int rollModifier,
            int saveDC,
            bool hasHitVisual,
            ref RollOutcome outcome,
            ref int outcomeDelta,
            List<EffectForm> effectForms)
        {
            if (outcome == RollOutcome.Success)
            {
                GameLocationCharacter.GetFromActor(rulesetActorDefender).UsedSpecialFeatures
                    .TryAdd(CircleOfMagicalNegationSavedTag, 0);
            }
        }
    }

    #endregion

    #region Empowered Knowledge

    internal static SpellDefinition BuildEmpoweredKnowledge()
    {
        const string NAME = "EmpoweredKnowledge";

        LimitEffectInstances limiter = new("EmpoweredKnowledge", _ => 1);

        var skillsDb = DatabaseRepository.GetDatabase<SkillDefinition>();
        var powers = new List<FeatureDefinitionPower>();
        var powerPool = FeatureDefinitionPowerBuilder
            .Create($"Power{NAME}")
            .SetGuiPresentationNoContent(true)
            .SetUsesFixed(ActivationTime.NoCost)
            .AddToDB();

        foreach (var skill in skillsDb)
        {
            var power = FeatureDefinitionPowerSharedPoolBuilder
                .Create($"Power{NAME}{skill.Name}")
                .SetGuiPresentation(skill.GuiPresentation.Title, skill.GuiPresentation.Description)
                .SetSharedPool(ActivationTime.NoCost, powerPool)
                .SetShowCasting(false)
                .SetEffectDescription(
                    EffectDescriptionBuilder
                        .Create()
                        .SetDurationData(DurationType.Hour, 1)
                        .SetTargetingData(Side.Ally, RangeType.Touch, 0, TargetType.IndividualsUnique)
                        .SetEffectForms(
                            EffectFormBuilder.ConditionForm(
                                ConditionDefinitionBuilder
                                    .Create($"Condition{NAME}{skill.Name}")
                                    .SetGuiPresentation(
                                        skill.GuiPresentation.Title, Gui.EmptyContent, ConditionBullsStrength)
                                    .SetPossessive()
                                    .SetFeatures(
                                        FeatureDefinitionProficiencyBuilder
                                            .Create($"Proficiency{NAME}{skill.Name}")
                                            .SetGuiPresentation(skill.GuiPresentation)
                                            .SetProficiencies(ProficiencyType.Expertise, skill.Name)
                                            .AddToDB())
                                    .AddCustomSubFeatures(new OnConditionAddedOrRemovedEmpoweredKnowledge(skill.Name))
                                    .AddToDB()))
                        .Build())
                .AddCustomSubFeatures(limiter)
                .AddToDB();

            power.GuiPresentation.hidden = true;

            powers.Add(power);
        }

        PowerBundle.RegisterPowerBundle(powerPool, false, powers);

        var spell = SpellDefinitionBuilder
            .Create(NAME)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(NAME, Resources.EmpoweredKnowledge, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolDivination)
            .SetSpellLevel(5)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.Mundane)
            .SetVerboseComponent(true)
            .SetSomaticComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Buff)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Hour, 1)
                    .SetTargetingData(Side.All, RangeType.Touch, 0, TargetType.IndividualsUnique)
                    .SetCasterEffectParameters(TrueSeeing)
                    .SetEffectEffectParameters(PowerPaladinCleansingTouch)
                    .Build())
            .AddCustomSubFeatures(new PowerOrSpellFinishedByMeEmpoweredKnowledge(powerPool, [.. powers]))
            .AddToDB();

        return spell;
    }

    private sealed class OnConditionAddedOrRemovedEmpoweredKnowledge(string skillName) : IOnConditionAddedOrRemoved
    {
        public void OnConditionAdded(RulesetCharacter target, RulesetCondition rulesetCondition)
        {
            // empty
        }

        public void OnConditionRemoved(RulesetCharacter target, RulesetCondition rulesetCondition)
        {
            var hero = target.GetOriginalHero();

            hero?.ExpertiseProficiencies.Remove(skillName);
        }
    }

    private sealed class PowerOrSpellFinishedByMeEmpoweredKnowledge(
        FeatureDefinitionPower powerPool,
        params FeatureDefinitionPower[] powers) : IPowerOrSpellFinishedByMe
    {
        public IEnumerator OnPowerOrSpellFinishedByMe(CharacterActionMagicEffect action, BaseDefinition baseDefinition)
        {
            if (action.Countered || action.ExecutionFailed)
            {
                yield break;
            }

            var actingCharacter = action.ActingCharacter;
            var rulesetCharacter = actingCharacter.RulesetCharacter;
            var target = action.ActionParams.TargetCharacters[0];
            var rulesetTarget = target.RulesetCharacter;
            var hero = rulesetTarget.GetOriginalHero();

            if (hero == null)
            {
                yield break;
            }

            var usablePowers = new List<RulesetUsablePower>();
            var skillsDb = DatabaseRepository.GetDatabase<SkillDefinition>();

            foreach (var power in powers)
            {
                var skillName = power.Name.Replace("PowerEmpoweredKnowledge", string.Empty);

                if (!skillsDb.TryGetElement(skillName, out var skill))
                {
                    continue;
                }

                var hasSkill =
                    hero.SkillProficiencies.Contains(skill.Name) ||
                    hero.TrainedSkills.Contains(skill) ||
                    hero.FeaturesByType<FeatureDefinitionProficiency>()
                        .Any(x =>
                            x.ProficiencyType == ProficiencyType.Skill &&
                            x.Proficiencies.Contains(skillName));

                var hasExpertise =
                    hero.TrainedExpertises.Contains(skill.name) ||
                    hero.ExpertiseProficiencies.Contains(skill.Name);

                if (!hasSkill || hasExpertise)
                {
                    continue;
                }

                var up = PowerProvider.Get(power, rulesetCharacter);

                usablePowers.Add(up);
                rulesetCharacter.UsablePowers.Add(up);
            }

            var usablePower = PowerProvider.Get(powerPool, rulesetCharacter);

            yield return actingCharacter.MyReactToSpendPowerBundle(
                usablePower,
                [target],
                actingCharacter,
                "EmpoweredKnowledge",
                reactionValidated: ReactionValidated);

            rulesetCharacter.UsablePowers.Remove(usablePower);
            usablePowers.ForEach(x => rulesetCharacter.UsablePowers.Remove(x));

            yield break;

            void ReactionValidated(ReactionRequestSpendBundlePower reactionRequest)
            {
                var selectedPower = powers[reactionRequest.SelectedSubOption];
                var locationCharacterService = ServiceRepository.GetService<IGameLocationCharacterService>();
                var contenders =
                    locationCharacterService.PartyCharacters.Union(locationCharacterService.GuestCharacters)
                        .ToArray();

                foreach (var contender in contenders)
                {
                    var rulesetContender = contender.RulesetCharacter;

                    foreach (var skill in skillsDb)
                    {
                        var conditionName = $"ConditionEmpoweredKnowledge{skill.Name}";

                        if (rulesetContender.TryGetConditionOfCategoryAndType(
                                AttributeDefinitions.TagEffect, conditionName, out var activeCondition) &&
                            activeCondition.SourceGuid == rulesetCharacter.Guid &&
                            (activeCondition.TargetGuid != rulesetTarget.Guid ||
                             !selectedPower.Name.Contains(skill.Name)))
                        {
                            rulesetContender.RemoveCondition(activeCondition);
                        }
                    }
                }
            }
        }
    }

    #endregion

    #region Holy Weapon

    internal static SpellDefinition BuildHolyWeapon()
    {
        const string NAME = "HolyWeapon";

        var additionalDamage = FeatureDefinitionAdditionalDamageBuilder
            .Create($"AdditionalDamage{NAME}")
            .SetGuiPresentation(Category.Feature, SpiritualWeapon)
            .SetNotificationTag("HolyWeapon")
            .SetDamageDice(DieType.D8, 2)
            .SetSpecificDamageType(DamageTypeRadiant)
            .AddCustomSubFeatures(
                new AddTagToWeapon(
                    TagsDefinitions.MagicalWeapon, TagsDefinitions.Criticity.Important, ValidatorsWeapon.AlwaysValid))
            .AddToDB();

        var power = FeatureDefinitionPowerBuilder
            .Create($"Power{NAME}")
            .SetGuiPresentation(Category.Feature, Sprites.GetSprite(NAME, Resources.PowerHolyWeapon, 256, 128))
            .SetUsesFixed(ActivationTime.BonusAction)
            .SetEffectDescription(EffectDescriptionBuilder.Create()
                .SetDurationData(DurationType.Minute, 1)
                .SetTargetingData(Side.All, RangeType.Distance, 24, TargetType.IndividualsUnique)
                .SetSavingThrowData(false, AttributeDefinitions.Constitution, true,
                    EffectDifficultyClassComputation.SpellCastingFeature)
                .SetEffectForms(
                    EffectFormBuilder.WithSavingThrow(EffectSavingThrowType.HalfDamage)
                        .DamageForm(DamageTypeRadiant, 4, DieType.D8),
                    EffectFormBuilder.WithSavingThrow(EffectSavingThrowType.Negates, TurnOccurenceType.EndOfTurn, true)
                        .SetConditionForm(ConditionDefinitions.ConditionBlinded, ConditionForm.ConditionOperation.Add)
                        .Build())
                .SetParticleEffectParameters(FaerieFire)
                .SetCasterEffectParameters(PowerOathOfDevotionTurnUnholy)
                .SetImpactEffectParameters(FeatureDefinitionAdditionalDamages.AdditionalDamageBrandingSmite)
                .SetConditionEffectParameters(ConditionDefinitions.ConditionBlinded)
                .Build())
            .AddCustomSubFeatures(new CustomBehaviorHolyWeapon(additionalDamage))
            .AddToDB();

        var condition = ConditionDefinitionBuilder
            .Create($"Condition{NAME}")
            .SetGuiPresentationNoContent(true)
            .SetSilent(Silent.WhenAddedOrRemoved)
            .SetFeatures(power)
            .AddCustomSubFeatures(AddUsablePowersFromCondition.Marker)
            .AddToDB();

        var light = Light.EffectDescription.GetFirstFormOfType(EffectForm.EffectFormType.LightSource).lightSourceForm;

        var spell = SpellDefinitionBuilder
            .Create(NAME)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(NAME, Resources.HolyWeapon, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolEvocation)
            .SetSpellLevel(5)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.None)
            .SetSomaticComponent(true)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Buff)
            .SetRequiresConcentration(true)
            .SetEffectDescription(EffectDescriptionBuilder.Create(Light)
                .SetDurationData(DurationType.Hour, 1)
                .SetTargetingData(Side.Enemy, RangeType.Touch, 0, TargetType.Item,
                    itemSelectionType: ActionDefinitions.ItemSelectionType.EquippedNoLightSource)
                .SetEffectForms(
                    EffectFormBuilder.LightSourceForm(LightSourceType.Basic, 6, 6, light.color,
                        light.graphicsPrefabReference),
                    EffectFormBuilder.ItemPropertyForm(ItemPropertyUsage.Unlimited, 0, additionalDamage),
                    EffectFormBuilder.ConditionForm(condition, ConditionForm.ConditionOperation.Add, true))
                .SetCasterEffectParameters(HolyAura)
                .SetEffectEffectParameters(PowerOathOfJugementPurgeCorruption)
                .Build())
            .AddToDB();

        return spell;
    }

    private sealed class CustomBehaviorHolyWeapon(FeatureDefinitionAdditionalDamage additionalDamage)
        : IPowerOrSpellInitiatedByMe, IFilterTargetingCharacter
    {
        public bool EnforceFullSelection => false;

        public bool IsValid(CursorLocationSelectTarget __instance, GameLocationCharacter target)
        {
            var hero = target.RulesetCharacter.GetOriginalHero();
            var rulesetItem = hero?.CharacterInventory.EnumerateAllSlots()
                .FirstOrDefault(x =>
                    x.EquipedItem?.DynamicItemProperties.Any(y => y.FeatureDefinition == additionalDamage) == true)
                ?.EquipedItem;
            var sourceEffectGuid = rulesetItem?.DynamicItemProperties
                .FirstOrDefault(x => x.FeatureDefinition == additionalDamage)?.SourceEffectGuid ?? 0;
            var caster = EffectHelpers.GetCharacterByEffectGuid(sourceEffectGuid);
            var hasHolyWeapon = caster != null && __instance.ActionParams.ActingCharacter.RulesetCharacter == caster;

            if (!hasHolyWeapon)
            {
                __instance.actionModifier.FailureFlags.Add("Failure/&TargetMustHaveHolyWeapon");
            }

            return hasHolyWeapon;
        }

        public IEnumerator OnPowerOrSpellInitiatedByMe(CharacterActionMagicEffect action, BaseDefinition baseDefinition)
        {
            var ally = action.ActionParams.TargetCharacters[0];

            EffectHelpers.StartVisualEffect(ally, ally, PowerOathOfDevotionTurnUnholy, EffectHelpers.EffectType.Caster);

            var targets = Gui.Battle?.GetContenders(ally, withinRange: 6) ?? [];

            action.ActionParams.TargetCharacters.SetRange(targets);
            action.ActionParams.ActionModifiers.SetRange(GetActionModifiers(targets.Count));
            action.ActingCharacter.RulesetCharacter.BreakConcentration();

            yield break;
        }
    }

    #endregion

    #region Steel Whirlwind

    internal static SpellDefinition BuildSteelWhirlwind()
    {
        const string Name = "SteelWhirlwind";

        var powerTeleport = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}Teleport")
            .SetGuiPresentation(Category.Feature, PowerMelekTeleport)
            .SetUsesFixed(ActivationTime.NoCost, RechargeRate.None)
            .SetEffectDescription(EffectDescriptionBuilder.Create()
                .SetTargetingData(Side.Ally, RangeType.Distance, 6, TargetType.Position)
                .SetEffectForms(EffectFormBuilder.MotionForm(MotionForm.MotionType.TeleportToDestination))
                .SetParticleEffectParameters(PowerMelekTeleport)
                .Build())
            .AddCustomSubFeatures(new FilterTargetingPositionSteelWhirlwind())
            .AddToDB();

        var conditionTeleport = ConditionDefinitionBuilder
            .Create($"Condition{Name}Teleport")
            .SetGuiPresentationNoContent(true)
            .SetSilent(Silent.WhenAddedOrRemoved)
            .SetSpecialInterruptions(ConditionInterruption.UsedActionOrReaction, ConditionInterruption.Moved)
            .SetFeatures(powerTeleport)
            .AddCustomSubFeatures(AddUsablePowersFromCondition.Marker)
            .AddToDB();

        var spell = SpellDefinitionBuilder
            .Create(Name)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(Name, Resources.SteelWhirlwind, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolConjuration)
            .SetSpellLevel(5)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.Specific)
            .SetSpecificMaterialComponent(TagsDefinitions.WeaponTagMelee, 0, false)
            .SetVerboseComponent(false)
            .SetSomaticComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Attack)
            .SetEffectDescription(EffectDescriptionBuilder.Create()
                .SetDurationData(DurationType.Round)
                .SetTargetingData(Side.Enemy, RangeType.MeleeHit, 6, TargetType.IndividualsUnique, 5)
                .SetEffectForms(
                    EffectFormBuilder.DamageForm(DamageTypeForce, 6, DieType.D10),
                    EffectFormBuilder.ConditionForm(conditionTeleport, applyToSelf: true))
                .SetParticleEffectParameters(GravitySlam)
                .SetImpactEffectParameters(ArcaneSword)
                .Build())
            .AddCustomSubFeatures(new PowerOrSpellFinishedByMeSteelWhirlwind())
            .AddToDB();

        return spell;
    }

    // keep a tab of all allowed positions for filtering using ContextualFormation collection
    // ContextualFormation is only used by the game when spawning new locations, so it's safe in this context
    private sealed class PowerOrSpellFinishedByMeSteelWhirlwind : IPowerOrSpellFinishedByMe
    {
        public IEnumerator OnPowerOrSpellFinishedByMe(CharacterActionMagicEffect action, BaseDefinition baseDefinition)
        {
            var positioningService = ServiceRepository.GetService<IGameLocationPositioningService>();
            var visibilityService = ServiceRepository.GetService<IGameLocationVisibilityService>();

            var actingCharacter = action.ActingCharacter;

            actingCharacter.contextualFormation = [];

            foreach (var boxInt in action.ActionParams.TargetCharacters
                         .Select(targetCharacter => new BoxInt(
                             targetCharacter.LocationPosition, new int3(-1, -1, -1), new int3(1, 1, 1))))
            {
                foreach (var position in boxInt.EnumerateAllPositionsWithin())
                {
                    if (!visibilityService.MyIsCellPerceivedByCharacter(position, actingCharacter) ||
                        !positioningService.CanPlaceCharacter(
                            actingCharacter, position, CellHelpers.PlacementMode.Station) ||
                        !positioningService.CanCharacterStayAtPosition_Floor(
                            actingCharacter, position, onlyCheckCellsWithRealGround: true))
                    {
                        continue;
                    }

                    actingCharacter.ContextualFormation.Add(position);
                }
            }

            yield break;
        }
    }

    private sealed class FilterTargetingPositionSteelWhirlwind : IFilterTargetingPosition
    {
        public IEnumerator ComputeValidPositions(CursorLocationSelectPosition cursorLocationSelectPosition)
        {
            var source = cursorLocationSelectPosition.ActionParams.ActingCharacter;

            cursorLocationSelectPosition.validPositionsCache.SetRange(source.ContextualFormation);

            yield break;
        }
    }

    #endregion

    #region Swift Quiver

    internal const string SwiftQuiverAttackTag = "SwiftQuiverAttack";

    internal static SpellDefinition BuildSwiftQuiver()
    {
        const string NAME = "SwiftQuiver";

        var condition = ConditionDefinitionBuilder
            .Create($"Condition{NAME}")
            .SetGuiPresentation(NAME, Category.Spell, ConditionDefinitions.ConditionReckless)
            .SetPossessive()
            .AddCustomSubFeatures(
                new AddExtraSwiftQuiverAttack(
                    ActionDefinitions.ActionType.Bonus,
                    ValidatorsCharacter.HasNoneOfConditions(ConditionMonkFlurryOfBlowsUnarmedStrikeBonus.Name)))
            .CopyParticleReferences(Haste)
            .AddToDB();

        condition.conditionParticleReference =
            SpiderClimb.EffectDescription.EffectParticleParameters.conditionParticleReference;

        var spell = SpellDefinitionBuilder
            .Create(NAME)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(NAME, Resources.SwiftQuiver, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolTransmutation)
            .SetSpellLevel(5)
            .SetCastingTime(ActivationTime.BonusAction)
            .SetMaterialComponent(MaterialComponentType.Specific)
            .SetSpecificMaterialComponent(TagsDefinitions.ItemTagConsumable, 0, false)
            .SetSomaticComponent(true)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Buff)
            .SetRequiresConcentration(true)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Minute, 1)
                    // 24 seems to be the max range on Solasta ranged weapons
                    .SetTargetingData(Side.Enemy, RangeType.Distance, 24, TargetType.Individuals, 2)
                    .SetEffectForms(EffectFormBuilder.ConditionForm(condition, applyToSelf: true))
                    .SetCasterEffectParameters(WindWall)
                    .Build())
            .AddCustomSubFeatures(AttackAfterMagicEffect.MarkerRangedWeaponAttack)
            .AddToDB();

        return spell;
    }

    internal sealed class AddExtraSwiftQuiverAttack : AddExtraAttackBase
    {
        internal AddExtraSwiftQuiverAttack(
            ActionDefinitions.ActionType actionType,
            params IsCharacterValidHandler[] validators) : base(actionType, validators)
        {
            // Empty
        }

        private static bool IsBowOrCrossbow(RulesetAttackMode mode, RulesetItem item, RulesetCharacterHero hero)
        {
            return ValidatorsWeapon.IsOfWeaponType(
                LongbowType, ShortbowType, HeavyCrossbowType, LightCrossbowType,
                CustomWeaponsContext.HandXbowWeaponType)(mode, item, hero);
        }

        protected override List<RulesetAttackMode> GetAttackModes([NotNull] RulesetCharacter character)
        {
            if (character is not RulesetCharacterHero hero)
            {
                return null;
            }

            var item = hero.CharacterInventory.InventorySlotsByName[EquipmentDefinitions.SlotTypeMainHand].EquipedItem;

            if (item == null ||
                !IsBowOrCrossbow(null, item, hero))
            {
                return null;
            }

            var strikeDefinition = item.ItemDefinition;
            var attackMode = hero.RefreshAttackMode(
                ActionType,
                strikeDefinition,
                strikeDefinition.WeaponDescription,
                ValidatorsCharacter.IsFreeOffhandVanilla(hero),
                true,
                EquipmentDefinitions.SlotTypeMainHand,
                hero.attackModifiers,
                hero.FeaturesOrigin,
                item
            );

            attackMode.AttacksNumber = 2;
            attackMode.AddAttackTagAsNeeded(SwiftQuiverAttackTag);

            return [attackMode];
        }
    }

    #endregion

    #region Telekinesis

    internal const string ConditionTelekinesisRestrainedName = "ConditionTelekinesisRestrained";

    private const int TelekinesisRange = 12;

    internal static SpellDefinition BuildTelekinesis()
    {
        const string Name = "Telekinesis";

        var conditionTelekinesisRestrained = ConditionDefinitionBuilder
            .Create(ConditionDefinitions.ConditionRestrained, ConditionTelekinesisRestrainedName)
            .SetParentCondition(ConditionDefinitions.ConditionRestrained)
            .SetFeatures(
                FeatureDefinitionMoveModeBuilder
                    .Create("MoveModeFly0")
                    .SetGuiPresentation(FeatureDefinitionMoveModes.MoveModeFly12.GuiPresentation)
                    .SetMode(MoveMode.Fly, 0)
                    .AddToDB())
            .AddToDB();

        // there is indeed a typo on tag
        // ReSharper disable once StringLiteralTypo
        conditionTelekinesisRestrained.ConditionTags.Add("Verticality");

        var sprite = Sprites.GetSprite(Name, Resources.Telekinesis, 128);

        var powerTelekinesis = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}")
            .SetGuiPresentation(Name, Category.Spell, sprite)
            .SetUsesFixed(ActivationTime.Action)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Round, 1, TurnOccurenceType.EndOfSourceTurn)
                    .SetTargetingData(Side.All, RangeType.Distance, TelekinesisRange, TargetType.IndividualsUnique)
                    .ExcludeCaster()
                    .SetParticleEffectParameters(MistyStep)
                    .Build())
            .AddToDB();

        var conditionTelekinesis = ConditionDefinitionBuilder
            .Create($"Condition{Name}")
            .SetGuiPresentation(Category.Condition, ConditionRevealedByDetectGoodOrEvil)
            .SetPossessive()
            .SetFeatures(powerTelekinesis)
            .AddCustomSubFeatures(
                AddUsablePowersFromCondition.Marker,
                OnConditionAddedOrRemovedTelekinesis.Marker)
            .CopyParticleReferences(SpiderClimb)
            .AddToDB();

        var powerTelekinesisNoCost = FeatureDefinitionPowerBuilder
            .Create(powerTelekinesis, $"Power{Name}NoCost")
            .SetUsesFixed(ActivationTime.NoCost)
            .AddToDB();

        var conditionTelekinesisNoCost = ConditionDefinitionBuilder
            .Create($"Condition{Name}NoCost")
            .SetGuiPresentationNoContent(true)
            .SetSilent(Silent.WhenAddedOrRemoved)
            .SetFeatures(powerTelekinesisNoCost)
            .AddCustomSubFeatures(
                AddUsablePowersFromCondition.Marker,
                OnConditionAddedOrRemovedTelekinesis.Marker)
            .SetSpecialInterruptions(ConditionInterruption.AnyBattleTurnEnd)
            .AddToDB();

        var spell = SpellDefinitionBuilder
            .Create(Name)
            .SetGuiPresentation(Category.Spell, sprite)
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolTransmutation)
            .SetSpellLevel(5)
            .SetCastingTime(ActivationTime.Action)
            .SetMaterialComponent(MaterialComponentType.None)
            .SetVerboseComponent(true)
            .SetSomaticComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Debuff)
            .SetRequiresConcentration(true)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Minute, 10)
                    .SetTargetingData(Side.Ally, RangeType.Self, 0, TargetType.Self)
                    .SetEffectForms(
                        EffectFormBuilder.ConditionForm(conditionTelekinesisNoCost),
                        EffectFormBuilder.ConditionForm(conditionTelekinesis))
                    .SetParticleEffectParameters(MindTwist)
                    .SetConditionEffectParameters()
                    .Build())
            .AddToDB();

        var customBehavior = new CustomBehaviorTelekinesis(conditionTelekinesisNoCost, spell);

        powerTelekinesis.AddCustomSubFeatures(customBehavior);
        powerTelekinesisNoCost.AddCustomSubFeatures(customBehavior, ValidatorsValidatePowerUse.InCombat);

        return spell;
    }

    private static void RemoveExistingRestrainedInstances(
        // ReSharper disable once SuggestBaseTypeForParameter
        RulesetCharacter rulesetCaster)
    {
        if (Gui.Battle == null)
        {
            return;
        }

        foreach (var rulesetContender in Gui.Battle.EnemyContenders
                     .Select(locationContender => locationContender.RulesetCharacter))
        {
            if (!rulesetContender.TryGetConditionOfCategoryAndType(AttributeDefinitions.TagEffect,
                    ConditionTelekinesisRestrainedName, out var activeCondition) ||
                activeCondition.SourceGuid != rulesetCaster.Guid)
            {
                continue;
            }

            rulesetContender.RemoveCondition(activeCondition);

            break;
        }
    }

    internal sealed class OnConditionAddedOrRemovedTelekinesis : IOnConditionAddedOrRemoved
    {
        internal static readonly OnConditionAddedOrRemovedTelekinesis Marker = new();

        public void OnConditionAdded(RulesetCharacter target, RulesetCondition rulesetCondition)
        {
            // empty
        }

        public void OnConditionRemoved(RulesetCharacter target, RulesetCondition rulesetCondition)
        {
            RemoveExistingRestrainedInstances(target);
        }
    }

    internal sealed class CustomBehaviorTelekinesis(
        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        ConditionDefinition conditionTelekinesisNoCost,
        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        SpellDefinition spellTelekinesis)
        : IFilterTargetingPosition, IPowerOrSpellFinishedByMe, ISelectPositionAfterCharacter
    {
        public IEnumerator ComputeValidPositions(CursorLocationSelectPosition cursorLocationSelectPosition)
        {
            var positioningService = ServiceRepository.GetService<IGameLocationPositioningService>();
            var actingCharacter = cursorLocationSelectPosition.ActionParams?.ActingCharacter;
            var targetCharacter = cursorLocationSelectPosition.ActionParams?.TargetCharacters[0];

            if (actingCharacter == null || targetCharacter == null)
            {
                yield break;
            }

            const int RANGE = TelekinesisRange / 2;
            var boxInt = new BoxInt(targetCharacter.LocationPosition, int3.zero, int3.zero);

            boxInt.Inflate(TelekinesisRange);

            foreach (var position in boxInt.EnumerateAllPositionsWithin())
            {
                if (
                    // must use vanilla distance calculation here
                    int3.Distance(targetCharacter.LocationPosition, position) > RANGE ||
                    // must use vanilla distance calculation here
                    int3.Distance(actingCharacter.LocationPosition, position) > TelekinesisRange ||
                    !positioningService.CanPlaceCharacter(
                        targetCharacter, position, CellHelpers.PlacementMode.Station))
                {
                    continue;
                }

                cursorLocationSelectPosition.validPositionsCache.Add(position);

                if (cursorLocationSelectPosition.stopwatch.Elapsed.TotalMilliseconds > 0.5)
                {
                    yield return null;
                }
            }
        }

        public IEnumerator OnPowerOrSpellFinishedByMe(CharacterActionMagicEffect action, BaseDefinition baseDefinition)
        {
            if (action.Countered || action.ExecutionFailed)
            {
                yield break;
            }

            var actingCharacter = action.ActingCharacter;
            var actingRulesetCharacter = actingCharacter.RulesetCharacter;

            if (actingRulesetCharacter.TryGetConditionOfCategoryAndType(
                    AttributeDefinitions.TagEffect, conditionTelekinesisNoCost.Name, out var activeTelekinesis))
            {
                actingRulesetCharacter.RemoveCondition(activeTelekinesis);
            }

            var rulesetSpell =
                actingRulesetCharacter.SpellsCastByMe.FirstOrDefault(x => x.SpellDefinition == spellTelekinesis);

            if (rulesetSpell == null)
            {
                yield break;
            }

            RemoveExistingRestrainedInstances(actingRulesetCharacter);

            var targetCharacter = action.ActionParams.TargetCharacters[0];

            yield return RollAbilityCheckAndTryMoveApplyRestrained(
                actingCharacter,
                targetCharacter,
                rulesetSpell,
                action);
        }

        public int PositionRange => TelekinesisRange;

        public bool EnforcePositionSelection(CursorLocationSelectPosition cursorLocationSelectPosition)
        {
            var targetCharacter = cursorLocationSelectPosition.ActionParams.TargetCharacters[0];

            return targetCharacter.Side == Side.Ally;
        }

        private static IEnumerator ResolveRolls(
            GameLocationCharacter actor,
            GameLocationCharacter opponent,
            string spellCastingAbility,
            ActionDefinitions.Id actionId,
            AbilityCheckData abilityCheckData,
            AbilityCheckData opponentAbilityCheckData)
        {
            var actionModifier1 = new ActionModifier();
            var actionModifier2 = new ActionModifier();

            var abilityCheckBonus1 = actor.RulesetCharacter.ComputeBaseAbilityCheckBonus(spellCastingAbility,
                actionModifier1.AbilityCheckModifierTrends, string.Empty);
            var abilityCheckBonus2 = opponent.RulesetCharacter.ComputeBaseAbilityCheckBonus(
                AttributeDefinitions.Strength,
                actionModifier2.AbilityCheckModifierTrends, string.Empty);

            var contextField1 = 0;

            if (!actor.RulesetCharacter.IsWearingHeavyArmor())
            {
                contextField1 |= 64;
            }

            actor.ComputeAbilityCheckActionModifier(spellCastingAbility, string.Empty, actionModifier1, contextField1);

            var contextField2 = 1;

            if (!opponent.RulesetCharacter.IsWearingHeavyArmor())
            {
                contextField2 |= 64;
            }

            opponent.ComputeAbilityCheckActionModifier(
                AttributeDefinitions.Strength, string.Empty, actionModifier2, contextField2);

            actor.RulesetCharacter.EnumerateFeaturesToBrowse<IActionPerformanceProvider>(
                actor.RulesetCharacter.FeaturesToBrowse, actor.RulesetCharacter.FeaturesOrigin);

            foreach (var key in actor.RulesetCharacter.FeaturesToBrowse)
            {
                foreach (var executionModifier in (key as IActionPerformanceProvider)!.ActionExecutionModifiers)
                {
                    if (executionModifier.actionId != actionId ||
                        !actor.RulesetCharacter.IsMatchingEquipementCondition(executionModifier.equipmentContext) ||
                        executionModifier.advantageType == AdvantageType.None)
                    {
                        continue;
                    }

                    var num = executionModifier.advantageType == AdvantageType.Advantage ? 1 : -1;
                    var featureOrigin = actor.RulesetCharacter.FeaturesOrigin[key];
                    actionModifier1.AbilityCheckAdvantageTrends.Add(new TrendInfo(num, featureOrigin.sourceType,
                        featureOrigin.sourceName, featureOrigin.source));
                }
            }

            yield return TryAlterOutcomeAttributeCheck.ResolveContestCheck(
                actor.RulesetCharacter,
                abilityCheckBonus1,
                actionModifier1.AbilityCheckModifier,
                spellCastingAbility,
                string.Empty,
                actionModifier1.AbilityCheckAdvantageTrends,
                actionModifier1.AbilityCheckModifierTrends,
                opponent.RulesetCharacter,
                abilityCheckBonus2,
                actionModifier2.AbilityCheckModifier,
                AttributeDefinitions.Strength,
                string.Empty,
                actionModifier2.AbilityCheckAdvantageTrends,
                actionModifier2.AbilityCheckModifierTrends,
                abilityCheckData,
                opponentAbilityCheckData);
        }

        private static IEnumerator RollAbilityCheckAndTryMoveApplyRestrained(
            GameLocationCharacter actingCharacter,
            // ReSharper disable once SuggestBaseTypeForParameter
            GameLocationCharacter targetCharacter,
            RulesetEffectSpell rulesetSpell,
            CharacterAction action)
        {
            var actingRulesetCharacter = actingCharacter.RulesetCharacter;
            var targetRulesetCharacter = targetCharacter.RulesetCharacter;
            var isEnemy = actingCharacter.IsOppositeSide(targetCharacter.Side);

            if (isEnemy)
            {
                var abilityCheckData = new AbilityCheckData { AbilityCheckActionModifier = new ActionModifier() };
                var opponentAbilityCheckData =
                    new AbilityCheckData { AbilityCheckActionModifier = new ActionModifier() };
                var spellCastingAbility = actingRulesetCharacter.SpellsCastByMe
                    .FirstOrDefault(x => x.SpellDefinition == rulesetSpell.SpellDefinition)?.SpellRepertoire?
                    // assume Intelligence if no repertoire (ritual spell only used on Force Knight)
                    .SpellCastingAbility ?? AttributeDefinitions.Intelligence;

                yield return ResolveRolls(
                    actingCharacter, targetCharacter, spellCastingAbility, action.ActionId,
                    abilityCheckData, opponentAbilityCheckData);

                if (abilityCheckData.AbilityCheckRollOutcome is RollOutcome.Failure or RollOutcome.CriticalFailure)
                {
                    yield break;
                }
            }

            if (action.ActionParams.Positions.Count > 0)
            {
                var actionParams = new CharacterActionParams(targetCharacter, ActionDefinitions.Id.SpiritRallyTeleport)
                {
                    Positions = { action.ActionParams.Positions[0] }
                };

                ServiceRepository.GetService<IGameLocationActionService>().ExecuteAction(actionParams, null, true);
            }

            if (!isEnemy)
            {
                yield break;
            }

            targetRulesetCharacter.InflictCondition(
                ConditionTelekinesisRestrainedName,
                DurationType.Round,
                1,
                TurnOccurenceType.EndOfSourceTurn,
                AttributeDefinitions.TagEffect,
                actingRulesetCharacter.guid,
                actingRulesetCharacter.CurrentFaction.Name,
                1,
                ConditionTelekinesisRestrainedName,
                0,
                0,
                0);
        }
    }

    #endregion

    #region Wall of Force

    internal static SpellDefinition BuildWallOfForce()
    {
        // vanilla ships Wall of Force flagged as not implemented, so reuse its definition, proxy and particles
        var spell = WallOfForce;

        spell.contentPack = CeContentPackContext.CeContentPack; // required otherwise it messes up spells UI
        spell.implemented = true;
        spell.materialComponentType = MaterialComponentType.Mundane;
        spell.specificMaterialComponentConsumed = false;
        spell.GuiPresentation.spriteReference = Sprites.GetSprite("WallOfForce", Resources.WallOfForce, 128);

        // vanilla MoveBlocker topology is never processed by the game; see GameLocationEnvironmentManagerPatcher
        // and GameLocationPositioningManagerPatcher for the code that makes it block movement
        spell.EffectDescription.EffectForms.Add(
            EffectFormBuilder.TopologyForm(TopologyForm.Type.ProjectileBlocker, true));

        // a Disintegrate spell destroys the wall instantly
        Disintegrate.AddCustomSubFeatures(new PowerOrSpellFinishedByMeDisintegrateWallOfForce());

        return spell;
    }

    private sealed class PowerOrSpellFinishedByMeDisintegrateWallOfForce : IPowerOrSpellFinishedByMe
    {
        public IEnumerator OnPowerOrSpellFinishedByMe(CharacterActionMagicEffect action, BaseDefinition baseDefinition)
        {
            // vanilla Disintegrate can already target effect proxies, so the wall is targeted through its proxy
            foreach (var target in action.ActionParams.TargetCharacters)
            {
                if (target.RulesetCharacter is RulesetCharacterEffectProxy proxy &&
                    RulesetEntity.TryGetEntity<RulesetEffectSpell>(proxy.EffectGuid, out var rulesetEffectSpell) &&
                    rulesetEffectSpell.SpellDefinition == WallOfForce)
                {
                    rulesetEffectSpell.Terminate(true);
                }
            }

            yield break;
        }
    }

    #endregion

    #region Negative Energy Flood

    internal static SpellDefinition BuildNegativeEnergyFlood()
    {
        const string NAME = "NegativeEnergyFlood";

        var baseZombie = TryGetDefinition<MonsterDefinition>("Zombie", out var zombie)
            ? zombie
            : MonsterDefinitions.Ghoul;

        var monsterZombie = MonsterDefinitionBuilder
            .Create(baseZombie, $"{NAME}Zombie")
            .SetOrUpdateGuiPresentation($"{NAME}Zombie", Category.Monster)
            .NoExperienceGain()
            .SetDroppedLootDefinition(null)
            .SetFullyControlledWhenAllied(true)
            .SetDefaultFaction(FactionDefinitions.Party)
            .SetBestiaryEntry(BestiaryDefinitions.BestiaryEntry.None)
            .SetDungeonMakerPresence(MonsterDefinition.DungeonMaker.None)
            .AddToDB();

        var powerZombie = FeatureDefinitionPowerBuilder
            .Create($"Power{NAME}Zombie")
            .SetGuiPresentation(NAME, Category.Spell, hidden: true)
            .SetUsesFixed(ActivationTime.NoCost)
            .SetShowCasting(false)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Hour, 24)
                    .SetTargetingData(Side.Ally, RangeType.Distance, 24, TargetType.Position)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetSummonCreatureForm(1, monsterZombie.Name)
                            .Build())
                    .SetParticleEffectParameters(VampiricTouch)
                    .Build())
            .AddToDB();

        var spell = SpellDefinitionBuilder
            .Create(NAME)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(NAME, Resources.NegativeEnergyFlood, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolNecromancy)
            .SetSpellLevel(5)
            .SetCastingTime(ActivationTime.Action)
            // RAW a broken bone and a square of black silk, covered by a pouch or focus
            .SetMaterialComponent(MaterialComponentType.Mundane)
            .SetSomaticComponent(false)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Attack)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.All, RangeType.Distance, 12, TargetType.IndividualsUnique)
                    .SetEffectAdvancement(EffectIncrementMethod.PerAdditionalSlotLevel, additionalDicePerIncrement: 1)
                    .SetSavingThrowData(false, AttributeDefinitions.Constitution, false,
                        EffectDifficultyClassComputation.SpellCastingFeature)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .HasSavingThrow(EffectSavingThrowType.HalfDamage)
                            .SetDamageForm(DamageTypeNecrotic, 3, DieType.D10, 25)
                            .Build())
                    .SetParticleEffectParameters(Blight)
                    .Build())
            .AddToDB();

        spell.AddCustomSubFeatures(new CustomBehaviorNegativeEnergyFlood(spell, powerZombie));

        return spell;
    }

    private sealed class CustomBehaviorNegativeEnergyFlood(
        SpellDefinition spellNegativeEnergyFlood,
        FeatureDefinitionPower powerZombie) : IMagicEffectBeforeHitConfirmedOnEnemy, IPowerOrSpellFinishedByMe
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
            var rulesetDefender = defender.RulesetCharacter;

            if (rulesetEffect.SourceDefinition != spellNegativeEnergyFlood ||
                rulesetDefender == null ||
                rulesetDefender.CharacterFamily != CharacterFamilyDefinitions.Undead.Name)
            {
                yield break;
            }

            // an Undead target takes no damage and gains 3d10 temporary hit points instead, which don't upcast
            actualEffectForms.Clear();

            var tempHitPoints = 0;

            for (var i = 0; i < 3; i++)
            {
                tempHitPoints += RollDie(DieType.D10, AdvantageType.None, out _, out _);
            }

            rulesetDefender.ReceiveTemporaryHitPoints(
                tempHitPoints, DurationType.UntilLongRest, 0, TurnOccurenceType.StartOfTurn,
                attacker.RulesetCharacter.Guid);
        }

        public IEnumerator OnPowerOrSpellFinishedByMe(CharacterActionMagicEffect action, BaseDefinition baseDefinition)
        {
            var caster = action.ActingCharacter;
            var rulesetCaster = caster.RulesetCharacter;

            foreach (var target in action.ActionParams.TargetCharacters)
            {
                // RAW a Humanoid killed by this spell rises at the start of the caster's next turn; here it rises
                // as soon as the spell finishes
                if (target.RulesetCharacter is not RulesetCharacterMonster { IsDeadOrDying: true } rulesetTarget ||
                    rulesetTarget.CharacterFamily != CharacterFamilyDefinitions.Humanoid.Name)
                {
                    continue;
                }

                var implementationService = ServiceRepository.GetService<IRulesetImplementationService>();
                var usablePower = PowerProvider.Get(powerZombie, rulesetCaster);
                var actionParams = new CharacterActionParams(caster, ActionDefinitions.Id.PowerNoCost)
                {
                    RulesetEffect = implementationService.InstantiateEffectPower(rulesetCaster, usablePower, false),
                    UsablePower = usablePower,
                    Positions = { target.LocationPosition }
                };

                ServiceRepository.GetService<IGameLocationActionService>().ExecuteAction(actionParams, null, true);
            }

            yield break;
        }
    }

    #endregion

    #region Grave Ground

    internal static SpellDefinition BuildGraveGround()
    {
        const string NAME = "GraveGround";

        var conditionGraveGround = ConditionDefinitionBuilder
            .Create($"Condition{NAME}")
            .SetGuiPresentation(Category.Condition, ConditionBaned)
            .SetConditionType(ConditionType.Detrimental)
            .SetPossessive()
            // until the end of the creature's next turn
            .SetSpecialDuration(DurationType.Round, 1)
            .SetFeatures(
                FeatureDefinitionBuilder
                    .Create($"Feature{NAME}")
                    .SetGuiPresentationNoContent(true)
                    .AddCustomSubFeatures(new SubtractDieFromDamageRolls(DieType.D6))
                    .AddToDB())
            .AddToDB();

        var spell = SpellDefinitionBuilder
            .Create(NAME)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(NAME, Resources.GraveGround, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolNecromancy)
            .SetSpellLevel(5)
            .SetCastingTime(ActivationTime.Action)
            // RAW a handful of grave dirt, covered by a pouch or focus
            .SetMaterialComponent(MaterialComponentType.Mundane)
            .SetSomaticComponent(true)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Debuff)
            .SetRequiresConcentration(true)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Minute, 1)
                    // RAW four 10-foot squares arranged as you like; here a 20-foot square
                    .SetTargetingData(Side.Enemy, RangeType.Distance, 24, TargetType.Cube, 4, 1)
                    .SetEffectAdvancement(EffectIncrementMethod.PerAdditionalSlotLevel, additionalDicePerIncrement: 1)
                    .SetSavingThrowData(false, AttributeDefinitions.Strength, false,
                        EffectDifficultyClassComputation.SpellCastingFeature)
                    .SetRecurrentEffect(
                        RecurrentEffect.OnActivation | RecurrentEffect.OnEnter | RecurrentEffect.OnTurnEnd)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .SetDamageForm(DamageTypeNecrotic, 6, DieType.D6)
                            .Build(),
                        EffectFormBuilder
                            .Create()
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .SetConditionForm(conditionGraveGround, ConditionForm.ConditionOperation.Add)
                            .Build(),
                        // RAW difficult terrain only for enemies; Solasta terrain affects every creature
                        EffectFormBuilder.TopologyForm(TopologyForm.Type.DifficultThrough, false))
                    .SetParticleEffectParameters(SpikeGrowth)
                    .SetImpactEffectParameters(Blight)
                    .Build())
            .AddCustomSubFeatures(RecurrentEffectOncePerTurn.Mark)
            .AddToDB();

        return spell;
    }

    #endregion

    #region Songal's Elemental Suffusion

    internal static SpellDefinition BuildSongalsElementalSuffusion()
    {
        const string NAME = "SongalsElementalSuffusion";

        var sprite = Sprites.GetSprite(NAME, Resources.SongalsElementalSuffusion, 128);
        // the aura shown on the caster while suffused, picked to match the chosen element
        var elements = new (string DamageType, FeatureDefinitionDamageAffinity Resistance, IMagicEffect Impact,
            AssetReference Particles)[]
            {
                (DamageTypeAcid, FeatureDefinitionDamageAffinitys.DamageAffinityAcidResistance, AcidSplash,
                    ConditionOnAcidPilgrim.conditionParticleReference),
                (DamageTypeCold, FeatureDefinitionDamageAffinitys.DamageAffinityColdResistance, RayOfFrost,
                    MonsterDefinitions.WindSnake.MonsterPresentation.attachedParticlesReference),
                (DamageTypeFire, FeatureDefinitionDamageAffinitys.DamageAffinityFireResistance, FireBolt,
                    MonsterDefinitions.Fire_Jester.MonsterPresentation.attachedParticlesReference),
                (DamageTypeLightning, FeatureDefinitionDamageAffinitys.DamageAffinityLightningResistance,
                    ShockingGrasp,
                    PowerTraditionShockArcanistArcaneFury.EffectDescription.EffectParticleParameters
                        .conditionParticleReference),
                (DamageTypeThunder, FeatureDefinitionDamageAffinitys.DamageAffinityThunderResistance, Shatter,
                    MonsterDefinitions.Air_Elemental.MonsterPresentation.attachedParticlesReference)
            };

        var subSpells = new List<SpellDefinition>();

        foreach (var (damageType, resistance, impact, particles) in elements)
        {
            var element = damageType.Replace("Damage", string.Empty);

            var pulseForms = new[]
            {
                EffectFormBuilder
                    .Create()
                    .HasSavingThrow(EffectSavingThrowType.HalfDamage)
                    .SetDamageForm(damageType, 2, DieType.D6)
                    .Build(),
                EffectFormBuilder
                    .Create()
                    .HasSavingThrow(EffectSavingThrowType.Negates)
                    .SetMotionForm(MotionForm.MotionType.FallProne)
                    .Build()
            };

            var powerPulse = FeatureDefinitionPowerBuilder
                .Create($"Power{NAME}{element}")
                .SetGuiPresentation(NAME, Category.Spell, hidden: true)
                .SetUsesFixed(ActivationTime.NoCost)
                .SetShowCasting(false)
                .SetEffectDescription(
                    EffectDescriptionBuilder
                        .Create()
                        // RAW each creature of your choice; here enemies are affected
                        .SetTargetingData(Side.Enemy, RangeType.Self, 0, TargetType.Sphere, 3)
                        .ExcludeCaster()
                        .SetSavingThrowData(false, AttributeDefinitions.Dexterity, false,
                            EffectDifficultyClassComputation.SpellCastingFeature)
                        .SetEffectForms(pulseForms)
                        .SetParticleEffectParameters(Thunderwave)
                        .SetImpactEffectParameters(impact)
                        .Build())
                .AddToDB();

            var condition = ConditionDefinitionBuilder
                .Create(ConditionDefinitions.ConditionFlyingAdaptive, $"Condition{NAME}{element}")
                .SetGuiPresentation(NAME, Category.Spell, ConditionDefinitions.ConditionFlying)
                .SetPossessive()
                .SetParentCondition(ConditionDefinitions.ConditionFlying)
                .SetFeatures(resistance, FeatureDefinitionMoveModes.MoveModeFly6)
                .SetConditionParticleReference(particles)
                .AddToDB();

            condition.GuiPresentation.description = Gui.EmptyContent;
            condition.AddCustomSubFeatures(new CharacterTurnStartListenerSongalsElementalSuffusion(powerPulse));

            var subSpell = SpellDefinitionBuilder
                .Create($"{NAME}{element}")
                .SetGuiPresentation(Category.Spell, sprite)
                .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolTransmutation)
                .SetSpellLevel(5)
                .SetCastingTime(ActivationTime.Action)
                .SetSpecificMaterialComponent("Pearl", 100, false)
                .SetSomaticComponent(true)
                .SetVerboseComponent(true)
                .SetVocalSpellSameType(VocalSpellSemeType.Buff)
                .SetRequiresConcentration(true)
                .SetEffectDescription(
                    EffectDescriptionBuilder
                        .Create()
                        .SetDurationData(DurationType.Minute, 1)
                        .SetTargetingData(Side.Enemy, RangeType.Self, 0, TargetType.Sphere, 3)
                        .ExcludeCaster()
                        .SetSavingThrowData(false, AttributeDefinitions.Dexterity, false,
                            EffectDifficultyClassComputation.SpellCastingFeature)
                        .SetEffectForms(
                        [
                            EffectFormBuilder.ConditionForm(condition, ConditionForm.ConditionOperation.Add, true,
                                true),
                            .. pulseForms
                        ])
                        .SetParticleEffectParameters(Thunderwave)
                        .SetCasterEffectParameters(ConjureElemental)
                        .SetImpactEffectParameters(impact)
                        .Build())
                .AddToDB();

            subSpells.Add(subSpell);
        }

        var spell = SpellDefinitionBuilder
            .Create(NAME)
            .SetGuiPresentation(Category.Spell, sprite)
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolTransmutation)
            .SetSpellLevel(5)
            .SetCastingTime(ActivationTime.Action)
            // RAW a pearl worth 100+ GP, like vanilla Identify
            .SetSpecificMaterialComponent("Pearl", 100, false)
            .SetSomaticComponent(true)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Buff)
            .SetRequiresConcentration(true)
            .SetEffectDescription(
                // UI Only
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Minute, 1)
                    .SetTargetingData(Side.Enemy, RangeType.Self, 0, TargetType.Sphere, 3)
                    .Build())
            .SetSubSpells([.. subSpells])
            .AddToDB();

        return spell;
    }

    private sealed class CharacterTurnStartListenerSongalsElementalSuffusion(FeatureDefinitionPower powerPulse)
        : ICharacterTurnStartListener
    {
        public void OnCharacterTurnStarted(GameLocationCharacter character)
        {
            var targets = Gui.Battle?.GetContenders(character, withinRange: 3) ?? [];

            if (targets.Count == 0)
            {
                return;
            }

            var usablePower = PowerProvider.Get(powerPulse, character.RulesetCharacter);

            character.MyExecuteActionSpendPower(usablePower, [.. targets]);
        }
    }

    #endregion

    #region Spirit Lantern

    private static ConditionDefinition _conditionSpiritLantern;
    private static FeatureDefinitionPower _powerSpiritLanternPool;

    internal static SpellDefinition BuildSpiritLantern()
    {
        const string NAME = "SpiritLantern";

        var sprite = Sprites.GetSprite(NAME, Resources.SpiritLantern, 128);

        // the pool holds the soul fragments: it starts empty and grows as enemies die in the lantern's light
        var powerPool = FeatureDefinitionPowerBuilder
            .Create($"Power{NAME}")
            .SetGuiPresentation(Category.Feature, sprite)
            .SetUsesFixed(ActivationTime.NoCost, RechargeRate.None, 1, 0)
            .AddCustomSubFeatures(ModifyPowerVisibility.Hidden)
            .AddToDB();

        var powerDrainLife = FeatureDefinitionPowerSharedPoolBuilder
            .Create($"Power{NAME}DrainLife")
            .SetGuiPresentation(Category.Feature, sprite)
            .SetSharedPool(ActivationTime.BonusAction, powerPool)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.All, RangeType.Distance, 12, TargetType.IndividualsUnique)
                    .SetSavingThrowData(false, AttributeDefinitions.Constitution, false,
                        EffectDifficultyClassComputation.SpellCastingFeature)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .HasSavingThrow(EffectSavingThrowType.HalfDamage)
                            .SetDamageForm(DamageTypeNecrotic, 4, DieType.D8)
                            .Build())
                    .SetParticleEffectParameters(VampiricTouch)
                    .SetImpactEffectParameters(Blight)
                    .Build())
            .AddToDB();

        var powerRepairUndead = FeatureDefinitionPowerSharedPoolBuilder
            .Create($"Power{NAME}RepairUndead")
            .SetGuiPresentation(Category.Feature, sprite)
            .SetSharedPool(ActivationTime.BonusAction, powerPool)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.All, RangeType.Distance, 12, TargetType.IndividualsUnique)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetHealingForm(HealingComputation.Dice, 0, DieType.D8, 4, false,
                                HealingCap.MaximumHitPoints)
                            .Build())
                    .SetParticleEffectParameters(VampiricTouch)
                    .SetImpactEffectParameters(CureWounds)
                    .Build())
            .AddToDB();

        var conditionWardAlly = ConditionDefinitionBuilder
            .Create($"Condition{NAME}WardAlly")
            .SetGuiPresentation(Category.Condition, ConditionShielded)
            .SetPossessive()
            .SetSpecialDuration(DurationType.Round, 1, (TurnOccurenceType)ExtraTurnOccurenceType.StartOfSourceTurn)
            .SetFeatures(
                FeatureDefinitionCombatAffinityBuilder
                    .Create($"CombatAffinity{NAME}WardAlly")
                    .SetGuiPresentation($"Condition{NAME}WardAlly", Category.Condition, Gui.NoLocalization)
                    .SetAttackOnMeAdvantage(AdvantageType.Disadvantage)
                    .AddToDB())
            .AddToDB();

        var powerWardAlly = FeatureDefinitionPowerSharedPoolBuilder
            .Create($"Power{NAME}WardAlly")
            .SetGuiPresentation(Category.Feature, sprite)
            .SetSharedPool(ActivationTime.BonusAction, powerPool)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Ally, RangeType.Distance, 12, TargetType.IndividualsUnique)
                    .SetEffectForms(EffectFormBuilder.ConditionForm(conditionWardAlly))
                    .SetParticleEffectParameters(ShieldOfFaith)
                    .Build())
            .AddToDB();

        var condition = ConditionDefinitionBuilder
            .Create($"Condition{NAME}")
            .SetGuiPresentation(NAME, Category.Spell, ConditionDefinitions.ConditionLightSensitive)
            .SetPossessive()
            .SetConditionType(ConditionType.Beneficial)
            .SetFeatures(powerPool, powerDrainLife, powerRepairUndead, powerWardAlly)
            .AddCustomSubFeatures(AddUsablePowersFromCondition.Marker)
            .AddToDB();

        condition.GuiPresentation.description = Gui.EmptyContent;

        powerPool.AddCustomSubFeatures(
            new ModifyPowerPoolAmount
            {
                PowerPool = powerPool,
                Type = PowerPoolBonusCalculationType.ConditionAmount,
                Attribute = condition.Name
            });

        powerDrainLife.AddCustomSubFeatures(new ModifyEffectDescriptionSpiritLantern(powerDrainLife, condition));
        powerRepairUndead.AddCustomSubFeatures(
            new ModifyEffectDescriptionSpiritLantern(powerRepairUndead, condition),
            new FilterTargetingCharacterUndead());

        var light = Light.EffectDescription.GetFirstFormOfType(EffectForm.EffectFormType.LightSource).lightSourceForm;

        var spell = SpellDefinitionBuilder
            .Create(NAME)
            .SetGuiPresentation(Category.Spell, sprite)
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolNecromancy)
            .SetSpellLevel(5)
            .SetCastingTime(ActivationTime.Action)
            // RAW a black lantern, covered by a pouch or focus
            .SetMaterialComponent(MaterialComponentType.Mundane)
            .SetSomaticComponent(true)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Buff)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create(Light)
                    .SetDurationData(DurationType.Minute, 10)
                    .SetTargetingData(Side.Ally, RangeType.Self, 0, TargetType.Self)
                    .SetEffectForms(
                        EffectFormBuilder.ConditionForm(condition),
                        // dim light only, in a 60-foot radius
                        EffectFormBuilder.LightSourceForm(LightSourceType.Basic, 0, 12, light.color,
                            light.graphicsPrefabReference))
                    .SetCasterEffectParameters(VampiricTouch)
                    .SetEffectEffectParameters(PowerOathOfJugementPurgeCorruption)
                    .Build())
            .AddToDB();

        spell.AddCustomSubFeatures(new PowerOrSpellFinishedByMeSpiritLantern(powerPool, condition));

        _conditionSpiritLantern = condition;
        _powerSpiritLanternPool = powerPool;

        return spell;
    }

    // an enemy died: each caster with a lantern within 60 feet of it gains a soul fragment, up to the lantern's limit
    internal static void HandleSpiritLanternFragment(
        GameLocationBattleManager battleManager, GameLocationCharacter downedCreature)
    {
        if (!_conditionSpiritLantern ||
            battleManager.Battle == null ||
            downedCreature.RulesetActor is not { IsDead: true })
        {
            return;
        }

        foreach (var caster in battleManager.Battle.AllContenders
                     .Where(x =>
                         x.RulesetCharacter is { IsDeadOrDyingOrUnconscious: false } &&
                         x.IsOppositeSide(downedCreature.Side) &&
                         x.IsWithinRange(downedCreature, 12))
                     .ToArray())
        {
            var rulesetCaster = caster.RulesetCharacter;

            if (!rulesetCaster.TryGetConditionOfCategoryAndType(
                    AttributeDefinitions.TagEffect, _conditionSpiritLantern.Name, out _))
            {
                continue;
            }

            var usablePool = PowerProvider.Get(_powerSpiritLanternPool, rulesetCaster);

            if (usablePool.RemainingUses >= rulesetCaster.GetMaxUsesOfPower(usablePool))
            {
                continue;
            }

            rulesetCaster.UpdateUsageForPowerPool(-1, usablePool);
            rulesetCaster.LogCharacterAffectedByCondition(_conditionSpiritLantern);
        }
    }

    private sealed class PowerOrSpellFinishedByMeSpiritLantern(
        FeatureDefinitionPower powerPool,
        ConditionDefinition conditionSpiritLantern) : IPowerOrSpellFinishedByMe
    {
        public IEnumerator OnPowerOrSpellFinishedByMe(CharacterActionMagicEffect action, BaseDefinition baseDefinition)
        {
            var rulesetCaster = action.ActingCharacter.RulesetCharacter;

            if (!rulesetCaster.TryGetConditionOfCategoryAndType(
                    AttributeDefinitions.TagEffect, conditionSpiritLantern.Name, out var activeCondition))
            {
                yield break;
            }

            // the lantern holds as many fragments as your spellcasting ability modifier, and starts empty
            var abilityBonus = action.ActionParams.RulesetEffect is RulesetEffectSpell rulesetEffectSpell
                ? rulesetEffectSpell.ComputeSourceAbilityBonus(rulesetCaster)
                : 0;

            activeCondition.Amount = System.Math.Max(1, abilityBonus);

            var usablePool = PowerProvider.Get(powerPool, rulesetCaster);

            rulesetCaster.UpdateUsageForPowerPool(rulesetCaster.GetMaxUsesOfPower(usablePool), usablePool);
        }
    }

    // Drain Life and Repair Undead add your spellcasting ability modifier, stored as the lantern's amount
    private sealed class ModifyEffectDescriptionSpiritLantern(
        FeatureDefinitionPower power,
        ConditionDefinition conditionSpiritLantern) : IModifyEffectDescription
    {
        // the caster also picks this up from the condition's powers, so only modify the power it belongs to
        public bool IsValid(BaseDefinition definition, RulesetCharacter character, EffectDescription effectDescription)
        {
            return definition == power &&
                   character.TryGetConditionOfCategoryAndType(
                       AttributeDefinitions.TagEffect, conditionSpiritLantern.Name, out _);
        }

        public EffectDescription GetEffectDescription(
            BaseDefinition definition,
            EffectDescription effectDescription,
            RulesetCharacter character,
            RulesetEffect rulesetEffect)
        {
            if (!character.TryGetConditionOfCategoryAndType(
                    AttributeDefinitions.TagEffect, conditionSpiritLantern.Name, out var activeCondition))
            {
                return effectDescription;
            }

            foreach (var effectForm in effectDescription.EffectForms)
            {
                // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                switch (effectForm.FormType)
                {
                    case EffectForm.EffectFormType.Damage:
                        effectForm.DamageForm.BonusDamage = activeCondition.Amount;
                        break;
                    case EffectForm.EffectFormType.Healing:
                        effectForm.HealingForm.BonusHealing = activeCondition.Amount;
                        break;
                }
            }

            return effectDescription;
        }
    }

    private sealed class FilterTargetingCharacterUndead : IFilterTargetingCharacter
    {
        public bool EnforceFullSelection => false;

        public bool IsValid(CursorLocationSelectTarget __instance, GameLocationCharacter target)
        {
            if (target.RulesetCharacter.CharacterFamily == CharacterFamilyDefinitions.Undead.Name)
            {
                return true;
            }

            __instance.actionModifier.FailureFlags.Add("Failure/&MustBeUndead");

            return false;
        }
    }

    #endregion

    #region Yolande's Regal Presence

    internal static SpellDefinition BuildYolandesRegalPresence()
    {
        const string NAME = "YolandesRegalPresence";

        // shows the emanation around the caster, the same way Spirit Guardians does
        var conditionSelf = ConditionDefinitionBuilder
            .Create($"Condition{NAME}Self")
            .SetGuiPresentationNoContent(true)
            .SetSilent(Silent.WhenAddedOrRemoved)
            .CopyParticleReferences(ConditionSpiritGuardiansSelf)
            .AddToDB();

        var spell = SpellDefinitionBuilder
            .Create(NAME)
            .SetGuiPresentation(Category.Spell, Sprites.GetSprite(NAME, Resources.YolandesRegalPresence, 128))
            .SetSchoolOfMagic(SchoolOfMagicDefinitions.SchoolEnchantment)
            .SetSpellLevel(5)
            .SetCastingTime(ActivationTime.Action)
            // RAW a miniature tiara, covered by a pouch or focus
            .SetMaterialComponent(MaterialComponentType.Mundane)
            .SetSomaticComponent(true)
            .SetVerboseComponent(true)
            .SetVocalSpellSameType(VocalSpellSemeType.Debuff)
            .SetRequiresConcentration(true)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Minute, 1)
                    // RAW you can force creatures you can see to save; here every enemy in the emanation saves
                    .SetTargetingData(Side.Enemy, RangeType.Self, 0, TargetType.Sphere, 2)
                    .ExcludeCaster()
                    .SetSavingThrowData(false, AttributeDefinitions.Wisdom, false,
                        EffectDifficultyClassComputation.SpellCastingFeature)
                    .SetRecurrentEffect(RecurrentEffect.OnEnter | RecurrentEffect.OnTurnEnd)
                    .SetParticleEffectParameters(SpiritGuardians)
                    .SetCasterEffectParameters(HolyAura)
                    .SetImpactEffectParameters(MindTwist)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetDamageForm(DamageTypePsychic, 4, DieType.D6)
                            .HasSavingThrow(EffectSavingThrowType.HalfDamage)
                            .Build(),
                        EffectFormBuilder
                            .Create()
                            .SetMotionForm(MotionForm.MotionType.FallProne)
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .Build(),
                        // RAW the push is optional; here it always pushes 10 feet
                        EffectFormBuilder
                            .Create()
                            .SetMotionForm(MotionForm.MotionType.PushFromOrigin, 2)
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .Build())
                    .Build())
            .AddCustomSubFeatures(
                ApplyRecurrentEffectWhenSourceMoves.Mark,
                RecurrentEffectOncePerTurn.Mark,
                new PowerOrSpellFinishedByMeYolandesRegalPresence(conditionSelf))
            .AddToDB();

        return spell;
    }

    // the spell has no effect on activation, so its effect forms never reach the caster on cast
    private sealed class PowerOrSpellFinishedByMeYolandesRegalPresence(ConditionDefinition conditionSelf)
        : IPowerOrSpellFinishedByMe
    {
        public IEnumerator OnPowerOrSpellFinishedByMe(CharacterActionMagicEffect action, BaseDefinition baseDefinition)
        {
            if (action.Countered || action.ExecutionFailed)
            {
                yield break;
            }

            var rulesetCharacter = action.ActingCharacter.RulesetCharacter;
            var rulesetEffect = action.ActionParams.RulesetEffect;
            var activeCondition = rulesetCharacter.InflictCondition(
                conditionSelf.Name,
                DurationType.Round,
                rulesetEffect.RemainingRounds,
                TurnOccurenceType.EndOfTurn,
                AttributeDefinitions.TagEffect,
                rulesetCharacter.guid,
                rulesetCharacter.CurrentFaction.Name,
                1,
                conditionSelf.Name,
                0,
                0,
                0);

            // ends the visual together with the spell, including when concentration is lost
            rulesetEffect.TrackCondition(
                rulesetCharacter, rulesetCharacter.guid, rulesetCharacter, rulesetCharacter.guid,
                activeCondition, AttributeDefinitions.TagEffect);
        }
    }

    #endregion
}
