using static RuleDefinitions;

namespace SolastaUnfinishedBusiness.Behaviors;

// the owner subtracts a die from each of its damage rolls (Grave Ground)
public class SubtractDieFromDamageRolls(DieType dieType)
{
    public DieType DieType { get; } = dieType;
}
