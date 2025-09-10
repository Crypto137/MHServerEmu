using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.GameData;
using MHServerEmu.PlayerManagement.Players;

namespace MHServerEmu.PlayerManagement.Social
{
    /// <summary>
    /// The authoritative representation of a party on the server (as apposed to local parties in game instances).
    /// </summary>
    public class Party
    {
        // This class has the same name as game-side local representations of parties.
        // This is not ideal, but I'm not sure what else to call it without making it more confusing.
        private static readonly Logger Logger = LogManager.CreateLogger();

        // We are not using a HashSet here because this is a small collection (<= 10 elements),
        // and we need the order of joining to pass leadership when the current leader leaves.
        private readonly List<PlayerHandle> _members = new();
        private readonly HashSet<PlayerHandle> _pendingMembers = new();

        public ulong Id { get; }
        public GroupType Type { get; private set; } = GroupType.GroupType_Party;
        public PlayerHandle Leader { get; private set; }

        public int MemberCount { get => _members.Count; }
        public bool HasEnoughMembersOrInvitations { get => _members.Count > 1 || _pendingMembers.Count > 0; }

        public Party(ulong id, PlayerHandle creator)
        {
            Id = id;

            AddMember(creator);
            SetLeader(creator);
        }

        public override string ToString()
        {
            return $"id={Id}";
        }

        public void GetMembers(HashSet<PlayerHandle> members)
        {
            foreach (PlayerHandle member in _members)
                members.Add(member);
        }

        public bool HasMember(PlayerHandle player)
        {
            return _members.Contains(player);
        }

        public bool IsFull()
        {
            int max = 0;
            switch (Type)
            {
                case GroupType.GroupType_Party:
                    max = GameDatabase.GlobalsPrototype.PlayerPartyMaxSize;
                    break;

                case GroupType.GroupType_Raid:
                    max = GameDatabase.GlobalsPrototype.PlayerRaidMaxSize;
                    break;
            }

            return _members.Count >= max;
        }

        public bool AddMember(PlayerHandle player)
        {
            if (player == null) return Logger.WarnReturn(false, "AddMember(): player == null");

            bool added = false;

            if (_members.Contains(player) == false)
            {
                // Send update to existing players (the player we are adding will get this in the party info sync below)
                SendPartyMemberInfoUpdate(player, PartyMemberEvent.ePME_Add, _members);

                _pendingMembers.Remove(player);
                player.PendingParty = null;

                _members.Add(player);
                player.CurrentParty = this;

                Logger.Info($"AddMember(): party=[{this}], player=[{player}]");

                added = true;
            }

            // Something may have gone out of sync if we got here when the player is already in this party, so sync anyway.
            SyncPartyInfo(player);
            return added;
        }

        public bool RemoveMember(PlayerHandle player, GroupLeaveReason reason)
        {
            if (player == null) return Logger.WarnReturn(false, "RemoveMember(): player == null");

            // Send this before removing so that the player we are removing gets the message as well.
            SendPartyMemberInfoUpdate(player, PartyMemberEvent.ePME_Remove, _members);

            if (_members.Remove(player) == false)
                return false;

            player.CurrentParty = null;

            Logger.Info($"RemoveMember(): party=[{this}], player=[{player}]");

            return true;
        }

        public bool UpdateMember(PlayerHandle player)
        {
            if (HasMember(player) == false)
                return Logger.WarnReturn(false, $"UpdateMember(): Attempting to update player [{player}] who is not a member of party [{this}]");

            SendPartyMemberInfoUpdate(player, PartyMemberEvent.ePME_Update, _members);
            return true;
        }

        public bool SetLeader(PlayerHandle player)
        {
            if (player == null) return Logger.WarnReturn(false, "SetLeader(): player == null");

            if (HasMember(player) == false)
                return Logger.WarnReturn(false, $"SetLeader(): Attempting to set player [{player}] as the leader of party [{this}], but this player is not in this party");

            Leader = player;
            SendPartyInfo(false, _members);

            Logger.Info($"SetLeader(): party=[{this}], player=[{player}]");

            return true;
        }

