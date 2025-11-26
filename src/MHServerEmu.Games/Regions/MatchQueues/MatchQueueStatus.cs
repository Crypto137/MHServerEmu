using System.Text;
using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Social.Communities;

namespace MHServerEmu.Games.Regions.MatchQueues
{
    // For reference see NetMessageMatchQueueUpdateClient

    /// <summary>
    /// Manages <see cref="MatchQueueRegionStatus"/> instances bound to a <see cref="Player"/>.
    /// </summary>
    public class MatchQueueStatus : ISerialize
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<(PrototypeId, PrototypeId), MatchQueueRegionStatus> _regionStatusDict = new();

        private Player _owner;

        /// <summary>
        /// Constructs a new <see cref="MatchQueueStatus"/> instance.
        /// </summary>
        public MatchQueueStatus()
        {
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            foreach (var kvp in _regionStatusDict)
            {
                sb.Append($"[{GameDatabase.GetFormattedPrototypeName(kvp.Key.Item1)}][{GameDatabase.GetFormattedPrototypeName(kvp.Key.Item2)}]: ");
                sb.AppendLine(kvp.Value.ToString());
            }

            return sb.ToString();
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            uint numRegionStatuses = (uint)_regionStatusDict.Count;
            success &= Serializer.Transfer(archive, ref numRegionStatuses);

            if (archive.IsPacking)
            {
                foreach (var kvp in _regionStatusDict)
                {
                    PrototypeId regionRef = kvp.Key.Item1;
                    PrototypeId difficultyTierRef = kvp.Key.Item2;
                    ulong groupId = kvp.Value.GroupId;
                    uint numPlayers = (uint)kvp.Value.PlayerInfos.Count;

                    success &= Serializer.Transfer(archive, ref regionRef);
                    success &= Serializer.Transfer(archive, ref difficultyTierRef);
                    success &= Serializer.Transfer(archive, ref groupId);
                    success &= Serializer.Transfer(archive, ref numPlayers);

                    foreach (var playerInfoKvp in kvp.Value.PlayerInfos)
                    {
                        ulong playerGuid = playerInfoKvp.Key;
                        string playerName = playerInfoKvp.Value.PlayerName;
                        uint status = (uint)playerInfoKvp.Value.Status;

                        success &= Serializer.Transfer(archive, ref playerGuid);
                        success &= Serializer.Transfer(archive, ref playerName);
                        success &= Serializer.Transfer(archive, ref status);
                    }
                }
            }
            else
            {
                for (uint i = 0; i < numRegionStatuses; i++)
                {
                    PrototypeId regionRef = PrototypeId.Invalid;
                    PrototypeId difficultyTierRef = PrototypeId.Invalid;
                    ulong groupId = 0;
                    uint numPlayers = 0;

                    success &= Serializer.Transfer(archive, ref regionRef);
                    success &= Serializer.Transfer(archive, ref difficultyTierRef);
                    success &= Serializer.Transfer(archive, ref groupId);
                    success &= Serializer.Transfer(archive, ref numPlayers);

                    if (regionRef == PrototypeId.Invalid)
                        continue;

                    for (uint j = 0; j < numPlayers; j++)
                    {
                        ulong playerGuid = 0;
                        string playerName = string.Empty;
                        uint status = 0;

                        success &= Serializer.Transfer(archive, ref playerGuid);
                        success &= Serializer.Transfer(archive, ref playerName);
                        success &= Serializer.Transfer(archive, ref status);

                        UpdatePlayerState(playerGuid, regionRef, difficultyTierRef, groupId, (RegionRequestQueueUpdateVar)status, playerName);
                    }
                }
            }

            return success;
        }

        public bool IsOwnerInQueue()
        {
            ulong ownerDbId = _owner.DatabaseUniqueId;

            foreach (MatchQueueRegionStatus regionStatus in _regionStatusDict.Values)
            {
                if (regionStatus.PlayerInfos.TryGetValue(ownerDbId, out MatchQueuePlayerInfoEntry entry) == false)
                    continue;

                switch (entry.Status)
                {
                    case RegionRequestQueueUpdateVar.eRRQ_SelectQueueMethod:
                    case RegionRequestQueueUpdateVar.eRRQ_RemovedFromGroup:
                        continue;

                    default:
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Sets the owner <see cref="Player"/> of this <see cref="MatchQueueStatus"/> instance.
        /// </summary>
        public void SetOwner(Player owner)
        {
            _owner = owner;
        }

        public void UpdateQueue(PrototypeId regionRef, PrototypeId difficultyTierRef, ulong groupId, int playersInQueue)
        {
            MatchQueueRegionStatus newRegionStatus = GetOrCreateRegionStatus(regionRef, difficultyTierRef, groupId);
            if (newRegionStatus == null)
            {
                Logger.Warn("UpdateQueue(): newRegionStatus == null");
                return;
            }

            newRegionStatus.UpdateQueue(playersInQueue);
        }

        /// <summary>
        /// Updates queue state of the specified player.
        /// </summary>
        public bool UpdatePlayerState(ulong playerGuid, PrototypeId regionRef, PrototypeId difficultyTierRef,
            ulong groupId, RegionRequestQueueUpdateVar status, string playerName)
        {
            if (_owner == null)
                return Logger.WarnReturn(false, "UpdatePlayerState(): _owner == null");

            // Some statuses cause the player to be removed.
            if (IsRemovePlayerStatus(status) && _owner.DatabaseUniqueId == playerGuid)
                return _regionStatusDict.Remove((regionRef, difficultyTierRef));

            MatchQueueRegionStatus newRegionStatus = GetOrCreateRegionStatus(regionRef, difficultyTierRef, groupId);

            // Fall back to community data if we don't have a valid name
            if (string.IsNullOrEmpty(playerName))
            {
                CommunityMember member = _owner.Community.GetMember(playerGuid);
                playerName = member != null ? member.GetName() : string.Empty;
            }

            return newRegionStatus.UpdatePlayer(playerGuid, status, playerName);
        }

        /// <summary>
        /// Returns <see langword="true"/> if the specified <see cref="RegionRequestQueueUpdateVar"/> requires the player to be removed.
        /// </summary>
        public static bool IsRemovePlayerStatus(RegionRequestQueueUpdateVar status)
        {
            switch (status)
            {
                case RegionRequestQueueUpdateVar.eRRQ_RemovedFromGroup:
                case RegionRequestQueueUpdateVar.eRRQ_RaidNotAllowed:
                case RegionRequestQueueUpdateVar.eRRQ_PartyTooLarge:
                case RegionRequestQueueUpdateVar.eRRQ_GroupInviteExpired:
                case RegionRequestQueueUpdateVar.eRRQ_MatchInviteExpired:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Retrieves or create a new <see cref="MatchQueueRegionStatus"/> instance for the specified arguments.
        /// </summary>
        private MatchQueueRegionStatus GetOrCreateRegionStatus(PrototypeId regionRef, PrototypeId difficultyTierRef, ulong groupId)
        {
            if (_regionStatusDict.TryGetValue((regionRef, difficultyTierRef), out MatchQueueRegionStatus regionStatus) == false)
            {
                regionStatus = new(regionRef, difficultyTierRef, groupId);
                _regionStatusDict.Add((regionRef, difficultyTierRef), regionStatus);
            }

            if (regionStatus.GroupId != groupId)
                regionStatus.GroupId = groupId;

            return regionStatus;
        }
    }
}
