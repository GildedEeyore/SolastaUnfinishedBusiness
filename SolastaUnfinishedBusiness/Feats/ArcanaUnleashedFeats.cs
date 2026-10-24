using System;
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
using SolastaUnfinishedBusiness.CustomUI;
using SolastaUnfinishedBusiness.Interfaces;
using SolastaUnfinishedBusiness.Models;
using SolastaUnfinishedBusiness.Properties;
using SolastaUnfinishedBusiness.Validators;
using static RuleDefinitions;
using static SolastaUnfinishedBusiness.Builders.Features.AutoPreparedSpellsGroupBuilder;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.FeatureDefinitionAttributeModifiers;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.SpellDefinitions;

namespace SolastaUnfinishedBusiness.Feats;

internal static class ArcanaUnleashedFeats
{
    // origin feats any background bonus feat pool can pick in place of the background's own feat
    internal static readonly List<string> OriginFeatChoices = [];

    internal static void CreateFeats([NotNull] List<FeatDefinition> feats)
    {
        var abjurationAdeptGroup = BuildAbjurationAdept(feats);
        var conjurationAdeptGroup = BuildConjurationAdept(feats);
        var evocationAdeptGroup = BuildEvocationAdept(feats);
        var necromancyAdeptGroup = BuildNecromancyAdept(feats);
        var arcaneEloquenceGroup = BuildArcaneEloquence(feats);
        var arcaneInfiltrator = BuildArcaneInfiltrator(feats);
        var arcaneOmensGroup = BuildArcaneOmens(feats);
        var portalJumperGroup = BuildPortalJumper(feats);
        var spellResistantGroup = BuildSpellResistant(feats);
        var transmutedAnatomy = BuildTransmutedAnatomy(feats);

        // listed on its own like Magic Initiate
        BuildMagicConnoisseur(feats);
        BuildRuneShaper(feats);

        GroupFeats.FeatGroupOrigin.AddFeats(
            arcaneEloquenceGroup, arcaneInfiltrator, arcaneOmensGroup, portalJumperGroup, transmutedAnatomy);

        GroupFeats.FeatGroupBodyResilience.AddFeats(spellResistantGroup);

        GroupFeats.FeatGroupSpellCombat.AddFeats(
            abjurationAdeptGroup,
            conjurationAdeptGroup,
            evocationAdeptGroup,
            necromancyAdeptGroup);
    }

    // only casts that expend a spell slot: no cantrips, rituals, invocations, items or free casts
    private static bool IsSpellSlotCast(CharacterAction action, string schoolOfMagic, out RulesetEffectSpell activeSpell)
    {
        activeSpell = null;

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
            return false;
        }

        activeSpell = actionCastSpell.ActiveSpell;

