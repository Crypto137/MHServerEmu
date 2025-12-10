using System.Text;
using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Social.Communities;
using MHServerEmu.Games.Social.Parties;

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

        public int Count { get => _regionStatusDict.Count; }

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

        public void Flush()
        {
            if (_regionStatusDict.Count == 0)
                return;

            ulong playerDbId = _owner.DatabaseUniqueId;
            string playerName = _owner.GetName();
            RegionRequestQueueUpdateVar status = RegionRequestQueueUpdateVar.eRRQ_RemovedFromGroup;

            foreach ((PrototypeId regionRef, PrototypeId difficultyTierRef) in _regionStatusDict.Keys)
            {
                RegionPrototype regionProto = regionRef.As<RegionPrototype>();

                NetMessageChatFromMetaGame chatLogMessage = NetMessageChatFromMetaGame.CreateBuilder()
                    .SetSourceStringId((ulong)GameDatabase.GlobalsPrototype.SystemLocalized)
                    .SetMessageStringId((ulong)GameDatabase.TransitionGlobalsPrototype.GetLocaleStringIdForLog(status))
                    .SetPlayerName1(playerName)
                    .AddArgStringIds(regionProto != null ? (ulong)regionProto.RegionName : 0)
                    .Build();

                _owner.SendMessage(chatLogMessage);

                _owner.SendMatchQueueUpdate(playerDbId, regionRef, difficultyTierRef, 0, status, playerName, 0);
            }

            _regionStatusDict.Clear();
        }

        /// <summary>
        /// Handles a <see cref="RegionRequestQueueCommandVar"/> request from a client.
        /// </summary>
        public bool TryRegionRequestCommand(PrototypeId regionRef, PrototypeId difficultyTierRef,
            ulong groupId, RegionRequestQueueCommandVar command)
        {
            if (regionRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "TryRegionRequestCommand(): regionRef == PrototypeId.Invalid");

            RegionPrototype regionProto = regionRef.As<RegionPrototype>();
            if (regionProto == null) return Logger.WarnReturn(false, "TryRegionRequestCommand(): regionProto == null");

            RegionRequestQueueUpdateVar status = RegionRequestQueueUpdateVar.eRRQ_RemovedFromGroup;

            if (_regionStatusDict.TryGetValue((regionRef, difficultyTierRef), out MatchQueueRegionStatus regionStatus) &&
                regionStatus.PlayerInfos.TryGetValue(_owner.DatabaseUniqueId, out MatchQueuePlayerInfoEntry entry))
            {
                status = entry.Status;
            }

            switch (command)
            {
                case RegionRequestQueueCommandVar.eRRQC_AddToQueueSolo:
                case RegionRequestQueueCommandVar.eRRQC_AddToQueueParty:
                case RegionRequestQueueCommandVar.eRRQC_AddToQueueBypass:
                    if (difficultyTierRef == PrototypeId.Invalid)
                        return Logger.WarnReturn(false, "TryRegionRequestCommand(): difficultyTierRef == PrototypeId.Invalid");

                    if (IsOwnerInQueue())
                        return false;

                    Party party = _owner.GetParty();
                    if (party != null && party.IsLeader(_owner) == false)
                        return false;

                    break;

                case RegionRequestQueueCommandVar.eRRQC_GroupInviteAccept:
                case RegionRequestQueueCommandVar.eRRQC_GroupInviteDecline:
                    if (status != RegionRequestQueueUpdateVar.eRRQ_GroupInvitePending)
                        return Logger.WarnReturn(false, $"TryRegionRequestCommand(): Invalid status {status} for command {command} from [{_owner}]");
                    break;

                case RegionRequestQueueCommandVar.eRRQC_RemoveFromQueue:
                    // does not require validation
                    break;

                case RegionRequestQueueCommandVar.eRRQC_MatchInviteAccept:
                case RegionRequestQueueCommandVar.eRRQC_MatchInviteDecline:
                    if (status != RegionRequestQueueUpdateVar.eRRQ_MatchInvitePending && status != RegionRequestQueueUpdateVar.eRRQ_RemovedGracePeriod)
                        return Logger.WarnReturn(false, $"TryRegionRequestCommand(): Invalid status {status} for command {command} from [{_owner}]");
                    break;

                // The client should not be sending these commands.
                case RegionRequestQueueCommandVar.eRRQC_DebugForceStart:
                case RegionRequestQueueCommandVar.eRRQC_DebugInfo:
                case RegionRequestQueueCommandVar.eRRQC_RequestToJoinGroup:
                    return Logger.WarnReturn(false, $"TryRegionRequestCommand(): Received command {command} from [{_owner}]");
            }

            if (command == RegionRequestQueueCommandVar.eRRQC_AddToQueueBypass && regionProto.AllowsQueueBypass == false)
                return false;

            _owner.SendRegionRequestQueueCommandToPlayerManager(regionRef, difficultyTierRef, command, groupId);
            return true;
        }

        public void RemoveFromAllQueues()
        {
            if (_owner == null)
                return;

            foreach ((PrototypeId regionRef, PrototypeId difficultyTierRef) in _regionStatusDict.Keys)
                _owner.SendRegionRequestQueueCommandToPlayerManager(regionRef, difficultyTierRef, RegionRequestQueueCommandVar.eRRQC_RemoveFromQueue);
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
