using MHServerEmu.Core.Logging;
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
        private static readonly Logger Logger = LogManager.CreateLogger();

        private Dictionary<PrototypeId, List<PrototypeId>> _omegaBonusPostreqDict = new();

        public OmegaBonusPostreqsTable()
        {
            AdvancementGlobalsPrototype advGlobalsProto = GameDatabase.AdvancementGlobalsPrototype;

            foreach (var omegaBonusSetRef in advGlobalsProto.OmegaBonusSets)
            {
                var omegaBonusSetProto = omegaBonusSetRef.As<OmegaBonusSetPrototype>();

                foreach (var omegaBonusRef in omegaBonusSetProto.OmegaBonuses)
                {
                    var omegaBonusProto = omegaBonusRef.As<OmegaBonusPrototype>();

                    foreach (var omegaBonusPrereqRef in omegaBonusProto.Prerequisites)
                    {
                        if (_omegaBonusPostreqDict.TryGetValue(omegaBonusPrereqRef, out var omegaBonusPrereqList) == false)
                        {
                            omegaBonusPrereqList = new();
                            _omegaBonusPostreqDict.Add(omegaBonusPrereqRef, omegaBonusPrereqList);
                        }

                        omegaBonusPrereqList.Add(omegaBonusProto.DataRef);
                    }
                }
            }
        }

        public bool CanOmegaBonusBeRemoved(PrototypeId omegaBonusRef, Avatar avatar, bool checkTempPoints)
        {
            if (omegaBonusRef == PrototypeId.Invalid)
                return Logger.WarnReturn(false, "CanOmegaBonusBeRemoved(): omegaBonusRef == PrototypeId.Invalid");

            // If there is no postreq list for this bonus, there is nothing to check
            if (_omegaBonusPostreqDict.TryGetValue(omegaBonusRef, out List<PrototypeId> postreqList) == false)
                return true;

            // Track all nodes we have already checked in a set
            HashSet<PrototypeId> checkedNodes = new();
            foreach (var postreqRef in postreqList)
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

            var omegaBonusProto = omegaBonusRef.As<OmegaBonusPrototype>();
            if (omegaBonusProto == null)
                return Logger.WarnReturn(false, "IsOmegaBonusConnectedToStartingNode(): omegaBonusProto == null");

            // No prereqs = starting node
            if (omegaBonusProto.Prerequisites == null || omegaBonusProto.Prerequisites.Length == 0)
                return true;

            // Do the check recursively
            foreach (var prereqRef in omegaBonusProto.Prerequisites)
            {
                if (IsOmegaBonusConnectedToStartingNode(prereqRef, avatar, checkTempPoints, checkedNodes))
                    return true;
            }

            // Not connected to a starting node
            return false;
        }
    }
}
