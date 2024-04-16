using System.Text;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Regions.MatchQueues
{
    // For reference see NetMessageMatchQueueUpdateClient

    /// <summary>
    /// Manages queue statuses for region / difficulty tier combinations.
    /// </summary>
    public class MatchQueueStatus
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<(PrototypeId, PrototypeId), MatchQueueRegionStatus> _regionStatusDict = new();

        private Player _owner;

        /// <summary>
        /// Constructs a new <see cref="MatchQueueStatus"/> instance.
        /// </summary>
        public MatchQueueStatus() { }

        public void Decode(CodedInputStream stream)
        {
            ulong numRegionStatuses = stream.ReadRawVarint64();

            for (ulong i = 0; i < numRegionStatuses; i++)
            {
                PrototypeId regionRef = stream.ReadPrototypeRef<Prototype>();
                PrototypeId difficultyTierRef = stream.ReadPrototypeRef<Prototype>();
                ulong regionRequestGroupId = stream.ReadRawVarint64();
                uint playersInQueue = stream.ReadRawVarint32();

                if (regionRef == PrototypeId.Invalid) continue;

                for (uint j = 0; j < playersInQueue; j++)
                {
                    ulong playerGuid = stream.ReadRawVarint64();
                    string playerName = stream.ReadRawString();
                    var status = (RegionRequestQueueUpdateVar)stream.ReadRawVarint32();

                    UpdatePlayerState(playerGuid, regionRef, difficultyTierRef, regionRequestGroupId, status, playerName);
                }
            }
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint64((ulong)_regionStatusDict.Count);
            foreach (var kvp in _regionStatusDict)
            {
                stream.WritePrototypeRef<Prototype>(kvp.Key.Item1);
                stream.WritePrototypeRef<Prototype>(kvp.Key.Item2);
                stream.WriteRawVarint64(kvp.Value.RegionRequestGroupId);
                stream.WriteRawVarint32((uint)kvp.Value.PlayerInfoDict.Count);

                foreach (var entry in kvp.Value.PlayerInfoDict)
                {
                    stream.WriteRawVarint64(entry.Key);
                    stream.WriteRawString(entry.Value.PlayerName);
                    stream.WriteRawVarint32((uint)entry.Value.Status);
                }
            }
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
