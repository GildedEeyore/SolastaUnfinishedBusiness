using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Api;
using SolastaUnfinishedBusiness.Api.GameExtensions;
using SolastaUnfinishedBusiness.Api.Helpers;
using SolastaUnfinishedBusiness.Api.LanguageExtensions;
using SolastaUnfinishedBusiness.Behaviors;
using SolastaUnfinishedBusiness.Behaviors.Specific;
using SolastaUnfinishedBusiness.Builders;
using SolastaUnfinishedBusiness.Builders.Features;
using SolastaUnfinishedBusiness.Interfaces;
using SolastaUnfinishedBusiness.Models;
using SolastaUnfinishedBusiness.Validators;
using static RuleDefinitions;
using static SolastaUnfinishedBusiness.Builders.Features.AutoPreparedSpellsGroupBuilder;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.FeatureDefinitionAttributeModifiers;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.SpellDefinitions;

namespace SolastaUnfinishedBusiness.Feats;

internal static class ArcanaUnleashedFeats
{
    internal static void CreateFeats([NotNull] List<FeatDefinition> feats)
    {
        var abjurationAdeptGroup = BuildAbjurationAdept(feats);
        var conjurationAdeptGroup = BuildConjurationAdept(feats);
        var evocationAdeptGroup = BuildEvocationAdept(feats);

        GroupFeats.FeatGroupSpellCombat.AddFeats(
            abjurationAdeptGroup,
            conjurationAdeptGroup,
            evocationAdeptGroup);
    }

    #region Abjuration Adept

    private static FeatDefinition BuildAbjurationAdept(List<FeatDefinition> feats)
    {
        const string NAME = "FeatAbjurationAdept";
        const string FAMILY = "AbjurationAdept";

        // spells are only added to the repertoire once the caster has spell slots of the spell level
        var autoPreparedSpells = FeatureDefinitionAutoPreparedSpellsBuilder
            .Create($"AutoPreparedSpells{NAME}")
            .SetGuiPresentationNoContent(true)
            .AddCustomSubFeatures(
                ValidateRepertoireForAutoprepared.AnyClassOrSubclass,
                AutoPreparedSpellsUpToMaxSpellSlotLevel.Marker)
            .SetPreparedSpellGroups(
                BuildSpellGroup(0,
                    Shield,
                    LesserRestoration,
                    ProtectionFromEnergy,
                    Banishment,
                    MassCureWounds))
            .SetSpellcastingClass(null)
            .SetAutoTag(FAMILY)
            .AddToDB();

        var featureProtectiveWard = FeatureDefinitionBuilder
            .Create($"Feature{NAME}ProtectiveWard")
            .SetGuiPresentation(Category.Feature)
            .AddToDB();

        featureProtectiveWard.AddCustomSubFeatures(
            new MagicEffectFinishedByMeProtectiveWard(featureProtectiveWard));

        var attributeIncreases = new List<(FeatureDefinition, string)>
        {
            (AttributeModifierCreed_Of_Pakri, "Int"),
            (AttributeModifierCreed_Of_Maraike, "Wis"),
            (AttributeModifierCreed_Of_Solasta, "Cha")
        };

        var groupFeats = new List<FeatDefinition>();

        foreach (var (attributeIncrease, postfix) in attributeIncreases)
        {
            groupFeats.Add(FeatDefinitionWithPrerequisitesBuilder
                .Create($"{NAME}{postfix}")
                .SetGuiPresentation(Category.Feat)
                .SetFeatures(attributeIncrease, autoPreparedSpells, featureProtectiveWard)
                .SetValidators(ValidatorsFeat.IsLevel4)
                .SetMustCastSpellsPrerequisite()
                .SetFeatFamily(FAMILY)
                .AddToDB());
        }

        feats.AddRange(groupFeats);

        var abjurationAdeptGroup = GroupFeats.MakeGroupWithPreRequisite(
            "FeatGroupAbjurationAdept", FAMILY, ValidatorsFeat.IsLevel4, [.. groupFeats]);

        abjurationAdeptGroup.mustCastSpellsPrerequisite = !Main.Settings.DisableCastSpellPreRequisitesOnModFeats;

        return abjurationAdeptGroup;
    }

