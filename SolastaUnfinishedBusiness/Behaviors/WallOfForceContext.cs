using System.Collections.Generic;
using TA;
using UnityEngine;
using static RuleDefinitions;

namespace SolastaUnfinishedBusiness.Behaviors;

// Wall of Force is the only effect with a MoveBlocker topology form, so any cell flagged as a dynamic move blocker
// is part of a wall of force (see GameLocationEnvironmentManagerPatcher.TopologyChangeInProgress_Patch)
internal static class WallOfForceContext
{
    private static List<RaycastGridHit> _hits = [];

    internal static bool IsMoveBlocker(EffectDescription effectDescription)
    {
        return effectDescription.EffectForms.Exists(x =>
            x.FormType == EffectForm.EffectFormType.Topology &&
            x.TopologyForm.ChangeType == TopologyForm.Type.MoveBlocker);
    }

    private static bool IsAnyWallActive()
    {
        if (ServiceRepository.GetService<IGameLocationEnvironmentService>()
            is not GameLocationEnvironmentManager environmentManager)
        {
            return false;
        }

        foreach (var rulesetEffect in environmentManager.topologyTrackersByEffect.Keys)
        {
            if (IsMoveBlocker(rulesetEffect.EffectDescription))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsLineBlocked(Vector3 origin, Vector3 destination)
    {
        var vector = destination - origin;

        if (vector.sqrMagnitude == 0 || !IsAnyWallActive())
        {
            return false;
        }

        var positioningService = ServiceRepository.GetService<IGameLocationPositioningService>();

        _hits.Clear();

        return positioningService.RaycastGridCellFlagsAll(
            new Ray(origin, vector.normalized), ref _hits, vector.magnitude,
            (int)CellFlags.Runtime.DynamicMoveBlocker, typeof(CellFlags.Runtime), true, false);
    }

    internal static bool IsLineBlocked(GameLocationCharacter source, GameLocationCharacter target)
    {
        // the wall's own proxy sits inside the wall and must stay targetable by Disintegrate
        if (source == null || target == null || target.RulesetActor is RulesetCharacterEffectProxy)
        {
            return false;
        }

        var positioningService = ServiceRepository.GetService<IGameLocationPositioningService>();

        return IsLineBlocked(
            positioningService.ComputeGravityCenterPosition(source),
            positioningService.ComputeGravityCenterPosition(target));
    }

    internal static bool IsLineBlocked(int3 sourcePosition, int3 targetPosition, GameLocationCharacter target)
    {
        // the wall's own proxy sits inside the wall and must stay targetable by Disintegrate
        if (target?.RulesetActor is RulesetCharacterEffectProxy)
        {
            return false;
        }

        var positioningService = ServiceRepository.GetService<IGameLocationPositioningService>();

        return IsLineBlocked(
            positioningService.GetWorldPositionFromGridPosition(sourcePosition),
            positioningService.GetWorldPositionFromGridPosition(targetPosition));
    }

    internal static void RemoveTargetsBehindWall(
        GameLocationCharacter sourceCharacter,
        EffectDescription effectDescription,
        Vector3 origin,
        List<GameLocationCharacter> affectedCharacters)
    {
        if (effectDescription == null ||
            affectedCharacters.Count == 0 ||
            effectDescription.TargetType is TargetType.WallLine or TargetType.WallRing ||
            IsMoveBlocker(effectDescription) ||
            !IsAnyWallActive())
        {
            return;
        }

        var positioningService = ServiceRepository.GetService<IGameLocationPositioningService>();

        // an area can't be centered on the other side of a wall from its caster
        if (sourceCharacter != null &&
            effectDescription.RangeType == RangeType.Distance &&
            IsLineBlocked(positioningService.ComputeGravityCenterPosition(sourceCharacter), origin))
        {
            affectedCharacters.Clear();

            return;
        }

        // an area doesn't spread through a wall
        affectedCharacters.RemoveAll(x =>
            x.RulesetActor is not RulesetCharacterEffectProxy &&
            IsLineBlocked(origin, positioningService.ComputeGravityCenterPosition(x)));
    }
}
