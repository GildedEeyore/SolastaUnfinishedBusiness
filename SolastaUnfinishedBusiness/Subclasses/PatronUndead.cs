using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Api.GameExtensions;
using SolastaUnfinishedBusiness.Api.Helpers;
using SolastaUnfinishedBusiness.Api.LanguageExtensions;
using SolastaUnfinishedBusiness.Behaviors;
using SolastaUnfinishedBusiness.Builders;
using SolastaUnfinishedBusiness.Builders.Features;
using SolastaUnfinishedBusiness.CustomUI;
using SolastaUnfinishedBusiness.Interfaces;
using SolastaUnfinishedBusiness.Models;
using SolastaUnfinishedBusiness.Properties;
using SolastaUnfinishedBusiness.Validators;
using static RuleDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.CharacterClassDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.FeatureDefinitionPowers;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.SpellDefinitions;
using static SolastaUnfinishedBusiness.Builders.Features.AutoPreparedSpellsGroupBuilder;

namespace SolastaUnfinishedBusiness.Subclasses;

[UsedImplicitly]
public sealed class PatronUndead : AbstractSubclass
{
    internal const string Name = "PatronUndead";

    private static ConditionDefinition _conditionFormOfDread;
    private static ConditionDefinition _conditionSuperiorDread;

    // once per turn benefits: character guid -> the battle turn they were last used on
    private static readonly Dictionary<ulong, int> FrightfulAvatarTurns = [];
    private static readonly Dictionary<ulong, int> ArcaneNecrosisTurns = [];
    private static readonly Dictionary<ulong, int> DreadedNecrosisTurns = [];

    // Frightful Avatar on spell attacks: caster guid -> first creature hit by the spell being cast
    private static readonly Dictionary<ulong, GameLocationCharacter> FrightfulAvatarSpellTargets = [];

