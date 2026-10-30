using System.Linq;
using static RuleDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper.SpellDefinitions;

namespace SolastaUnfinishedBusiness.Models;

// casts Guidance on a hero right before they roll a skill check out of combat
internal static class AutoGuidanceContext
{
    private const string ConditionGuided = "ConditionGuided";

    internal static void TryCastGuidanceBeforeAbilityCheck(
        GameLocationCharacter roller, string proficiencyName, bool passive)
    {
        if (!Main.Settings.EnableAutoGuidanceOnSkillChecks ||
            passive ||
            string.IsNullOrEmpty(proficiencyName) ||
            Gui.Battle != null ||
            roller?.RulesetCharacter is not { IsDeadOrDyingOrUnconscious: false } rulesetTarget)
        {
            return;
        }

        var partyCharacters = ServiceRepository.GetService<IGameLocationCharacterService>()?.PartyCharacters;

        if (partyCharacters == null || !partyCharacters.Contains(roller))
        {
            return;
        }

        // 2024 Guidance is a bundle with one sub spell per skill, vanilla Guidance helps any check
        var spell = Guidance;

        if (Guidance.SpellsBundle)
        {
            spell = Guidance.SubspellsList.FirstOrDefault(x => x.Name == $"Guidance{proficiencyName}");

            if (!spell)
            {
                return;
            }
        }

        // don't stack Guidance on a hero who already has it for this check
        if (rulesetTarget.HasConditionOfType(ConditionGuided) ||
            rulesetTarget.HasConditionOfType($"ConditionGuidance{proficiencyName}"))
        {
            return;
        }

        // prefer casters who aren't concentrating, so no one's Guidance on another hero gets moved
        RulesetCharacter caster = null;
        RulesetSpellRepertoire repertoire = null;

        foreach (var candidate in partyCharacters
                     .Select(x => x.RulesetCharacter)
                     .Where(x => x is { IsDeadOrDyingOrUnconscious: false, IsIncapacitated: false })
                     .Where(x => x.ConcentratedSpell == null || IsGuidance(x.ConcentratedSpell.SpellDefinition))
                     .OrderBy(x => x.ConcentratedSpell != null))
        {
            if (!candidate.CanCastSpell(Guidance, false, out var candidateRepertoire) ||
                !candidate.AreSpellComponentsValid(Guidance))
            {
                continue;
            }

            caster = candidate;
            repertoire = candidateRepertoire;
            break;
        }

        if (caster == null)
        {
            return;
        }

        // same steps the game takes when casting a spell, without the action and its animation
        var implementationService = ServiceRepository.GetService<IRulesetImplementationService>();
        var effectSpell = implementationService.InstantiateEffectSpell(caster, repertoire, spell, 0, false);

        caster.CastSpell(effectSpell, caster == rulesetTarget);

        // only a Guidance cast can be dropped here as other casters were filtered out above
        if (caster.ConcentratedSpell != null)
        {
            caster.TerminateSpell(caster.ConcentratedSpell);
        }

        caster.AddActiveSpell(effectSpell);

        var formsParams = new RulesetImplementationDefinitions.ApplyFormsParams();

        formsParams.FillSourceAndTarget(caster, rulesetTarget);
        formsParams.FillFromActiveEffect(effectSpell);
        formsParams.FillSpecialParameters(false, 0, 0, 0, effectSpell.EffectLevel,
            new ActionModifier(), RollOutcome.Neutral, 0, false, 0, 1, null);
        formsParams.effectSourceType = effectSpell.EffectSourceType;

        implementationService.ApplyEffectForms(
            effectSpell.EffectDescription.EffectForms,
            formsParams,
            null,
            out _,
            out _,
            effectApplication: effectSpell.EffectDescription.EffectApplication,
            filters: effectSpell.EffectDescription.EffectFormFilters);

        if (effectSpell.IsTrackingAnything())
        {
            caster.ConcentrateOnSpell(effectSpell);
        }
    }

    private static bool IsGuidance(SpellDefinition spellDefinition)
    {
        return spellDefinition == Guidance || Guidance.SubspellsList.Contains(spellDefinition);
    }
}
