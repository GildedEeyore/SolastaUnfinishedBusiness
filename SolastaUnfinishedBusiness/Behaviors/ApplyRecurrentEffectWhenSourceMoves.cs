namespace SolastaUnfinishedBusiness.Behaviors;

// recurrent effects with OnEnter also apply when the source moves its emanation onto a creature
public class ApplyRecurrentEffectWhenSourceMoves
{
    private ApplyRecurrentEffectWhenSourceMoves()
    {
    }

    public static ApplyRecurrentEffectWhenSourceMoves Mark { get; } = new();
}