    public PatronUndead()
    {
        // LEVEL 01 - Undead Spells

        // the 2024 table grants spells at warlock levels 3, 5, 7 and 9, whatever level the patron is learned at;
        // spells the game doesn't have are swapped for ones of the same level
        var autoPreparedSpellsUndead = FeatureDefinitionAutoPreparedSpellsBuilder
            .Create($"AutoPreparedSpells{Name}")
            .SetGuiPresentation($"AutoPreparedSpells{Name}", Category.Feature)
            .SetAutoTag("Patron")
            .SetPreparedSpellGroups(
                BuildSpellGroup(3, Bane, Blindness, RayOfEnfeeblement, SpellsContext.RayOfSickness),
                BuildSpellGroup(5, VampiricTouch, SpellsContext.SpiritShroud),
                BuildSpellGroup(7, GreaterInvisibility, PhantasmalKiller),
                BuildSpellGroup(9, Contagion, CloudKill))
            .SetSpellcastingClass(Warlock)
            .AddToDB();

        // the subclass selection screen doesn't scroll, so spells are listed on one line by warlock level
        autoPreparedSpellsUndead.GuiPresentation.description = Gui.Format(
            $"Feature/&AutoPreparedSpells{Name}Description",
            string.Join("; ", autoPreparedSpellsUndead.AutoPreparedSpellsGroups
                .Select(x => Gui.Format($"Feature/&AutoPreparedSpells{Name}LevelFormat",
                    x.ClassLevel.ToString(), string.Join(", ", x.SpellsList.Select(s => s.FormatTitle()))))));

        // LEVEL 01 - Form of Dread

        var conditionFormOfDread = ConditionDefinitionBuilder
            .Create($"Condition{Name}FormOfDread")
            .SetGuiPresentation(Category.Condition, ConditionDefinitions.ConditionSpiritGuardiansSelf)
            .SetPossessive()
            .SetFeatures(
                FeatureDefinitionConditionAffinitys.ConditionAffinityFrightenedImmunity,
                FeatureDefinitionConditionAffinitys.ConditionAffinityFrightenedFearImmunity)
            .SetCancellingConditions(
                DatabaseRepository.GetDatabase<ConditionDefinition>().Where(x =>
                    x.IsSubtypeOf(ConditionDefinitions.ConditionIncapacitated.Name)).ToArray())
            .AddCancellingConditions(ConditionDefinitions.ConditionPetrified)
            .AddToDB();

        _conditionFormOfDread = conditionFormOfDread;

        var powerFormOfDread = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}FormOfDread")
            .SetGuiPresentation(Category.Feature, PowerSorcererHauntedSoulSpiritVisage)
            .SetUsesAbilityBonus(ActivationTime.BonusAction, RechargeRate.LongRest, AttributeDefinitions.Charisma)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Minute, 1)
                    .SetTargetingData(Side.Ally, RangeType.Self, 0, TargetType.Self)
                    .SetEffectForms(
                        EffectFormBuilder.ConditionForm(conditionFormOfDread),
                        // the warlock level bonus is added on GetEffectDescription
                        EffectFormBuilder.Create().SetTempHpForm(0, DieType.D10, 1).Build())
                    .SetParticleEffectParameters(PowerSorcererHauntedSoulSpiritVisage)
                    .Build())
            .AddCustomSubFeatures(new ValidatorsValidatePowerUse(
                ValidatorsCharacter.HasNoneOfConditions(conditionFormOfDread.Name)))
            .AddToDB();

        var powerFormOfDreadDismiss = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}FormOfDreadDismiss")
            .SetGuiPresentation(Category.Feature,
                Sprites.GetSprite("FlightDismiss", Resources.PowerAngelicFormDismiss, 256, 128))
            .SetUsesFixed(ActivationTime.NoCost)
            .SetShowCasting(false)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Ally, RangeType.Self, 0, TargetType.Self)
                    .SetEffectForms(
                        EffectFormBuilder.ConditionForm(conditionFormOfDread,
                            ConditionForm.ConditionOperation.Remove))
                    .Build())
            .AddCustomSubFeatures(new ValidatorsValidatePowerUse(
                ValidatorsCharacter.HasAnyOfConditions(conditionFormOfDread.Name)))
            .AddToDB();

        var powerFrightfulAvatar = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}FrightfulAvatar")
            .SetGuiPresentation(Category.Feature, hidden: true)
            .SetUsesFixed(ActivationTime.NoCost)
            .SetShowCasting(false)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Round, 1, TurnOccurenceType.EndOfSourceTurn)
                    .SetTargetingData(Side.All, RangeType.Distance, 24, TargetType.IndividualsUnique)
                    .SetSavingThrowData(false, AttributeDefinitions.Wisdom, false,
                        EffectDifficultyClassComputation.SpellCastingFeature)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .HasSavingThrow(EffectSavingThrowType.Negates)
                            .SetConditionForm(ConditionDefinitions.ConditionFrightened,
                                ConditionForm.ConditionOperation.Add)
                            .Build())
                    .SetImpactEffectParameters(Fear)
                    .Build())
            .AddToDB();

        powerFrightfulAvatar.AddCustomSubFeatures(ModifyPowerVisibility.Hidden);

        var featureSetFormOfDread = FeatureDefinitionFeatureSetBuilder
            .Create($"FeatureSet{Name}FormOfDread")
            .SetGuiPresentation(Category.Feature)
            .AddFeatureSet(powerFormOfDread, powerFormOfDreadDismiss, powerFrightfulAvatar)
            .AddToDB();

        // LEVEL 06 - Grave Touched

        var featureArcaneNecrosis = FeatureDefinitionBuilder
            .Create($"Feature{Name}ArcaneNecrosis")
            .SetGuiPresentation(Category.Feature, hidden: true)
            .AddToDB();

        featureArcaneNecrosis.AddCustomSubFeatures(new ModifyDamageAffinityArcaneNecrosis(featureArcaneNecrosis));

        var featureDreadedNecrosis = FeatureDefinitionBuilder
            .Create($"Feature{Name}DreadedNecrosis")
            .SetGuiPresentation(Category.Feature, hidden: true)
            .AddToDB();

        var conditionAffinityUndeadEndurance = FeatureDefinitionConditionAffinityBuilder
            .Create(FeatureDefinitionConditionAffinitys.ConditionAffinityElfFeyAncestrySleep,
                $"ConditionAffinity{Name}UndeadEndurance")
            .SetGuiPresentationNoContent(true)
            .AddToDB();

        var featureSetGraveTouched = FeatureDefinitionFeatureSetBuilder
            .Create($"FeatureSet{Name}GraveTouched")
            .SetGuiPresentation(Category.Feature)
            .AddFeatureSet(featureArcaneNecrosis, featureDreadedNecrosis, conditionAffinityUndeadEndurance)
            .AddToDB();

        // LEVEL 10 - Necrotic Husk

        var conditionNecroticHusk = ConditionDefinitionBuilder
            .Create($"Condition{Name}NecroticHusk")
            .SetGuiPresentationNoContent(true)
            .SetSilent(Silent.WhenAddedOrRemoved)
            .SetFeatures(FeatureDefinitionDamageAffinitys.DamageAffinityNecroticImmunity)
            .AddToDB();

        var featureNecroticResilience = FeatureDefinitionBuilder
            .Create($"Feature{Name}NecroticResilience")
            .SetGuiPresentationNoContent(true)
            .AddToDB();

        var powerUnholyResuscitation = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}UnholyResuscitation")
            .SetGuiPresentation(Category.Feature)
            .SetUsesFixed(ActivationTime.NoCost, RechargeRate.ShortRest)
            .SetExplicitAbilityScore(AttributeDefinitions.Charisma)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Enemy, RangeType.Distance, 6, TargetType.IndividualsUnique)
                    .SetSavingThrowData(false, AttributeDefinitions.Constitution, false,
                        EffectDifficultyClassComputation.SpellCastingFeature)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetDamageForm(DamageTypeNecrotic, 2, DieType.D10)
                            .SetBonusMode(AddBonusMode.AbilityBonus)
                            .HasSavingThrow(EffectSavingThrowType.HalfDamage)
                            .Build())
                    .SetParticleEffectParameters(CircleOfDeath)
                    .Build())
            .AddToDB();

        powerUnholyResuscitation.AddCustomSubFeatures(
            ModifyPowerVisibility.Hidden,
            new OnReducedToZeroHpByEnemyUnholyResuscitation(powerUnholyResuscitation));

        var featureSetNecroticHusk = FeatureDefinitionFeatureSetBuilder
            .Create($"FeatureSet{Name}NecroticHusk")
            .SetGuiPresentation(Category.Feature)
            .AddFeatureSet(
                FeatureDefinitionDamageAffinitys.DamageAffinityNecroticResistance,
                featureNecroticResilience,
                powerUnholyResuscitation)
            .AddToDB();

        // LEVEL 14 - Superior Dread

        // flies like the Dragonborn Draconic Flight, the adaptive affinity raises fly speed to walk speed
        var conditionSuperiorDread = ConditionDefinitionBuilder
            .Create(ConditionDefinitions.ConditionFlyingAdaptive, $"Condition{Name}SuperiorDread")
            .SetGuiPresentation(Category.Condition, ConditionDefinitions.ConditionFlying)
            .SetPossessive()
            .AddFeatures(
                FeatureDefinitionMoveModes.MoveModeFly6,
                FeatureDefinitionDamageAffinitys.DamageAffinityBludgeoningResistanceTrue,
                FeatureDefinitionDamageAffinitys.DamageAffinityPiercingResistanceTrue,
                FeatureDefinitionDamageAffinitys.DamageAffinitySlashingResistanceTrue)
            .AddToDB();

        _conditionSuperiorDread = conditionSuperiorDread;

        var featureSuperiorDread = FeatureDefinitionBuilder
            .Create($"Feature{Name}SuperiorDread")
            .SetGuiPresentation(Category.Feature)
            .AddToDB();

        // Form of Dread adds the level 10 and 14 benefits as their own conditions, which end with it
        conditionFormOfDread.AddCustomSubFeatures(
            new OnConditionAddedOrRemovedFormOfDread(
                (featureNecroticResilience, conditionNecroticHusk),
                (featureSuperiorDread, conditionSuperiorDread)));

        // Form of Dread carries the behaviors of all the features that depend on it
        powerFormOfDread.AddCustomSubFeatures(
            new CustomBehaviorFormOfDread(
                powerFormOfDread, powerFrightfulAvatar, featureArcaneNecrosis, featureDreadedNecrosis));

        // MAIN

        Subclass = CharacterSubclassDefinitionBuilder
            .Create(Name)
            .SetGuiPresentation(Category.Subclass, CharacterSubclassDefinitions.SorcerousHauntedSoul)
            .AddFeaturesAtLevel(1, featureSetFormOfDread, autoPreparedSpellsUndead)
            .AddFeaturesAtLevel(6, featureSetGraveTouched)
            .AddFeaturesAtLevel(10, featureSetNecroticHusk)
            .AddFeaturesAtLevel(14, featureSuperiorDread)
            .AddToDB();
    }

    internal override CharacterClassDefinition Klass => Warlock;

    internal override CharacterSubclassDefinition Subclass { get; }

    internal override FeatureDefinitionSubclassChoice SubclassChoice =>
        FeatureDefinitionSubclassChoices.SubclassChoiceWarlockOtherworldlyPatrons;

    // ReSharper disable once UnassignedGetOnlyAutoProperty
    internal override DeityDefinition DeityDefinition { get; }

    // any turn counts, so these can also be used on reactions; outside battles there are no turns to track
    private static int GetBattleTurn()
    {
        var battle = Gui.Battle;

        return battle == null
            ? -1
            : (battle.CurrentRound * 1000) +
              battle.InitiativeSortedContenders.IndexOf(battle.ActiveContenderIgnoringLegendary);
    }

    private static bool WasUsedThisTurn(Dictionary<ulong, int> turns, GameLocationCharacter character)
    {
        var turn = GetBattleTurn();

        return turn >= 0 && turns.TryGetValue(character.Guid, out var usedTurn) && usedTurn == turn;
    }

    private static bool TryUseOncePerTurn(Dictionary<ulong, int> turns, GameLocationCharacter character)
    {
        if (WasUsedThisTurn(turns, character))
        {
            return false;
        }

        turns[character.Guid] = GetBattleTurn();

        return true;
    }

    private static bool IsInFormOfDread(RulesetCharacter character)
    {
        return _conditionFormOfDread && character.HasConditionOfType(_conditionFormOfDread.Name);
    }

    private static bool IsAttackRoll(RulesetEffect rulesetEffect)
    {
        return rulesetEffect.EffectDescription.RangeType is RangeType.MeleeHit or RangeType.RangeHit;
    }

    private static string ArcaneNecrosisTag(RulesetEffect rulesetEffect)
    {
        return $"ArcaneNecrosis:{rulesetEffect.Name}:{rulesetEffect.Guid}";
    }

    //
    // Profane Casting
    //

    // called on RulesetCharacter.IsComponentVerbalValid, IsComponentSomaticValid and IsComponentMaterialValid
    internal static void ValidateProfaneCasting(
        RulesetCharacter caster, SpellDefinition spell, bool isMaterial, ref bool result, ref string failure)
    {
        if (result ||
            !_conditionSuperiorDread ||
            !caster.HasConditionOfType(_conditionSuperiorDread.Name) ||
            (spell.SchoolOfMagic != SchoolOfMagicDefinitions.SchoolConjuration.Name &&
             spell.SchoolOfMagic != SchoolOfMagicDefinitions.SchoolNecromancy.Name) ||
            caster.GetClassSpellRepertoire(Warlock)?.HasKnowledgeOfSpell(spell) != true)
        {
            return;
        }

        // costly or consumed material components are still required
        if (isMaterial &&
            spell.MaterialComponentType == MaterialComponentType.Specific &&
            (spell.SpecificMaterialComponentCostGp > 0 || spell.SpecificMaterialComponentConsumed))
        {
            return;
        }

        result = true;
        failure = string.Empty;
    }

    //
    // Form of Dread
    //

    private sealed class OnConditionAddedOrRemovedFormOfDread(
        params (FeatureDefinition feature, ConditionDefinition condition)[] extraConditions)
        : IOnConditionAddedOrRemoved
    {
        public void OnConditionAdded(RulesetCharacter target, RulesetCondition rulesetCondition)
        {
            // Fearless Form: a Frightened condition ends when you transform
            foreach (var frightened in target.ConditionsByCategory
                         .SelectMany(x => x.Value)
                         .Where(x => x.ConditionDefinition.IsSubtypeOf(ConditionDefinitions.ConditionFrightened.Name))
                         .ToArray())
            {
                target.RemoveCondition(frightened);
            }

            foreach (var (feature, condition) in extraConditions)
            {
                if (!target.HasAnyFeature(feature) || target.HasConditionOfType(condition.Name))
                {
                    continue;
                }

                target.InflictCondition(
                    condition.Name,
                    rulesetCondition.DurationType,
                    rulesetCondition.DurationParameter,
                    rulesetCondition.EndOccurence,
                    AttributeDefinitions.TagEffect,
                    target.guid,
                    target.CurrentFaction.Name,
                    1,
                    condition.Name,
                    0,
                    0,
                    0);
            }
        }

        public void OnConditionRemoved(RulesetCharacter target, RulesetCondition rulesetCondition)
        {
            foreach (var (_, condition) in extraConditions)
            {
                target.RemoveAllConditionsOfType(condition.Name);
            }
        }
    }

    private sealed class CustomBehaviorFormOfDread(
        FeatureDefinitionPower powerFormOfDread,
        FeatureDefinitionPower powerFrightfulAvatar,
        FeatureDefinition featureArcaneNecrosis,
        FeatureDefinition featureDreadedNecrosis)
        : IModifyEffectDescription, IMagicEffectInitiatedByMe, IMagicEffectBeforeHitConfirmedOnEnemy,
            IPhysicalAttackBeforeHitConfirmedOnEnemy, IMagicEffectFinishedByMe, IPhysicalAttackFinishedByMe
    {
        private const string ArcaneNecrosis = "ArcaneNecrosis";
        private const string FrightfulAvatar = "FrightfulAvatar";

        //
        // Facsimile of Life: 1d10 + warlock level temporary hit points
        //

        public bool IsValid(BaseDefinition definition, RulesetCharacter character, EffectDescription effectDescription)
        {
            return definition == powerFormOfDread;
        }

        public EffectDescription GetEffectDescription(
            BaseDefinition definition,
            EffectDescription effectDescription,
            RulesetCharacter character,
            RulesetEffect rulesetEffect)
        {
            effectDescription.EffectForms[1] = EffectFormBuilder
                .Create()
                .SetTempHpForm(character.GetClassLevel(Warlock), DieType.D10, 1)
                .Build();

            return effectDescription;
        }

        //
        // Arcane Necrosis: once per turn a damaging spell can deal Necrotic damage instead
        //

        public IEnumerator OnMagicEffectInitiatedByMe(
            CharacterAction action,
            RulesetEffect activeEffect,
            GameLocationCharacter attacker,
            List<GameLocationCharacter> targets)
        {
            FrightfulAvatarSpellTargets.Remove(attacker.Guid);

            var rulesetAttacker = attacker.RulesetCharacter;

            if (activeEffect is not RulesetEffectSpell rulesetEffectSpell ||
                !rulesetAttacker.HasAnyFeature(featureArcaneNecrosis) ||
                !rulesetEffectSpell.EffectDescription.EffectForms.Any(x =>
                    x.FormType == EffectForm.EffectFormType.Damage &&
                    x.DamageForm.DamageType != DamageTypeNecrotic) ||
                WasUsedThisTurn(ArcaneNecrosisTurns, attacker))
            {
                yield break;
            }

            yield return attacker.MyReactToDoNothing(
                ExtraActionId.DoNothingFree,
                attacker,
                ArcaneNecrosis,
                $"CustomReaction{ArcaneNecrosis}Description".Formatted(
                    Category.Reaction, rulesetEffectSpell.SpellDefinition.FormatTitle()),
                ReactionValidated);

            yield break;

            void ReactionValidated()
            {
                TryUseOncePerTurn(ArcaneNecrosisTurns, attacker);
                attacker.SetSpecialFeatureUses(ArcaneNecrosisTag(activeEffect), 1);
                rulesetAttacker.LogCharacterUsedFeature(featureArcaneNecrosis);
            }
        }

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
            if (attacker.GetSpecialFeatureUses(ArcaneNecrosisTag(rulesetEffect)) == 1)
            {
                foreach (var effectForm in actualEffectForms
                             .Where(x => x.FormType == EffectForm.EffectFormType.Damage))
                {
                    effectForm.DamageForm.damageType = DamageTypeNecrotic;
                }
            }

            if (!IsAttackRoll(rulesetEffect))
            {
                yield break;
            }

            HandleDreadedNecrosis(attacker, actualEffectForms);

            // Frightful Avatar is offered on the first creature hit, once the spell or power finishes
            FrightfulAvatarSpellTargets.TryAdd(attacker.Guid, defender);
        }

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
            HandleDreadedNecrosis(attacker, actualEffectForms);

            yield break;
        }

        public IEnumerator OnMagicEffectFinishedByMe(
            CharacterAction action,
            GameLocationCharacter attacker,
            List<GameLocationCharacter> targets)
        {
            var rulesetEffect = action.ActionParams.RulesetEffect;

            if (rulesetEffect != null)
            {
                attacker.SetSpecialFeatureUses(ArcaneNecrosisTag(rulesetEffect), -1);
            }

            if (!FrightfulAvatarSpellTargets.TryGetValue(attacker.Guid, out var defender))
            {
                yield break;
            }

            FrightfulAvatarSpellTargets.Remove(attacker.Guid);

            yield return HandleFrightfulAvatar(null, attacker, defender);
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
            if (rollOutcome is not (RollOutcome.Success or RollOutcome.CriticalSuccess))
            {
                yield break;
            }

            yield return HandleFrightfulAvatar(battleManager, attacker, defender);
        }

        //
        // Dreaded Necrosis: once per turn, one more damage die of the Necrotic damage while in Form of Dread
        //

        private void HandleDreadedNecrosis(GameLocationCharacter attacker, List<EffectForm> actualEffectForms)
        {
            var rulesetAttacker = attacker.RulesetCharacter;
            var necroticForm = actualEffectForms.FirstOrDefault(x =>
                x.FormType == EffectForm.EffectFormType.Damage &&
                x.DamageForm.DamageType == DamageTypeNecrotic &&
                x.DamageForm.DiceNumber > 0 &&
                x.DamageForm.DieType != DieType.D1);

            if (necroticForm == null ||
                !rulesetAttacker.HasAnyFeature(featureDreadedNecrosis) ||
                !IsInFormOfDread(rulesetAttacker) ||
                !TryUseOncePerTurn(DreadedNecrosisTurns, attacker))
            {
                return;
            }

            var dreadedNecrosisForm =
                EffectFormBuilder.DamageForm(DamageTypeNecrotic, 1, necroticForm.DamageForm.DieType);

            // a creature that negates the damage on a successful save takes no extra damage either
            dreadedNecrosisForm.savingThrowAffinity = necroticForm.SavingThrowAffinity;
            actualEffectForms.Add(dreadedNecrosisForm);
            rulesetAttacker.LogCharacterUsedFeature(featureDreadedNecrosis);
        }

        //
        // Frightful Avatar: once per turn on a hit, the target makes a Wisdom save or is Frightened
        //

        private IEnumerator HandleFrightfulAvatar(
            GameLocationBattleManager battleManager, GameLocationCharacter attacker, GameLocationCharacter defender)
        {
            var rulesetAttacker = attacker.RulesetCharacter;

            if (rulesetAttacker is not { IsDeadOrDyingOrUnconscious: false } ||
                defender.RulesetCharacter is not { IsDeadOrDyingOrUnconscious: false } ||
                !IsInFormOfDread(rulesetAttacker) ||
                WasUsedThisTurn(FrightfulAvatarTurns, attacker))
            {
                yield break;
            }

            var usablePower = PowerProvider.Get(powerFrightfulAvatar, rulesetAttacker);

            yield return attacker.MyReactToUsePower(
                ActionDefinitions.Id.PowerNoCost,
                usablePower,
                [defender],
                attacker,
                FrightfulAvatar,
                $"UsePower{FrightfulAvatar}Description".Formatted(Category.Reaction, defender.Name),
                () => TryUseOncePerTurn(FrightfulAvatarTurns, attacker),
                battleManager: battleManager);
        }
    }

    //
    // Arcane Necrosis
    //

    private sealed class ModifyDamageAffinityArcaneNecrosis(FeatureDefinition featureArcaneNecrosis)
        : IModifyDamageAffinity
    {
        // this is also called when the warlock is the one taking damage, so it checks who is dealing it
        public void ModifyDamageAffinity(RulesetActor defender, RulesetActor attacker, List<FeatureDefinition> features)
        {
            if (attacker == null ||
                attacker == defender ||
                !attacker.HasAnyFeature(featureArcaneNecrosis))
            {
                return;
            }

            features.RemoveAll(x =>
                x is IDamageAffinityProvider
                {
                    DamageAffinityType: DamageAffinityType.Resistance, DamageType: DamageTypeNecrotic
                });
        }
    }

    //
    // Unholy Resuscitation
    //

    private sealed class OnReducedToZeroHpByEnemyUnholyResuscitation(FeatureDefinitionPower powerUnholyResuscitation)
        : IOnReducedToZeroHpByEnemy
    {
        private const string UnholyResuscitation = "UnholyResuscitation";

        public IEnumerator HandleReducedToZeroHpByEnemy(
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            RulesetAttackMode attackMode,
            RulesetEffect activeEffect)
        {
            var rulesetCharacter = defender.RulesetCharacter;
            var usablePower = PowerProvider.Get(powerUnholyResuscitation, rulesetCharacter);

            // not when killed outright
            if (Gui.Battle == null ||
                rulesetCharacter is not { IsDead: false } ||
                rulesetCharacter.GetRemainingUsesOfPower(usablePower) == 0)
            {
                yield break;
            }

            var hitPoints = Math.Min(
                2 * rulesetCharacter.GetClassLevel(Warlock),
                rulesetCharacter.TryGetAttributeValue(AttributeDefinitions.HitPoints));
            var targets = Gui.Battle.GetContenders(defender, withinRange: 6);

            yield return defender.MyReactToUsePower(
                ActionDefinitions.Id.PowerNoCost,
                usablePower,
                targets,
                attacker,
                UnholyResuscitation,
                $"UsePower{UnholyResuscitation}Description".Formatted(Category.Reaction, hitPoints.ToString()),
                ReactionValidated);

            yield break;

            void ReactionValidated()
            {
                var conditionExhausted = ConditionDefinitions.ConditionExhausted;

                defender.MyExecuteActionStabilizeAndStandUp(hitPoints);
                rulesetCharacter.InflictCondition(
                    conditionExhausted.Name,
                    conditionExhausted.DurationType,
                    conditionExhausted.DurationParameter,
                    conditionExhausted.TurnOccurence,
                    AttributeDefinitions.TagEffect,
                    rulesetCharacter.guid,
                    rulesetCharacter.CurrentFaction.Name,
                    1,
                    conditionExhausted.Name,
                    0,
                    0,
                    0);
            }
        }
    }
}
