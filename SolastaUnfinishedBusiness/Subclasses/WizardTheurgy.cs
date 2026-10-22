using System;
using System.Collections.Generic;
using System.Linq;
using SolastaUnfinishedBusiness.Api.Helpers;
using SolastaUnfinishedBusiness.Api.LanguageExtensions;
using SolastaUnfinishedBusiness.Builders;
using SolastaUnfinishedBusiness.Builders.Features;
using static RuleDefinitions;
using static FeatureDefinitionAttributeModifier;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.CharacterClassDefinitions;

namespace SolastaUnfinishedBusiness.Subclasses;

// Theurgy builds one wizard subclass per cleric domain, so picking the arcane tradition also picks the domain.
// All of them are switched on and off together by the single WizardTheurgy settings entry.
internal static class WizardTheurgy
{
    private const string Name = "WizardTheurgy";

    // domain level -> Theurgy level (Channel Divinity option, Arcane Acolyte, Arcane Priest, Arcane High Priest)
    private static readonly Dictionary<int, int> DomainLevelToTheurgyLevel = new()
    {
        { 2, 2 }, { 1, 6 }, { 6, 10 }, { 17, 14 }
    };

    private static readonly List<(CharacterSubclassDefinition Domain, CharacterSubclassDefinition Variant)>
        DomainVariants = [];

    private static readonly Dictionary<CharacterSubclassDefinition, SpellListDefinition> DomainSpellLists = [];

    // Theurgy level -> name of the feature set that groups what the domain grants at that level
    private static readonly Dictionary<int, string> DomainFeatureSetNames = new()
    {
        { 2, "ChannelArcana" }, { 6, "ArcaneAcolyte" }, { 10, "ArcanePriest" }, { 14, "ArcaneHighPriest" }
    };

    private static FeatureDefinitionAttributeModifier _attributeModifierChannelArcana;
    private static FeatureDefinitionPower _powerDivineArcana;

    internal static CharacterSubclassDefinition SubclassToggle { get; private set; }

    internal static CharacterSubclassDefinition[] Variants => DomainVariants.Select(x => x.Variant).ToArray();