    private sealed class MagicEffectFinishedByMeProtectiveWard(
        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        FeatureDefinition featureProtectiveWard) : IMagicEffectFinishedByMe
    {
        private const string ProtectiveWard = "ProtectiveWard";
        private const int RangeInCells = 6;

        public IEnumerator OnMagicEffectFinishedByMe(
            CharacterAction action,
            GameLocationCharacter attacker,
            List<GameLocationCharacter> targets)
        {
            // only casts that expend a spell slot: no cantrips, rituals, invocations, items or free casts
            if (action is not CharacterActionCastSpell actionCastSpell ||
                actionCastSpell.Countered ||
                actionCastSpell.ExecutionFailed ||
                actionCastSpell.ActionId is not (
                    ActionDefinitions.Id.CastMain or
                    ActionDefinitions.Id.CastBonus or
                    ActionDefinitions.Id.CastReaction or
                    ActionDefinitions.Id.CastReadied or
                    (ActionDefinitions.Id)ExtraActionId.CastQuickened))
            {
                yield break;
            }

            var activeSpell = actionCastSpell.ActiveSpell;

            if (activeSpell.SchoolOfMagic != SchoolAbjuration ||
                activeSpell.SlotLevel <= 0 ||
                activeSpell.SpellRepertoire?.SpellCastingFeature.SpellCastingOrigin is not (
                    FeatureDefinitionCastSpell.CastingOrigin.Class or
                    FeatureDefinitionCastSpell.CastingOrigin.Subclass))
            {
                yield break;
            }

            var rulesetAttacker = attacker.RulesetCharacter;

            if (rulesetAttacker is not { IsDeadOrDyingOrUnconscious: false })
            {
                yield break;
            }

            var tempHitPoints = 2 * activeSpell.SlotLevel;
            var locationCharacterService = ServiceRepository.GetService<IGameLocationCharacterService>();

            // you first, then allies you can see within 30 feet, closest first
            var candidates = new List<GameLocationCharacter> { attacker };

            candidates.AddRange(locationCharacterService.PartyCharacters
                .Union(locationCharacterService.GuestCharacters)
                .Where(x =>
                    x != attacker &&
                    x.RulesetCharacter is { IsDeadOrDyingOrUnconscious: false } &&
                    attacker.IsWithinRange(x, RangeInCells) &&
                    attacker.CanPerceiveTarget(x))
                .OrderBy(x => DistanceCalculation.GetDistanceFromCharacters(attacker, x)));

            GameLocationCharacter selectedTarget = null;

            if (candidates.Count == 1)
            {
                selectedTarget = attacker;
            }
            else
            {
                foreach (var candidate in candidates)
                {
                    yield return attacker.MyReactToDoNothing(
                        ExtraActionId.DoNothingFree,
                        attacker,
                        ProtectiveWard,
                        "CustomReactionProtectiveWardDescription".Formatted(
                            Category.Reaction, tempHitPoints.ToString(), candidate.Name),
                        () => selectedTarget = candidate);

                    if (selectedTarget != null)
                    {
                        break;
                    }
                }
            }

            if (selectedTarget == null)
            {
                yield break;
            }

            selectedTarget.RulesetCharacter.ReceiveTemporaryHitPoints(
                tempHitPoints, DurationType.UntilLongRest, 0, TurnOccurenceType.StartOfTurn, rulesetAttacker.Guid);

            EffectHelpers.StartVisualEffect(
                selectedTarget, selectedTarget,
                DatabaseHelper.FeatureDefinitionPowers.PowerTraditionCourtMageSpellShield,
                EffectHelpers.EffectType.Caster);

            rulesetAttacker.LogCharacterUsedFeature(
                featureProtectiveWard,
                "Feedback/&ProtectiveWardTemporaryHitPoints",
                true,
                (ConsoleStyleDuplet.ParameterType.Player, selectedTarget.Name),
                (ConsoleStyleDuplet.ParameterType.Positive, tempHitPoints.ToString()));
        }
    }

