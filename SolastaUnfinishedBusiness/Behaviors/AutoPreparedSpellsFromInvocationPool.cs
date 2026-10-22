namespace SolastaUnfinishedBusiness.Behaviors;

// adds the spells granted by the invocations known from a custom pool as auto prepared spells so they can also be
// cast with spell slots
internal class AutoPreparedSpellsFromInvocationPool(InvocationPoolTypeCustom poolType)
{
    internal InvocationPoolTypeCustom PoolType { get; } = poolType;
}
