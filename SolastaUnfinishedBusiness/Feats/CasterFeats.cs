using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Api;
using SolastaUnfinishedBusiness.Api.GameExtensions;
using SolastaUnfinishedBusiness.Api.LanguageExtensions;
using SolastaUnfinishedBusiness.Behaviors;
using SolastaUnfinishedBusiness.Builders;
using SolastaUnfinishedBusiness.Builders.Features;
using SolastaUnfinishedBusiness.CustomUI;
using SolastaUnfinishedBusiness.Models;
using SolastaUnfinishedBusiness.Properties;
using SolastaUnfinishedBusiness.Validators;
using UnityEngine.AddressableAssets;
using static RuleDefinitions;
using static SolastaUnfinishedBusiness.Builders.Features.AutoPreparedSpellsGroupBuilder;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.FeatureDefinitionPowers;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.SpellDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.FeatureDefinitionAttributeModifiers;

namespace SolastaUnfinishedBusiness.Feats;

internal static class CasterFeats
{
    internal static readonly Dictionary<string, SpellDefinition[]> MagicTouchedData = new()
    {
        { "AegisTouched", [ShieldOfFaith, ProtectionFromEvilGood, ProtectionFromPoison] },
        { "CelestialTouched", [HealingWord, CureWounds, LesserRestoration] },
        { "FlameTouched", [BurningHands, HellishRebuke, ScorchingRay] },
        { "IridescentTouched", [ColorSpray, FaerieFire, SpellsContext.ColorBurst] },
        { "PeregrinationTouched", [Longstrider, ExpeditiousRetreat, SpiderClimb] },
        { "RetinueTouched", [Bless, Heroism, EnhanceAbility] },
        { "ShadowTouched", [Invisibility, FalseLife, InflictWounds] },
        { "VerdantTouched", [Barkskin, Entangle, Goodberry] }
    };

