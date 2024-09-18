using System.Text;
using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Regions.MatchQueues
{
    // For reference see NetMessageMatchQueueUpdateClient

    /// <summary>
    /// Manages queue statuses for region / difficulty tier combinations.
    /// </summary>
    public class MatchQueueStatus : ISerialize
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<(PrototypeId, PrototypeId), MatchQueueRegionStatus> _regionStatusDict = new();

        private Player _owner;

        /// <summary>
        /// Constructs a new <see cref="MatchQueueStatus"/> instance.
        /// </summary>
        public MatchQueueStatus() { }

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
                    ulong regionRequestGroupId = kvp.Value.RegionRequestGroupId;
                    uint numPlayers = (uint)kvp.Value.PlayerInfoDict.Count;

                    success &= Serializer.Transfer(archive, ref regionRef);
                    success &= Serializer.Transfer(archive, ref difficultyTierRef);
                    success &= Serializer.Transfer(archive, ref regionRequestGroupId);
                    success &= Serializer.Transfer(archive, ref numPlayers);

                    foreach (var playerInfoKvp in kvp.Value.PlayerInfoDict)
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
                    ulong regionRequestGroupId = 0;
                    uint numPlayers = 0;

                    success &= Serializer.Transfer(archive, ref regionRef);
                    success &= Serializer.Transfer(archive, ref difficultyTierRef);
                    success &= Serializer.Transfer(archive, ref regionRequestGroupId);
                    success &= Serializer.Transfer(archive, ref numPlayers);

                    if (regionRef == PrototypeId.Invalid) continue;

                    for (uint j = 0; j < numPlayers; j++)
                    {
                        ulong playerGuid = 0;
                        string playerName = string.Empty;
                        uint status = 0;

                        success &= Serializer.Transfer(archive, ref playerGuid);
                        success &= Serializer.Transfer(archive, ref playerName);
                        success &= Serializer.Transfer(archive, ref status);

                        UpdatePlayerState(playerGuid, regionRef, difficultyTierRef, regionRequestGroupId,
                            (RegionRequestQueueUpdateVar)status, playerName);
                    }
                }
            }

            return success;
        }

        /// <summary>
        /// Updates queue state of the specified player.
        /// </summary>
        public bool UpdatePlayerState(ulong playerGuid, PrototypeId regionRef, PrototypeId difficultyTierRef,
            ulong regionRequestGroupId, RegionRequestQueueUpdateVar status, string playerName)
        {
            if (_owner == null)
                return Logger.WarnReturn(false, "UpdatePlayerState(): _owner == null");

            // Check if we need to remove the player
            if (RemovePlayerOnStatus(status) && _owner.DatabaseUniqueId == playerGuid)
                return _regionStatusDict.Remove((regionRef, difficultyTierRef));

            // Set new status
            MatchQueueRegionStatus newRegionStatus = GetOrCreateRegionStatus(regionRef, difficultyTierRef, regionRequestGroupId);

            // TODO: fall back to community data
            if (string.IsNullOrEmpty(playerName))
            {
                Logger.Warn($"UpdatePlayerState(): playerName is empty");
                playerName = "TODO: GET NAME FROM COMMUNITY";
            }
                

            return newRegionStatus.UpdatePlayer(playerGuid, status, playerName);
        }

        /// <summary>
        /// Sets the owner <see cref="Player"/> of this <see cref="MatchQueueStatus"/> instance.
        /// </summary>
        public void SetOwner(Player owner)
        {
            _owner = owner;
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

        /// <summary>
        /// Returns <see langword="true"/> if the specified <see cref="RegionRequestQueueUpdateVar"/> requires the player to be removed.
        /// </summary>
        public static bool RemovePlayerOnStatus(RegionRequestQueueUpdateVar status)
        {
            return status == RegionRequestQueueUpdateVar.eRRQ_RemovedFromGroup
                || status == RegionRequestQueueUpdateVar.eRRQ_RaidNotAllowed
                || status == RegionRequestQueueUpdateVar.eRRQ_PartyTooLarge
                || status == RegionRequestQueueUpdateVar.eRRQ_GroupInviteExpired
                || status == RegionRequestQueueUpdateVar.eRRQ_MatchInviteExpired;
        }

        /// <summary>
        /// Retrieves or create a new <see cref="MatchQueueRegionStatus"/> instance for the specified arguments.
        /// </summary>
        private MatchQueueRegionStatus GetOrCreateRegionStatus(PrototypeId regionRef, PrototypeId difficultyTierRef, ulong regionRequestGroupId)
        {
            if (_regionStatusDict.TryGetValue((regionRef, difficultyTierRef), out MatchQueueRegionStatus regionStatus) == false)
            {
                regionStatus = new(regionRef, difficultyTierRef, regionRequestGroupId);
                _regionStatusDict.Add((regionRef, difficultyTierRef), regionStatus);
            }

            if (regionStatus.RegionRequestGroupId != regionRequestGroupId)
                regionStatus.RegionRequestGroupId = regionRequestGroupId;

            return regionStatus;
        }
    }
}