        return activeSpell.SchoolOfMagic == schoolOfMagic &&
               activeSpell.SlotLevel > 0 &&
               activeSpell.SpellRepertoire?.SpellCastingFeature.SpellCastingOrigin is
                   FeatureDefinitionCastSpell.CastingOrigin.Class or
                   FeatureDefinitionCastSpell.CastingOrigin.Subclass;
    }

    // same die selection as vanilla RulesetCharacterHero.RollHitDie: the largest die type still available
    internal static DieType ExpendHitDie(RulesetCharacterHero hero)
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

    #region Arcane Omens

    private static FeatDefinition BuildArcaneOmens(List<FeatDefinition> feats)
    {
        const string NAME = "FeatArcaneOmens";
        const string FAMILY = "ArcaneOmens";

        var spellList = SpellListDefinitionBuilder
            .Create($"SpellList{NAME}")
            .SetGuiPresentationNoContent(true)
            .ClearSpells()
            .SetSpellsAtLevel(0, Guidance)
            .FinalizeSpells()
            .AddToDB();

        var powerHelpfulPremonition = FeatureDefinitionPowerBuilder
            .Create($"Power{NAME}HelpfulPremonition")
            .SetGuiPresentation(Category.Feature, hidden: true)
            .SetUsesProficiencyBonus(ActivationTime.NoCost)
            .SetShowCasting(false)
            .AddToDB();

        powerHelpfulPremonition.AddCustomSubFeatures(
            new TryAlterOutcomeSavingThrowHelpfulPremonition(powerHelpfulPremonition));

        var castingAbilities = new List<(string, string)>
        {
            (AttributeDefinitions.Intelligence, "Int"),
            (AttributeDefinitions.Wisdom, "Wis"),
            (AttributeDefinitions.Charisma, "Cha")
        };

        var groupFeats = new List<FeatDefinition>();

        foreach (var (castingAbility, postfix) in castingAbilities)
        {
            // same cantrip only repertoire as vanilla gnome shadow, with the ability chosen with the feat
            var castSpell = FeatureDefinitionCastSpellBuilder
                .Create(DatabaseHelper.FeatureDefinitionCastSpells.CastSpellGnomeShadow, $"CastSpell{NAME}{postfix}")
                .SetGuiPresentation($"CastSpell{NAME}", Category.Feature)
                .SetSpellCastingOrigin(FeatureDefinitionCastSpell.CastingOrigin.Race)
                .SetSpellCastingAbility(castingAbility)
                .SetSpellKnowledge(SpellKnowledge.FixedList)
                .SetKnownCantrips(1, 1, FeatureDefinitionCastSpellBuilder.CasterProgression.Flat)
                .AddCustomSubFeatures(new FeatHelpers.SpellTag(FAMILY))
                .SetSpellList(spellList)
                .AddToDB();

            groupFeats.Add(FeatDefinitionBuilder
                .Create($"{NAME}{postfix}")
                .SetGuiPresentation(Category.Feat)
                .SetFeatures(castSpell, powerHelpfulPremonition)
                .SetFeatFamily(FAMILY)
                .AddToDB());
        }

        feats.AddRange(groupFeats);

        var group = GroupFeats.MakeGroup("FeatGroupArcaneOmens", FAMILY, groupFeats);

        OriginFeatChoices.Add(group.Name);
        OriginFeatChoices.AddRange(groupFeats.Select(x => x.Name));

        return group;
    }

    private sealed class TryAlterOutcomeSavingThrowHelpfulPremonition(
        FeatureDefinitionPower powerHelpfulPremonition) : ITryAlterOutcomeSavingThrow
    {
        private const DieType DieType = RuleDefinitions.DieType.D4;
        private const int RangeInCells = 6;
        private static readonly int MaxDieTypeValue = DiceMaxValue[(int)DieType];

        public IEnumerator OnTryAlterOutcomeSavingThrow(
            GameLocationBattleManager battleManager,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            GameLocationCharacter helper,
            SavingThrowData savingThrowData,
            bool hasHitVisual)
        {
            var rulesetHelper = helper.RulesetCharacter;
            var usablePower = PowerProvider.Get(powerHelpfulPremonition, rulesetHelper);

            // only offered when a d4 can still turn the failure into a success
            if (savingThrowData.SaveOutcome != RollOutcome.Failure ||
                savingThrowData.SaveOutcomeDelta + MaxDieTypeValue < 0 ||
                !helper.CanReact() ||
                helper.IsOppositeSide(defender.Side) ||
                (helper != defender &&
                 (!helper.IsWithinRange(defender, RangeInCells) || !helper.CanPerceiveTarget(defender))) ||
                rulesetHelper.GetRemainingUsesOfPower(usablePower) == 0)
            {
                yield break;
            }

            // any reaction within a saving flow must use the yielder as waiter
            yield return helper.MyReactToSpendPower(
                usablePower,
                helper,
                "ArcaneOmensHelpfulPremonition",
                "SpendPowerArcaneOmensHelpfulPremonitionDescription".Formatted(
                    Category.Reaction, defender.Name, attacker?.Name ?? ReactionRequestCustom.EnvTitle,
                    savingThrowData.Title),
                ReactionValidated,
                battleManager);

            yield break;

            void ReactionValidated()
            {
                helper.SpendActionType(ActionDefinitions.ActionType.Reaction);
                usablePower.Consume();

                var dieRoll = rulesetHelper.RollDie(DieType, RollContext.None, false, AdvantageType.None, out _, out _);

                savingThrowData.SaveOutcomeDelta += dieRoll;

                (ConsoleStyleDuplet.ParameterType, string) extra;

                if (savingThrowData.SaveOutcomeDelta >= 0)
                {
                    savingThrowData.SaveOutcome = RollOutcome.Success;
                    extra = (ConsoleStyleDuplet.ParameterType.Positive, "Feedback/&RollCheckSuccessTitle");
                }
                else
                {
                    savingThrowData.SaveOutcome = RollOutcome.Failure;
                    extra = (ConsoleStyleDuplet.ParameterType.Negative, "Feedback/&RollCheckFailureTitle");
                }

                rulesetHelper.LogCharacterUsedPower(
                    powerHelpfulPremonition,
                    "Feedback/&HelpfulPremonitionSavingRoll",
                    extra:
                    [
                        (ConsoleStyleDuplet.ParameterType.Positive, dieRoll.ToString()),
                        extra
                    ]);
            }
        }
    }

    #endregion

    #region Arcane Eloquence

    private static FeatDefinition BuildArcaneEloquence(List<FeatDefinition> feats)
    {
        const string NAME = "FeatArcaneEloquence";
        const string FAMILY = "ArcaneEloquence";

        var spellList = SpellListDefinitionBuilder
            .Create($"SpellList{NAME}")
            .SetGuiPresentationNoContent(true)
            .ClearSpells()
            .SetSpellsAtLevel(0, ViciousMockery)
            .FinalizeSpells()
            .AddToDB();

        // the d4 is always added as it can only help the check
        var abilityCheckAffinitySmoothTalker = FeatureDefinitionAbilityCheckAffinityBuilder
            .Create($"AbilityCheckAffinity{NAME}SmoothTalker")
            .SetGuiPresentation(Category.Feature)
            .BuildAndSetAffinityGroups(CharacterAbilityCheckAffinity.None, DieType.D4, 1,
                AbilityCheckGroupOperation.AddDie,
                (AttributeDefinitions.Charisma, SkillDefinitions.Deception),
                (AttributeDefinitions.Charisma, SkillDefinitions.Intimidation),
                (AttributeDefinitions.Charisma, SkillDefinitions.Persuasion))
            .AddToDB();

        var castingAbilities = new List<(string, string)>
        {
            (AttributeDefinitions.Intelligence, "Int"),
            (AttributeDefinitions.Wisdom, "Wis"),
            (AttributeDefinitions.Charisma, "Cha")
        };

        var groupFeats = new List<FeatDefinition>();

        foreach (var (castingAbility, postfix) in castingAbilities)
        {
            // same cantrip only repertoire as Arcane Omens, with the ability chosen with the feat
            var castSpell = FeatureDefinitionCastSpellBuilder
                .Create(DatabaseHelper.FeatureDefinitionCastSpells.CastSpellGnomeShadow, $"CastSpell{NAME}{postfix}")
                .SetGuiPresentation($"CastSpell{NAME}", Category.Feature)
                .SetSpellCastingOrigin(FeatureDefinitionCastSpell.CastingOrigin.Race)
                .SetSpellCastingAbility(castingAbility)
                .SetSpellKnowledge(SpellKnowledge.FixedList)
                .SetKnownCantrips(1, 1, FeatureDefinitionCastSpellBuilder.CasterProgression.Flat)
                .AddCustomSubFeatures(new FeatHelpers.SpellTag(FAMILY))
                .SetSpellList(spellList)
                .AddToDB();

            groupFeats.Add(FeatDefinitionBuilder
                .Create($"{NAME}{postfix}")
                .SetGuiPresentation(Category.Feat)
                .SetFeatures(castSpell, abilityCheckAffinitySmoothTalker)
                .SetFeatFamily(FAMILY)
                .AddToDB());
        }

        feats.AddRange(groupFeats);

        return GroupFeats.MakeGroup("FeatGroupArcaneEloquence", FAMILY, groupFeats);
    }

    #endregion

    #region Arcane Infiltrator

    private static FeatDefinition BuildArcaneInfiltrator(List<FeatDefinition> feats)
    {
        const string NAME = "FeatArcaneInfiltrator";

        // same Dodge as the vanilla action: lasts until the start of your next turn
        var powerCunningDiversion = FeatureDefinitionPowerBuilder
            .Create($"Power{NAME}CunningDiversion")
            .SetGuiPresentation(Category.Feature,
                Sprites.GetSprite("PowerEleganceDodge", Resources.PowerEleganceDodge, 256, 128))
            .SetUsesProficiencyBonus(ActivationTime.BonusAction)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.Round, 1, TurnOccurenceType.StartOfTurn)
                    .SetTargetingData(Side.Ally, RangeType.Self, 0, TargetType.Self)
                    .SetEffectForms(
                        EffectFormBuilder.ConditionForm(DatabaseHelper.ConditionDefinitions.ConditionDodging))
                    .Build())
            .AddToDB();

        var feat = FeatDefinitionBuilder
            .Create(NAME)
            .SetGuiPresentation(Category.Feat)
            .SetFeatures(powerCunningDiversion)
            .AddToDB();

        feats.Add(feat);

        return feat;
    }

    #endregion

    #region Portal Jumper

    private static FeatDefinition BuildPortalJumper(List<FeatDefinition> feats)
    {
        const string NAME = "FeatPortalJumper";
        const string FAMILY = "PortalJumper";

        var powerPortalStep = FeatureDefinitionPowerBuilder
            .Create($"Power{NAME}PortalStep")
            .SetGuiPresentation(Category.Feature, MistyStep)
            .SetUsesProficiencyBonus(ActivationTime.NoCost)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.Ally, RangeType.Distance, CustomBehaviorPortalStep.MovementCostInCells,
                        TargetType.Position)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetMotionForm(MotionForm.MotionType.TeleportToDestination)
                            .Build())
                    .UseQuickAnimations()
                    .SetParticleEffectParameters(MistyStep)
                    .Build())
            .AddToDB();

        powerPortalStep.AddCustomSubFeatures(
            new ValidatorsValidatePowerUse(CustomBehaviorPortalStep.CanUsePortalStep),
            new CustomBehaviorPortalStep());

        var damageResistances = new List<(FeatureDefinition, string)>
        {
            (DatabaseHelper.FeatureDefinitionDamageAffinitys.DamageAffinityNecroticResistance, "Necrotic"),
            (DatabaseHelper.FeatureDefinitionDamageAffinitys.DamageAffinityPsychicResistance, "Psychic"),
            (DatabaseHelper.FeatureDefinitionDamageAffinitys.DamageAffinityRadiantResistance, "Radiant")
        };

        var groupFeats = new List<FeatDefinition>();

        foreach (var (damageResistance, postfix) in damageResistances)
        {
            groupFeats.Add(FeatDefinitionBuilder
                .Create($"{NAME}{postfix}")
                .SetGuiPresentation(Category.Feat)
                .SetFeatures(damageResistance, powerPortalStep)
                .SetFeatFamily(FAMILY)
                .AddToDB());
        }

        feats.AddRange(groupFeats);

        var group = GroupFeats.MakeGroup("FeatGroupPortalJumper", FAMILY, groupFeats);

        OriginFeatChoices.Add(group.Name);
        OriginFeatChoices.AddRange(groupFeats.Select(x => x.Name));

        return group;
    }

    private sealed class CustomBehaviorPortalStep : IPowerOrSpellInitiatedByMe
    {
        // 15 feet of movement
        internal const int MovementCostInCells = 3;
        private const string PortalStep = "PortalStep";

        // in combat only on your turn, once per turn and with 15 feet of movement left
        internal static bool CanUsePortalStep(RulesetCharacter character)
        {
            if (Gui.Battle == null)
            {
                return true;
            }

            var locationCharacter = GameLocationCharacter.GetFromActor(character);

            return locationCharacter != null &&
                   locationCharacter.OnceInMyTurnIsValid(PortalStep) &&
                   locationCharacter.RemainingTacticalMoves >= MovementCostInCells;
        }

        public IEnumerator OnPowerOrSpellInitiatedByMe(CharacterActionMagicEffect action, BaseDefinition baseDefinition)
        {
            var actingCharacter = action.ActingCharacter;

            if (Gui.Battle == null)
            {
                yield break;
            }

            actingCharacter.UsedSpecialFeatures.TryAdd(PortalStep, 1);
            actingCharacter.UsedTacticalMoves += MovementCostInCells;
            actingCharacter.UsedTacticalMovesChanged?.Invoke(actingCharacter);
        }
    }

    #endregion

    #region Spell Resistant

    private static FeatDefinition BuildSpellResistant(List<FeatDefinition> feats)
    {
        const string NAME = "FeatSpellResistant";
        const string FAMILY = "SpellResistant";

        var powerMagicResistant = FeatureDefinitionPowerBuilder
            .Create($"Power{NAME}MagicResistant")
            .SetGuiPresentation(Category.Feature, hidden: true)
            .SetUsesProficiencyBonus(ActivationTime.NoCost)
            .SetShowCasting(false)
            .AddToDB();

        powerMagicResistant.AddCustomSubFeatures(
            new TryAlterOutcomeSavingThrowMagicResistant(powerMagicResistant));

        var attributeIncreases = new List<(FeatureDefinition, string)>
        {
            (AttributeModifierCreed_Of_Misaye, "Dex"),
            (AttributeModifierCreed_Of_Arun, "Con")
        };

        var damageResistances = new List<(FeatureDefinition, string)>
        {
            (DatabaseHelper.FeatureDefinitionDamageAffinitys.DamageAffinityNecroticResistance, "Necrotic"),
            (DatabaseHelper.FeatureDefinitionDamageAffinitys.DamageAffinityPsychicResistance, "Psychic"),
            (DatabaseHelper.FeatureDefinitionDamageAffinitys.DamageAffinityRadiantResistance, "Radiant"),
            (DatabaseHelper.FeatureDefinitionDamageAffinitys.DamageAffinityThunderResistance, "Thunder")
        };

        var groupFeats = new List<FeatDefinition>();

        foreach (var (attributeIncrease, attributePostfix) in attributeIncreases)
        {
            foreach (var (damageResistance, damagePostfix) in damageResistances)
            {
                groupFeats.Add(FeatDefinitionWithPrerequisitesBuilder
                    .Create($"{NAME}{attributePostfix}{damagePostfix}")
                    .SetGuiPresentation(Category.Feat)
                    .SetFeatures(attributeIncrease, damageResistance, powerMagicResistant)
                    .SetValidators(ValidatorsFeat.IsLevel4)
                    .SetFeatFamily(FAMILY)
                    .AddToDB());
            }
        }

        feats.AddRange(groupFeats);

        return GroupFeats.MakeGroupWithPreRequisite(
            "FeatGroupSpellResistant", FAMILY, ValidatorsFeat.IsLevel4, [.. groupFeats]);
    }

    private sealed class TryAlterOutcomeSavingThrowMagicResistant(
        FeatureDefinitionPower powerMagicResistant) : ITryAlterOutcomeSavingThrow
    {
        private const DieType DieType = RuleDefinitions.DieType.D6;
        private static readonly int MaxDieTypeValue = DiceMaxValue[(int)DieType];

        // spells (including the ones cast from scrolls or items) and powers granted by magic items
        private static bool IsSpellOrMagicalEffect(SavingThrowData savingThrowData)
        {
            return savingThrowData.SourceDefinition is SpellDefinition ||
                   savingThrowData.Action is CharacterActionCastSpell ||
                   savingThrowData.Action?.ActionParams.RulesetEffect is
                       RulesetEffectSpell or RulesetEffectPower { OriginItem: not null };
        }

        public IEnumerator OnTryAlterOutcomeSavingThrow(
            GameLocationBattleManager battleManager,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            GameLocationCharacter helper,
            SavingThrowData savingThrowData,
            bool hasHitVisual)
        {
            var rulesetHelper = helper.RulesetCharacter;
            var usablePower = PowerProvider.Get(powerMagicResistant, rulesetHelper);

            // only offered when a d6 can still turn the failure into a success
            if (savingThrowData.SaveOutcome != RollOutcome.Failure ||
                savingThrowData.SaveOutcomeDelta + MaxDieTypeValue < 0 ||
                helper != defender ||
                !IsSpellOrMagicalEffect(savingThrowData) ||
                rulesetHelper.GetRemainingUsesOfPower(usablePower) == 0)
            {
                yield break;
            }

            // any reaction within a saving flow must use the yielder as waiter
            yield return helper.MyReactToSpendPower(
                usablePower,
                helper,
                "SpellResistantMagicResistant",
                "SpendPowerSpellResistantMagicResistantDescription".Formatted(
                    Category.Reaction, attacker?.Name ?? ReactionRequestCustom.EnvTitle, savingThrowData.Title),
                ReactionValidated,
                battleManager);

            yield break;

            void ReactionValidated()
            {
                usablePower.Consume();

                var dieRoll = rulesetHelper.RollDie(DieType, RollContext.None, false, AdvantageType.None, out _, out _);

                savingThrowData.SaveOutcomeDelta += dieRoll;

                (ConsoleStyleDuplet.ParameterType, string) extra;

                if (savingThrowData.SaveOutcomeDelta >= 0)
                {
                    savingThrowData.SaveOutcome = RollOutcome.Success;
                    extra = (ConsoleStyleDuplet.ParameterType.Positive, "Feedback/&RollCheckSuccessTitle");
                }
                else
                {
                    savingThrowData.SaveOutcome = RollOutcome.Failure;
                    extra = (ConsoleStyleDuplet.ParameterType.Negative, "Feedback/&RollCheckFailureTitle");
                }

                rulesetHelper.LogCharacterUsedPower(
                    powerMagicResistant,
                    "Feedback/&MagicResistantSavingRoll",
                    extra:
                    [
                        (ConsoleStyleDuplet.ParameterType.Positive, dieRoll.ToString()),
                        extra
                    ]);
            }
        }
    }

    #endregion

    #region Transmuted Anatomy

    private static FeatDefinition BuildTransmutedAnatomy(List<FeatDefinition> feats)
    {
        const string NAME = "FeatTransmutedAnatomy";

        var movementAffinityLengthenedStride = FeatureDefinitionMovementAffinityBuilder
            .Create($"MovementAffinity{NAME}LengthenedStride")
            .SetGuiPresentation(Category.Feature)
            .SetBaseSpeedAdditiveModifier(1)
            .AddToDB();

        var featureResilientAnatomy = FeatureDefinitionBuilder
            .Create($"Feature{NAME}ResilientAnatomy")
            .SetGuiPresentation(Category.Feature)
            .AddToDB();

        var powerResilientAnatomy = FeatureDefinitionPowerBuilder
            .Create($"Power{NAME}ResilientAnatomy")
            .SetGuiPresentation(Category.Feature, hidden: true)
            .SetUsesProficiencyBonus(ActivationTime.NoCost)
            .SetShowCasting(false)
            .AddToDB();

        featureResilientAnatomy.AddCustomSubFeatures(
            new RollSavingThrowInitiatedResilientAnatomy(featureResilientAnatomy));

        powerResilientAnatomy.AddCustomSubFeatures(
            new TryAlterOutcomeSavingThrowResilientAnatomy(powerResilientAnatomy));

        var feat = FeatDefinitionBuilder
            .Create(NAME)
            .SetGuiPresentation(Category.Feat)
            .SetFeatures(movementAffinityLengthenedStride, featureResilientAnatomy, powerResilientAnatomy)
            .AddToDB();

        feats.Add(feat);

        return feat;
    }

    private sealed class RollSavingThrowInitiatedResilientAnatomy(
        FeatureDefinition featureResilientAnatomy) : IRollSavingThrowInitiated
    {
        public void OnSavingThrowInitiated(
            RulesetActor rulesetActorCaster,
            RulesetActor rulesetActorDefender,
            ref int saveBonus,
            ref string abilityScoreName,
            BaseDefinition sourceDefinition,
            List<TrendInfo> modifierTrends,
            List<TrendInfo> advantageTrends,
            ref int rollModifier,
            ref int saveDC,
            ref bool hasHitVisual,
            RollOutcome outcome,
            int outcomeDelta,
            List<EffectForm> effectForms)
        {
            // only effects that would shape-shift you against your will
            if (rulesetActorCaster == rulesetActorDefender ||
                effectForms == null ||
                effectForms.All(x => x.FormType != EffectForm.EffectFormType.ShapeChange))
            {
                return;
            }

            advantageTrends.Add(
                new TrendInfo(1, FeatureSourceType.CharacterFeature, featureResilientAnatomy.Name, featureResilientAnatomy));
        }
    }

    private sealed class TryAlterOutcomeSavingThrowResilientAnatomy(
        FeatureDefinitionPower powerResilientAnatomy) : ITryAlterOutcomeSavingThrow
    {
        private const DieType DieType = RuleDefinitions.DieType.D4;
        private static readonly int MaxDieTypeValue = DiceMaxValue[(int)DieType];

        public IEnumerator OnTryAlterOutcomeSavingThrow(
            GameLocationBattleManager battleManager,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            GameLocationCharacter helper,
            SavingThrowData savingThrowData,
            bool hasHitVisual)
        {
            var rulesetHelper = helper.RulesetCharacter;
            var usablePower = PowerProvider.Get(powerResilientAnatomy, rulesetHelper);

            // only offered on your own failed Constitution saves when a d4 can still turn them into a success
            if (savingThrowData.SaveOutcome != RollOutcome.Failure ||
                savingThrowData.SaveOutcomeDelta + MaxDieTypeValue < 0 ||
                helper != defender ||
                savingThrowData.SavingThrowAbility != AttributeDefinitions.Constitution ||
                !helper.CanReact() ||
                rulesetHelper.GetRemainingUsesOfPower(usablePower) == 0)
            {
                yield break;
            }

            // any reaction within a saving flow must use the yielder as waiter
            yield return helper.MyReactToSpendPower(
                usablePower,
                helper,
                "TransmutedAnatomyResilientAnatomy",
                "SpendPowerTransmutedAnatomyResilientAnatomyDescription".Formatted(
                    Category.Reaction, attacker?.Name ?? ReactionRequestCustom.EnvTitle, savingThrowData.Title),
                ReactionValidated,
                battleManager);

            yield break;

            void ReactionValidated()
            {
                helper.SpendActionType(ActionDefinitions.ActionType.Reaction);
                usablePower.Consume();

                var dieRoll = rulesetHelper.RollDie(DieType, RollContext.None, false, AdvantageType.None, out _, out _);

                savingThrowData.SaveOutcomeDelta += dieRoll;

                (ConsoleStyleDuplet.ParameterType, string) extra;

                if (savingThrowData.SaveOutcomeDelta >= 0)
                {
                    savingThrowData.SaveOutcome = RollOutcome.Success;
                    extra = (ConsoleStyleDuplet.ParameterType.Positive, "Feedback/&RollCheckSuccessTitle");
                }
                else
                {
                    savingThrowData.SaveOutcome = RollOutcome.Failure;
                    extra = (ConsoleStyleDuplet.ParameterType.Negative, "Feedback/&RollCheckFailureTitle");
                }

                rulesetHelper.LogCharacterUsedPower(
                    powerResilientAnatomy,
                    "Feedback/&ResilientAnatomySavingRoll",
                    extra:
                    [
                        (ConsoleStyleDuplet.ParameterType.Positive, dieRoll.ToString()),
                        extra
                    ]);
            }
        }
    }

    #endregion

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
                AutoPreparedSpellsUpToMaxSpellSlotLevel.Marker,
                AutoPreparedSpellsScribedInSpellbook.Marker)
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
            if (!IsSpellSlotCast(action, SchoolAbjuration, out var activeSpell))
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
                // one prompt listing you and every eligible ally
                yield return attacker.MyReactToSelectOption(
                    attacker,
                    ProtectiveWard,
                    "CustomReactionProtectiveWardDescription".Formatted(
                        Category.Reaction, tempHitPoints.ToString()),
                    candidates
                        .Select(x => (x == attacker
                            ? "CustomReactionProtectiveWardSelf".Formatted(Category.Reaction, x.Name)
                            : x.Name, true))
                        .ToList(),
                    option => selectedTarget = candidates[option]);
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
                AutoPreparedSpellsUpToMaxSpellSlotLevel.Marker,
                AutoPreparedSpellsScribedInSpellbook.Marker)
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
                AutoPreparedSpellsUpToMaxSpellSlotLevel.Marker,
                AutoPreparedSpellsScribedInSpellbook.Marker)
            .SetPreparedSpellGroups(
                BuildSpellGroup(0,
                    SpellsContext.ChromaticOrb,
                    Shatter,
                    Fireball,
                    SpellsContext.VitriolicSphere,
                    WallOfForce))
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

            // the number of dice is chosen before rolling, all in one prompt where the option index is the dice count
            yield return attacker.MyReactToSelectOption(
                attacker,
                FueledEvocation,
                "CustomReactionFueledEvocationDescription".Formatted(
                    Category.Reaction, remainingHitDice.ToString()),
                [
                    (Gui.Localize("Reaction/&CustomReactionFueledEvocationZeroDice"), true),
                    (Gui.Localize("Reaction/&CustomReactionFueledEvocationOneDie"), true),
                    (Gui.Localize("Reaction/&CustomReactionFueledEvocationTwoDice"), remainingHitDice >= 2)
                ],
                option => hitDiceToSpend = option,
                battleManager: battleManager);

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
    }

    #endregion

    #region Necromancy Adept

    private static FeatDefinition BuildNecromancyAdept(List<FeatDefinition> feats)
    {
        const string NAME = "FeatNecromancyAdept";
        const string FAMILY = "NecromancyAdept";

        // spells are only added to the repertoire once the caster has spell slots of the spell level
        var autoPreparedSpells = FeatureDefinitionAutoPreparedSpellsBuilder
            .Create($"AutoPreparedSpells{NAME}")
            .SetGuiPresentationNoContent(true)
            .AddCustomSubFeatures(
                ValidateRepertoireForAutoprepared.AnyClassOrSubclass,
                AutoPreparedSpellsUpToMaxSpellSlotLevel.Marker,
                AutoPreparedSpellsScribedInSpellbook.Marker)
            .SetPreparedSpellGroups(
                BuildSpellGroup(0,
                    InflictWounds,
                    RayOfEnfeeblement,
                    VampiricTouch,
                    Blight,
                    RaiseDead))
            .SetSpellcastingClass(null)
            .SetAutoTag(FAMILY)
            .AddToDB();

        var featureLifeManipulation = FeatureDefinitionBuilder
            .Create($"Feature{NAME}LifeManipulation")
            .SetGuiPresentation(Category.Feature)
            .AddToDB();

        featureLifeManipulation.AddCustomSubFeatures(
            new MagicEffectFinishedByMeLifeManipulation(featureLifeManipulation));

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
                .SetFeatures(attributeIncrease, autoPreparedSpells, featureLifeManipulation)
                .SetValidators(ValidatorsFeat.IsLevel4)
                .SetMustCastSpellsPrerequisite()
                .SetFeatFamily(FAMILY)
                .AddToDB());
        }

        feats.AddRange(groupFeats);

        var necromancyAdeptGroup = GroupFeats.MakeGroupWithPreRequisite(
            "FeatGroupNecromancyAdept", FAMILY, ValidatorsFeat.IsLevel4, [.. groupFeats]);

        necromancyAdeptGroup.mustCastSpellsPrerequisite = !Main.Settings.DisableCastSpellPreRequisitesOnModFeats;

        return necromancyAdeptGroup;
    }

    private sealed class MagicEffectFinishedByMeLifeManipulation(
        // ReSharper disable once SuggestBaseTypeForParameterInConstructor
        FeatureDefinition featureLifeManipulation) : IMagicEffectFinishedByMe
    {
        private const string LifeManipulation = "LifeManipulation";

        public IEnumerator OnMagicEffectFinishedByMe(
            CharacterAction action,
            GameLocationCharacter attacker,
            List<GameLocationCharacter> targets)
        {
            if (!IsSpellSlotCast(action, SchoolNecromancy, out var activeSpell))
            {
                yield break;
            }

            var rulesetAttacker = attacker.RulesetCharacter;

            if (rulesetAttacker is not { IsDeadOrDyingOrUnconscious: false })
            {
                yield break;
            }

            var rulesetHero = rulesetAttacker.GetOriginalHero();

            if (rulesetHero == null)
            {
                yield break;
            }

            var remainingHitDice = rulesetHero.RemainingHitDiceCount();

            if (remainingHitDice <= 0)
            {
                yield break;
            }

            var slotLevel = activeSpell.SlotLevel;
            var hitDiceToSpend = 0;

            // the number of dice is chosen before rolling, all in one prompt where the option index is the dice count
            yield return attacker.MyReactToSelectOption(
                attacker,
                LifeManipulation,
                "CustomReactionLifeManipulationDescription".Formatted(
                    Category.Reaction, remainingHitDice.ToString(), slotLevel.ToString()),
                [
                    (Gui.Localize("Reaction/&CustomReactionLifeManipulationZeroDice"), true),
                    (Gui.Localize("Reaction/&CustomReactionLifeManipulationOneDie"), true),
                    (Gui.Localize("Reaction/&CustomReactionLifeManipulationTwoDice"), remainingHitDice >= 2)
                ],
                option => hitDiceToSpend = option);

            if (hitDiceToSpend == 0)
            {
                yield break;
            }

            var rolls = new List<string>();
            var total = 0;

            for (var i = 0; i < hitDiceToSpend; i++)
            {
                var dieType = ExpendHitDie(rulesetHero);

                if (dieType == DieType.D1)
                {
                    break;
                }

                var roll = RollDie(dieType, AdvantageType.None, out _, out _);

                rulesetAttacker.ShowDieRoll(dieType, roll, title: featureLifeManipulation.GuiPresentation.Title);
                rolls.Add($"{Gui.FormatDieTitle(dieType)}:{roll}");
                total += roll;
            }

            if (rolls.Count == 0)
            {
                yield break;
            }

            var healing = total + slotLevel;

            rulesetAttacker.LogCharacterActivatesAbility(
                featureLifeManipulation.GuiPresentation.Title,
                "Feedback/&LifeManipulationHitDiceRolled",
                extra:
                [
                    (ConsoleStyleDuplet.ParameterType.AbilityInfo, string.Join(", ", rolls)),
                    (ConsoleStyleDuplet.ParameterType.Positive, slotLevel.ToString()),
                    (ConsoleStyleDuplet.ParameterType.Positive, healing.ToString())
                ]);

            EffectHelpers.StartVisualEffect(attacker, attacker, CureWounds, EffectHelpers.EffectType.Effect);
            rulesetAttacker.ReceiveHealing(healing, true, rulesetAttacker.Guid);
        }
    }

    #endregion

    #region Magic Connoisseur

    internal const string FeatMagicConnoisseurTag = "Connoisseur";

    // class groups nested in the Magic Connoisseur group, so they don't get their own toggle in mod settings
    internal static readonly HashSet<string> NestedFeatGroups = [];

    private static FeatDefinition BuildMagicConnoisseur(List<FeatDefinition> feats)
    {
        const string NAME = "FeatMagicConnoisseur";
        const string FAMILY = "MagicConnoisseur";
        // each pool tag must end with the spell tag so level up finds this repertoire for both pools
        const string LEVEL1_POOL_TAG = $"Level1{FeatMagicConnoisseurTag}";
        const string LEVEL2_POOL_TAG = $"Level2{FeatMagicConnoisseurTag}";

        // a 1st and a 2nd level slot recharged on a long rest to cast each spell once without a spell slot
        var castingSlots = Enumerable.Range(1, 20)
            .Select(level => new FeatureDefinitionCastSpell.SlotsByLevelDuplet { Slots = [1, 1], Level = level })
            .ToList();

        // same classes as Magic Initiate, as the spells come from the same list and use the same ability
        var castSpells = new List<(FeatureDefinitionCastSpell feature, ClassHolder clazz)>
        {
            (DatabaseHelper.FeatureDefinitionCastSpells.CastSpellBard, ClassHolder.Bard),
            (DatabaseHelper.FeatureDefinitionCastSpells.CastSpellCleric, ClassHolder.Cleric),
            (DatabaseHelper.FeatureDefinitionCastSpells.CastSpellDruid, ClassHolder.Druid),
            (DatabaseHelper.FeatureDefinitionCastSpells.CastSpellSorcerer, ClassHolder.Sorcerer),
            (DatabaseHelper.FeatureDefinitionCastSpells.CastSpellWarlock, ClassHolder.Warlock),
            (DatabaseHelper.FeatureDefinitionCastSpells.CastSpellWizard, ClassHolder.Wizard)
        };

        var attributeIncreases = new List<(FeatureDefinition, string)>
        {
            (AttributeModifierCreed_Of_Pakri, "Int"),
            (AttributeModifierCreed_Of_Maraike, "Wis"),
            (AttributeModifierCreed_Of_Solasta, "Cha")
        };

        var classGroups = new List<FeatDefinition>();
        var magicInitiateNames = new List<string>();

        foreach (var (classCastSpell, clazz) in castSpells)
        {
            var spellList = classCastSpell.SpellListDefinition;
            var className = spellList.Name.Replace("SpellList", "");
            var classTitle = DatabaseHelper.GetDefinition<CharacterClassDefinition>(className).FormatTitle();
            // Magic Initiate feats are created later on so they are validated by name
            var magicInitiateName = $"FeatMagicInitiate{className}";
            var validateMagicInitiate = ValidateHasMagicInitiate(magicInitiateName);

            magicInitiateNames.Add(magicInitiateName);

            // same repertoire setup as Magic Initiate, so it also uses the class spellcasting ability
            var castSpell = FeatureDefinitionCastSpellBuilder
                .Create(classCastSpell, $"CastSpell{NAME}{className}")
                .SetGuiPresentation(
                    Gui.Format($"Feature/&CastSpell{NAME}Title", classTitle),
                    Gui.Format($"Feature/&CastSpell{NAME}Description", classTitle))
                .SetSpellCastingOrigin(FeatureDefinitionCastSpell.CastingOrigin.Race)
                .SetSpellKnowledge(SpellKnowledge.Selection)
                .SetSpellReadyness(SpellReadyness.AllKnown)
                .SetSlotsRecharge(RechargeRate.LongRest)
                .SetSlotsPerLevel(castingSlots)
                .SetKnownCantrips(0, 1, FeatureDefinitionCastSpellBuilder.CasterProgression.Flat)
                .SetKnownSpells(2, FeatureDefinitionCastSpellBuilder.CasterProgression.Flat)
                .SetReplacedSpells(1, 0)
                .SetUniqueLevelSlots(false)
                .AddCustomSubFeatures(
                    new FeatHelpers.SpellTag(FeatMagicConnoisseurTag, false, LEVEL1_POOL_TAG, LEVEL2_POOL_TAG),
                    clazz)
                .AddToDB();

            var pointPoolLevel1 = FeatureDefinitionPointPoolBuilder
                .Create($"PointPool{NAME}{className}Level1")
                .SetGuiPresentationNoContent(true)
                .SetSpellOrCantripPool(HeroDefinitions.PointsPoolType.Spell, 1, spellList, LEVEL1_POOL_TAG, 1, 1)
                .AddToDB();

            var pointPoolLevel2 = FeatureDefinitionPointPoolBuilder
                .Create($"PointPool{NAME}{className}Level2")
                .SetGuiPresentationNoContent(true)
                .SetSpellOrCantripPool(HeroDefinitions.PointsPoolType.Spell, 1, spellList, LEVEL2_POOL_TAG, 2, 2)
                .AddToDB();

            // the spells are also prepared on class repertoires so they can be cast with any spell slots
            var autoPreparedSpells = FeatureDefinitionAutoPreparedSpellsBuilder
                .Create($"AutoPreparedSpells{NAME}{className}")
                .SetGuiPresentationNoContent(true)
                .AddCustomSubFeatures(
                    ValidateRepertoireForAutoprepared.AnyClassOrSubclass,
                    AutoPreparedSpellsUpToMaxSpellSlotLevel.Marker,
                    new AutoPreparedSpellsFromFeatRepertoire(castSpell))
                .SetSpellcastingClass(null)
                .SetAutoTag(FAMILY)
                .AddToDB();

            var groupFeats = new List<FeatDefinition>();

            foreach (var (attributeIncrease, postfix) in attributeIncreases)
            {
                groupFeats.Add(FeatDefinitionWithPrerequisitesBuilder
                    .Create($"{NAME}{className}{postfix}")
                    .SetGuiPresentation(
                        Gui.Format($"Feat/&{NAME}{postfix}Title", classTitle),
                        Gui.Format($"Feat/&{NAME}{postfix}Description", classTitle))
                    .SetFeatures(attributeIncrease, castSpell, pointPoolLevel1, pointPoolLevel2, autoPreparedSpells)
                    .SetValidators(ValidatorsFeat.IsLevel4, validateMagicInitiate)
                    .SetFeatFamily(FAMILY)
                    .AddToDB());
            }

            feats.AddRange(groupFeats);

            var classGroup = (FeatDefinitionWithPrerequisites)GroupFeats.MakeGroupWithPreRequisite(
                $"FeatGroup{FAMILY}{className}", FAMILY, ValidatorsFeat.IsLevel4, [.. groupFeats]);

            classGroup.Validators.Add(validateMagicInitiate);
            classGroup.GuiPresentation.Title = Gui.Format($"Feat/&FeatGroup{FAMILY}ClassTitle", classTitle);
            classGroup.GuiPresentation.Description = Gui.Format($"Feat/&FeatGroup{FAMILY}ClassDescription", classTitle);

            NestedFeatGroups.Add(classGroup.Name);
            classGroups.Add(classGroup);
        }

        var magicConnoisseurGroup = (FeatDefinitionWithPrerequisites)GroupFeats.MakeGroupWithPreRequisite(
            $"FeatGroup{FAMILY}", FAMILY, ValidatorsFeat.IsLevel4, [.. classGroups]);

        magicConnoisseurGroup.Validators.Add(ValidateHasMagicInitiate([.. magicInitiateNames]));

        return magicConnoisseurGroup;
    }

    // Magic Initiate can also be granted by a background, so check the feat and the repertoire it grants
    private static Func<FeatDefinition, RulesetCharacterHero, (bool result, string output)> ValidateHasMagicInitiate(
        params string[] featNames)
    {
        return (_, hero) =>
        {
            var title = featNames.Length == 1
                ? DatabaseHelper.GetDefinition<FeatDefinition>(featNames[0]).FormatTitle()
                : Gui.Localize("Feat/&FeatGroupMagicInitiateTitle");
            var guiFormat = Gui.Format("Tooltip/&PreReqMustKnow", title);
            var hasMagicInitiate = featNames.Any(featName =>
                hero.TrainedFeats.Any(feat => feat.Name == featName) ||
                hero.SpellRepertoires.Any(repertoire =>
                    repertoire.SpellCastingFeature.Name == $"CastSpell{featName}"));

            return hasMagicInitiate ? (true, guiFormat) : (false, Gui.Colorize(guiFormat, Gui.ColorFailure));
        };
    }

    #endregion

    #region Rune Shaper

    private static FeatDefinition BuildRuneShaper(List<FeatDefinition> feats)
    {
        const string NAME = "FeatRuneShaper";
        const string FAMILY = "RuneShaper";

        // the Enemy (Disguise Self) and Friend (Speak with Animals) runes are left out as neither spell is in the game
        var runes = new List<(string, SpellDefinition)>
        {
            ("Cloud", FogCloud),
            ("Death", InflictWounds),
            ("Dragon", SpellsContext.ChromaticOrb),
            ("Fire", BurningHands),
            ("Frost", SpellsContext.SkinOfRetribution),
            ("Hill", Goodberry),
            ("Journey", Longstrider),
            ("King", SpellsContext.Command),
            ("Mountain", Entangle),
            ("Stone", SpellsContext.Sanctuary),
            ("Storm", Thunderwave)
        };

        var castingAbilities = new List<(string, string)>
        {
            (AttributeDefinitions.Intelligence, "Int"),
            (AttributeDefinitions.Wisdom, "Wis"),
            (AttributeDefinitions.Charisma, "Cha")
        };

        var castSpellNames = castingAbilities
            .Select(x => $"CastSpell{NAME}{x.Item2}")
            .ToArray();

        // the feat spells are cast from the empty feat repertoire so they use the ability chosen with the feat
        RepertoireValidForAutoPreparedFeature isFeatRepertoire = (repertoire, _) =>
            castSpellNames.Contains(repertoire.spellCastingFeature.Name);

        // each rune is cast once per long rest without a spell slot or material components
        foreach (var (rune, spell) in runes)
        {
            _ = CustomInvocationDefinitionBuilder
                .Create($"CustomInvocation{FAMILY}{rune}")
                .SetGuiPresentation(Category.Feature, spell)
                .AddCustomSubFeatures(isFeatRepertoire)
                .SetPoolType(InvocationPoolTypeCustom.Pools.RuneShaper)
                .SetGrantedSpell(spell, longRestRecharge: true)
                .AddToDB();
        }

        var invocationComprehendLanguages = CustomInvocationDefinitionBuilder
            .Create($"CustomInvocation{FAMILY}{ComprehendLanguages.Name}")
            .SetGuiPresentation(ComprehendLanguages.GuiPresentation)
            .AddCustomSubFeatures(isFeatRepertoire)
            .SetPoolType(InvocationPoolTypeCustom.Pools.PlaneMagic)
            .SetGrantedSpell(ComprehendLanguages, longRestRecharge: true)
            .AddToDB();

        var grantInvocations = FeatureDefinitionGrantInvocationsBuilder
            .Create($"GrantInvocations{NAME}")
            .SetGuiPresentationNoContent(true)
            .SetInvocations(invocationComprehendLanguages)
            .AddToDB();

        // comprehend languages and the rune spells are also prepared on class repertoires to be cast with spell slots
        var autoPreparedSpells = FeatureDefinitionAutoPreparedSpellsBuilder
            .Create($"AutoPreparedSpells{NAME}")
            .SetGuiPresentationNoContent(true)
            .AddCustomSubFeatures(
                ValidateRepertoireForAutoprepared.AnyClassOrSubclass,
                AutoPreparedSpellsUpToMaxSpellSlotLevel.Marker,
                new AutoPreparedSpellsFromInvocationPool(InvocationPoolTypeCustom.Pools.RuneShaper))
            .SetPreparedSpellGroups(BuildSpellGroup(0, ComprehendLanguages))
            .SetSpellcastingClass(null)
            .SetAutoTag(FAMILY)
            .AddToDB();

        // runes known are half the proficiency bonus rounded down, and one can be replaced on each level up
        var learnPools = Enumerable.Range(1, 3)
            .Select(points => CustomInvocationPoolDefinitionBuilder
                .Create($"InvocationPool{NAME}Learn{points}")
                .SetGuiPresentationNoContent(true)
                .Setup(InvocationPoolTypeCustom.Pools.RuneShaper, points)
                .AddToDB())
            .ToArray();

        var unlearnPool = CustomInvocationPoolDefinitionBuilder
            .Create($"InvocationPool{NAME}Unlearn")
            .SetGuiPresentationNoContent(true)
            .Setup(InvocationPoolTypeCustom.Pools.RuneShaper, 1, true)
            .AddToDB();

        var poolsByCharacterLevel = new CustomInvocationPoolsByCharacterLevel(
            characterLevel => (2 + ((characterLevel - 1) / 4)) / 2, learnPools, unlearnPool);

        var groupFeats = new List<FeatDefinition>();

        foreach (var (castingAbility, postfix) in castingAbilities)
        {
            var castSpell = FeatureDefinitionCastSpellBuilder
                .Create($"CastSpell{NAME}{postfix}")
                .SetGuiPresentationNoContent(true)
                .SetFocusType(EquipmentDefinitions.FocusType.None)
                .SetSpellCastingOrigin(FeatureDefinitionCastSpell.CastingOrigin.Race)
                .SetSpellReadyness(SpellReadyness.AllKnown)
                .SetSlotsRecharge(RechargeRate.None)
                .SetSlotsPerLevel(FeatureDefinitionCastSpellBuilder.CasterProgression.None)
                .SetSpellList(SpellsContext.EmptySpellList)
                .SetSpellCastingAbility(castingAbility)
                .AddToDB();

            groupFeats.Add(FeatDefinitionBuilder
                .Create($"{NAME}{postfix}")
                .SetGuiPresentation(Category.Feat)
                .SetFeatures(castSpell, grantInvocations, autoPreparedSpells)
                .AddCustomSubFeatures(poolsByCharacterLevel)
                .SetMustCastSpellsPrerequisite()
                .SetFeatFamily(FAMILY)
                .AddToDB());
        }

        feats.AddRange(groupFeats);

        var runeShaperGroup = GroupFeats.MakeGroup("FeatGroupRuneShaper", FAMILY, groupFeats);

        runeShaperGroup.mustCastSpellsPrerequisite = !Main.Settings.DisableCastSpellPreRequisitesOnModFeats;

        return runeShaperGroup;
    }

    #endregion
}
