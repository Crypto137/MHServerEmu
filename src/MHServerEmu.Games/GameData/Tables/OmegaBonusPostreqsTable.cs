using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData.Tables
{
    public class OmegaBonusPostreqsTable
    {
        // Prereq - a node that is required to get to another node
        // Postreq - a node that is dependant on this node
        // Starting node - a node without prereqs
        // [Prereq] -> [Postreq]

        private readonly Dictionary<PrototypeId, List<PrototypeId>> _omegaBonusPostreqs = new();

        public OmegaBonusPostreqsTable()
        {
            AdvancementGlobalsPrototype advGlobalsProto = GameDatabase.AdvancementGlobalsPrototype;
            if (!Verify.IsNotNull(advGlobalsProto)) return;

            foreach (OmegaBonusSetPrototype omegaBonusSetProto in advGlobalsProto.OmegaBonusSets)
            {
                foreach (OmegaBonusPrototype omegaBonusProto in omegaBonusSetProto.OmegaBonuses)
                {
                    foreach (PrototypeId omegaBonusPrereqRef in omegaBonusProto.Prerequisites)
                    {
                        if (_omegaBonusPostreqs.TryGetValue(omegaBonusPrereqRef, out List<PrototypeId> omegaBonusPostreqs) == false)
                        {
                            omegaBonusPostreqs = new();
                            _omegaBonusPostreqs.Add(omegaBonusPrereqRef, omegaBonusPostreqs);
                        }

                        omegaBonusPostreqs.Add(omegaBonusProto.DataRef);
                    }
                }
            }
        }

        public bool CanOmegaBonusBeRemoved(PrototypeId omegaBonusRef, Avatar avatar, bool checkTempPoints)
        {
            if (!Verify.IsTrue(omegaBonusRef != PrototypeId.Invalid)) return false;

            // If there is no postreq list for this bonus, there is nothing to check
            if (_omegaBonusPostreqs.TryGetValue(omegaBonusRef, out List<PrototypeId> postreqs) == false)
                return true;

            // Track all nodes we have already checked in a set
            using var checkedNodesHandle = HashSetPool<PrototypeId>.Instance.Get(out HashSet<PrototypeId> checkedNodes);
            foreach (PrototypeId postreqRef in postreqs)
            {
                checkedNodes.Add(omegaBonusRef);

                // Skip nodes that don't have any points spent on them
                if (avatar.GetOmegaPointsSpentOnBonus(postreqRef, checkTempPoints) <= 0)
                    continue;

                if (IsOmegaBonusConnectedToStartingNode(postreqRef, avatar, checkTempPoints, checkedNodes) == false)
                    return false;

                checkedNodes.Clear();
            }

            return true;
        }

        private bool IsOmegaBonusConnectedToStartingNode(PrototypeId omegaBonusRef, Avatar avatar, bool checkTempPoints, HashSet<PrototypeId> checkedNodes)
        {
            // Skip if we have already checked this node
            if (checkedNodes.Add(omegaBonusRef) == false)
                return false;

            // Skip nodes that don't have any points spent on them
            if (avatar.GetOmegaPointsSpentOnBonus(omegaBonusRef, checkTempPoints) <= 0)
                return false;

            OmegaBonusPrototype omegaBonusProto = omegaBonusRef.As<OmegaBonusPrototype>();
            if (!Verify.IsNotNull(omegaBonusProto)) return false;

            // No prereqs = starting node
            if (omegaBonusProto.Prerequisites == null || omegaBonusProto.Prerequisites.Length == 0)
                return true;

            // Do the check recursively
            foreach (PrototypeId prereqRef in omegaBonusProto.Prerequisites)
            {
                if (IsOmegaBonusConnectedToStartingNode(prereqRef, avatar, checkTempPoints, checkedNodes))
                    return true;
            }

            // Not connected to a starting node
            return false;
        }
    }
}
