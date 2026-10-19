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
public sealed class DomainArcana : AbstractSubclass
{
    internal const string Name = "DomainArcana";

    private const string MagicalMasteryTag = "ArcanaMastery";

    private static readonly int[] MagicalMasterySpellLevels = [6, 7, 8, 9];

    // Modify Magic changes a spell in only one way, so a cast that used Tenacious Spell can't be fortified
    private static readonly HashSet<CharacterAction> ModifiedSpellCasts = [];

    public DomainArcana()
    {
        var divinePowerPrefix = Gui.Localize("Feature/&ClericChannelDivinityTitle") + ": ";

        // LEVEL 01 - Arcana Domain Spells

        // Nystul's Magic Aura -> See Invisibility, Arcane Eye -> Identify Creatures,
        // Bigby's Hand -> Telekinesis, Teleportation Circle -> Far Step, Leomund's Secret Chest removed
        var autoPreparedSpellsDomainArcana = FeatureDefinitionAutoPreparedSpellsBuilder
            .Create($"AutoPreparedSpells{Name}")
            .SetGuiPresentation("ExpandedSpells", Category.Feature)
            .SetAutoTag("Domain")
            .SetPreparedSpellGroups(
                BuildSpellGroup(1, DetectMagic, MagicMissile),
                BuildSpellGroup(3, MagicWeapon, SeeInvisibility),
                BuildSpellGroup(5, Counterspell, DispelMagic),
                BuildSpellGroup(7, IdentifyCreatures),
                BuildSpellGroup(9, SpellsContext.Telekinesis, SpellsContext.FarStep))
            .SetSpellcastingClass(Cleric)
            .AddToDB();

        // LEVEL 01 - Student of Arcana

        var pointPoolSkills = FeatureDefinitionPointPoolBuilder
            .Create($"PointPool{Name}Skills")
            .SetGuiPresentationNoContent(true)
            .SetPool(HeroDefinitions.PointsPoolType.Skill, 1)
            .RestrictChoices(
                [
                    SkillDefinitions.Arcana,
                    .. FeatureDefinitionPointPools.PointPoolClericSkillPoints.RestrictedChoices
                        .Where(x => x != SkillDefinitions.Arcana)
                ])
            .AddToDB();

        var pointPoolCantrips = FeatureDefinitionPointPoolBuilder
            .Create($"PointPool{Name}Cantrips")
            .SetGuiPresentationNoContent(true)
            .SetSpellOrCantripPool(
                HeroDefinitions.PointsPoolType.Cantrip, 2, SpellListDefinitions.SpellListWizard, Name)
            .AddToDB();

        // cantrips are granted on CharacterBuildingManager.FinalizeCharacter as with Domain Nature
        var featureSetStudentOfArcana = FeatureDefinitionFeatureSetBuilder
            .Create($"FeatureSet{Name}StudentOfArcana")
            .SetGuiPresentation(Category.Feature)
            .AddFeatureSet(pointPoolSkills, pointPoolCantrips)
            .AddToDB();

        // LEVEL 02 - Channel Divinity: Modify Magic

        var powerModifyMagic = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}ModifyMagic")
            .SetGuiPresentation(Category.Feature)
            .SetUsesFixed(ActivationTime.NoCost, RechargeRate.ChannelDivinity)
            .SetShowCasting(false)
            .AddToDB();

        powerModifyMagic.AddCustomSubFeatures(
            ModifyPowerVisibility.Hidden,
            new CustomBehaviorModifyMagic(powerModifyMagic));

        var featureSetModifyMagic = FeatureDefinitionFeatureSetBuilder
            .Create($"FeatureSet{Name}ModifyMagic")
            .SetGuiPresentation(
                divinePowerPrefix + powerModifyMagic.FormatTitle(), powerModifyMagic.FormatDescription())
            .AddFeatureSet(powerModifyMagic)
            .AddToDB();

        // LEVEL 06 - Dispelling Recovery