    #endregion

    #region Conjuration Adept

    private static FeatDefinition BuildConjurationAdept(List<FeatDefinition> feats)
    {
        const string NAME = "FeatConjurationAdept";
        const string FAMILY = "ConjurationAdept";

        // spells are only added to the repertoire once the caster has spell slots of the spell level
        var autoPreparedSpells = FeatureDefinitionAutoPreparedSpellsBuilder
            .Create($"AutoPreparedSpells{NAME}")
            .SetGuiPresentationNoContent(true)
            .AddCustomSubFeatures(
                ValidateRepertoireForAutoprepared.AnyClassOrSubclass,
                AutoPreparedSpellsUpToMaxSpellSlotLevel.Marker)
            .SetPreparedSpellGroups(
                BuildSpellGroup(0,
                    Entangle,
                    MistyStep,
                    ConjureAnimals,
                    DimensionDoor,
                    ConjureElemental))
            .SetSpellcastingClass(null)
            .SetAutoTag(FAMILY)
            .AddToDB();

        var attributeIncreases = new List<(FeatureDefinition, string, string)>
        {
            (AttributeModifierCreed_Of_Pakri, "Int", AttributeDefinitions.Intelligence),
            (AttributeModifierCreed_Of_Maraike, "Wis", AttributeDefinitions.Wisdom),
            (AttributeModifierCreed_Of_Solasta, "Cha", AttributeDefinitions.Charisma)
        };

        var groupFeats = new List<FeatDefinition>();

        foreach (var (attributeIncrease, postfix, attributeName) in attributeIncreases)
        {
            // the bonus uses the ability this feat increased so each variant needs its own feature
            var featurePersistentConjuration = FeatureDefinitionBuilder
                .Create($"Feature{NAME}PersistentConjuration{postfix}")
                .SetGuiPresentation($"Feature{NAME}PersistentConjuration", Category.Feature)
                .AddToDB();

            featurePersistentConjuration.AddCustomSubFeatures(
                new RollSavingCheckInitiatedPersistentConjuration(featurePersistentConjuration, attributeName));

            groupFeats.Add(FeatDefinitionWithPrerequisitesBuilder
                .Create($"{NAME}{postfix}")
                .SetGuiPresentation(Category.Feat)
                .SetFeatures(attributeIncrease, autoPreparedSpells, featurePersistentConjuration)
                .SetValidators(ValidatorsFeat.IsLevel4)
                .SetMustCastSpellsPrerequisite()
                .SetFeatFamily(FAMILY)
                .AddToDB());
        }

        feats.AddRange(groupFeats);

        var conjurationAdeptGroup = GroupFeats.MakeGroupWithPreRequisite(
            "FeatGroupConjurationAdept", FAMILY, ValidatorsFeat.IsLevel4, [.. groupFeats]);

        conjurationAdeptGroup.mustCastSpellsPrerequisite = !Main.Settings.DisableCastSpellPreRequisitesOnModFeats;

        return conjurationAdeptGroup;
    }

    // only called from concentration checks
    private sealed class RollSavingCheckInitiatedPersistentConjuration(
        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        FeatureDefinition featurePersistentConjuration,
        string attributeName) : IRollSavingCheckInitiated
    {
        public void OnRollSavingCheckInitiated(
            RulesetCharacter defender,
            int saveDC,
            string damageType,
            ref ActionModifier actionModifier,
            ref int modifier)
        {
            var concentratedSpell = defender.ConcentratedSpell;

            if (concentratedSpell == null ||
                concentratedSpell.SpellDefinition.SchoolOfMagic !=
                DatabaseHelper.SchoolOfMagicDefinitions.SchoolConjuration.Name)
            {
                return;
            }

            var bonus = AttributeDefinitions.ComputeAbilityScoreModifier(defender.TryGetAttributeValue(attributeName));

            if (bonus == 0)
            {
                return;
            }

            modifier += bonus;
            actionModifier.SavingThrowModifierTrends.Add(
                new TrendInfo(bonus, FeatureSourceType.CharacterFeature, featurePersistentConjuration.Name,
                    featurePersistentConjuration));
        }
    }

