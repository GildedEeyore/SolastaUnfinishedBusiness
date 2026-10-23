using System;
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
using SolastaUnfinishedBusiness.Feats;
using SolastaUnfinishedBusiness.Interfaces;
using SolastaUnfinishedBusiness.Models;
using static RuleDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.CharacterClassDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.FeatureDefinitionPowers;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.SpellDefinitions;
using static SolastaUnfinishedBusiness.Builders.Features.AutoPreparedSpellsGroupBuilder;

namespace SolastaUnfinishedBusiness.Subclasses;

[UsedImplicitly]
public sealed class DomainGrave : AbstractSubclass
{
    internal const string Name = "DomainGrave";

    private const int KeeperOfSoulsMinSlotLevel = 6;

    private static FeatureDefinition _featureCircleOfMortality;
    private static FeatureDefinitionPower _powerKeeperOfSouls;

    // spells of the Grave Domain Spells table, which Enhanced Necromancy can always target a second creature with
    private static readonly HashSet<SpellDefinition> GraveDomainSpells = [];

    // Pull of Death: attacker guid -> the battle turn it was last used on
    private static readonly Dictionary<ulong, int> PullOfDeathTurns = [];

    // Circle of Mortality: set while a healing form from a spell or Channel Divinity targets a creature at 0 hit points
    private static bool _maximizeHealingDice;

    public DomainGrave()
    {
        var divinePowerPrefix = Gui.Localize("Feature/&ClericChannelDivinityTitle") + ": ";

        // LEVEL 01 - Grave Domain Spells

        // the 2024 table grants 1st and 2nd level spells at cleric level 3, as do the 1 and 3 groups when the domain
        // is learned at level 3; Spare the Dying is a cantrip, so it's a bonus cantrip instead
        var autoPreparedSpellsDomainGrave = FeatureDefinitionAutoPreparedSpellsBuilder
            .Create($"AutoPreparedSpells{Name}")
            .SetGuiPresentation("ExpandedSpells", Category.Feature)
            .SetAutoTag("Domain")
            .SetPreparedSpellGroups(
                BuildSpellGroup(1, DetectEvilAndGood, FalseLife),
                BuildSpellGroup(3, SpellsContext.GentleRepose, RayOfEnfeeblement),
                BuildSpellGroup(5, Revivify, VampiricTouch),
                BuildSpellGroup(7, Blight, DeathWard),
                BuildSpellGroup(9, DispelEvilAndGood, RaiseDead))
            .SetSpellcastingClass(Cleric)
            .AddToDB();

        GraveDomainSpells.UnionWith(autoPreparedSpellsDomainGrave.AutoPreparedSpellsGroups
            .SelectMany(x => x.SpellsList));
        GraveDomainSpells.Add(SpareTheDying);

        var bonusCantripDomainGrave = FeatureDefinitionBonusCantripsBuilder
            .Create($"BonusCantrip{Name}")
            .SetGuiPresentation(Category.Feature)
            .SetBonusCantrips(SpareTheDying)
            .AddToDB();

        // LEVEL 01 - Circle of Mortality

        var featurePullOfDeath = FeatureDefinitionBuilder
            .Create($"Feature{Name}PullOfDeath")
            .SetGuiPresentation(Category.Feature, hidden: true)
            .AddToDB();

        featurePullOfDeath.AddCustomSubFeatures(new CustomBehaviorPullOfDeath(featurePullOfDeath));

        var powerReturnToLife = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}ReturnToLife")
            .SetGuiPresentation(Category.Feature, SpareTheDying)
            .SetUsesFixed(ActivationTime.BonusAction)
            .SetEffectDescription(SpareTheDying.EffectDescription)
            .AddToDB();

        // healing dice are maximized on RulesetCharacter.RollAndHeal
        _featureCircleOfMortality = FeatureDefinitionFeatureSetBuilder
            .Create($"FeatureSet{Name}CircleOfMortality")
            .SetGuiPresentation(Category.Feature)
            .AddFeatureSet(featurePullOfDeath, powerReturnToLife)
            .AddToDB();

        // LEVEL 02 - Channel Divinity: Path to the Grave

