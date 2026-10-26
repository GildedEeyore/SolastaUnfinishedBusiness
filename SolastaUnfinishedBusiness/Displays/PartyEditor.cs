// Copyright < 2021 > Narria (github user Cabarius) - License: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using SolastaUnfinishedBusiness.Api.Helpers;
using SolastaUnfinishedBusiness.Api.LanguageExtensions;
using SolastaUnfinishedBusiness.DataViewer;
using UnityEngine;
using static SolastaUnfinishedBusiness.Api.ModKit.UI;

namespace SolastaUnfinishedBusiness.Displays;

public static class PartyEditor
{
    private static ToggleChoice _selectedToggle = ToggleChoice.None;
    private static int _selectedCharacterIndex;
    private static RulesetCharacter _selectedCharacter;
    private static (string, string) _nameEditState = (null, null);
    private static (RulesetSpellRepertoire repertoire, SpellDefinition spell) _spellToReplace;
    private static string _replacementSearchText = "";

#if DEBUG
    private static ICharacterPoolService PoolService => ServiceRepository.GetService<ICharacterPoolService>();
#endif

    internal static void OnGUI()
    {
        Label("Experimental Preview:".Localized().Orange().Bold() + " " + "PartyEditorMessage".Localized().Green());

        var characters = GetCharacterList();

        if (characters == null)
        {
            Label("PartyEditorUnavailable".Localized().Yellow().Bold());
        }
        else
        {
            var commandService = ServiceRepository.GetService<ICommandService>();

            Space(15);

            HStack("Quickies".Localized(), 2,
                () => ActionButton("Long Rest",
                    () => commandService.StartRest(RuleDefinitions.RestType.LongRest, false),
                    AutoWidth()),
                () => ActionButton("Refresh Powers",
                    () => characters.ForEach(ch =>
                    {
                        ch.GrantPowers();
                        ch.GrantInvocations();
                        ch.GrantFixedSpells();
                    }),
                    AutoWidth()),
                () => { }
            );

            Div();
            Label("Current Party".Localized().Cyan().Bold());

            using (VerticalScope())
            {
                foreach (var ch in characters)
                {
                    if (ch is not RulesetCharacterHero hero)
                    {
                        continue;
                    }

                    var changed = false;
                    var level = ch.TryGetAttributeValue(AttributeDefinitions.CharacterLevel);

                    using (HorizontalScope())
                    {
                        var name = hero.Name + " " + hero.SurName;

                        if (EditableLabel(ref name, ref _nameEditState, 200,
                                n => n.Orange().Bold(),
                                MinWidth(100),
                                MaxWidth(600)))
                        {
                            var parts = name.Split();

                            switch (parts.Length)
                            {
                                case > 1:
                                    hero.Name = parts[0];
                                    hero.SurName = string.Join(" ", parts.Skip(1).ToArray()).Trim();
                                    break;
                                case 1:
                                    hero.Name = parts[0];
                                    hero.SurName = "";
                                    break;
                            }

                            changed = true;
                        }

                        Space(5);
                        Label((level < 10 ? "   lvl" : "   lv").Green() + $" {level}", Width(90));
                        Space(5);
                        OnDetailToggle(ToggleChoice.Stats, ch);
                        OnDetailToggle(ToggleChoice.Skills, ch);
                        OnDetailToggle(ToggleChoice.Feats, ch);
                        OnDetailToggle(ToggleChoice.Invocations, ch);
                        OnDetailToggle(ToggleChoice.Spells, ch);
                    }

                    if (ch == _selectedCharacter && _selectedToggle == ToggleChoice.Stats)
                    {
                        Div(100, 20, 755);

                        foreach (var attr in ch.Attributes)
                        {
                            var attrName = attr.Key;
                            var attribute = attr.Value;
                            var baseValue = attribute.baseValue;
                            var modifiers = attribute.ActiveModifiers.Where(m => m.Value != 0).Select(m =>
                                    $"{m.Value:+0;-#} {string.Join(" ", m.Tags).TrimStart('0', '1', '2', '3', '4', '5', '6', '7', '8', '9').Cyan()}")
                                .ToArray();
                            var modifiersString = string.Join(" ", modifiers);

                            using (HorizontalScope())
                            {
                                Space(100);
                                Label(attrName, Width(400f));
                                Space(25);

                                ActionButton(" < ", () =>
                                {
                                    attribute.baseValue -= 1;
                                    changed = true;
                                }, GUI.skin.box, AutoWidth());

                                Space(20);
                                Label($"{attribute.currentValue}".Orange().Bold(), Width(50f));

                                ActionButton(" > ", () =>
                                {
                                    attribute.baseValue += 1;
                                    changed = true;
                                }, GUI.skin.box, AutoWidth());

                                Space(25);

                                ActionIntTextField(ref baseValue, v =>
                                {
                                    attribute.baseValue = v;
                                    changed = true;
                                }, Width(75));

                                Space(10);
                                Label($"{modifiersString}");
                            }
                        }
                    }

                    if (ch == _selectedCharacter && _selectedToggle == ToggleChoice.Spells)
                    {
                        Div(100, 20, 755);
                        changed |= SpellsOnGUI(hero);
                    }

                    if (ch == _selectedCharacter && BlueprintLoader.Shared.LoadInProgress())
                    {
                        using (HorizontalScope())
                        {
                            GUILayout.Label("Loading: " +
                                            BlueprintLoader.Shared.Progress.ToString("P2").Cyan().Bold());
                        }

                        continue;
                    }

                    if (ch == _selectedCharacter && _selectedToggle == ToggleChoice.Skills)
                    {
                        var skills = BlueprintDisplay.GetBlueprints()?.OfType<SkillDefinition>()
                            .Cast<BaseDefinition>();
                        var tools = BlueprintDisplay.GetBlueprints()?.OfType<ToolTypeDefinition>()
                            .Cast<BaseDefinition>();

                        if (skills != null && tools != null)
                        {
                            var available = skills.Union(tools);
                            var baseDefinitions = available as BaseDefinition[] ?? available.ToArray();
                            var skillsDict = baseDefinitions.ToDictionary(e => e.Name, e => e);
                            var expertisesHash = hero.TrainedExpertises.ToHashSet();
                            var currentSkills = hero.TrainedSkills.Cast<BaseDefinition>();
                            var currentTools = hero.TrainedToolTypes.Cast<BaseDefinition>();
                            var current = currentSkills.Union(currentTools).ToArray();

                            Browser<RulesetCharacterHero, BaseDefinition, BaseDefinition>.OnGUI(
                                _selectedToggle.ToString(), ref changed,
                                hero,
                                current,
                                baseDefinitions,
                                item => skillsDict[item.Name],
                                def => def.Name,
                                def => def.Name,
                                def => def.FormatDescription(),
                                item => skillsDict.ContainsKey(item.Name)
                                    ? expertisesHash.Contains(item.Name)
                                        ? "Expert".Localized()
                                        : "Skilled".Localized()
                                    : "-",
                                null,
                                (chr, item) =>
                                {
                                    // Increment
                                    if (!skillsDict.ContainsKey(item.Name))
                                    {
                                        return () =>
                                        {
                                            switch (item)
                                            {
                                                case SkillDefinition skill:
                                                    chr.TrainedSkills.Add(skill);
                                                    break;
                                                case ToolTypeDefinition tool:
                                                    chr.TrainedToolTypes.Add(tool);
                                                    break;
                                            }
                                        };
                                    }

                                    if (expertisesHash.Contains(item.Name))
                                    {
                                        return null;
                                    }

                                    return () => chr.TrainedExpertises.Add(item.Name);
                                },
                                (chr, item) =>
                                {
                                    // Decrement
                                    if (!skillsDict.ContainsKey(item.Name))
                                    {
                                        return null;
                                    }

                                    if (expertisesHash.Contains(item.Name))
                                    {
                                        return () => chr.TrainedExpertises.Remove(item.Name);
                                    }

                                    return () =>
                                    {
                                        switch (item)
                                        {
                                            case SkillDefinition skill:
                                                chr.TrainedSkills.Remove(skill);
                                                break;
                                            case ToolTypeDefinition tool:
                                                chr.TrainedToolTypes.Remove(tool);
                                                break;
                                        }

                                        chr.TrainedExpertises.Remove(item.Name);
                                    };
                                },
                                (chr, def) =>
                                {
                                    return def switch
                                    {
                                        // Add
                                        SkillDefinition skill when !chr.TrainedSkills.Contains(skill) => () =>
                                            chr.TrainedSkills.Add(skill),
                                        ToolTypeDefinition tool when !chr.TrainedToolTypes.Contains(tool) => () =>
                                            chr.TrainedToolTypes.Add(tool),
                                        _ => null
                                    };
                                },
                                (chr, def) =>
                                {
                                    return def switch
                                    {
                                        // Remove
                                        SkillDefinition skill when chr.TrainedSkills.Contains(skill) => () =>
                                        {
                                            chr.TrainedSkills.Remove(skill);
                                            chr.TrainedExpertises.Remove(skill.Name);
                                        },
                                        ToolTypeDefinition tool when chr.TrainedToolTypes.Contains(tool) => () =>
                                        {
                                            chr.TrainedToolTypes.Remove(tool);
                                            chr.TrainedExpertises.Remove(tool.Name);
                                        },
                                        _ => null
                                    };
                                }
                            );
                        }
                    }

                    if (ch == _selectedCharacter && _selectedToggle == ToggleChoice.Feats)
                    {
                        var available = BlueprintDisplay.GetBlueprints()?.OfType<FeatDefinition>();

                        if (available != null)
                        {
                            Browser<RulesetCharacterHero, FeatDefinition, FeatDefinition>.OnGUI(
                                _selectedToggle.ToString(), ref changed,
                                hero,
                                hero.TrainedFeats,
                                available,
                                feat => feat,
                                feat => feat.Name,
                                feat => feat.Name,
                                feat => feat.FormatDescription(),
                                null,
                                null,
                                null,
                                null,
                                (chr, feat) => !chr.TrainedFeats.Contains(feat)
                                    ? () =>
                                    {
                                        chr.TrainFeats([feat]);

                                        foreach (var power in feat.Features.OfType<FeatureDefinitionPower>())
                                        {
                                            var usablePower = new RulesetUsablePower(power, null, null);

                                            chr.UsablePowers.Add(usablePower);
                                        }

                                        LevelUpHelper.RecursiveGrantCustomFeatures(
                                            chr, AttributeDefinitions.TagFeat, feat.Features);
                                    }
                                    : null,
                                (chr, feat) => chr.TrainedFeats.Contains(feat)
                                    ? () =>
                                    {
                                        chr.TrainedFeats.Remove(feat);

                                        foreach (var power in feat.Features.OfType<FeatureDefinitionPower>())
                                        {
                                            var usablePower =
                                                chr.UsablePowers.FirstOrDefault(p => p.PowerDefinition == power);

                                            if (usablePower != null)
                                            {
                                                chr.UsablePowers.Remove(usablePower);
                                            }
                                        }

                                        LevelUpHelper.RecursiveRemoveCustomFeatures(
                                            chr, AttributeDefinitions.TagFeat, feat.Features);
                                    }
                                    : null
                            );
                        }

                        if (changed)
                        {
                            hero.GrantPowers();
                        }
                    }

                    if (ch == _selectedCharacter && _selectedToggle == ToggleChoice.Invocations)
                    {
                        var available = BlueprintDisplay.GetBlueprints()?.OfType<InvocationDefinition>();
                        //.Where(x => x is not InvocationDefinitionCustom);

                        if (available != null)
                        {
                            Browser<RulesetCharacterHero, InvocationDefinition, InvocationDefinition>.OnGUI(
                                _selectedToggle.ToString(), ref changed,
                                hero,
                                hero.TrainedInvocations,
                                available,
                                item => item,
                                def => def.Name,
                                def => def.Name,
                                def => def.FormatDescription(),
                                null,
                                null,
                                null,
                                null,
                                (chr, def) => !chr.TrainedInvocations.Contains(def)
                                    ? () =>
                                    {
                                        chr.TrainInvocations([def]);
                                        LevelUpHelper.RecursiveGrantCustomFeatures(
                                            chr, null, [def.grantedFeature]);
                                    }
                                    : null,
                                (chr, def) => chr.TrainedInvocations.Contains(def)
                                    ? () =>
                                    {
                                        chr.TrainedInvocations.Remove(def);
                                        if (def.grantedFeature is FeatureDefinitionPower power)
                                        {
                                            chr.usablePowers.RemoveAll(x => x.PowerDefinition == power);
                                        }

                                        LevelUpHelper.RecursiveRemoveCustomFeatures(
                                            chr, null, [def.grantedFeature]);
                                    }
                                    : null
                            );
                        }

                        if (changed)
                        {
                            hero.GrantInvocations();
                        }
                    }

                    if (changed)
                    {
                        ch.RefreshAll();
                    }

                    if (_selectedCharacter != GetSelectedCharacter())
                    {
                        _selectedCharacterIndex = GetCharacterList().IndexOf(_selectedCharacter);
                    }
                }
            }
        }
    }