    // called on SubclassesContext.Load once all cleric domains are in the database
    internal static void Load()
    {
        // LEVEL 02

        // Channel Arcana (granted with Divine Arcana and the domain options in one feature set per domain)

        _attributeModifierChannelArcana = FeatureDefinitionAttributeModifierBuilder
            .Create($"AttributeModifier{Name}ChannelArcana")
            .SetGuiPresentationNoContent(true)
            .SetModifier(AttributeModifierOperation.Additive, AttributeDefinitions.ChannelDivinityNumber, 1)
            .AddToDB();

        var attributeModifierChannelArcanaAdd = FeatureDefinitionAttributeModifierBuilder
            .Create($"AttributeModifier{Name}ChannelArcanaAdd")
            .SetGuiPresentation(Category.Feature)
            .SetModifier(AttributeModifierOperation.Additive, AttributeDefinitions.ChannelDivinityNumber, 1)
            .AddToDB();

        // Divine Arcana

        var magicAffinityDivineArcana = FeatureDefinitionMagicAffinityBuilder
            .Create($"MagicAffinity{Name}DivineArcana")
            .SetGuiPresentationNoContent(true)
            .SetCastingModifiers(2, SpellParamsModifierType.FlatValue, 2)
            .AddToDB();

        var conditionDivineArcana = ConditionDefinitionBuilder
            .Create($"Condition{Name}DivineArcana")
            .SetGuiPresentation(Category.Condition, ConditionDefinitions.ConditionBlessed)
            .SetPossessive()
            .SetFeatures(magicAffinityDivineArcana)
            .SetSpecialInterruptions(ConditionInterruption.CastSpellExecuted)
            .AddToDB();

        _powerDivineArcana = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}DivineArcana")
            .SetGuiPresentation(Category.Feature, SpellDefinitions.Bless)
            .SetUsesFixed(ActivationTime.BonusAction, RechargeRate.ChannelDivinity)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetDurationData(DurationType.UntilAnyRest)
                    .SetTargetingData(Side.Ally, RangeType.Self, 0, TargetType.Self)
                    .SetEffectForms(EffectFormBuilder.ConditionForm(conditionDivineArcana))
                    .SetCasterEffectParameters(SpellDefinitions.Bless)
                    .Build())
            .AddToDB();

        // settings entry only, never offered on level up
        SubclassToggle = CharacterSubclassDefinitionBuilder
            .Create(Name)
            .SetGuiPresentation(Category.Subclass, CharacterSubclassDefinitions.DomainInsight)
            .AddToDB();

        foreach (var domain in DatabaseRepository.GetDatabase<CharacterSubclassDefinition>()
                     .Where(x => x.Name.StartsWith("Domain") && !x.GuiPresentation.Hidden)
                     .OrderBy(x => x.FormatTitle())
                     .ToArray())
        {
            var domainTitle = domain.FormatTitle();

            // Arcane Initiate
            var spellListDomain = BuildDomainSpellList(domain);
            var magicAffinityArcaneInitiate = FeatureDefinitionMagicAffinityBuilder
                .Create($"MagicAffinity{Name}ArcaneInitiate{domain.Name}")
                .SetGuiPresentation(
                    Gui.Localize($"Feature/&MagicAffinity{Name}ArcaneInitiateTitle"),
                    // spells in one line instead of the usual list by level, to fit the subclass selection screen
                    Gui.Format($"Feature/&MagicAffinity{Name}ArcaneInitiateDescription",
                        string.Join(", ", spellListDomain.SpellsByLevel
                            .SelectMany(x => x.Spells)
                            .OrderBy(x => x.SpellLevel)
                            .Select(x => x.FormatTitle()))))
                .SetExtendedSpellList(spellListDomain)
                .AddToDB();

            var variant = CharacterSubclassDefinitionBuilder
                .Create($"{Name}{domain.Name}")
                .SetGuiPresentation(
                    Gui.Format($"Subclass/&{Name}DomainTitle", domainTitle),
                    Gui.Format($"Subclass/&{Name}DomainDescription", domainTitle),
                    domain)
                .AddFeaturesAtLevel(2, magicAffinityArcaneInitiate)
                .AddFeaturesAtLevel(6, attributeModifierChannelArcanaAdd)
                .AddFeaturesAtLevel(18, attributeModifierChannelArcanaAdd)
                .AddToDB();

            DomainVariants.Add((domain, variant));
            DomainSpellLists.Add(variant, spellListDomain);
        }
    }

    // called on BootContext after Level20Context.Load so domain features added for levels 17 and 20 are there,
    // and before any Tabletop 2024 switch moves domain features to other levels
    internal static void LoadDomainFeatures()
    {
        foreach (var (domain, variant) in DomainVariants)
        {
            var theurgyLevelByFeature = new Dictionary<FeatureDefinition, int>();
            var unlocks = new List<FeatureUnlockByLevel>();
            var upgrades = new List<FeatureUnlockByLevel>();
            var featuresByLevel = DomainFeatureSetNames.Keys.ToDictionary(x => x, _ => new List<FeatureDefinition>());

            featuresByLevel[2].AddRange([_attributeModifierChannelArcana, _powerDivineArcana]);

            foreach (var featureUnlock in domain.FeatureUnlocks.OrderBy(x => x.Level))
            {
                var feature = featureUnlock.FeatureDefinition;

                // power upgrades follow the power they upgrade (i.e. Decisive Strike at 5, 8, 11 and 14)
                if (feature is FeatureDefinitionPower { OverriddenPower: not null })
                {
                    upgrades.Add(featureUnlock);
                    continue;
                }

                if (!DomainLevelToTheurgyLevel.TryGetValue(featureUnlock.Level, out var theurgyLevel))
                {
                    continue;
                }

                // only the 2nd level Channel Divinity option comes along at 2nd level
                if (featureUnlock.Level == 2 && !HasChannelDivinityPower(feature))
                {
                    continue;
                }

                AddFeature(variant, feature, theurgyLevel, featureUnlock.Level == 1, unlocks, featuresByLevel,
                    theurgyLevelByFeature);
            }

            foreach (var featureUnlock in upgrades)
            {
                var power = (FeatureDefinitionPower)featureUnlock.FeatureDefinition;
                var basePower = power.OverriddenPower;

                while (basePower && !theurgyLevelByFeature.ContainsKey(basePower))
                {
                    basePower = basePower.OverriddenPower;
                }

                if (basePower)
                {
                    var level = Math.Max(featureUnlock.Level, theurgyLevelByFeature[basePower]);

                    unlocks.Add(new FeatureUnlockByLevel(power, level));
                    theurgyLevelByFeature.TryAdd(power, level);
                }
                else if (DomainLevelToTheurgyLevel.TryGetValue(featureUnlock.Level, out var theurgyLevel) &&
                         (featureUnlock.Level != 2 || HasChannelDivinityPower(power)))
                {
                    AddFeature(variant, power, theurgyLevel, featureUnlock.Level == 1, unlocks, featuresByLevel,
                        theurgyLevelByFeature);
                }
            }

            // the subclass selection screen doesn't scroll, so level 2 is one short entry naming the options,
            // while higher levels get a short header plus the domain features as their own entries
            foreach (var kvp in featuresByLevel)
            {
                var level = kvp.Key;
                var features = kvp.Value;
                var setName = DomainFeatureSetNames[level];
                var featureTitles = string.Join(", ", features
                    .Where(x => !x.GuiPresentation.Hidden)
                    .Select(x => x.FormatTitle()));

                if (featureTitles == string.Empty)
                {
                    featureTitles = Gui.Localize($"Feature/&Feature{Name}NoDomainFeatures");
                }

                var title = Gui.Localize($"Feature/&Feature{Name}{setName}Title");
                var description = Gui.Format($"Feature/&Feature{Name}{setName}Description", featureTitles);

                if (level == 2)
                {
                    var featureSet = FeatureDefinitionFeatureSetBuilder
                        .Create($"FeatureSet{Name}{setName}{domain.Name}")
                        .SetGuiPresentation(title, description)
                        .SetFeatureSet([.. features])
                        .AddToDB();

                    unlocks.Add(new FeatureUnlockByLevel(featureSet, level));

                    continue;
                }

                var featureHeader = FeatureDefinitionBuilder
                    .Create($"Feature{Name}{setName}{domain.Name}")
                    .SetGuiPresentation(title, description)
                    .AddToDB();

                unlocks.Add(new FeatureUnlockByLevel(featureHeader, level));
                unlocks.AddRange(features.Select(x => new FeatureUnlockByLevel(x, level)));
            }

            variant.FeatureUnlocks.AddRange(unlocks);
            variant.FeatureUnlocks.Sort(Sorting.CompareFeatureUnlock);
        }
    }

    private static void AddFeature(
        CharacterSubclassDefinition variant,
        FeatureDefinition feature,
        int theurgyLevel,
        bool isArcaneAcolyte,
        List<FeatureUnlockByLevel> unlocks,
        Dictionary<int, List<FeatureDefinition>> featuresByLevel,
        Dictionary<FeatureDefinition, int> theurgyLevelByFeature)
    {
        var adapted = AdaptFeature(variant, feature, theurgyLevel, isArcaneAcolyte, unlocks);

        if (!adapted)
        {
            return;
        }

        featuresByLevel[theurgyLevel].Add(adapted);
        TrackFeature(adapted, theurgyLevel, theurgyLevelByFeature);
    }

    private static void TrackFeature(
        FeatureDefinition feature, int theurgyLevel, Dictionary<FeatureDefinition, int> theurgyLevelByFeature)
    {
        theurgyLevelByFeature.TryAdd(feature, theurgyLevel);

        if (feature is not FeatureDefinitionFeatureSet featureSet)
        {
            return;
        }

        foreach (var subFeature in featureSet.FeatureSet)
        {
            TrackFeature(subFeature, theurgyLevel, theurgyLevelByFeature);
        }
    }

    // returns the feature as is, a filtered copy of it, or null if nothing of it applies to a Theurgy wizard
    private static FeatureDefinition AdaptFeature(
        CharacterSubclassDefinition variant,
        FeatureDefinition feature,
        int theurgyLevel,
        bool isArcaneAcolyte,
        List<FeatureUnlockByLevel> unlocks)
    {
        switch (feature)
        {
            case FeatureDefinitionFeatureSet featureSet:
            {
                var features = featureSet.FeatureSet
                    .Select(x => AdaptFeature(variant, x, theurgyLevel, isArcaneAcolyte, unlocks))
                    .Where(x => x)
                    .ToArray();

                if (features.Length == 0)
                {
                    return null;
                }

                if (features.SequenceEqual(featureSet.FeatureSet))
                {
                    return featureSet;
                }

                return FeatureDefinitionFeatureSetBuilder
                    .Create(featureSet, $"{featureSet.Name}{variant.Name}")
                    .SetFeatureSet(features)
                    .AddToDB();
            }

            // Arcane Initiate replaces the domain spells
            case FeatureDefinitionAutoPreparedSpells when isArcaneAcolyte:
                return null;

            // other always prepared spells go to the wizard repertoire from the level they are granted
            case FeatureDefinitionAutoPreparedSpells autoPreparedSpells:
            {
                var autoPreparedSpellsWizard = FeatureDefinitionAutoPreparedSpellsBuilder
                    .Create(autoPreparedSpells, $"{autoPreparedSpells.Name}{variant.Name}")
                    .SetSpellcastingClass(Wizard)
                    .AddToDB();

                foreach (var group in autoPreparedSpellsWizard.AutoPreparedSpellsGroups)
                {
                    group.ClassLevel = Math.Min(group.ClassLevel, theurgyLevel);
                }

                return autoPreparedSpellsWizard;
            }

            // Arcane Acolyte doesn't grant weapon or armor proficiencies
            case FeatureDefinitionProficiency
            {
                ProficiencyType: ProficiencyType.Armor or ProficiencyType.Weapon
            } when isArcaneAcolyte:
                return null;

            // spell choices of a level the wizard can't cast yet wait until that level is available
            case FeatureDefinitionPointPool { poolType: HeroDefinitions.PointsPoolType.Spell } pointPool
                when pointPool.minSpellLevel > MaxSpellLevel(theurgyLevel):
                unlocks.Add(new FeatureUnlockByLevel(pointPool, (pointPool.minSpellLevel * 2) - 1));
                return null;

            default:
                return feature;
        }
    }

    private static int MaxSpellLevel(int wizardLevel)
    {
        return Math.Min(9, (wizardLevel + 1) / 2);
    }

    private static bool HasChannelDivinityPower(FeatureDefinition feature)
    {
        return feature switch
        {
            FeatureDefinitionPower power => power.RechargeRate == RechargeRate.ChannelDivinity,
            FeatureDefinitionFeatureSet featureSet => featureSet.FeatureSet.Any(HasChannelDivinityPower),
            _ => false
        };
    }

    private static SpellListDefinition BuildDomainSpellList(CharacterSubclassDefinition domain)
    {
        var spells = domain.FeatureUnlocks
            .Where(x => x.Level == 1)
            .SelectMany(x => GetAutoPreparedSpells(x.FeatureDefinition))
            .Distinct()
            .ToArray();

        var builder = SpellListDefinitionBuilder
            .Create(SpellListDefinitions.SpellListWizard, $"SpellList{Name}{domain.Name}")
            .SetGuiPresentationNoContent(true)
            .ClearSpells();

        foreach (var spellsByLevel in spells.GroupBy(x => x.SpellLevel))
        {
            builder.SetSpellsAtLevel(spellsByLevel.Key, [.. spellsByLevel]);
        }

        return builder
            .FinalizeSpells(true, 9)
            .AddToDB();
    }

    private static IEnumerable<SpellDefinition> GetAutoPreparedSpells(FeatureDefinition feature)
    {
        return feature switch
        {
            FeatureDefinitionAutoPreparedSpells autoPreparedSpells => autoPreparedSpells.AutoPreparedSpellsGroups
                .SelectMany(x => x.SpellsList),
            FeatureDefinitionFeatureSet featureSet => featureSet.FeatureSet.SelectMany(GetAutoPreparedSpells),
            _ => []
        };
    }

    internal static bool IsVariantOf(CharacterSubclassDefinition subclass, string domainName)
    {
        return DomainVariants.Any(x => x.Variant == subclass && x.Domain.Name == domainName);
    }

    // Arcane Initiate: once every domain spell is in the spellbook, any cleric spell can be learned
    internal static IEnumerable<SpellDefinition> GetArcaneInitiateClericSpells(RulesetCharacterHero hero)
    {
        if (!hero.ClassesAndSubclasses.TryGetValue(Wizard, out var subclass) ||
            !subclass ||
            !DomainSpellLists.TryGetValue(subclass, out var spellListDomain))
        {
            return [];
        }

        var repertoire = hero.SpellRepertoires.FirstOrDefault(x => x.SpellCastingClass == Wizard);

        if (repertoire == null)
        {
            return [];
        }

        // wizard spells are scribed in spellbook items, and spells picked on this level up aren't scribed yet
        var spellbookSpells = repertoire.EnumerateAvailableScribedSpells().ToHashSet();

        spellbookSpells.UnionWith(repertoire.KnownSpells);

        var heroBuildingData = hero.GetHeroBuildingData();

        if (heroBuildingData != null)
        {
            spellbookSpells.UnionWith(heroBuildingData.AcquiredSpells.Values.SelectMany(x => x).Where(x => x));
        }

        if (!spellListDomain.SpellsByLevel
                .SelectMany(x => x.Spells)
                .All(spellbookSpells.Contains))
        {
            return [];
        }

        return SpellListDefinitions.SpellListCleric.SpellsByLevel
            .SelectMany(x => x.Spells)
            .Where(x => x.SpellLevel > 0);
    }
}
