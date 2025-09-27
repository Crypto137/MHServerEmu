using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Tables;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Entities.Avatars
{
    public partial class Avatar
    {
        #region Omega

        public bool IsOmegaSystemUnlocked()
        {
            // Omega is unlocked per-avatar at level 30
            AdvancementGlobalsPrototype advGlobals = GameDatabase.AdvancementGlobalsPrototype;
            if (advGlobals == null) return Logger.WarnReturn(false, "IsOmegaSystemUnlocked(): advGlobals == null");

            return CharacterLevel >= advGlobals.OmegaSystemLevelUnlock;
        }

        public int GetOmegaPointsSpentOnBonus(PrototypeId omegaBonusRef, bool getTempPoints)
        {
            if (getTempPoints)
            {
                int pointsSpent = Properties[PropertyEnum.OmegaSpecTemp, 0, omegaBonusRef];
                if (pointsSpent >= 0)
                    return pointsSpent;
            }

            return Properties[PropertyEnum.OmegaSpec, 0, omegaBonusRef];
        }

        public static int GetOmegaRankForPointCost(PrototypeId omegaBonusProtoRef, long points, out long remainder)
        {
            remainder = 0;

            OmegaBonusPrototype omegaBonusProto = omegaBonusProtoRef.As<OmegaBonusPrototype>();
            if (omegaBonusProto == null) return Logger.WarnReturn(0, "GetOmegaRankForPointCost(): omegaBonusProto == null");

            return ModRankFromPoints(omegaBonusProtoRef, points, out remainder);
        }

        public CanSetOmegaRankResult CanSetOmegaRank(PrototypeId omegaBonusProtoRef, int rank, bool checkTempPoints)
        {
            if (omegaBonusProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(CanSetOmegaRankResult.ErrorGeneric, "CanSetOmegaRank(): omegaBonusProtoRef == PrototypeId.Invalid");
            if (rank < 0) return Logger.WarnReturn(CanSetOmegaRankResult.ErrorGeneric, "CanSetOmegaRank(): rank < 0");

            if (IsOmegaSystemUnlocked() == false)
                return CanSetOmegaRankResult.ErrorLevelRequirement;

            if (rank > 0)
            {
                if (IsOmegaBonusPrerequisiteRequirementMet(omegaBonusProtoRef, checkTempPoints) == false)
                    return CanSetOmegaRankResult.ErrorPrerequisiteRequirement;
            }
            else
            {
                if (GameDataTables.Instance.OmegaBonusPostreqsTable.CanOmegaBonusBeRemoved(omegaBonusProtoRef, this, checkTempPoints) == false)
                    return CanSetOmegaRankResult.ErrorCannotRemove;
            }

            return CanSetOmegaRankResult.Success;
        }

        public bool IsOmegaBonusPrerequisiteRequirementMet(PrototypeId omegaBonusProtoRef, bool checkTempPoints)
        {
            if (omegaBonusProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "IsOmegaBonusPrerequisiteRequirementMet(): omegaBonusProtoRef == PrototypeId.Invalid");

            OmegaBonusPrototype omegaBonusProto = omegaBonusProtoRef.As<OmegaBonusPrototype>();
            if (omegaBonusProto == null) return Logger.WarnReturn(false, "IsOmegaBonusPrerequisiteRequirementMet(): omegaBonusProto == null");

            if (omegaBonusProto.Prerequisites.IsNullOrEmpty())
                return true;

            foreach (PrototypeId prereqBonusProtoRef in omegaBonusProto.Prerequisites)
            {
                // Any of the prereq bonuses is enough to satisfy this
                if (GetOmegaPointsSpentOnBonus(prereqBonusProtoRef, checkTempPoints) > 0)
                    return true;
            }

            return false;
        }

        public void OmegaPointAllocationCommit(NetMessageOmegaBonusAllocationCommit commitMessage)
        {
            Player player = GetOwnerOfType<Player>();
            if (player == null)
            {
                Logger.Warn("OmegaPointAllocationCommit(): player == null");
                return;
            }

            if (OmegaPointAllocationClearTemporary())
                Logger.Warn($"OmegaPointAllocationCommit(): [{this}] already had a pending allocation");

            Dictionary<PropertyId, PropertyValue> setDict = DictionaryPool<PropertyId, PropertyValue>.Instance.Get();

            // Set temp properties received from the client
            long pointsSpent = 0;

            for (int i = 0; i < commitMessage.AllocationsCount; i++)
            {
                NetMessageSelectOmegaBonus allocation = commitMessage.AllocationsList[i];

                PrototypeId omegaBonusProtoRef = (PrototypeId)allocation.OmegaBonusProtoRefID;

                // Get the prototype for validation
                OmegaBonusPrototype omegaBonusProto = omegaBonusProtoRef.As<OmegaBonusPrototype>();
                if (omegaBonusProto == null)
                {
                    Logger.Warn("OmegaPointAllocationCommit(): omegaBonusProto == null");
                    goto end;
                }

                Properties[PropertyEnum.OmegaSpecTemp, 0, omegaBonusProtoRef] = allocation.Points;
                pointsSpent += GetOmegaPointsSpentOnBonus(omegaBonusProtoRef, true);
            }

            // Validate the spent number of points
            long omegaPoints = player.GetOmegaPoints();
            if (pointsSpent > omegaPoints)
            {
                Logger.Warn($"OmegaPointAllocationCommit(): Number of points spent [{pointsSpent}] exceeds the total available number [{omegaPoints}] for [{this}]");
                goto end;
            }

            // Calculate rank for each bonus
            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.OmegaSpecTemp))
            {
                Property.FromParam(kvp.Key, 1, out PrototypeId omegaBonusProtoRef);

                // The number of points received from the client should not have a remainder
                int rank = GetOmegaRankForPointCost(omegaBonusProtoRef, kvp.Value, out long remainder);
                if (remainder != 0)
                {
                    Logger.Warn("OmegaPointAllocationCommit(): remainder != 0");
                    goto end;
                }

                // Validate the rank
                if (CanSetOmegaRank(omegaBonusProtoRef, rank, true) != CanSetOmegaRankResult.Success)
                {
                    Logger.Warn($"OmegaPointAllocationCommit(): Rank validation failed for Omega bonus [{omegaBonusProtoRef.GetName()} on [{this}]");
                    goto end;
                }

                setDict[new(PropertyEnum.OmegaRankTemp, omegaBonusProtoRef)] = rank;
            }

            foreach (var kvp in setDict)
                Properties[kvp.Key] = kvp.Value;

            // Commit temporary allocation
            setDict.Clear();

            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.OmegaSpecTemp))
            {
                Property.FromParam(kvp.Key, 1, out PrototypeId omegaBonusProtoRef);

                setDict[new(PropertyEnum.OmegaSpec, 0, omegaBonusProtoRef)] = kvp.Value;
                setDict[new(PropertyEnum.OmegaRank, omegaBonusProtoRef)] = Properties[PropertyEnum.OmegaRankTemp, omegaBonusProtoRef];
            }

            foreach (var kvp in setDict)
                Properties[kvp.Key] = kvp.Value;

            Properties[PropertyEnum.OmegaPointsSpent] = pointsSpent;

        // Clean up
        end:
            OmegaPointAllocationClearTemporary();
            DictionaryPool<PropertyId, PropertyValue>.Instance.Return(setDict);
        }

        public void RespecOmegaBonus()
        {
            //PropertyEnum.OmegaRespecResult?
            Properties.RemovePropertyRange(PropertyEnum.OmegaRank);
            Properties.RemovePropertyRange(PropertyEnum.OmegaSpec);
            Properties.RemoveProperty(PropertyEnum.OmegaPointsSpent);
        }

        public void ApplyOmegaBonuses()
        {
            List<(PrototypeId, int)> bonusList = ListPool<(PrototypeId, int)>.Instance.Get();

            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.OmegaRank))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId omegaBonusProtoRef);
                int rank = kvp.Value;
                bonusList.Add((omegaBonusProtoRef, rank));
            }

            foreach (var bonus in bonusList)
                ModChangeModEffects(bonus.Item1, bonus.Item2);

            ListPool<(PrototypeId, int)>.Instance.Return(bonusList);
        }

        private bool OmegaPointAllocationClearTemporary()
        {
            Properties.RemovePropertyRange(PropertyEnum.OmegaRankTemp);
            return Properties.RemovePropertyRange(PropertyEnum.OmegaSpecTemp);
        }

        private void InitializeOmegaBonuses()
        {
            Dictionary<PropertyId, PropertyValue> setDict = DictionaryPool<PropertyId, PropertyValue>.Instance.Get();

            // Omega bonus ranks are not persistent, so they need to be recalculated

            // Calculate rank for each bonus
            long pointsSpent = 0;

            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.OmegaSpec))
            {
                long omegaSpec = kvp.Value;
                Property.FromParam(kvp.Key, 1, out PrototypeId omegaBonusProtoRef);

                int rank = GetOmegaRankForPointCost(omegaBonusProtoRef, kvp.Value, out long remainder);

                // Refund the remainder
                if (remainder != 0)
                {
                    omegaSpec -= remainder;
                    setDict[kvp.Key] = omegaSpec;
                }

                pointsSpent += omegaSpec;
                setDict[new(PropertyEnum.OmegaRank, omegaBonusProtoRef)] = rank;
            }

            foreach (var kvp in setDict)
                Properties[kvp.Key] = kvp.Value;

            Properties[PropertyEnum.OmegaPointsSpent] = pointsSpent;

            DictionaryPool<PropertyId, PropertyValue>.Instance.Return(setDict);
        }

        #endregion

        #region Shared

        public static int ModRankFromPoints(PrototypeId modProtoRef, long points, out long remainder)
        {
            remainder = 0;

            ModPrototype modProto = modProtoRef.As<ModPrototype>();
            if (modProto == null) return Logger.WarnReturn(0, "ModRankFromPoints(): modProto == null");

            Curve curve = modProto.RankCostCurve.AsCurve();
            if (curve == null) return Logger.WarnReturn(0, "ModRankFromPoints(): curve == null");

            int rank = 0;
            int ranksMax = modProto.GetRanksMax();
            remainder = points;

            while (remainder > 0 && rank < ranksMax)
            {
                int nextRankCost = curve.GetIntAt(rank + 1);
                if (nextRankCost > remainder)
                    break;

                remainder -= nextRankCost;
                rank++;
            }

            return rank;
        }

        #endregion
    }
}
