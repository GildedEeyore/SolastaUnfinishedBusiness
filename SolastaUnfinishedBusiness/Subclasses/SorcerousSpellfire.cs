using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Api.GameExtensions;
using SolastaUnfinishedBusiness.Api.Helpers;
using SolastaUnfinishedBusiness.Api.LanguageExtensions;
using SolastaUnfinishedBusiness.Behaviors;
using SolastaUnfinishedBusiness.Behaviors.Specific;
using SolastaUnfinishedBusiness.Builders;
using SolastaUnfinishedBusiness.Builders.Features;
using SolastaUnfinishedBusiness.Feats;
using SolastaUnfinishedBusiness.Interfaces;
using SolastaUnfinishedBusiness.Models;
using SolastaUnfinishedBusiness.Validators;
using static RuleDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.CharacterClassDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.SpellDefinitions;

namespace SolastaUnfinishedBusiness.Subclasses;

[UsedImplicitly]
public sealed class SorcerousSpellfire : AbstractSubclass
{
    private const string Name = "SorcerousSpellfire";
    private const string OriginTag = "Origin";

    // condition the 2024 Innate Sorcery power grants, see Tabletop2024Context
    private const string ConditionInnateSorceryName = "ConditionSorcererInnateSorcery";

    private const int CrownRestoreCost = 5;

    private static ConditionDefinition _conditionSpellfireBurst;
    private static ConditionDefinition _conditionCrownOfSpellfire;
    private static FeatureDefinitionPower _powerSpellfireBurst;

    public SorcerousSpellfire()
    {
        // LEVEL 01 (moved to 3 by the 2024 sorcerer origin option)

        // Spellfire Burst

        // marks that sorcery points were spent this turn, the burst powers can only be used while it's present
        _conditionSpellfireBurst = ConditionDefinitionBuilder
            .Create($"Condition{Name}SpellfireBurst")
            .SetGuiPresentation(Category.Condition, ConditionDefinitions.ConditionHeraldOfBattle)
            .SetPossessive()
            .SetSilent(Silent.WhenAddedOrRemoved)
            .AddToDB();

        var featureHonedSpellfire = FeatureDefinitionBuilder
            .Create($"Feature{Name}HonedSpellfire")
            .SetGuiPresentation(Category.Feature)
            .AddToDB();

        _powerSpellfireBurst = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}SpellfireBurst")
            .SetGuiPresentation(Category.Feature, FeatureDefinitionPowers.PowerDomainElementalFireBurst)
            .SetUsesFixed(ActivationTime.NoCost)
            .AddCustomSubFeatures(
                new ValidatorsValidatePowerUse(ValidatorsCharacter.HasAnyOfConditions(_conditionSpellfireBurst.Name)))
            .AddToDB();

