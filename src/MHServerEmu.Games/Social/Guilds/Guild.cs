using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;

namespace MHServerEmu.Games.Social.Guilds
{
    public class Guild
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<ulong, GuildMember> _members = new();
        private readonly EventGroup _pendingEvents = new();

        public Game Game { get; }

        public ulong Id { get; }
        public string Name { get; }
        public string Motd { get; }

        public ulong LeaderDbId { get; private set; }

        public Guild(Game game, GuildCompleteInfo guildCompleteInfo)
        {
            Game = game;

            Id = guildCompleteInfo.GuildId;
            Name = guildCompleteInfo.GuildName;
            Motd = guildCompleteInfo.HasGuildMotd ? guildCompleteInfo.GuildMotd : string.Empty;
        }

        public override string ToString()
        {
            return $"{Name} (0x{Id:X})";
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

            return true;
        }

        public GuildMember GetMember(ulong playerDbId)
        {
            if (_members.TryGetValue(playerDbId, out GuildMember member) == false)
                return null;

            return member;
        }
    }
}