    // replaces known cantrips and spells chosen by selection, keeping each one on its own repertoire's spell list
    private static bool SpellsOnGUI(RulesetCharacterHero hero)
    {
        var changed = false;
        var repertoires = hero.SpellRepertoires
            .Where(r => r.SpellCastingFeature &&
                        (CanReplaceCantrips(r) && r.KnownCantrips.Count > 0 ||
                         CanReplaceSpells(r) && r.KnownSpells.Count > 0))
            .ToList();

        if (repertoires.Count == 0)
        {
            using (HorizontalScope())
            {
                Space(100);
                Label("No known cantrips or spells to replace".Yellow());
            }

            return false;
        }

        foreach (var repertoire in repertoires)
        {
            using (HorizontalScope())
            {
                Space(100);
                Label(GetRepertoireTitle(repertoire).Cyan().Bold(), Width(400f));

                if (CanReplaceSpells(repertoire))
                {
                    Label("Max spell level: ".Green() + $"{repertoire.MaxSpellLevelOfSpellCastingLevel}".Orange());
                }
            }

            var known = new List<SpellDefinition>();

            if (CanReplaceCantrips(repertoire))
            {
                known.AddRange(repertoire.KnownCantrips);
            }

            if (CanReplaceSpells(repertoire))
            {
                known.AddRange(repertoire.KnownSpells);
            }

            foreach (var spell in known.Where(s => s).ToList())
            {
                var isSelected = _spellToReplace.repertoire == repertoire && _spellToReplace.spell == spell;

                using (HorizontalScope())
                {
                    Space(125);
                    Label(FormatSpellLevel(spell), Width(100f));
                    Label(isSelected ? spell.FormatTitle().Orange().Bold() : spell.FormatTitle(), Width(300f));
                    ActionButton(isSelected ? "Cancel" : "Replace", () =>
                    {
                        _spellToReplace = isSelected ? (null, null) : (repertoire, spell);
                        _replacementSearchText = "";
                    }, AutoWidth());
                }

                if (isSelected)
                {
                    changed |= ReplacementOnGUI(hero, repertoire, spell);
                }
            }

            Space(10);
        }

        return changed;
    }