        var conditionPathToTheGrave = ConditionDefinitionBuilder
            .Create($"Condition{Name}PathToTheGrave")
            .SetGuiPresentation(Category.Condition, ConditionDefinitions.ConditionBaned)
            .SetConditionType(ConditionType.Detrimental)
            .SetPossessive()
            .SetFeatures(
                FeatureDefinitionCombatAffinityBuilder
                    .Create($"CombatAffinity{Name}PathToTheGrave")
                    .SetGuiPresentation($"Condition{Name}PathToTheGrave", Category.Condition, Gui.NoLocalization)
                    .SetMyAttackAdvantage(AdvantageType.Disadvantage)
                    .AddToDB(),
                FeatureDefinitionSavingThrowAffinityBuilder
                    .Create($"SavingThrowAffinity{Name}PathToTheGrave")
                    .SetGuiPresentation($"Condition{Name}PathToTheGrave", Category.Condition, Gui.NoLocalization)
                    .SetAffinities(CharacterSavingThrowAffinity.Disadvantage, false,
                        AttributeDefinitions.AbilityScoreNames)
                    .AddToDB())
            .AddToDB();

        conditionPathToTheGrave.AddCustomSubFeatures(new CustomBehaviorPathToTheGrave(conditionPathToTheGrave));

        var powerPathToTheGrave = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}PathToTheGrave")
            .SetGuiPresentation(Category.Feature, PowerDomainOblivionMarkOfFate)
            .SetUsesFixed(ActivationTime.BonusAction, RechargeRate.ChannelDivinity)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Round, 1, TurnOccurenceType.StartOfTurn)
                    .SetTargetingData(Side.All, RangeType.Distance, 6, TargetType.IndividualsUnique)
                    .SetEffectForms(EffectFormBuilder.ConditionForm(conditionPathToTheGrave))
                    .SetParticleEffectParameters(PowerDomainOblivionMarkOfFate)
                    .Build())
            .AddToDB();

        var featureSetPathToTheGrave = FeatureDefinitionFeatureSetBuilder
            .Create($"FeatureSet{Name}PathToTheGrave")
            .SetGuiPresentation(
                divinePowerPrefix + powerPathToTheGrave.FormatTitle(), powerPathToTheGrave.FormatDescription())
            .AddFeatureSet(powerPathToTheGrave)
            .AddToDB();

        // LEVEL 06 - Sentinel at Death's Door

        // Solasta turns a critical hit into a normal hit when the target is immune to critical hits
        var combatAffinitySentinelAtDeathsDoor = FeatureDefinitionCombatAffinityBuilder
            .Create($"CombatAffinity{Name}SentinelAtDeathsDoor")
            .SetGuiPresentationNoContent(true)
            .AddToDB();

        combatAffinitySentinelAtDeathsDoor.criticalHitImmunity = true;

        var conditionSentinelAtDeathsDoor = ConditionDefinitionBuilder
            .Create($"Condition{Name}SentinelAtDeathsDoor")
            .SetGuiPresentationNoContent(true)
            .SetSilent(Silent.WhenAddedOrRemoved)
            .SetSpecialInterruptions(ExtraConditionInterruption.AfterWasAttacked)
            .SetFeatures(combatAffinitySentinelAtDeathsDoor)
            .AddToDB();

        var powerSentinelAtDeathsDoor = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}SentinelAtDeathsDoor")
            .SetGuiPresentation(Category.Feature)
            .SetUsesAbilityBonus(ActivationTime.NoCost, RechargeRate.LongRest, AttributeDefinitions.Wisdom)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Round, 0, TurnOccurenceType.StartOfTurn)
                    .SetTargetingData(Side.All, RangeType.Distance, 12, TargetType.IndividualsUnique)
                    .SetEffectForms(EffectFormBuilder.ConditionForm(conditionSentinelAtDeathsDoor))
                    .SetParticleEffectParameters(PowerTraditionCourtMageSpellShield)
                    .Build())
            .AddToDB();

        powerSentinelAtDeathsDoor.AddCustomSubFeatures(
            ModifyPowerVisibility.Hidden,
            new TryAlterOutcomeAttackSentinelAtDeathsDoor(powerSentinelAtDeathsDoor));

        // LEVEL 08 - Potent Spellcasting

        // swapped by Blessed Strikes when that 2024 option is enabled
        var featurePotentSpellcasting = FeatureDefinitionBuilder
            .Create($"Feature{Name}PotentSpellcasting")
            .SetGuiPresentation(Category.Feature)
            .AddToDB();

        featurePotentSpellcasting.AddCustomSubFeatures(
            new ClassFeats.CustomBehaviorFeatPotentSpellcaster(featurePotentSpellcasting, Cleric));

        // LEVEL 17 - Divine Reaper

        // Enhanced Necromancy: the extra target is offered while a use of Channel Divinity is left,
        // and the use is only expended when a second creature is targeted
        var powerEnhancedNecromancy = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}EnhancedNecromancy")
            .SetGuiPresentation(Category.Feature)
            .SetUsesFixed(ActivationTime.NoCost, RechargeRate.ChannelDivinity)
            .SetShowCasting(false)
            .AddToDB();

        powerEnhancedNecromancy.AddCustomSubFeatures(
            ModifyPowerVisibility.Hidden,
            new CustomBehaviorEnhancedNecromancy(powerEnhancedNecromancy));

        // Keeper of Souls is handled on GameLocationBattleManager.HandleTargetReducedToZeroHP
        _powerKeeperOfSouls = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}KeeperOfSouls")
            .SetGuiPresentation(Category.Feature)
            .SetUsesFixed(ActivationTime.NoCost, RechargeRate.ShortRest)
            .SetShowCasting(false)
            .AddToDB();

        _powerKeeperOfSouls.AddCustomSubFeatures(ModifyPowerVisibility.Hidden);

        var featureSetDivineReaper = FeatureDefinitionFeatureSetBuilder
            .Create($"FeatureSet{Name}DivineReaper")
            .SetGuiPresentation(Category.Feature)
            .AddFeatureSet(powerEnhancedNecromancy, _powerKeeperOfSouls)
            .AddToDB();

        // MAIN

        Subclass = CharacterSubclassDefinitionBuilder
            .Create(Name)
            .SetGuiPresentation(Category.Subclass, CharacterSubclassDefinitions.DomainOblivion)
            .AddFeaturesAtLevel(1, autoPreparedSpellsDomainGrave, bonusCantripDomainGrave, _featureCircleOfMortality)
            .AddFeaturesAtLevel(2, featureSetPathToTheGrave)
            .AddFeaturesAtLevel(6, powerSentinelAtDeathsDoor)
            .AddFeaturesAtLevel(8, featurePotentSpellcasting)
            .AddFeaturesAtLevel(10, PowerClericDivineInterventionCleric)
            .AddFeaturesAtLevel(17, featureSetDivineReaper)
            .AddFeaturesAtLevel(20, Level20SubclassesContext.PowerClericDivineInterventionImprovementCleric)
            .AddToDB();
    }

    internal override CharacterClassDefinition Klass => Cleric;

    internal override CharacterSubclassDefinition Subclass { get; }

    // ReSharper disable once UnassignedGetOnlyAutoProperty
    internal override FeatureDefinitionSubclassChoice SubclassChoice { get; }

    internal override DeityDefinition DeityDefinition => DeityDefinitions.Maraike;

    // cleric level, or wizard level for a Theurgy wizard of the Grave domain
    private static int GetClericLevel(RulesetCharacter character)
    {
        var hero = character.GetOriginalHero();

        if (hero == null)
        {
            return 0;
        }

        var clericLevel = hero.GetClassLevel(Cleric);

        if (hero.ClassesAndSubclasses.TryGetValue(Wizard, out var subclass) &&
            subclass &&
            WizardTheurgy.IsVariantOf(subclass, Name))
        {
            clericLevel = Math.Max(clericLevel, hero.GetClassLevel(Wizard));
        }

        return clericLevel;
    }

    private static RulesetSpellRepertoire GetSpellRepertoire(RulesetCharacter character)
    {
        return character.GetClassSpellRepertoire(Cleric) ?? character.GetClassSpellRepertoire(Wizard);
    }

    private static bool IsAttackRoll(RulesetEffect rulesetEffect)
    {
        return rulesetEffect.EffectDescription.RangeType is RangeType.MeleeHit or RangeType.RangeHit;
    }

    //
    // Circle of Mortality
    //

    // called on RulesetImplementationManager.ApplyHealingForm
    internal static void BeforeApplyHealingForm(RulesetImplementationDefinitions.ApplyFormsParams formsParams)
    {
        _maximizeHealingDice =
            _featureCircleOfMortality &&
            formsParams.sourceCharacter != null &&
            formsParams.targetCharacter is { CurrentHitPoints: 0 } &&
            formsParams.activeEffect is RulesetEffectSpell or RulesetEffectPower
            {
                PowerDefinition.RechargeRate: RechargeRate.ChannelDivinity
            } &&
            formsParams.sourceCharacter.HasAnyFeature(_featureCircleOfMortality);
    }

    internal static void AfterApplyHealingForm()
    {
        _maximizeHealingDice = false;
    }

    // called on RulesetCharacter.RollAndHeal
    internal static bool ShouldMaximizeHealingDice()
    {
        return _maximizeHealingDice;
    }

    private sealed class CustomBehaviorPullOfDeath(FeatureDefinition featurePullOfDeath)
        : IMagicEffectBeforeHitConfirmedOnEnemy, IPhysicalAttackBeforeHitConfirmedOnEnemy
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
            // a spell, or anything else that hits with an attack roll
            if (rulesetEffect is not RulesetEffectSpell && !IsAttackRoll(rulesetEffect))
            {
                yield break;
            }

            HandlePullOfDeath(attacker, defender, actualEffectForms);
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
            HandlePullOfDeath(attacker, defender, actualEffectForms);

            yield break;
        }

        private void HandlePullOfDeath(
            GameLocationCharacter attacker, GameLocationCharacter defender, List<EffectForm> actualEffectForms)
        {
            var rulesetAttacker = attacker.RulesetCharacter;
            var damageForm = actualEffectForms.FirstOrDefault(x => x.FormType == EffectForm.EffectFormType.Damage);

            if (damageForm == null ||
                rulesetAttacker is not { IsDeadOrDyingOrUnconscious: false } ||
                defender.RulesetCharacter is not { IsDeadOrDyingOrUnconscious: false } rulesetDefender ||
                rulesetDefender.MissingHitPoints <= 0)
            {
                return;
            }

            // once per turn, on anyone's turn
            var battle = Gui.Battle;
            var turn = battle == null
                ? -1
                : (battle.CurrentRound * 1000) +
                  battle.InitiativeSortedContenders.IndexOf(battle.ActiveContenderIgnoringLegendary);

            if (turn >= 0 && PullOfDeathTurns.TryGetValue(attacker.Guid, out var usedTurn) && usedTurn == turn)
            {
                return;
            }

            PullOfDeathTurns[attacker.Guid] = turn;

            var dieType = GetClericLevel(rulesetAttacker) >= 11 ? DieType.D6 : DieType.D4;
            var pullOfDeathForm = EffectFormBuilder.DamageForm(DamageTypeNecrotic, 1, dieType);

            // a creature that negates the damage on a successful save takes no damage, so no extra damage either
            if (damageForm.SavingThrowAffinity == EffectSavingThrowType.Negates)
            {
                pullOfDeathForm.savingThrowAffinity = EffectSavingThrowType.Negates;
            }

            actualEffectForms.Add(pullOfDeathForm);
            rulesetAttacker.LogCharacterUsedFeature(featurePullOfDeath);
        }
    }

    //
    // Path to the Grave
    //

    private sealed class CustomBehaviorPathToTheGrave(ConditionDefinition conditionPathToTheGrave)
        : IMagicEffectBeforeHitConfirmedOnMe, IPhysicalAttackBeforeHitConfirmedOnMe
    {
        private const string PathToTheGrave = "PathToTheGrave";

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
            if (!IsAttackRoll(rulesetEffect))
            {
                yield break;
            }

            yield return HandlePathToTheGrave(battleManager, attacker, defender, actualEffectForms);
        }

        public IEnumerator OnPhysicalAttackBeforeHitConfirmedOnMe(
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
            yield return HandlePathToTheGrave(battleManager, attacker, defender, actualEffectForms);
        }

        // when you or an ally you can see hits the cursed target, you can end the curse for extra damage
        private IEnumerator HandlePathToTheGrave(
            GameLocationBattleManager battleManager,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            List<EffectForm> actualEffectForms)
        {
            var rulesetDefender = defender.RulesetActor;

            if (!rulesetDefender.TryGetConditionOfCategoryAndType(
                    AttributeDefinitions.TagEffect, conditionPathToTheGrave.Name, out var activeCondition))
            {
                yield break;
            }

            var rulesetCaster = EffectHelpers.GetCharacterByGuid(activeCondition.SourceGuid);
            var caster = rulesetCaster != null ? GameLocationCharacter.GetFromActor(rulesetCaster) : null;

            if (caster == null ||
                rulesetCaster is not { IsDeadOrDyingOrUnconscious: false } ||
                rulesetCaster.IsIncapacitated ||
                (attacker != caster && (attacker.IsOppositeSide(caster.Side) || !caster.CanPerceiveTarget(attacker))))
            {
                yield break;
            }

            var clericLevel = GetClericLevel(rulesetCaster);
            var damageType = string.Empty;
            List<string> damageTypes = [DamageTypeNecrotic, DamageTypeRadiant];

            yield return caster.MyReactToSelectOption(
                attacker,
                PathToTheGrave,
                $"CustomReaction{PathToTheGrave}Description".Formatted(
                    Category.Reaction, attacker.Name, defender.Name, clericLevel.ToString()),
                damageTypes
                    .Select(x => (Gui.Localize($"Tooltip/&Tag{x}Title"), true))
                    .ToList(),
                option => damageType = damageTypes[option],
                battleManager: battleManager);

            if (damageType == string.Empty)
            {
                yield break;
            }

            rulesetDefender.RemoveCondition(activeCondition);
            actualEffectForms.Add(EffectFormBuilder.DamageForm(damageType, bonusDamage: clericLevel));
            rulesetCaster.LogCharacterActivatesAbility(
                Gui.Localize($"Feature/&Power{Name}{PathToTheGrave}Title"),
                "Feedback/&PathToTheGraveExtraDamage",
                tooltipContent: $"Power{Name}{PathToTheGrave}",
                tooltipClass: "PowerDefinition",
                extra:
                [
                    (ConsoleStyleDuplet.ParameterType.Positive, clericLevel.ToString()),
                    (ConsoleStyleDuplet.ParameterType.AbilityInfo, Gui.Localize($"Tooltip/&Tag{damageType}Title"))
                ]);
        }
    }

    //
    // Sentinel at Death's Door
    //

    private sealed class TryAlterOutcomeAttackSentinelAtDeathsDoor(FeatureDefinitionPower powerSentinelAtDeathsDoor)
        : ITryAlterOutcomeAttack
    {
        private const string SentinelAtDeathsDoor = "SentinelAtDeathsDoor";

        // anything that reduces damage
        public int HandlerPriority => 20;

        public IEnumerator OnTryAlterOutcomeAttack(
            GameLocationBattleManager battleManager,
            CharacterAction action,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            GameLocationCharacter helper,
            ActionModifier actionModifier,
            RulesetAttackMode attackMode,
            RulesetEffect rulesetEffect)
        {
            var rulesetHelper = helper.RulesetCharacter;
            var usablePower = PowerProvider.Get(powerSentinelAtDeathsDoor, rulesetHelper);

            // you, or a bloodied creature you can see within 60 feet
            if (action.AttackRollOutcome is not (RollOutcome.Success or RollOutcome.CriticalSuccess) ||
                helper == attacker ||
                defender.RulesetCharacter is not { IsDeadOrDyingOrUnconscious: false } rulesetDefender ||
                defender.IsOppositeSide(helper.Side) ||
                !helper.CanReact() ||
                rulesetHelper.GetRemainingUsesOfPower(usablePower) == 0 ||
                (helper != defender &&
                 (!helper.IsWithinRange(defender, 12) ||
                  !helper.CanPerceiveTarget(defender) ||
                  rulesetDefender.CurrentHitPoints >
                  rulesetDefender.TryGetAttributeValue(AttributeDefinitions.HitPoints) / 2)))
            {
                yield break;
            }

            var isCritical = action.AttackRollOutcome == RollOutcome.CriticalSuccess;

            // any reaction within an attack flow must use the attacker as waiter
            yield return helper.MyReactToUsePower(
                ActionDefinitions.Id.PowerReaction,
                usablePower,
                [defender],
                attacker,
                SentinelAtDeathsDoor,
                $"UsePower{SentinelAtDeathsDoor}Description".Formatted(
                    Category.Reaction, defender.Name, attacker.Name) +
                (isCritical
                    ? " " + Gui.Localize($"Reaction/&UsePower{SentinelAtDeathsDoor}CriticalDescription")
                    : string.Empty),
                ReactionValidated,
                battleManager: battleManager);

            yield break;

            // the power condition makes the defender immune to critical hits from this attack
            void ReactionValidated()
            {
                actionModifier.DefenderDamageMultiplier *= 0.5f;
                rulesetDefender.DamageHalved?.Invoke(rulesetDefender, powerSentinelAtDeathsDoor);
            }
        }
    }

    //
    // Enhanced Necromancy
    //

    private sealed class CustomBehaviorEnhancedNecromancy(FeatureDefinitionPower powerEnhancedNecromancy)
        : IModifyEffectDescription, IMagicEffectInitiatedByMe
    {
        public bool IsValid(BaseDefinition definition, RulesetCharacter character, EffectDescription effectDescription)
        {
            return definition is SpellDefinition spell &&
                   IsEnhancedNecromancySpell(spell) &&
                   effectDescription.TargetType is TargetType.Individuals or TargetType.IndividualsUnique &&
                   effectDescription.TargetParameter == 1 &&
                   character.GetRemainingUsesOfPower(PowerProvider.Get(powerEnhancedNecromancy, character)) > 0 &&
                   HasMaterialComponentsForSecondTarget(character, spell);
        }

        public EffectDescription GetEffectDescription(
            BaseDefinition definition,
            EffectDescription effectDescription,
            RulesetCharacter character,
            RulesetEffect rulesetEffect)
        {
            effectDescription.targetType = TargetType.IndividualsUnique;
            effectDescription.targetParameter = 2;

            return effectDescription;
        }

        // the use of Channel Divinity is only expended if the spell targets more creatures than it could without it
        public IEnumerator OnMagicEffectInitiatedByMe(
            CharacterAction action,
            RulesetEffect activeEffect,
            GameLocationCharacter attacker,
            List<GameLocationCharacter> targets)
        {
            if (activeEffect is not RulesetEffectSpell rulesetEffectSpell ||
                !IsEnhancedNecromancySpell(rulesetEffectSpell.SpellDefinition))
            {
                yield break;
            }

            var spellEffect = rulesetEffectSpell.SpellDefinition.EffectDescription;

            if (spellEffect.TargetType is not (TargetType.Individuals or TargetType.IndividualsUnique) ||
                spellEffect.TargetParameter != 1)
            {
                yield break;
            }

            var maxTargetsWithoutEnhancedNecromancy =
                rulesetEffectSpell.MetamagicOption?.Type == MetamagicType.TwinnedSpell ? 2 : 1;

            if (targets.Distinct().Count() <= maxTargetsWithoutEnhancedNecromancy)
            {
                yield break;
            }

            var rulesetAttacker = attacker.RulesetCharacter;
            var usablePower = PowerProvider.Get(powerEnhancedNecromancy, rulesetAttacker);

            rulesetAttacker.UsePower(usablePower);
            rulesetAttacker.LogCharacterUsedPower(powerEnhancedNecromancy);

            // costly consumed components are provided for each target
            if (UsesConsumedMaterialComponent(rulesetEffectSpell.SpellDefinition))
            {
                rulesetAttacker.SpendSpellMaterialComponentAsNeeded(rulesetEffectSpell);
            }

            // the extra target depends on the remaining uses of Channel Divinity
            PowerBundle.ClearSpellEffectCache(rulesetAttacker);
        }

        private static bool IsEnhancedNecromancySpell(SpellDefinition spell)
        {
            return GraveDomainSpells.Contains(spell) ||
                   (spell.SchoolOfMagic == SchoolOfMagicDefinitions.SchoolNecromancy.Name && spell.SpellLevel <= 5);
        }

        private static bool UsesConsumedMaterialComponent(SpellDefinition spell)
        {
            return ServiceRepository.GetService<IGameSettingsService>().MaterialComponent == "Full" &&
                   spell.MaterialComponentType == MaterialComponentType.Specific &&
                   spell.SpecificMaterialComponentConsumed &&
                   !string.IsNullOrEmpty(spell.SpecificMaterialComponentTag) &&
                   spell.SpecificMaterialComponentCostGp > 0;
        }

        private static bool HasMaterialComponentsForSecondTarget(RulesetCharacter character, SpellDefinition spell)
        {
            if (!UsesConsumedMaterialComponent(spell))
            {
                return true;
            }

            if (character.CharacterInventory == null)
            {
                return false;
            }

            var items = new List<RulesetItem>();

            character.CharacterInventory.EnumerateAllItems(items);

            return items
                .Where(x =>
                    x.ItemDefinition.ItemTags.Contains(spell.SpecificMaterialComponentTag) &&
                    x.ItemDefinition.Costs[1] >= spell.SpecificMaterialComponentCostGp)
                .Sum(x => x.ItemDefinition.CanBeStacked ? Math.Max(1, x.StackCount) : 1) >= 2;
        }
    }

    //
    // Keeper of Souls
    //

    // called on GameLocationBattleManager.HandleTargetReducedToZeroHP
    internal static IEnumerator HandleKeeperOfSouls(
        GameLocationBattleManager battleManager, GameLocationCharacter downedCreature)
    {
        const string KeeperOfSouls = "KeeperOfSouls";

        if (!_powerKeeperOfSouls ||
            battleManager.Battle == null ||
            downedCreature.RulesetActor is not { IsDead: true })
        {
            yield break;
        }

        foreach (var cleric in battleManager.Battle.AllContenders
                     .Where(x =>
                         x.RulesetCharacter is { IsDeadOrDyingOrUnconscious: false, IsIncapacitated: false } &&
                         x.RulesetCharacter.HasAnyFeature(_powerKeeperOfSouls) &&
                         x.IsOppositeSide(downedCreature.Side) &&
                         x.IsWithinRange(downedCreature, 12))
                     .ToArray())
        {
            var rulesetCleric = cleric.RulesetCharacter;
            var usablePower = PowerProvider.Get(_powerKeeperOfSouls, rulesetCleric);
            var hasUse = rulesetCleric.GetRemainingUsesOfPower(usablePower) > 0;
            var spellRepertoire = GetSpellRepertoire(rulesetCleric);
            var slotLevel = 0;

            // a level 6+ spell slot restores the use
            if (!hasUse && spellRepertoire != null)
            {
                for (var level = KeeperOfSoulsMinSlotLevel;
                     level <= spellRepertoire.MaxSpellLevelOfSpellCastingLevel;
                     level++)
                {
                    spellRepertoire.GetSlotsNumber(level, out var remaining, out _);

                    if (remaining <= 0)
                    {
                        continue;
                    }

                    slotLevel = level;
                    break;
                }
            }

            if (!hasUse && slotLevel == 0)
            {
                continue;
            }

            // you or one creature you can see within 60 feet, only offering those missing hit points
            var candidates = battleManager.Battle.AllContenders
                .Where(x =>
                    x.RulesetCharacter is { IsDead: false, MissingHitPoints: > 0 } &&
                    !x.IsOppositeSide(cleric.Side) &&
                    (x == cleric || (cleric.IsWithinRange(x, 12) && cleric.CanPerceiveTarget(x))))
                .OrderBy(x => x == cleric ? 0 : 1)
                .ToList();

            if (candidates.Count == 0)
            {
                continue;
            }

            var healing = 2 * GetClericLevel(rulesetCleric);
            GameLocationCharacter selectedTarget = null;

            yield return cleric.MyReactToSelectOption(
                cleric,
                KeeperOfSouls,
                hasUse
                    ? $"CustomReaction{KeeperOfSouls}Description".Formatted(
                        Category.Reaction, downedCreature.Name, healing.ToString())
                    : $"CustomReaction{KeeperOfSouls}SpellSlotDescription".Formatted(
                        Category.Reaction, downedCreature.Name, healing.ToString(), slotLevel.ToString()),
                candidates
                    .Select(x => (x == cleric
                        ? $"CustomReaction{KeeperOfSouls}Self".Formatted(Category.Reaction, x.Name)
                        : x.Name, true))
                    .ToList(),
                option => selectedTarget = candidates[option],
                battleManager: battleManager);

            if (selectedTarget == null)
            {
                continue;
            }

            // expending the slot restores the use, which this use then expends again
            if (hasUse)
            {
                rulesetCleric.UsePower(usablePower);
            }
            else
            {
                spellRepertoire!.SpendSpellSlot(slotLevel);
            }

            rulesetCleric.LogCharacterUsedPower(_powerKeeperOfSouls);
            EffectHelpers.StartVisualEffect(cleric, selectedTarget, CureWounds, EffectHelpers.EffectType.Effect);
            selectedTarget.RulesetCharacter.ReceiveHealing(healing, true, cleric.Guid);
        }
    }
}