    #endregion

    #region Evocation Adept

    private static FeatDefinition BuildEvocationAdept(List<FeatDefinition> feats)
    {
        const string NAME = "FeatEvocationAdept";
        const string FAMILY = "EvocationAdept";

        // spells are only added to the repertoire once the caster has spell slots of the spell level
        var autoPreparedSpells = FeatureDefinitionAutoPreparedSpellsBuilder
            .Create($"AutoPreparedSpells{NAME}")
            .SetGuiPresentationNoContent(true)
            .AddCustomSubFeatures(
                ValidateRepertoireForAutoprepared.AnyClassOrSubclass,
                AutoPreparedSpellsUpToMaxSpellSlotLevel.Marker)
            .SetPreparedSpellGroups(
                BuildSpellGroup(0,
                    SpellsContext.ChromaticOrb,
                    Shatter,
                    Fireball,
                    SpellsContext.VitriolicSphere,
                    // Wall of Force exists in the database but vanilla flags it as not implemented so it can't be cast
                    ConeOfCold))
            .SetSpellcastingClass(null)
            .SetAutoTag(FAMILY)
            .AddToDB();

        var featureFueledEvocation = FeatureDefinitionBuilder
            .Create($"Feature{NAME}FueledEvocation")
            .SetGuiPresentation(Category.Feature)
            .AddToDB();

        featureFueledEvocation.AddCustomSubFeatures(
            new MagicEffectBeforeHitConfirmedOnEnemyFueledEvocation(featureFueledEvocation));

        var attributeIncreases = new List<(FeatureDefinition, string)>
        {
            (AttributeModifierCreed_Of_Pakri, "Int"),
            (AttributeModifierCreed_Of_Maraike, "Wis"),
            (AttributeModifierCreed_Of_Solasta, "Cha")
        };

        var groupFeats = new List<FeatDefinition>();

        foreach (var (attributeIncrease, postfix) in attributeIncreases)
        {
            groupFeats.Add(FeatDefinitionWithPrerequisitesBuilder
                .Create($"{NAME}{postfix}")
                .SetGuiPresentation(Category.Feat)
                .SetFeatures(attributeIncrease, autoPreparedSpells, featureFueledEvocation)
                .SetValidators(ValidatorsFeat.IsLevel4)
                .SetMustCastSpellsPrerequisite()
                .SetFeatFamily(FAMILY)
                .AddToDB());
        }

        feats.AddRange(groupFeats);

        var evocationAdeptGroup = GroupFeats.MakeGroupWithPreRequisite(
            "FeatGroupEvocationAdept", FAMILY, ValidatorsFeat.IsLevel4, [.. groupFeats]);

        evocationAdeptGroup.mustCastSpellsPrerequisite = !Main.Settings.DisableCastSpellPreRequisitesOnModFeats;

        return evocationAdeptGroup;
    }