    private static bool ReplacementOnGUI(
        RulesetCharacterHero hero, RulesetSpellRepertoire repertoire, SpellDefinition spell)
    {
        var changed = false;

        using (HorizontalScope())
        {
            Space(150);
            Label("Replace with:".Green(), Width(150f));
            ActionTextField(ref _replacementSearchText, "replacementSearchText", null, null, Width(320));
        }

        var candidates = GetReplacementCandidates(hero, repertoire, spell)
            .Where(s => _replacementSearchText.Length == 0 ||
                        s.FormatTitle().IndexOf(_replacementSearchText, StringComparison.OrdinalIgnoreCase) >= 0)
            .ToList();

        if (candidates.Count == 0)
        {
            using (HorizontalScope())
            {
                Space(150);
                Label("No other spells available".Yellow());
            }
        }

        foreach (var candidate in candidates)
        {
            using (HorizontalScope())
            {
                Space(150);
                Label(FormatSpellLevel(candidate), Width(100f));
                Label(candidate.FormatTitle(), Width(300f));
                ActionButton("Choose", () =>
                {
                    ReplaceSpell(repertoire, spell, candidate);
                    _spellToReplace = (null, null);
                    _replacementSearchText = "";
                    changed = true;
                }, AutoWidth());
            }
        }

        return changed;
    }