    internal static void CreateFeats([NotNull] List<FeatDefinition> feats)
    {
        var groups = new List<FeatDefinition>();
        var groupFeats = new List<FeatDefinition>();

        #region Telekinetic

        const string TELEKINETIC = "Telekinetic";

        groupFeats.SetRange(FeatDefinitionBuilder
                .Create($"Feat{TELEKINETIC}Int")
                .SetGuiPresentation(Category.Feat)
                .SetFeatures(
                    BuildTelekinesis(AttributeDefinitions.Intelligence, MotionForm.MotionType.DragToOrigin),
                    BuildTelekinesis(AttributeDefinitions.Intelligence, MotionForm.MotionType.PushFromOrigin),
                    AttributeModifierCreed_Of_Pakri)
                .SetFeatFamily(TELEKINETIC)
                .AddToDB(),
            FeatDefinitionBuilder
                .Create($"Feat{TELEKINETIC}Cha")
                .SetGuiPresentation(Category.Feat)
                .SetFeatures(
                    BuildTelekinesis(AttributeDefinitions.Charisma, MotionForm.MotionType.DragToOrigin),
                    BuildTelekinesis(AttributeDefinitions.Charisma, MotionForm.MotionType.PushFromOrigin),
                    AttributeModifierCreed_Of_Solasta)
                .SetFeatFamily(TELEKINETIC)
                .AddToDB(),
            FeatDefinitionBuilder
                .Create($"Feat{TELEKINETIC}Wis")
                .SetGuiPresentation(Category.Feat)
                .SetFeatures(
                    BuildTelekinesis(AttributeDefinitions.Wisdom, MotionForm.MotionType.DragToOrigin),
                    BuildTelekinesis(AttributeDefinitions.Wisdom, MotionForm.MotionType.PushFromOrigin),
                    AttributeModifierCreed_Of_Maraike)
                .SetFeatFamily(TELEKINETIC)
                .AddToDB());

        groups.Add(GroupFeats.MakeGroup($"FeatGroup{TELEKINETIC}", TELEKINETIC, groupFeats));
        feats.AddRange(groupFeats);

        #endregion

        #region Fey Touched

        groups.Add(BuildFeyTouched(feats));

        #endregion

        #region Touched Magic

        const string TOUCHED_MAGIC = "TouchedMagic";

        var featGroups = new List<FeatDefinition>();

        foreach (var kvp in MagicTouchedData)
        {
            var tag = kvp.Key;

            var spells = BuildSpellGroup(0, kvp.Value);
            var autoPreparedSpells = FeatureDefinitionAutoPreparedSpellsBuilder
                .Create($"AutoPreparedSpellsFeat{tag}")
                .SetGuiPresentationNoContent(true)
                .AddCustomSubFeatures(ValidateRepertoireForAutoprepared.AnyClassOrSubclass)
                .SetPreparedSpellGroups(spells)
                .SetSpellcastingClass(null)
                .SetAutoTag(tag)
                .AddToDB();

            groupFeats.SetRange(
                FeatDefinitionBuilder
                    .Create($"Feat{tag}Int")
                    .SetFeatures(autoPreparedSpells, AttributeModifierCreed_Of_Pakri)
                    .AddFeatures(MakeSpellFeatureAndInvocations(spells, tag, AttributeDefinitions.Intelligence))
                    .SetGuiPresentation(Category.Feat)
                    .SetFeatFamily(TOUCHED_MAGIC)
                    .SetMustCastSpellsPrerequisite()
                    .AddToDB(),
                FeatDefinitionBuilder
                    .Create($"Feat{tag}Wis")
                    .SetFeatures(autoPreparedSpells, AttributeModifierCreed_Of_Maraike)
                    .AddFeatures(MakeSpellFeatureAndInvocations(spells, tag, AttributeDefinitions.Wisdom))
                    .SetGuiPresentation(Category.Feat)
                    .SetFeatFamily(TOUCHED_MAGIC)
                    .SetMustCastSpellsPrerequisite()
                    .AddToDB(),
                FeatDefinitionBuilder
                    .Create($"Feat{tag}Cha")
                    .SetFeatures(autoPreparedSpells, AttributeModifierCreed_Of_Solasta)
                    .AddFeatures(MakeSpellFeatureAndInvocations(spells, tag, AttributeDefinitions.Charisma))
                    .SetGuiPresentation(Category.Feat)
                    .SetFeatFamily(TOUCHED_MAGIC)
                    .SetMustCastSpellsPrerequisite()
                    .AddToDB());

            featGroups.Add(GroupFeats.MakeGroup($"FeatGroup{tag}", TOUCHED_MAGIC, groupFeats));
        }

        groups.Add(GroupFeats.MakeGroup("FeatGroupTouchedMagic", TOUCHED_MAGIC, featGroups));

        #endregion

        GroupFeats.MakeGroup("FeatGroupPlaneMagic", null, groups);
    }

