using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData.Tables
{
    public class InfinityGemBonusPostreqsTable
    {
        // NOTE: This whole table appears to have been copy-pasted from Omega, none of the infinity nodes actually have any prereqs

        // Prereq - a node that is required to get to another node
        // Postreq - a node that is dependant on this node
        // Starting node - a node without prereqs
        // [Prereq] -> [Postreq]

        private readonly Dictionary<PrototypeId, List<PrototypeId>> _infinityGemBonusPostreqs = new();

        public InfinityGemBonusPostreqsTable()
        {
            AdvancementGlobalsPrototype advGlobalsProto = GameDatabase.AdvancementGlobalsPrototype;
            if (!Verify.IsNotNull(advGlobalsProto)) return;

            foreach (InfinityGemSetPrototype infinityGemSetProto in advGlobalsProto.InfinityGemSets)
            {
                foreach (InfinityGemBonusPrototype gemBonusProto in infinityGemSetProto.Bonuses)
                {
                    foreach (PrototypeId gemBonusPrereqRef in gemBonusProto.Prerequisites)
                    {
                        if (_infinityGemBonusPostreqs.TryGetValue(gemBonusPrereqRef, out List<PrototypeId> gemBonusPostreqs) == false)
                        {
                            gemBonusPostreqs = new();
                            _infinityGemBonusPostreqs.Add(gemBonusPrereqRef, gemBonusPostreqs);
                        }

                        gemBonusPostreqs.Add(gemBonusProto.DataRef);
                    }
                }
            }
        }

        public bool CanInfinityGemBonusBeRemoved(PrototypeId infinityGemBonusRef, Avatar avatar, bool checkTempPoints)
        {
            if (!Verify.IsTrue(infinityGemBonusRef != PrototypeId.Invalid)) return false;

            // If there is no postreq list for this bonus, there is nothing to check
            if (_infinityGemBonusPostreqs.TryGetValue(infinityGemBonusRef, out List<PrototypeId> postreqs) == false)
                return true;

            // Track all nodes we have already checked in a set
            using var checkedNodesHandle = HashSetPool<PrototypeId>.Instance.Get(out HashSet<PrototypeId> checkedNodes);
            foreach (PrototypeId postreqRef in postreqs)
            {
                checkedNodes.Add(infinityGemBonusRef);

                // Skip nodes that don't have any points spent on them
                if (avatar.GetInfinityPointsSpentOnBonus(postreqRef, checkTempPoints) <= 0)
                    continue;

                if (IsInfinityGemBonusConnectedToStartingNode(postreqRef, avatar, checkTempPoints, checkedNodes) == false)
                    return false;

                checkedNodes.Clear();
            }

            return true;
        }

        private bool IsInfinityGemBonusConnectedToStartingNode(PrototypeId infinityGemBonusRef, Avatar avatar, bool checkTempPoints, HashSet<PrototypeId> checkedNodes)
        {
            // Skip if we have already checked this node
            if (checkedNodes.Add(infinityGemBonusRef) == false)
                return false;

            // Skip nodes that don't have any points spent on them
            if (avatar.GetInfinityPointsSpentOnBonus(infinityGemBonusRef, checkTempPoints) <= 0)
                return false;

            InfinityGemBonusPrototype gemBonusProto = infinityGemBonusRef.As<InfinityGemBonusPrototype>();
            if (!Verify.IsNotNull(gemBonusProto)) return false;

            // No prereqs = starting node
            if (gemBonusProto.Prerequisites == null || gemBonusProto.Prerequisites.Length == 0)
                return true;

            // Do the check recursively
            foreach (PrototypeId prereqRef in gemBonusProto.Prerequisites)
            {
                if (IsInfinityGemBonusConnectedToStartingNode(prereqRef, avatar, checkTempPoints, checkedNodes))
                    return true;
            }

            // Not connected to a starting node
            return false;
        }
    }
}