        var powerDispellingRecovery = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}DispellingRecovery")
            .SetGuiPresentation(Category.Feature)
            .SetUsesFixed(ActivationTime.NoCost, RechargeRate.ShortRest)
            .SetShowCasting(false)
            .AddToDB();

        powerDispellingRecovery.AddCustomSubFeatures(
            ModifyPowerVisibility.Hidden,
            new MagicEffectFinishedByMeDispellingRecovery(powerDispellingRecovery, powerModifyMagic));

        // LEVEL 08 - Potent Spellcasting

        // swapped by Blessed Strikes when that 2024 option is enabled
        var featurePotentSpellcasting = FeatureDefinitionBuilder
            .Create($"Feature{Name}PotentSpellcasting")
            .SetGuiPresentation(Category.Feature)
            .AddToDB();

        featurePotentSpellcasting.AddCustomSubFeatures(
            new ClassFeats.CustomBehaviorFeatPotentSpellcaster(featurePotentSpellcasting, Cleric));

        // LEVEL 17 - Magical Mastery

        var pointPoolsMagicalMastery = MagicalMasterySpellLevels
            .Select(level => FeatureDefinitionPointPoolBuilder
                .Create($"PointPool{Name}MagicalMastery{level}")
                .SetGuiPresentationNoContent(true)
                .SetSpellOrCantripPool(
                    HeroDefinitions.PointsPoolType.Spell, 1, SpellListDefinitions.SpellListWizard,
                    $"{MagicalMasteryTag}{level}", level, level)
                .AddToDB())
            .OfType<FeatureDefinition>()
            .ToArray();

        // spells are granted on CharacterBuildingManager.FinalizeCharacter
        var featureSetMagicalMastery = FeatureDefinitionFeatureSetBuilder
            .Create($"FeatureSet{Name}MagicalMastery")
            .SetGuiPresentation(Category.Feature)
            .AddFeatureSet(pointPoolsMagicalMastery)
            .AddToDB();

        // MAIN

        Subclass = CharacterSubclassDefinitionBuilder
            .Create(Name)
            .SetGuiPresentation(Category.Subclass, CharacterSubclassDefinitions.DomainInsight)
            .AddFeaturesAtLevel(1, autoPreparedSpellsDomainArcana, featureSetStudentOfArcana)
            .AddFeaturesAtLevel(2, featureSetModifyMagic)
            .AddFeaturesAtLevel(6, powerDispellingRecovery)
            .AddFeaturesAtLevel(8, featurePotentSpellcasting)
            .AddFeaturesAtLevel(10, PowerClericDivineInterventionWizard)
            .AddFeaturesAtLevel(17, featureSetMagicalMastery)
            .AddFeaturesAtLevel(20, Level20SubclassesContext.PowerClericDivineInterventionImprovementWizard)
            .AddToDB();
    }

    internal override CharacterClassDefinition Klass => Cleric;

    internal override CharacterSubclassDefinition Subclass { get; }

    // ReSharper disable once UnassignedGetOnlyAutoProperty
    internal override FeatureDefinitionSubclassChoice SubclassChoice { get; }

    internal override DeityDefinition DeityDefinition => DeityDefinitions.Pakri;

    // spell pools with custom tags aren't granted by vanilla, so add them as extra spells always prepared
    internal static void GrantMagicalMasterySpells(RulesetCharacterHero hero)
    {
        var selectedClass = LevelUpHelper.GetSelectedClass(hero);

        if (selectedClass != Cleric)
        {
            return;
        }

        var selectedSubclass = LevelUpHelper.GetSelectedSubclass(hero);

        if (!selectedSubclass || selectedSubclass.Name != Name)
        {
            return;
        }

        var spellRepertoire = hero.GetClassSpellRepertoire(Cleric);

        if (spellRepertoire == null)
        {
            return;
        }

        var heroBuildingData = hero.GetHeroBuildingData();
        var classLevel = LevelUpHelper.GetSelectedClassLevel(hero);
        var classTag = AttributeDefinitions.GetClassTag(selectedClass, classLevel);
        var subclassTag = AttributeDefinitions.GetSubclassTag(selectedClass, classLevel, selectedSubclass);

        foreach (var tag in MagicalMasterySpellLevels.Select(level => $"{MagicalMasteryTag}{level}"))
        {
            foreach (var poolName in new[] { $"{classTag}{tag}", $"{subclassTag}{tag}" })
            {
                if (!heroBuildingData.AcquiredSpells.TryGetValue(poolName, out var spells))
                {
                    continue;
                }

                foreach (var spell in spells.Where(x => x))
                {
                    hero.GrantSpell(spell, spellRepertoire.SpellCastingFeature, tag);
                }
            }
        }
    }

    private static bool IsSpellCast(CharacterAction action, out CharacterActionCastSpell actionCastSpell)
    {
        actionCastSpell = action as CharacterActionCastSpell;

        return actionCastSpell is { Countered: false, ExecutionFailed: false };
    }

    //
    // Modify Magic
    //

    private sealed class CustomBehaviorModifyMagic(FeatureDefinitionPower powerModifyMagic)
        : IMagicEffectFinishedByMe, ITryAlterOutcomeSavingThrow
    {
        private const string FortifyingSpell = "ModifyMagicFortifyingSpell";
        private const string TenaciousSpell = "ModifyMagicTenaciousSpell";

        // Fortifying Spell: one target of the spell gains 2d8 + cleric level temporary hit points
        public IEnumerator OnMagicEffectFinishedByMe(
            CharacterAction action,
            GameLocationCharacter attacker,
            List<GameLocationCharacter> targets)
        {
            if (ModifiedSpellCasts.Remove(action) ||
                !IsSpellCast(action, out _))
            {
                yield break;
            }

            var rulesetAttacker = attacker.RulesetCharacter;
            var usablePower = PowerProvider.Get(powerModifyMagic, rulesetAttacker);

            if (rulesetAttacker is not { IsDeadOrDyingOrUnconscious: false } ||
                rulesetAttacker.GetRemainingUsesOfPower(usablePower) == 0)
            {
                yield break;
            }

            // offering temporary hit points to enemies would only be a trap, so only list allies and yourself
            var candidates = targets
                .Where(x =>
                    x.RulesetCharacter is { IsDeadOrDyingOrUnconscious: false } &&
                    !x.IsOppositeSide(attacker.Side))
                .Distinct()
                .OrderBy(x => x == attacker ? 0 : 1)
                .ToList();

            if (candidates.Count == 0)
            {
                yield break;
            }

            var clericLevel = rulesetAttacker.GetClassLevel(Cleric);
            GameLocationCharacter selectedTarget = null;

            // one prompt listing every eligible target of the spell
            yield return attacker.MyReactToSelectOption(
                attacker,
                FortifyingSpell,
                $"CustomReaction{FortifyingSpell}Description".Formatted(
                    Category.Reaction, clericLevel.ToString()),
                candidates
                    .Select(x => (x == attacker
                        ? $"CustomReaction{FortifyingSpell}Self".Formatted(Category.Reaction, x.Name)
                        : x.Name, true))
                    .ToList(),
                option => selectedTarget = candidates[option]);

            if (selectedTarget == null)
            {
                yield break;
            }

            rulesetAttacker.UsePower(usablePower);

            var rolls = new List<int>();
            var tempHitPoints =
                rulesetAttacker.RollDiceAndSum(DieType.D8, RollContext.None, 2, rolls, false) + clericLevel;

            selectedTarget.RulesetCharacter.ReceiveTemporaryHitPoints(
                tempHitPoints, DurationType.UntilLongRest, 0, TurnOccurenceType.StartOfTurn, rulesetAttacker.Guid);

            EffectHelpers.StartVisualEffect(
                attacker, selectedTarget, PowerTraditionCourtMageSpellShield, EffectHelpers.EffectType.Caster);

            rulesetAttacker.LogCharacterUsedPower(
                powerModifyMagic,
                "Feedback/&ModifyMagicFortifyingSpellTemporaryHitPoints",
                true,
                (ConsoleStyleDuplet.ParameterType.Player, selectedTarget.Name),
                (ConsoleStyleDuplet.ParameterType.Positive, tempHitPoints.ToString()));
        }

        // Tenacious Spell: after a creature you can see succeeds on its first save against your spell,
        // roll 1d6 and subtract it from that save
        public IEnumerator OnTryAlterOutcomeSavingThrow(
            GameLocationBattleManager battleManager,
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            GameLocationCharacter helper,
            SavingThrowData savingThrowData,
            bool hasHitVisual)
        {
            var action = savingThrowData.Action;

            // a d6 can only turn the save into a failure if it succeeded by 5 or less
            if (helper != attacker ||
                savingThrowData.SaveOutcome != RollOutcome.Success ||
                savingThrowData.SaveOutcomeDelta > 5 ||
                !IsSpellCast(action, out _) ||
                action.ActingCharacter != helper ||
                ModifiedSpellCasts.Contains(action) ||
                !defender.IsOppositeSide(helper.Side) ||
                !helper.CanPerceiveTarget(defender))
            {
                yield break;
            }

            var rulesetHelper = helper.RulesetCharacter;
            var usablePower = PowerProvider.Get(powerModifyMagic, rulesetHelper);

            if (rulesetHelper.GetRemainingUsesOfPower(usablePower) == 0)
            {
                yield break;
            }

            // any reaction within a saving flow must use the yielder as waiter
            yield return helper.MyReactToSpendPower(
                usablePower,
                helper,
                TenaciousSpell,
                $"SpendPower{TenaciousSpell}Description".Formatted(
                    Category.Reaction, defender.Name, savingThrowData.Title),
                ReactionValidated,
                battleManager);

            yield break;

            void ReactionValidated()
            {
                ModifiedSpellCasts.Add(action);

                EffectHelpers.StartVisualEffect(helper, defender,
                    PowerDomainLawHolyRetribution, EffectHelpers.EffectType.Caster);

                var dieRoll = rulesetHelper.RollDie(
                    DieType.D6, RollContext.None, false, AdvantageType.None, out _, out _);

                savingThrowData.SaveOutcomeDelta -= dieRoll;
                savingThrowData.SaveOutcome =
                    savingThrowData.SaveOutcomeDelta >= 0 ? RollOutcome.Success : RollOutcome.Failure;

                rulesetHelper.LogCharacterActivatesAbility(
                    powerModifyMagic.GuiPresentation.Title,
                    "Feedback/&ModifyMagicTenaciousSpellRoll",
                    tooltipContent: powerModifyMagic.Name,
                    tooltipClass: "PowerDefinition",
                    extra:
                    [
                        (ConsoleStyleDuplet.ParameterType.AbilityInfo, Gui.FormatDieTitle(DieType.D6)),
                        (savingThrowData.SaveOutcomeDelta >= 0
                            ? ConsoleStyleDuplet.ParameterType.Negative
                            : ConsoleStyleDuplet.ParameterType.Positive, dieRoll.ToString())
                    ]);
            }
        }
    }

    //
    // Dispelling Recovery
    //

    private sealed class MagicEffectFinishedByMeDispellingRecovery(
        FeatureDefinitionPower powerDispellingRecovery,
        FeatureDefinitionPower powerModifyMagic) : IMagicEffectFinishedByMe
    {
        private const string DispellingRecovery = "DispellingRecovery";
        private const int DispelMagicSlotLevel = 3;

        public IEnumerator OnMagicEffectFinishedByMe(
            CharacterAction action,
            GameLocationCharacter attacker,
            List<GameLocationCharacter> targets)
        {
            // only casts that expend a spell slot: no cantrips, rituals, items or free casts
            if (!IsSpellCast(action, out var actionCastSpell) ||
                actionCastSpell.ActionId is not (
                    ActionDefinitions.Id.CastMain or
                    ActionDefinitions.Id.CastBonus or
                    ActionDefinitions.Id.CastReaction or
                    ActionDefinitions.Id.CastReadied or
                    (ActionDefinitions.Id)ExtraActionId.CastQuickened) ||
                actionCastSpell.ActiveSpell.SlotLevel == 0 ||
                !RestoresHitPointsOrEndsCondition(actionCastSpell.ActiveSpell.EffectDescription) ||
                !targets.Any(x => x.RulesetCharacter != null))
            {
                yield break;
            }

            var rulesetAttacker = attacker.RulesetCharacter;

            if (rulesetAttacker is not { IsDeadOrDyingOrUnconscious: false })
            {
                yield break;
            }

            // the use comes back by expending one use of Channel Divinity
            var usablePower = PowerProvider.Get(powerDispellingRecovery, rulesetAttacker);
            var usablePowerChannelDivinity = PowerProvider.Get(powerModifyMagic, rulesetAttacker);
            var hasUse = rulesetAttacker.GetRemainingUsesOfPower(usablePower) > 0;

            if (!hasUse && rulesetAttacker.GetRemainingUsesOfPower(usablePowerChannelDivinity) == 0)
            {
                yield break;
            }

            var candidates = GetCandidates(attacker);

            if (candidates.Count == 0)
            {
                yield break;
            }

            GameLocationCharacter selectedTarget = null;

            // one prompt listing every creature in range that is under a spell
            yield return attacker.MyReactToSelectOption(
                attacker,
                DispellingRecovery,
                Gui.Localize(hasUse
                    ? $"Reaction/&CustomReaction{DispellingRecovery}Description"
                    : $"Reaction/&CustomReaction{DispellingRecovery}ChannelDivinityDescription"),
                candidates
                    .Select(x => (Gui.Format($"Reaction/&CustomReaction{DispellingRecovery}Target",
                        x.character.Name, string.Join(", ", x.spells)), true))
                    .ToList(),
                option => selectedTarget = candidates[option].character);

            if (selectedTarget == null)
            {
                yield break;
            }

            rulesetAttacker.UsePower(hasUse ? usablePower : usablePowerChannelDivinity);
            rulesetAttacker.LogCharacterUsedPower(powerDispellingRecovery);

            var actionParams = new CharacterActionParams(attacker, ActionDefinitions.Id.CastNoCost)
            {
                ActionModifiers = { new ActionModifier() }, TargetCharacters = { selectedTarget }
            };
            var spellRepertoire =
                rulesetAttacker.GetClassSpellRepertoire(Cleric) ?? actionCastSpell.ActiveSpell.SpellRepertoire;

            attacker.MyExecuteActionCastNoCost(DispelMagic, DispelMagicSlotLevel, actionParams, spellRepertoire);
        }

        private static bool RestoresHitPointsOrEndsCondition(EffectDescription effectDescription)
        {
            return effectDescription.EffectForms.Any(x =>
                x.FormType is EffectForm.EffectFormType.Healing or EffectForm.EffectFormType.Revive ||
                (x.FormType == EffectForm.EffectFormType.Condition &&
                 x.ConditionForm.Operation is
                     ConditionForm.ConditionOperation.Remove or
                     ConditionForm.ConditionOperation.RemoveDetrimentalAll or
                     ConditionForm.ConditionOperation.RemoveDetrimentalRandom));
        }

        // same rule as vanilla Dispel Magic: conditions in the effect category that come from a spell
        private static List<(GameLocationCharacter character, List<string> spells)> GetCandidates(
            GameLocationCharacter caster)
        {
            var locationCharacterService = ServiceRepository.GetService<IGameLocationCharacterService>();
            var contenders =
                Gui.Battle?.AllContenders ??
                locationCharacterService.PartyCharacters.Union(locationCharacterService.GuestCharacters);
            var spellDatabase = DatabaseRepository.GetDatabase<SpellDefinition>();
            var range = DispelMagic.EffectDescription.RangeParameter;
            var result = new List<(GameLocationCharacter, List<string>)>();

            foreach (var contender in contenders
                         .Where(x =>
                             x.RulesetCharacter is { IsDeadOrDyingOrUnconscious: false } &&
                             (x == caster || (caster.IsWithinRange(x, range) && caster.CanPerceiveTarget(x))))
                         .OrderBy(x => DistanceCalculation.GetDistanceFromCharacters(caster, x)))
            {
                if (!contender.RulesetCharacter.ConditionsByCategory.TryGetValue(
                        AttributeDefinitions.TagEffect, out var conditions))
                {
                    continue;
                }

                var spells = conditions
                    .Select(x => x.EffectDefinitionName)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Distinct()
                    .Select(x => spellDatabase.TryGetElement(x, out var spell) ? spell.FormatTitle() : null)
                    .Where(x => x != null)
                    .ToList();

                if (spells.Count > 0)
                {
                    result.Add((contender, spells));
                }
            }

            return result;
        }
    }
}
