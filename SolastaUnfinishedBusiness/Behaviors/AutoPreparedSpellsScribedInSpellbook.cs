namespace SolastaUnfinishedBusiness.Behaviors;

// also scribes the auto prepared spells in the spellbook of repertoires that learn spells from one, like wizards
public class AutoPreparedSpellsScribedInSpellbook
{
    private AutoPreparedSpellsScribedInSpellbook()
    {
    }

    public static AutoPreparedSpellsScribedInSpellbook Marker { get; } = new();
}