    private sealed class MagicEffectBeforeHitConfirmedOnEnemyFueledEvocation(
        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        FeatureDefinition featureFueledEvocation) : IMagicEffectBeforeHitConfirmedOnEnemy
    {
        private const string FueledEvocation = "FueledEvocation";

        private int _bonusDamage;
        private RulesetEffect _rulesetEffect;

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
            if (rulesetEffect.SourceDefinition is not SpellDefinition ||
                rulesetEffect.SchoolOfMagic != SchoolEvocation)
            {
                yield break;
            }

            var effectForm = actualEffectForms
                .FirstOrDefault(x => x.FormType == EffectForm.EffectFormType.Damage);

            // the hit dice total is added to one damage roll, and area spells roll damage once for all targets
            if (!firstTarget)
            {
                if (effectForm != null &&
                    _bonusDamage > 0 &&
                    rulesetEffect == _rulesetEffect &&
                    rulesetEffect.EffectDescription.TargetType
                        is not (TargetType.Individuals or TargetType.IndividualsUnique))
                {
                    effectForm.DamageForm.BonusDamage += _bonusDamage;
                }

                yield break;
            }

            _rulesetEffect = rulesetEffect;
            _bonusDamage = 0;

            if (effectForm == null)
            {
                yield break;
            }

            // once per turn, which isn't a restriction outside combat
            var battle = Gui.Battle;
            var usageKey = battle == null
                ? null
                : $"{FueledEvocation}{battle.CurrentRound}{battle.ActiveContender?.Guid}";

            if (usageKey != null && !attacker.OncePerTurnIsValid(usageKey))
            {
                yield break;
            }

            var rulesetHero = attacker.RulesetCharacter.GetOriginalHero();

            if (rulesetHero == null)
            {
                yield break;
            }

            var remainingHitDice = rulesetHero.RemainingHitDiceCount();

            if (remainingHitDice <= 0)
            {
                yield break;
            }

            var hitDiceToSpend = 0;

            // the number of dice is chosen before rolling
            if (remainingHitDice >= 2)
            {
                yield return attacker.MyReactToDoNothing(
                    ExtraActionId.DoNothingFree,
                    attacker,
                    $"{FueledEvocation}TwoDice",
                    "CustomReactionFueledEvocationTwoDiceDescription".Formatted(
                        Category.Reaction, remainingHitDice.ToString()),
                    () => hitDiceToSpend = 2,
                    battleManager: battleManager);
            }

            if (hitDiceToSpend == 0)
            {
                yield return attacker.MyReactToDoNothing(
                    ExtraActionId.DoNothingFree,
                    attacker,
                    $"{FueledEvocation}OneDie",
                    "CustomReactionFueledEvocationOneDieDescription".Formatted(
                        Category.Reaction, remainingHitDice.ToString()),
                    () => hitDiceToSpend = 1,
                    battleManager: battleManager);
            }

            if (hitDiceToSpend == 0)
            {
                yield break;
            }

            if (usageKey != null)
            {
                attacker.SetSpecialFeatureUses(usageKey, 1);
            }

            var rulesetAttacker = attacker.RulesetCharacter;
            var rolls = new List<string>();

            for (var i = 0; i < hitDiceToSpend; i++)
            {
                var dieType = ExpendHitDie(rulesetHero);

                if (dieType == DieType.D1)
                {
                    break;
                }

                var roll = RollDie(dieType, AdvantageType.None, out _, out _);

                rulesetAttacker.ShowDieRoll(dieType, roll, title: featureFueledEvocation.GuiPresentation.Title);
                rolls.Add($"{Gui.FormatDieTitle(dieType)}:{roll}");
                _bonusDamage += roll;
            }

            if (_bonusDamage <= 0)
            {
                yield break;
            }

            rulesetAttacker.LogCharacterActivatesAbility(
                featureFueledEvocation.GuiPresentation.Title,
                "Feedback/&FueledEvocationHitDiceRolled",
                extra:
                [
                    (ConsoleStyleDuplet.ParameterType.AbilityInfo, string.Join(", ", rolls)),
                    (ConsoleStyleDuplet.ParameterType.Positive, _bonusDamage.ToString())
                ]);

            effectForm.DamageForm.BonusDamage += _bonusDamage;
        }

        // same die selection as vanilla RulesetCharacterHero.RollHitDie: the largest die type still available
        private static DieType ExpendHitDie(RulesetCharacterHero hero)
        {
            var dieType = DieType.D1;
            var maxValue = 0;

            foreach (var hitDiceGroup in hero.ClassesHistory.GroupBy(x => x.HitDice))
            {
                hero.spentHitDice.TryGetValue(hitDiceGroup.Key, out var spent);

                var value = DiceMaxValue[(int)hitDiceGroup.Key];

                if (spent >= hitDiceGroup.Count() || value <= maxValue)
                {
                    continue;
                }

                dieType = hitDiceGroup.Key;
                maxValue = value;
            }

            if (dieType == DieType.D1)
            {
                return dieType;
            }

            hero.spentHitDice.TryGetValue(dieType, out var alreadySpent);
            hero.spentHitDice[dieType] = alreadySpent + 1;

            return dieType;
        }
    }

    #endregion
}
