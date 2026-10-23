namespace SolastaUnfinishedBusiness.Behaviors;

// limits auto prepared spells to the ones of a level the character has spell slots for
public class AutoPreparedSpellsUpToMaxSpellSlotLevel
{
    private AutoPreparedSpellsUpToMaxSpellSlotLevel()
    {
    }

    public static AutoPreparedSpellsUpToMaxSpellSlotLevel Marker { get; } = new();
}