    // wizard spellbooks, prepared casters and fixed lists have no chosen spells known other than cantrips
    private static bool CanReplaceSpells(RulesetSpellRepertoire repertoire)
    {
        return repertoire.SpellCastingFeature.SpellKnowledge == RuleDefinitions.SpellKnowledge.Selection;
    }

    private static bool CanReplaceCantrips(RulesetSpellRepertoire repertoire)
    {
        return repertoire.SpellCastingFeature.SpellKnowledge != RuleDefinitions.SpellKnowledge.FixedList;
    }

    private static string FormatSpellLevel(SpellDefinition spell)
    {
        return spell.SpellLevel == 0 ? "Cantrip" : $"Level {spell.SpellLevel}";
    }

    private static string GetRepertoireTitle(RulesetSpellRepertoire repertoire)
    {
        if (repertoire.SpellCastingClass)
        {
            return repertoire.SpellCastingClass.FormatTitle();
        }

        if (repertoire.SpellCastingSubclass)
        {
            return repertoire.SpellCastingSubclass.FormatTitle();
        }

        return repertoire.SpellCastingRace
            ? repertoire.SpellCastingRace.FormatTitle()
            : repertoire.SpellCastingFeature.FormatTitle();
    }

    private static List<SpellDefinition> GetReplacementCandidates(
        RulesetCharacterHero hero, RulesetSpellRepertoire repertoire, SpellDefinition spell)
    {
        var isCantrip = spell.SpellLevel == 0;
        var maxSpellLevel = Math.Max(repertoire.MaxSpellLevelOfSpellCastingLevel, spell.SpellLevel);
        var spellLists = new List<SpellListDefinition> { repertoire.SpellCastingFeature.SpellListDefinition };

        spellLists.AddRange(GetExtendedSpellLists(hero, repertoire));

        return spellLists
            .Where(spellList => spellList)
            .SelectMany(spellList => spellList.SpellsByLevel)
            .SelectMany(spellsByLevel => spellsByLevel.Spells)
            .Where(s => s &&
                        (isCantrip ? s.SpellLevel == 0 : s.SpellLevel > 0 && s.SpellLevel <= maxSpellLevel) &&
                        !IsSpellOnRepertoire(repertoire, s))
            .Distinct()
            .OrderBy(s => s.SpellLevel)
            .ThenBy(s => s.FormatTitle())
            .ToList();
    }