        public PlayerHandle GetNextLeader()
        {
            // Leadership is passed in the order of joining the party, which is reflected in the member index.
            if (_members.Count == 0)
                return null;

            return _members[0];
        }

        public bool HasInvite(PlayerHandle player)
        {
            return _pendingMembers.Contains(player);
        }

        public void AddInvite(PlayerHandle player)
        {
            _pendingMembers.Add(player);
            player.PendingParty = this;
        }

        public void RemoveInvite(PlayerHandle player)
        {
            _pendingMembers.Remove(player);
            player.PendingParty = null;
        }

        public void CancelAllInvites()
        {
            foreach (PlayerHandle player in _pendingMembers)
            {
                if (player.PendingParty != null && player.PendingParty != this)
                {
                    Logger.Warn($"CancelAllInvites(): Player pending party desync (expected [{this}], got [{player.PendingParty}])");
                    continue;
                }

                player.PendingParty = null;

                // Notify the player of cancellation if in-game
                if (player.CurrentGame == null)
                    continue;

                var request = PartyOperationPayload.CreateBuilder()
                    .SetRequestingPlayerDbId(player.PlayerDbId)
                    .SetRequestingPlayerName(player.PlayerName)
                    .SetOperation(GroupingOperationType.eGOP_ServerNotification)
                    .Build();

                ServiceMessage.PartyOperationRequestServerResult message = new(player.CurrentGame.Id, player.PlayerDbId,
                    request, GroupingOperationResult.eGOPR_PendingPartyDisbanded);
                ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, message);
            }

            _pendingMembers.Clear();
        }

        public void SyncPartyInfo(PlayerHandle player)
        {
            List<PlayerHandle> recipients = ListPool<PlayerHandle>.Instance.Get();
            recipients.Add(player);
            SendPartyInfo(true, recipients);
            ListPool<PlayerHandle>.Instance.Return(recipients);
        }

        private void SendPartyInfo(bool includeMemberInfo, List<PlayerHandle> recipients)
        {
            if (recipients.Count == 0)
            {
                Logger.Warn("SendPartyInfo(): recipients.Count == 0");
                return;
            }

            var partyInfoBuilder = PartyInfo.CreateBuilder()
                .SetGroupId(Id)
                .SetType(Type)
                .SetLeaderDbId(Leader != null ? Leader.PlayerDbId : 0)
                .SetDifficultyTierProtoId((ulong)GameDatabase.GlobalsPrototype.DifficultyTierDefault);  // TODO

            if (includeMemberInfo)
            {
                foreach (PlayerHandle player in _members)
                {
                    partyInfoBuilder.AddMembers(PartyMemberInfo.CreateBuilder()
                        .SetPlayerDbId(player.PlayerDbId)
                        .SetPlayerName(player.PlayerName));
                }
            }

            PartyInfo partyInfo = partyInfoBuilder.Build();

            foreach (PlayerHandle player in recipients)
            {
                if (player.CurrentGame == null)
                    continue;

                ServiceMessage.PartyInfoServerUpdate message = new(player.CurrentGame.Id, player.PlayerDbId, Id, partyInfo);
                ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, message);
            }
        }

        private void SendPartyMemberInfoUpdate(PlayerHandle member, PartyMemberEvent memberEvent, List<PlayerHandle> recipients)
        {
            if (recipients.Count == 0)
            {
                Logger.Warn("SendPartyMemberInfo(): recipients.Count == 0");
                return;
            }

            PartyMemberInfo memberInfo = null;
            if (memberEvent != PartyMemberEvent.ePME_Remove)
            {
                memberInfo = PartyMemberInfo.CreateBuilder()
                    .SetPlayerDbId(member.PlayerDbId)
                    .SetPlayerName(member.PlayerName)
                    .Build();
            }

            foreach (PlayerHandle player in recipients)
            {
                if (player.CurrentGame == null)
                    continue;

                ServiceMessage.PartyMemberInfoServerUpdate message = new(player.CurrentGame.Id, player.PlayerDbId,
                    Id, member.PlayerDbId, memberEvent, memberInfo);
                ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, message);
            }
        }
    }
}
