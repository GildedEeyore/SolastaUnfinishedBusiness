using System;
using System.Collections.Generic;

namespace SolastaUnfinishedBusiness.Behaviors;

// feats can't grant features at later levels, so this gives the custom invocation pools of a feat on each level up:
// all the known invocations on the level the feat is taken, then the new ones and one replacement on later levels
internal class CustomInvocationPoolsByCharacterLevel(
    Func<int, int> knownByCharacterLevel,
    FeatureDefinitionCustomInvocationPool[] learnPoolsByPoints,
    FeatureDefinitionCustomInvocationPool unlearnPool)
{
    internal IEnumerable<FeatureDefinitionCustomInvocationPool> GetPools(int characterLevel, bool isNewFeat)
    {
        var known = knownByCharacterLevel(characterLevel);
        var learn = isNewFeat ? known : known - knownByCharacterLevel(characterLevel - 1);

        if (learn > 0)
        {
            yield return learnPoolsByPoints[Math.Min(learn, learnPoolsByPoints.Length) - 1];
        }

        if (!isNewFeat)
        {
            yield return unlearnPool;
        }
    }
}