    private static bool IsSpellOnRepertoire(RulesetSpellRepertoire repertoire, SpellDefinition spell)
    {
        return repertoire.KnownCantrips.Contains(spell) ||
               repertoire.KnownSpells.Contains(spell) ||
               repertoire.AutoPreparedSpells.Contains(spell) ||
               repertoire.ExtraSpellsByTag.Values.Any(spells => spells.Contains(spell));
    }

    // patron and other expanded spell lists granted by the class or subclass that owns this repertoire
    private static IEnumerable<SpellListDefinition> GetExtendedSpellLists(
        RulesetCharacterHero hero, RulesetSpellRepertoire repertoire)
    {
        var characterClass = repertoire.SpellCastingClass;

        if (!characterClass && repertoire.SpellCastingSubclass)
        {
            characterClass = hero.ClassesAndSubclasses
                .FirstOrDefault(x => x.Value == repertoire.SpellCastingSubclass).Key;
        }

        if (!characterClass || !hero.ClassesAndLevels.TryGetValue(characterClass, out var classLevel))
        {
            return [];
        }

        hero.ClassesAndSubclasses.TryGetValue(characterClass, out var subclass);

        var features = new List<FeatureDefinition>();

        for (var level = 1; level <= classLevel; level++)
        {
            if (hero.ActiveFeatures.TryGetValue(AttributeDefinitions.GetClassTag(characterClass, level),
                    out var classFeatures))
            {
                features.AddRange(classFeatures);
            }

            if (subclass &&
                hero.ActiveFeatures.TryGetValue(AttributeDefinitions.GetSubclassTag(characterClass, level, subclass),
                    out var subclassFeatures))
            {
                features.AddRange(subclassFeatures);
            }
        }

        return GetExtendedSpellLists(features);
    }

