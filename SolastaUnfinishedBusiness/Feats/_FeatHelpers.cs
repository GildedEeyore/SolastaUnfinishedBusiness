using System.Linq;
using SolastaUnfinishedBusiness.Interfaces;
using static RuleDefinitions;

namespace SolastaUnfinishedBusiness.Feats;

internal static class FeatHelpers
{
    internal sealed class ModifyWeaponAttackModeTypeFilter(
        FeatDefinition source,
        params WeaponTypeDefinition[] weaponTypeDefinition) : IModifyWeaponAttackMode
    {
        private readonly TrendInfo _trendInfo = new(1, FeatureSourceType.Feat, source.Name, source);

        public void ModifyWeaponAttackMode(
            RulesetCharacter character,
            RulesetAttackMode attackMode,
            RulesetItem weapon,
            bool canAddAbilityDamageBonus)
        {
            if (attackMode.SourceDefinition is not ItemDefinition { IsWeapon: true } sourceDefinition ||
                !weaponTypeDefinition.Contains(sourceDefinition.WeaponDescription.WeaponTypeDefinition))
            {
                return;
            }

            attackMode.ToHitBonus += 1;
            attackMode.ToHitBonusTrends.Add(_trendInfo);
        }
    }

    internal sealed class SpellTag
    {
        internal SpellTag(string spellTag, bool forceFixedList = false, params string[] poolTags)
        {
            Name = spellTag;
            ForceFixedList = forceFixedList;
            PoolTags = poolTags.Length > 0 ? poolTags : [spellTag];
        }

        internal string Name { get; }
        internal bool ForceFixedList { get; }

        // extra spells tags of the point pools that learn spells for this repertoire, each must end with Name
        internal string[] PoolTags { get; }
    }
}
