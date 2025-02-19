using MHServerEmu.Core.Logging;
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
        private static readonly Logger Logger = LogManager.CreateLogger();

        private Dictionary<PrototypeId, List<PrototypeId>> _infinityGemBonusPostreqDict = new();

        public InfinityGemBonusPostreqsTable()
        {
            AdvancementGlobalsPrototype advGlobalsProto = GameDatabase.AdvancementGlobalsPrototype;

            foreach (var infinityGemSetRef in advGlobalsProto.InfinityGemSets)
            {
                var infinityGemSetProto = infinityGemSetRef.As<InfinityGemSetPrototype>();

                foreach (var gemBonusRef in infinityGemSetProto.Bonuses)
                {
                    var gemBonusProto = gemBonusRef.As<InfinityGemBonusPrototype>();

                    foreach (var gemBonusPrereqRef in gemBonusProto.Prerequisites)
                    {
                        if (_infinityGemBonusPostreqDict.TryGetValue(gemBonusPrereqRef, out var gemBonusPrereqList) == false)
                        {
                            gemBonusPrereqList = new();
                            _infinityGemBonusPostreqDict.Add(gemBonusPrereqRef, gemBonusPrereqList);
                        }

                        gemBonusPrereqList.Add(gemBonusProto.DataRef);
                    }
                }

            }
        }

        public bool CanInfinityGemBonusBeRemoved(PrototypeId infinityGemBonusRef, Avatar avatar, bool checkTempPoints)
        {
            if (infinityGemBonusRef == PrototypeId.Invalid)
                return Logger.WarnReturn(false, "CanInfinityGemBonusBeRemoved(): infinityGemBonusRef == PrototypeId.Invalid");

            // If there is no postreq list for this bonus, there is nothing to check
            if (_infinityGemBonusPostreqDict.TryGetValue(infinityGemBonusRef, out List<PrototypeId> postreqList) == false)
                return true;

            // Track all nodes we have already checked in a set
            HashSet<PrototypeId> checkedNodes = new();
            foreach (var postreqRef in postreqList)
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

            var gemBonusProto = infinityGemBonusRef.As<InfinityGemBonusPrototype>();
            if (gemBonusProto == null)
                return Logger.WarnReturn(false, "IsInfinityGemBonusConnectedToStartingNode(): gemBonusProto == null");

            // No prereqs = starting node
            if (gemBonusProto.Prerequisites == null || gemBonusProto.Prerequisites.Length == 0)
                return true;

            // Do the check recursively
            foreach (var prereqRef in gemBonusProto.Prerequisites)
            {
                if (IsInfinityGemBonusConnectedToStartingNode(prereqRef, avatar, checkTempPoints, checkedNodes))
                    return true;
            }

            // Not connected to a starting node
            return false;
        }
    }
}
