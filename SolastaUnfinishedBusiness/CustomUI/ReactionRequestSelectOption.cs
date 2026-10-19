using System.Collections.Generic;
using System.Linq;

namespace SolastaUnfinishedBusiness.CustomUI;

// a single prompt listing text options, the chosen one is read from SelectedSubOption once validated
internal sealed class ReactionRequestSelectOption : ReactionRequest
{
    internal const string Name = "CustomSelectOption";

    private readonly string _type;
    private int _selectedOption = -1;

    internal ReactionRequestSelectOption(
        string type,
        CharacterActionParams reactionParams,
        List<(string label, bool available)> options,
        int defaultOption = 0)
        : base(Name, reactionParams)
    {
        _type = type;
        Labels = options.Select(x => x.label).ToList();
        BuildSuboptions(options, defaultOption);
    }

    internal List<string> Labels { get; }

    public override int SelectedSubOption => _selectedOption;

    public override string SuboptionTag => _type;

    private void BuildSuboptions(List<(string label, bool available)> options, int defaultOption)
    {
        SubOptionsAvailability.Clear();

        reactionParams.SpellRepertoire = new RulesetSpellRepertoire();

        for (var index = 0; index < options.Count; index++)
        {
            SubOptionsAvailability.Add(index, options[index].available);
        }

        if (SubOptionsAvailability.TryGetValue(defaultOption, out var available) && available)
        {
            SelectSubOption(defaultOption);
            return;
        }

        foreach (var pair in SubOptionsAvailability.Where(pair => pair.Value))
        {
            SelectSubOption(pair.Key);
            break;
        }
    }

    public override void SelectSubOption(int option)
    {
        _selectedOption = option;
    }

    public override string FormatTitle()
    {
        return Gui.Localize($"Reaction/&CustomReaction{_type}Title");
    }

    public override string FormatDescription()
    {
        return ReactionParams.StringParameter;
    }

    public override string FormatReactTitle()
    {
        return Gui.Localize($"Reaction/&CustomReaction{_type}ReactTitle");
    }

    public override string FormatReactDescription()
    {
        return Gui.Localize($"Reaction/&CustomReaction{_type}ReactDescription");
    }
}
