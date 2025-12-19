using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Social.Guilds
{
    public class Guild
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<ulong, GuildMember> _members = new();

        private readonly EventGroup _pendingEvents = new();
        private readonly EventPointer<CommunityUpdateEvent> _communityUpdateEvent = new();

        private NetMessageGuildMessageToClient _guildCompleteInfoCache = null;

        public Game Game { get; }

        public ulong Id { get; }
        public string Name { get; }
        public string Motd { get; }

        public ulong LeaderDbId { get; private set; }

        public int MemberCount { get => _members.Count; }

        public Guild(Game game, GuildCompleteInfo guildCompleteInfo)
        {
            Game = game;

            Id = guildCompleteInfo.GuildId;
            Name = guildCompleteInfo.GuildName;
            Motd = guildCompleteInfo.HasGuildMotd ? guildCompleteInfo.GuildMotd : string.Empty;

            CacheGuildCompleteInfo(guildCompleteInfo);
        }

        public override string ToString()
        {
            return $"{Name} ({Id})";
        }

        public Dictionary<ulong, GuildMember>.ValueCollection.Enumerator GetEnumerator()
        {
            return _members.Values.GetEnumerator();
        }

        public void Sync(GuildCompleteInfo guildCompleteInfo)
        {
            bool hasChanged = false;

            // Remove members that are not listed in the GuildCompleteInfo.
            HashSet<ulong> removedMembers = HashSetPool<ulong>.Instance.Get();

            foreach (GuildMember member in this)
            {
                bool infoHasMember = false;
                for (int i = 0; i < guildCompleteInfo.MembersCount; i++)
                {
                    GuildMemberInfo memberInfo = guildCompleteInfo.MembersList[i];
                    if (memberInfo.PlayerId == member.Id)
                    {
                        infoHasMember = true;
                        break;
                    }
                }

                if (infoHasMember == false)
                    removedMembers.Add(member.Id);
            }

            foreach (ulong playerDbId in removedMembers)
                hasChanged |= RemoveMember(playerDbId);

            HashSetPool<ulong>.Instance.Return(removedMembers);

            // Add new members
            for (int i = 0; i < guildCompleteInfo.MembersCount; i++)
            {
                GuildMemberInfo memberInfo = guildCompleteInfo.MembersList[i];
                if (GetMember(memberInfo.PlayerId) != null)
                    continue;

                hasChanged |= AddMember(memberInfo) != null;

                // NOTE: The client doesn't sync membership here, is this a bug?
            }

            // Update cache and send to members if changed
            if (hasChanged == false)
                return;

            CacheGuildCompleteInfo(guildCompleteInfo);
            ReplicateToOnlineMembers();
        }

        public void Shutdown()
        {
            while (_members.Count > 0)
            {
                foreach (ulong playerDbId in _members.Keys)
                {
                    RemoveMember(playerDbId);
                    break;
                }
            }

            // The CancelAllEvents() call here is the same as the destructor in Gazillion's implementation.
            Game.GameEventScheduler.CancelAllEvents(_pendingEvents);
        }

        public GuildMember AddMember(GuildMemberInfo guildMemberInfo)
        {
            ulong playerDbId = guildMemberInfo.PlayerId;

            GuildMember existingMember = GetMember(playerDbId);
            if (existingMember != null)
                return Logger.WarnReturn<GuildMember>(null, $"AddMember(): Duplicate guild member found.  existingMember={existingMember}");

            GuildMember member = new(this, guildMemberInfo);
            _members.Add(playerDbId, member);

            if (member.Membership == GuildMembership.eGMLeader)
                LeaderDbId = member.Id;

            Player player = Game.EntityManager.GetEntityByDbGuid<Player>(playerDbId);
            player?.SetGuildMembership(Id, Name, member.Membership);

            ScheduleCommunityUpdate();

            return member;
        }

        public bool RemoveMember(ulong playerDbId)
        {
            GuildMember member = GetMember(playerDbId);
            if (member == null)
                return Logger.WarnReturn(false, $"RemoveMember(): Guild member not found. id=0x{playerDbId:X}");

            if (member.Membership == GuildMembership.eGMLeader)
                LeaderDbId = 0;

            _members.Remove(playerDbId);

            Player player = Game.EntityManager.GetEntityByDbGuid<Player>(playerDbId);
            player?.SetGuildMembership(GuildManager.InvalidGuildId, string.Empty, GuildMembership.eGMNone);

            ScheduleCommunityUpdate();

            return true;
        }

        public GuildMember GetMember(ulong playerDbId)
        {
            if (_members.TryGetValue(playerDbId, out GuildMember member) == false)
                return null;

            return member;
        }

        public int GetOnlineMemberCount()
        {
            int count = 0;

            EntityManager entityManager = Game.EntityManager;

            foreach (GuildMember member in this)
            {
                Player player = entityManager.GetEntityByDbGuid<Player>(member.Id);
                if (player != null)
                    count++;
            }

            return count;
        }

        public void ReplicateToOnlineMembers()
        {
            if (_guildCompleteInfoCache == null)
                CacheGuildCompleteInfo();

            SendMessageToOnlineMembers(_guildCompleteInfoCache);
        }

        public void ReplicateToPlayer(Player player)
        {
            if (_members.ContainsKey(player.DatabaseUniqueId) == false)
            {
                Logger.Warn($"ReplicateToPlayer(): Player [{player}] is not a member of guild [{this}]");
                return;
            }

            if (_guildCompleteInfoCache == null)
                CacheGuildCompleteInfo();

            player.SendMessage(_guildCompleteInfoCache);
        }

        private void CacheGuildCompleteInfo(GuildCompleteInfo guildCompleteInfo = null)
        {
            if (guildCompleteInfo == null)
            {
                var builder = GuildCompleteInfo.CreateBuilder()
                    .SetGuildId(Id)
                    .SetGuildName(Name);

                foreach (GuildMember member in this)
                    builder.AddMembers(member.ToGuildMemberInfo());

                if (string.IsNullOrWhiteSpace(Motd) == false)
                    builder.SetGuildMotd(Motd);

                guildCompleteInfo = builder.Build();
            }

            _guildCompleteInfoCache = NetMessageGuildMessageToClient.CreateBuilder()
                .SetMessages(GuildMessageSetToClient.CreateBuilder()
                    .SetGuildCompleteInfo(guildCompleteInfo))
                .Build();
        }

        private void SendMessageToOnlineMembers(IMessage message)
        {
            List<PlayerConnection> clients = ListPool<PlayerConnection>.Instance.Get();
            EntityManager entityManager = Game.EntityManager;

            foreach (GuildMember member in this)
            {
                Player player = entityManager.GetEntityByDbGuid<Player>(member.Id);
                if (player == null)
                    continue;

                clients.Add(player.PlayerConnection);
            }

            Game.NetworkManager.SendMessageToMultiple(clients, message);
            ListPool<PlayerConnection>.Instance.Return(clients);
        }

        private void ScheduleCommunityUpdate()
        {
            if (_communityUpdateEvent.IsValid)
                return;

            Game.GameEventScheduler.ScheduleEvent(_communityUpdateEvent, TimeSpan.Zero, _pendingEvents);
            _communityUpdateEvent.Get().Initialize(this);
        }

        private void UpdateCommunities()
        {
            EntityManager entityManager = Game.EntityManager;

            foreach (GuildMember member in this)
            {
                Player player = entityManager.GetEntityByDbGuid<Player>(member.Id);
                player?.Community.UpdateGuild(this);
            }
        }

        private class CommunityUpdateEvent : CallMethodEvent<Guild>
        {
            protected override CallbackDelegate GetCallback() => static (t) => t.UpdateCommunities();
        }
    }
}