    private static IEnumerable<SpellListDefinition> GetExtendedSpellLists(IEnumerable<FeatureDefinition> features)
    {
        foreach (var feature in features)
        {
            switch (feature)
            {
                case FeatureDefinitionMagicAffinity magicAffinity when magicAffinity.ExtendedSpellList:
                    yield return magicAffinity.ExtendedSpellList;
                    break;
                case FeatureDefinitionFeatureSet { uniqueChoices: false } featureSet:
                    foreach (var spellList in GetExtendedSpellLists(featureSet.FeatureSet))
                    {
                        yield return spellList;
                    }

                    break;
            }
        }
    }

    private static void ReplaceSpell(
        RulesetSpellRepertoire repertoire, SpellDefinition oldSpell, SpellDefinition newSpell)
    {
        var spells = oldSpell.SpellLevel == 0 ? repertoire.KnownCantrips : repertoire.KnownSpells;
        var index = spells.IndexOf(oldSpell);

        if (index < 0)
        {
            return;
        }

        spells[index] = newSpell;
        spells.Sort(repertoire);
        repertoire.PreparedSpells.Remove(oldSpell);
        repertoire.RepertoireRefreshed?.Invoke(repertoire);
    }

    private static void OnDetailToggle(ToggleChoice choice, RulesetCharacter ch)
    {
        var show = ch == _selectedCharacter && _selectedToggle == choice;

        if (!DisclosureToggle(choice.ToString(), ref show, 125))
        {
            return;
        }

        _spellToReplace = (null, null);
        _replacementSearchText = "";

        if (show)
        {
            _selectedCharacter = ch;
            _selectedToggle = choice;
        }
        else
        {
            _selectedToggle = ToggleChoice.None;
        }
    }

    private static List<RulesetCharacter> GetCharacterList()
    {
#pragma warning disable IDE0031
        // don't use ? or ?? or a type deriving from an UnityEngine.Object to avoid bypassing lifetime check
        var chars = !Gui.GameCampaign
            ? null
            : Gui.GameCampaign.Party?.CharactersList?.Select(ch => ch.RulesetCharacter).ToList();
#if DEBUG
        if (chars != null)
        {
            return chars;
        }

        chars = CharacterPool;
#endif
#pragma warning restore IDE0031

        return chars;
    }

    private static RulesetCharacter GetSelectedCharacter()
    {
        var characterList = GetCharacterList();

        if (characterList == null || characterList.Count == 0)
        {
            return null;
        }

        if (_selectedCharacterIndex >= characterList.Count || _selectedCharacterIndex < 0)
        {
            _selectedCharacterIndex = 0;
        }

        return characterList[_selectedCharacterIndex];
    }

#if DEBUG
    private static void RefreshPool()
    {
        _characterPool = [];
        PoolService.EnumeratePool();

        foreach (var filename in PoolService.Pool.Select(item => item.Key))
        {
            // ReSharper disable once InvocationIsSkipped
            Main.Log("Loading: " + filename);
            PoolService.LoadCharacter(filename, out var h, out _);
#if false
                        Mod.Debug(h.Name + " " + h);
                        PropertyInfo[] infos = h.GetType().GetProperties();
                        Mod.Debug("" +  infos);
                        foreach (PropertyInfo info in infos)
                        {
                            Mod.Debug(String.Format("    {0} : {1}", info.Name, info.GetValue(h, null)?.ToString()) ?? "null");
                        }
#endif
            _characterPool.Add(h);
        }

        // ReSharper disable once InvocationIsSkipped
        Main.Log($"{_characterPool.Count} Characters Loaded");
    }
#endif

    private enum ToggleChoice
    {
        Skills,
        Feats,
        Invocations,
        Spells,

        // Classes,
        Stats,
        None
    }

#if DEBUG
    private static List<RulesetCharacter> _characterPool;

    private static List<RulesetCharacter> CharacterPool
    {
        get
        {
            if (_characterPool == null)
            {
                RefreshPool();
            }

            return _characterPool;
        }
        // set => _characterPool = value;
    }
#endif
}