        var powerBolsteringFlames = FeatureDefinitionPowerSharedPoolBuilder
            .Create($"Power{Name}BolsteringFlames")
            .SetGuiPresentation(Category.Feature, FalseLife, hidden: true)
            .SetSharedPool(ActivationTime.NoCost, _powerSpellfireBurst)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Ally, RangeType.Distance, 6, TargetType.IndividualsUnique)
                    .SetEffectForms(EffectFormBuilder.Create().SetTempHpForm(0, DieType.D4, 1).Build())
                    .SetParticleEffectParameters(FalseLife)
                    .Build())
            .AddToDB();

        var powerRadiantFireFire = FeatureDefinitionPowerSharedPoolBuilder
            .Create($"Power{Name}RadiantFireFire")
            .SetGuiPresentation(Category.Feature, FireBolt, hidden: true)
            .SetSharedPool(ActivationTime.NoCost, _powerSpellfireBurst)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Enemy, RangeType.Distance, 6, TargetType.IndividualsUnique)
                    .SetEffectForms(EffectFormBuilder.DamageForm(DamageTypeFire, 1, DieType.D4))
                    .SetParticleEffectParameters(FireBolt)
                    .Build())
            .AddToDB();

        var powerRadiantFireRadiant = FeatureDefinitionPowerSharedPoolBuilder
            .Create($"Power{Name}RadiantFireRadiant")
            .SetGuiPresentation(Category.Feature, SacredFlame, hidden: true)
            .SetSharedPool(ActivationTime.NoCost, _powerSpellfireBurst)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Enemy, RangeType.Distance, 6, TargetType.IndividualsUnique)
                    .SetEffectForms(EffectFormBuilder.DamageForm(DamageTypeRadiant, 1, DieType.D4))
                    .SetParticleEffectParameters(SacredFlame)
                    .Build())
            .AddToDB();

        var spellfireBurstPowers = new[] { powerBolsteringFlames, powerRadiantFireFire, powerRadiantFireRadiant };
        var customBehaviorSpellfireBurst = new CustomBehaviorSpellfireBurst(
            featureHonedSpellfire, powerBolsteringFlames, spellfireBurstPowers);

        foreach (var power in spellfireBurstPowers)
        {
            power.AddCustomSubFeatures(
                new ValidatorsValidatePowerUse(ValidatorsCharacter.HasAnyOfConditions(_conditionSpellfireBurst.Name)),
                customBehaviorSpellfireBurst);
        }

        PowerBundle.RegisterPowerBundle(_powerSpellfireBurst, false, spellfireBurstPowers);

        var featureSetSpellfireBurst = FeatureDefinitionFeatureSetBuilder
            .Create($"FeatureSet{Name}SpellfireBurst")
            .SetGuiPresentation($"Power{Name}SpellfireBurst", Category.Feature)
            .AddFeatureSet(_powerSpellfireBurst, powerBolsteringFlames, powerRadiantFireFire, powerRadiantFireRadiant)
            .AddToDB();

        // Spellfire Spells

        var autoPreparedSpellsSpellfireSpells = FeatureDefinitionAutoPreparedSpellsBuilder
            .Create($"AutoPreparedSpells{Name}SpellfireSpells")
            .SetGuiPresentation(Category.Feature)
            .SetAutoTag(OriginTag)
            .SetSpellcastingClass(Sorcerer)
            .AddPreparedSpellGroup(3, CureWounds, GuidingBolt, LesserRestoration, ScorchingRay)
            .AddPreparedSpellGroup(5, SpellsContext.AuraOfVitality, DispelMagic)
            .AddPreparedSpellGroup(7, FireShield, WallOfFire)
            .AddPreparedSpellGroup(9, GreaterRestoration, FlameStrike)
            .AddToDB();

        // LEVEL 06

        // Absorb Spells

        var autoPreparedSpellsAbsorbSpells = FeatureDefinitionAutoPreparedSpellsBuilder
            .Create($"AutoPreparedSpells{Name}AbsorbSpells")
            .SetGuiPresentation(Category.Feature)
            .SetAutoTag(OriginTag)
            .SetSpellcastingClass(Sorcerer)
            .AddPreparedSpellGroup(6, Counterspell)
            .AddToDB();

        autoPreparedSpellsAbsorbSpells.AddCustomSubFeatures(
            new MagicEffectFinishedByMeAbsorbSpells(autoPreparedSpellsAbsorbSpells));

        // LEVEL 18

        // Crown of Spellfire

        var featureBurningLifeForce = FeatureDefinitionBuilder
            .Create($"Feature{Name}BurningLifeForce")
            .SetGuiPresentation(Category.Feature)
            .AddToDB();

        _conditionCrownOfSpellfire = ConditionDefinitionBuilder
            .Create(ConditionDefinitions.ConditionFlying, $"Condition{Name}CrownOfSpellfire")
            .SetGuiPresentation(Category.Condition, ConditionDefinitions.ConditionFlying)
            .SetPossessive()
            .SetParentCondition(ConditionDefinitions.ConditionFlying)
            // 60 ft. fly speed, and unlike Draconic Flight it isn't cancelled by incapacitation so the caster hovers
            .SetFeatures(FeatureDefinitionMoveModes.MoveModeFly12)
            .AddCustomSubFeatures(new CustomBehaviorBurningLifeForce(featureBurningLifeForce))
            .AddToDB();

        var powerCrownOfSpellfire = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}CrownOfSpellfire")
            .SetGuiPresentation(Category.Feature, FeatureDefinitionPowers.PowerDomainElementalFireBurst)
            .SetUsesFixed(ActivationTime.NoCost, RechargeRate.LongRest)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Ally, RangeType.Self, 0, TargetType.Self)
                    .SetCasterEffectParameters(FeatureDefinitionPowers.PowerSorcererDraconicElementalResistance)
                    .Build())
            .AddToDB();

        powerCrownOfSpellfire.AddCustomSubFeatures(
            new ValidatorsValidatePowerUse(
                ValidatorsCharacter.HasAnyOfConditions(ConditionInnateSorceryName),
                ValidatorsCharacter.HasNoneOfConditions(_conditionCrownOfSpellfire.Name)),
            new PowerOrSpellFinishedByMeCrownOfSpellfire(false));

        var powerCrownOfSpellfireRestore = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}CrownOfSpellfireRestore")
            .SetGuiPresentation(Category.Feature, FeatureDefinitionPowers.PowerDomainElementalFireBurst)
            .SetUsesFixed(ActivationTime.NoCost)
            .SetEffectDescription(powerCrownOfSpellfire.EffectDescription)
            .AddToDB();

        powerCrownOfSpellfireRestore.AddCustomSubFeatures(
            new ValidatorsValidatePowerUse(
                ValidatorsCharacter.HasAnyOfConditions(ConditionInnateSorceryName),
                ValidatorsCharacter.HasNoneOfConditions(_conditionCrownOfSpellfire.Name),
                c => c.GetRemainingPowerUses(powerCrownOfSpellfire) == 0 &&
                     c.RemainingSorceryPoints >= CrownRestoreCost),
            new PowerOrSpellFinishedByMeCrownOfSpellfire(true));

        var featureSetCrownOfSpellfire = FeatureDefinitionFeatureSetBuilder
            .Create($"FeatureSet{Name}CrownOfSpellfire")
            .SetGuiPresentation(Category.Feature)
            .AddFeatureSet(powerCrownOfSpellfire, powerCrownOfSpellfireRestore)
            .AddToDB();

        Subclass = CharacterSubclassDefinitionBuilder
            .Create(Name)
            .SetGuiPresentation(Category.Subclass, CharacterSubclassDefinitions.SorcerousManaPainter)
            .AddFeaturesAtLevel(1, featureSetSpellfireBurst, autoPreparedSpellsSpellfireSpells)
            .AddFeaturesAtLevel(6, autoPreparedSpellsAbsorbSpells)
            .AddFeaturesAtLevel(14, featureHonedSpellfire)
            .AddFeaturesAtLevel(18, featureSetCrownOfSpellfire)
            .AddToDB();
    }

    internal override CharacterClassDefinition Klass => Sorcerer;

    internal override CharacterSubclassDefinition Subclass { get; }

    internal override FeatureDefinitionSubclassChoice SubclassChoice =>
        FeatureDefinitionSubclassChoices.SubclassChoiceSorcerousOrigin;

    // ReSharper disable once UnassignedGetOnlyAutoProperty
    internal override DeityDefinition DeityDefinition { get; }

    private static string SpellfireBurstUsageKey => $"Power{Name}SpellfireBurst";

    // called from RulesetCharacter.SpendSorceryPoints, which every sorcery point expense goes through
    internal static void OnSorceryPointsSpent(RulesetCharacter rulesetCharacter, int spentPoints)
    {
        if (spentPoints <= 0 ||
            _powerSpellfireBurst == null ||
            !rulesetCharacter.HasAnyFeature(_powerSpellfireBurst) ||
            rulesetCharacter.HasConditionOfType(_conditionSpellfireBurst.Name))
        {
            return;
        }

        var character = GameLocationCharacter.GetFromActor(rulesetCharacter);

        if (character == null)
        {
            return;
        }

        var battle = Gui.Battle;

        // only on your own turn and once per turn, which isn't a restriction outside combat
        if (battle != null &&
            (battle.ActiveContender != character || !character.OncePerTurnIsValid(SpellfireBurstUsageKey)))
        {
            return;
        }

        rulesetCharacter.InflictCondition(
            _conditionSpellfireBurst.Name,
            battle != null ? DurationType.Round : DurationType.Minute,
            battle != null ? 0 : 1,
            TurnOccurenceType.EndOfTurn,
            AttributeDefinitions.TagEffect,
            rulesetCharacter.guid,
            rulesetCharacter.CurrentFaction.Name,
            1,
            _conditionSpellfireBurst.Name,
            0,
            0,
            0);
    }

    // called from RulesetActor.HasSpecialHalfDamage to apply Spell Avoidance
    internal static bool HasSpellAvoidance(RulesetActor rulesetActor)
    {
        return _conditionCrownOfSpellfire != null &&
               rulesetActor is RulesetCharacter { IsIncapacitated: false } rulesetCharacter &&
               rulesetCharacter.HasConditionOfType(_conditionCrownOfSpellfire.Name);
    }

    private sealed class CustomBehaviorSpellfireBurst(
        FeatureDefinition featureHonedSpellfire,
        FeatureDefinitionPower powerBolsteringFlames,
        FeatureDefinitionPower[] spellfireBurstPowers) : IModifyEffectDescription, IPowerOrSpellFinishedByMe
    {
        public bool IsValid(BaseDefinition definition, RulesetCharacter character, EffectDescription effectDescription)
        {
            return Array.IndexOf(spellfireBurstPowers, definition) >= 0;
        }

        public EffectDescription GetEffectDescription(
            BaseDefinition definition,
            EffectDescription effectDescription,
            RulesetCharacter character,
            RulesetEffect rulesetEffect)
        {
            var honed = character.HasAnyFeature(featureHonedSpellfire);

            if (definition == powerBolsteringFlames)
            {
                var charisma = character.TryGetAttributeValue(AttributeDefinitions.Charisma);
                var bonus = AttributeDefinitions.ComputeAbilityScoreModifier(charisma);

                if (honed)
                {
                    bonus += character.GetClassLevel(Sorcerer);
                }

                effectDescription.EffectForms[0] =
                    EffectFormBuilder.Create().SetTempHpForm(bonus, DieType.D4, 1).Build();
            }
            else if (honed)
            {
                var damageType = effectDescription.EffectForms[0].DamageForm.DamageType;

                effectDescription.EffectForms[0] = EffectFormBuilder.DamageForm(damageType, 1, DieType.D8);
            }

            return effectDescription;
        }

        public IEnumerator OnPowerOrSpellFinishedByMe(CharacterActionMagicEffect action, BaseDefinition baseDefinition)
        {
            var actingCharacter = action.ActingCharacter;
            var rulesetCharacter = actingCharacter.RulesetCharacter;

            rulesetCharacter.RemoveAllConditionsOfType(_conditionSpellfireBurst.Name);

            if (Gui.Battle != null)
            {
                actingCharacter.SetSpecialFeatureUses(SpellfireBurstUsageKey, 1);
            }

            yield break;
        }
    }

    private sealed class MagicEffectFinishedByMeAbsorbSpells(FeatureDefinition featureAbsorbSpells)
        : IMagicEffectFinishedByMe
    {
        public IEnumerator OnMagicEffectFinishedByMe(
            CharacterAction action,
            GameLocationCharacter attacker,
            List<GameLocationCharacter> targets)
        {
            // Solasta's Counterspell has no saving throw, it counters automatically or on a spellcasting ability check
            if (action.ActionParams.RulesetEffect?.SourceDefinition != Counterspell ||
                action.ActionParams.TargetAction?.Countered != true)
            {
                yield break;
            }

            var rulesetCharacter = attacker.RulesetCharacter;
            var roll = RollDie(DieType.D4, AdvantageType.None, out _, out _);

            rulesetCharacter.ShowDieRoll(DieType.D4, roll, title: featureAbsorbSpells.GuiPresentation.Title);
            rulesetCharacter.GainSorceryPoints(roll);
            rulesetCharacter.LogCharacterUsedFeature(featureAbsorbSpells,
                "Feedback/&SorcerousSpellfireAbsorbSpellsGain",
                extra: [(ConsoleStyleDuplet.ParameterType.Positive, roll.ToString())]);
        }
    }

    private sealed class PowerOrSpellFinishedByMeCrownOfSpellfire(bool spendSorceryPoints) : IPowerOrSpellFinishedByMe
    {
        public IEnumerator OnPowerOrSpellFinishedByMe(CharacterActionMagicEffect action, BaseDefinition baseDefinition)
        {
            var rulesetCharacter = action.ActingCharacter.RulesetCharacter;

            if (!rulesetCharacter.TryGetConditionOfCategoryAndType(
                    AttributeDefinitions.TagEffect, ConditionInnateSorceryName, out var activeInnateSorcery))
            {
                yield break;
            }

            // restoring the use costs sorcery points with no action required, so it doesn't trigger Spellfire Burst
            if (spendSorceryPoints)
            {
                rulesetCharacter.usedSorceryPoints += CrownRestoreCost;
                rulesetCharacter.SorceryPointsAltered?.Invoke(
                    rulesetCharacter, rulesetCharacter.RemainingSorceryPoints);
            }

            // the crown lasts while this use of Innate Sorcery is active
            var activeCrown = rulesetCharacter.InflictCondition(
                _conditionCrownOfSpellfire.Name,
                activeInnateSorcery.DurationType,
                activeInnateSorcery.DurationParameter,
                activeInnateSorcery.EndOccurence,
                AttributeDefinitions.TagEffect,
                rulesetCharacter.guid,
                rulesetCharacter.CurrentFaction.Name,
                1,
                _conditionCrownOfSpellfire.Name,
                0,
                0,
                0);

            if (activeCrown != null)
            {
                activeCrown.RemainingRounds = activeInnateSorcery.RemainingRounds;
            }
        }
    }

    private sealed class CustomBehaviorBurningLifeForce(FeatureDefinition featureBurningLifeForce)
        : IPhysicalAttackBeforeHitConfirmedOnMe, IMagicEffectBeforeHitConfirmedOnMe
    {
        private const string BurningLifeForce = "SorcerousSpellfireBurningLifeForce";

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
            // only magical effects that use an attack roll
            if (rulesetEffect.EffectDescription.RangeType is not (RangeType.MeleeHit or RangeType.RangeHit))
            {
                yield break;
            }

            yield return HandleBurningLifeForce(battleManager, defender, actionModifier, actualEffectForms);
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
            yield return HandleBurningLifeForce(battleManager, defender, actionModifier, actualEffectForms);
        }

        private IEnumerator HandleBurningLifeForce(
            GameLocationBattleManager battleManager,
            GameLocationCharacter defender,
            ActionModifier actionModifier,
            List<EffectForm> actualEffectForms)
        {
            if (!actualEffectForms.Exists(x => x.FormType == EffectForm.EffectFormType.Damage))
            {
                yield break;
            }

            // once per turn, which isn't a restriction outside combat
            var battle = Gui.Battle;
            var usageKey = battle == null
                ? null
                : $"{BurningLifeForce}{battle.CurrentRound}{battle.ActiveContender?.Guid}";

            if (usageKey != null && !defender.OncePerTurnIsValid(usageKey))
            {
                yield break;
            }

            var rulesetDefender = defender.RulesetCharacter;
            var rulesetHero = rulesetDefender.GetOriginalHero();

            if (rulesetHero == null)
            {
                yield break;
            }

            var remainingHitDice = rulesetHero.RemainingHitDiceCount();

            if (remainingHitDice <= 0)
            {
                yield break;
            }

            var charisma = rulesetDefender.TryGetAttributeValue(AttributeDefinitions.Charisma);
            var maxHitDice = Math.Max(1, AttributeDefinitions.ComputeAbilityScoreModifier(charisma));
            var options = new List<(string label, bool available)>
            {
                (Gui.Localize("Reaction/&CustomReactionSorcerousSpellfireBurningLifeForceZeroDice"), true)
            };

            for (var i = 1; i <= maxHitDice; i++)
            {
                options.Add((
                    "CustomReactionSorcerousSpellfireBurningLifeForceDice".Formatted(
                        Category.Reaction, i.ToString()),
                    remainingHitDice >= i));
            }

            var hitDiceToSpend = 0;

            // the number of dice is chosen before rolling, all in one prompt where the option index is the dice count
            yield return defender.MyReactToSelectOption(
                defender,
                BurningLifeForce,
                "CustomReactionSorcerousSpellfireBurningLifeForceDescription".Formatted(
                    Category.Reaction, remainingHitDice.ToString(), maxHitDice.ToString()),
                options,
                option => hitDiceToSpend = option,
                battleManager: battleManager);

            if (hitDiceToSpend <= 0)
            {
                yield break;
            }

            if (usageKey != null)
            {
                defender.SetSpecialFeatureUses(usageKey, 1);
            }

            var rolls = new List<string>();
            var reduction = 0;

            for (var i = 0; i < hitDiceToSpend; i++)
            {
                var dieType = ArcanaUnleashedFeats.ExpendHitDie(rulesetHero);

                if (dieType == DieType.D1)
                {
                    break;
                }

                var roll = RollDie(dieType, AdvantageType.None, out _, out _);

                rulesetDefender.ShowDieRoll(dieType, roll, title: featureBurningLifeForce.GuiPresentation.Title);
                rolls.Add($"{Gui.FormatDieTitle(dieType)}:{roll}");
                reduction += roll;
            }

            if (reduction <= 0)
            {
                yield break;
            }

            actionModifier.DamageRollReduction += reduction;
            rulesetDefender.DamageReduced?.Invoke(rulesetDefender, featureBurningLifeForce, reduction);
            rulesetDefender.LogCharacterActivatesAbility(
                featureBurningLifeForce.GuiPresentation.Title,
                "Feedback/&SorcerousSpellfireBurningLifeForceReduction",
                extra:
                [
                    (ConsoleStyleDuplet.ParameterType.AbilityInfo, string.Join(", ", rolls)),
                    (ConsoleStyleDuplet.ParameterType.Positive, reduction.ToString())
                ]);
        }
    }
}