    [NotNull]
    private static FeatDefinition BuildFeyTouched(List<FeatDefinition> feats)
    {
        const string NAME = "FeatFeyTouched";
        const string FEY_TOUCHED = "FeyTouched";

        // a single 1st level slot recharged on a long rest to cast the chosen spell once without a spell slot
        var castingSlots = Enumerable.Range(1, 20)
            .Select(level => new FeatureDefinitionCastSpell.SlotsByLevelDuplet { Slots = [1], Level = level })
            .ToList();

        var attributeIncreases = new List<(string, FeatureDefinition, string)>
        {
            (AttributeDefinitions.Intelligence, AttributeModifierCreed_Of_Pakri, "Int"),
            (AttributeDefinitions.Wisdom, AttributeModifierCreed_Of_Maraike, "Wis"),
            (AttributeDefinitions.Charisma, AttributeModifierCreed_Of_Solasta, "Cha")
        };

        var groupFeats = new List<FeatDefinition>();

        foreach (var (castingAttribute, attributeIncrease, postfix) in attributeIncreases)
        {
            // learns one level 1 divination or enchantment spell from all spells, cast with the increased ability
            var castSpell = FeatureDefinitionCastSpellBuilder
                .Create($"CastSpell{NAME}{postfix}")
                .SetGuiPresentation($"CastSpell{NAME}", Category.Feature)
                .SetFocusType(EquipmentDefinitions.FocusType.None)
                .SetSpellCastingOrigin(FeatureDefinitionCastSpell.CastingOrigin.Race)
                .SetSpellCastingAbility(castingAttribute)
                .SetSpellKnowledge(SpellKnowledge.Selection)
                .SetSpellReadyness(SpellReadyness.AllKnown)
                .SetSlotsRecharge(RechargeRate.LongRest)
                .SetSlotsPerLevel(castingSlots)
                .SetKnownCantrips(0, 1, FeatureDefinitionCastSpellBuilder.CasterProgression.Flat)
                .SetKnownSpells(1, FeatureDefinitionCastSpellBuilder.CasterProgression.Flat)
                .SetReplacedSpells(1, 0)
                .SetUniqueLevelSlots(false)
                .SetSpellList(DatabaseHelper.SpellListDefinitions.SpellListAllSpells)
                .AddRestrictedSchools(
                    DatabaseHelper.SchoolOfMagicDefinitions.SchoolDivination,
                    DatabaseHelper.SchoolOfMagicDefinitions.SchoolEnchantment)
                .AddCustomSubFeatures(new FeatHelpers.SpellTag(FEY_TOUCHED))
                .AddToDB();

            // no spell list override so the level up screen keeps the repertoire school restriction
            var pointPool = FeatureDefinitionPointPoolBuilder
                .Create($"PointPool{NAME}{postfix}")
                .SetGuiPresentationNoContent(true)
                .SetSpellOrCantripPool(HeroDefinitions.PointsPoolType.Spell, 1, null, FEY_TOUCHED, 1, 1)
                .AddToDB();

            // misty step is cast once per long rest without a spell slot from the feat repertoire
            var invocationMistyStep = CustomInvocationDefinitionBuilder
                .Create($"CustomInvocation{FEY_TOUCHED}{MistyStep.Name}{castingAttribute}")
                .SetGuiPresentation(MistyStep.GuiPresentation)
                .AddCustomSubFeatures(ValidateRepertoireForAutoprepared.HasSpellCastingFeature(castSpell.Name))
                .SetPoolType(InvocationPoolTypeCustom.Pools.PlaneMagic)
                .SetGrantedSpell(MistyStep, longRestRecharge: true)
                .AddToDB();

            var grantInvocations = FeatureDefinitionGrantInvocationsBuilder
                .Create($"GrantInvocations{FEY_TOUCHED}{castingAttribute}")
                .SetGuiPresentationNoContent(true)
                .SetInvocations(invocationMistyStep)
                .AddToDB();

            // misty step and the chosen spell are also prepared on class repertoires to be cast with spell slots
            var autoPreparedSpells = FeatureDefinitionAutoPreparedSpellsBuilder
                .Create($"AutoPreparedSpells{NAME}{postfix}")
                .SetGuiPresentationNoContent(true)
                .AddCustomSubFeatures(
                    ValidateRepertoireForAutoprepared.AnyClassOrSubclass,
                    AutoPreparedSpellsUpToMaxSpellSlotLevel.Marker,
                    new AutoPreparedSpellsFromFeatRepertoire(castSpell))
                .SetPreparedSpellGroups(BuildSpellGroup(0, MistyStep))
                .SetSpellcastingClass(null)
                .SetAutoTag(FEY_TOUCHED)
                .AddToDB();

            groupFeats.Add(FeatDefinitionWithPrerequisitesBuilder
                .Create($"{NAME}{postfix}")
                .SetGuiPresentation(Category.Feat)
                .SetFeatures(attributeIncrease, castSpell, pointPool, grantInvocations, autoPreparedSpells)
                .SetValidators(ValidatorsFeat.IsLevel4)
                .SetFeatFamily(FEY_TOUCHED)
                .AddToDB());
        }

        feats.AddRange(groupFeats);

        return GroupFeats.MakeGroupWithPreRequisite(
            $"FeatGroup{FEY_TOUCHED}", FEY_TOUCHED, ValidatorsFeat.IsLevel4, [.. groupFeats]);
    }

