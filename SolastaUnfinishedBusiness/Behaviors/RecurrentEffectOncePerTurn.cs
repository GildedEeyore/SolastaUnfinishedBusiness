using System.Collections.Generic;

namespace SolastaUnfinishedBusiness.Behaviors;

// recurrent effects apply to the same creature at most once per turn
public class RecurrentEffectOncePerTurn
{
    private static readonly HashSet<(ulong, ulong)> AppliedThisTurn = [];
    private static (int, ulong) _currentTurn;

    private RecurrentEffectOncePerTurn()
    {
    }

    public static RecurrentEffectOncePerTurn Mark { get; } = new();

    internal static bool TryRegisterApplication(RulesetEffect effect, RulesetActor target)
    {
        var battle = Gui.Battle;

        if (battle == null)
        {
            return true;
        }

        var turn = (battle.CurrentRound, battle.ActiveContenderIgnoringLegendary?.Guid ?? 0);

        if (turn != _currentTurn)
        {
            _currentTurn = turn;
            AppliedThisTurn.Clear();
        }

        return AppliedThisTurn.Add((effect.Guid, target.Guid));
    }
}
