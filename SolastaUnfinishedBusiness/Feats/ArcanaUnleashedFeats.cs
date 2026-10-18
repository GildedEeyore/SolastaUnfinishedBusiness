using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Api;
using SolastaUnfinishedBusiness.Api.GameExtensions;
using SolastaUnfinishedBusiness.Api.Helpers;
using SolastaUnfinishedBusiness.Api.LanguageExtensions;
using SolastaUnfinishedBusiness.Behaviors;
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
        var evocationAdeptGroup = BuildEvocationAdept(feats);

        GroupFeats.FeatGroupSpellCombat.AddFeats(
            evocationAdeptGroup);
    }

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