    [NotNull]
    private static FeatureDefinition[] MakeSpellFeatureAndInvocations(
        FeatureDefinitionAutoPreparedSpells.AutoPreparedSpellsGroup spellGroup,
        string name,
        string castingAttribute, bool longRest = true)
    {
        var featureName = $"CastSpell{name}{castingAttribute}";
        var spellFeature = FeatureDefinitionCastSpellBuilder
            .Create(featureName)
            .SetGuiPresentationNoContent(true)
            .SetFocusType(EquipmentDefinitions.FocusType.None)
            .SetSpellCastingOrigin(FeatureDefinitionCastSpell.CastingOrigin.Race)
            .SetSpellReadyness(SpellReadyness.AllKnown)
            .SetSlotsRecharge(RechargeRate.None)
            .SetSlotsPerLevel(FeatureDefinitionCastSpellBuilder.CasterProgression.None)
            .SetSpellList(SpellsContext.EmptySpellList)
            .SetSpellCastingAbility(castingAttribute)
            .AddToDB();

        var invocations = new List<InvocationDefinition>();

        foreach (var spell in spellGroup.SpellsList
                     .Where(x => x.castingTime is not ActivationTime.Reaction))
        {
            var invocation = CustomInvocationDefinitionBuilder
                .Create($"CustomInvocation{name}{spell.Name}{castingAttribute}")
                .SetGuiPresentation(spell.GuiPresentation)
                .AddCustomSubFeatures(ValidateRepertoireForAutoprepared.HasSpellCastingFeature(featureName))
                .SetPoolType(InvocationPoolTypeCustom.Pools.PlaneMagic)
                .SetGrantedSpell(spell, longRestRecharge: longRest)
                .AddToDB();

            if (!longRest)
            {
                invocation.AddCustomSubFeatures(RechargeInvocationOnShortRest.Marker);
            }

            invocations.Add(invocation);
        }

        var grant = FeatureDefinitionGrantInvocationsBuilder
            .Create($"GrantInvocations{name}{castingAttribute}")
            .SetGuiPresentationNoContent(true)
            .SetInvocations(invocations)
            .AddToDB();

        return [spellFeature, grant];
    }

    [NotNull]
    private static FeatureDefinitionGrantInvocations BuildTelekinesis(
        string savingThrowDifficultyAbility, MotionForm.MotionType motionType)
    {
        const string NAME = "FeatTelekinetic";

        var motionTypeName = motionType.ToString();

        AssetReferenceSprite sprite;

        if (motionTypeName == "DragToOrigin")
        {
            sprite = Sprites.GetSprite(motionTypeName, Resources.TelekinesisPull, 128);

            motionTypeName = string.Empty;
        }
        else
        {
            sprite = Sprites.GetSprite(motionTypeName, Resources.TelekinesisPush, 128);
        }

        var power = FeatureDefinitionPowerBuilder
            .Create($"Power{NAME}{savingThrowDifficultyAbility}{motionTypeName}")
            .SetGuiPresentation($"{NAME}{motionTypeName}", Category.Feature, sprite)
            .AddCustomSubFeatures(ModifyPowerFromInvocation.Marker)
            .SetUsesFixed(ActivationTime.BonusAction)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetTargetingData(Side.All, RangeType.Distance, 6, TargetType.IndividualsUnique)
                    .SetSavingThrowData(
                        true,
                        AttributeDefinitions.Strength,
                        true,
                        EffectDifficultyClassComputation.AbilityScoreAndProficiency,
                        savingThrowDifficultyAbility)
                    .ExcludeCaster()
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetMotionForm(motionType, 1)
                            .Build())
                    .SetParticleEffectParameters(PowerSpellBladeSpellTyrant)
                    .Build())
            .AddToDB();

        var invocation = CustomInvocationDefinitionBuilder
            .Create($"CustomInvocation{NAME}{savingThrowDifficultyAbility}{motionTypeName}")
            .SetGuiPresentation(power.GuiPresentation)
            .SetPoolType(InvocationPoolTypeCustom.Pools.PlaneMagic)
            .SetGrantedFeature(power)
            .AddToDB();

        return FeatureDefinitionGrantInvocationsBuilder
            .Create($"GrantInvocations{NAME}{savingThrowDifficultyAbility}{motionTypeName}")
            .SetGuiPresentationNoContent(true)
            .SetInvocations(invocation)
            .AddToDB();
    }
}
