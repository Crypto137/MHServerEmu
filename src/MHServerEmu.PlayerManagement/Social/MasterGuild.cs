using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.PlayerManagement.Social
{
    public class MasterGuild
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private static bool PersistenceEnabled { get => PlayerManagerService.Instance.Config.EnablePersistence; }

        // NOTE: All guild DB operations are currently synchronous.
        // May need some kind of async job queue for these, especially for potential non-SQLite backends.
        private readonly DBGuild _data;

        private readonly Dictionary<ulong, MemberEntry> _members = new();

        private MemberEntry? _leader;

        public ulong Id { get => (ulong)_data.Id; }
        public string Name { get => _data.Name; }
        public string Motd { get => _data.Motd; }
        public int MemberCount { get => _data.Members.Count; }

        public MasterGuild(DBGuild data, bool saveToDatabase)
        {
            _data = data;

            foreach (DBGuildMember member in _data.Members)
                AddMember(member);

            if (saveToDatabase)
            {
                SaveToDatabase();

                foreach (MemberEntry member in _members.Values)
                    member.SaveToDatabase();
            }
        }

        public override string ToString()
        {
            return _data.ToString();
        }

        private bool SaveToDatabase()
        {
            if (PersistenceEnabled == false)
                return true;

            return IDBManager.Instance.SaveGuild(_data);
        }

        private bool DeleteFromDatabase()
        {
            if (PersistenceEnabled == false)
                return true;

            return IDBManager.Instance.DeleteGuild(_data);
        }

        private MemberEntry? AddMember(DBGuildMember data)
        {
            ulong playerDbId = (ulong)data.PlayerDbGuid;

            MemberEntry? existingMember = GetMember(playerDbId);
            if (existingMember != null)
                return Logger.WarnReturn<MemberEntry?>(null, $"AddMember(): Attempted to add existing member [{existingMember}] to guild [{this}]");

            bool isLeader = data.Membership == (int)GuildMembership.eGMLeader;
            if (isLeader && _leader != null)
                return Logger.WarnReturn<MemberEntry?>(null, $"AddMember(): Attempted to add a second leader [{data}] when there is an existing leader [{_leader}] in guild [{this}]");

            MemberEntry member = new(data);
            _members.Add(playerDbId, member);

            if (isLeader)
                _leader = member;

            return member;
        }

        private MemberEntry? GetMember(ulong playerDbId)
        {
            if (_members.TryGetValue(playerDbId, out MemberEntry member) == false)
                return null;

            return member;
        }

        /// <summary>
        /// A wrapper for <see cref="DBGuildMember"/> for easier data access.
        /// </summary>
        private readonly struct MemberEntry(DBGuildMember data)
        {
            private readonly DBGuildMember _data = data;

            public ulong PlayerDbId { get => (ulong)_data.PlayerDbGuid; }
            public string PlayerName { get => GetPlayerName(); }
            public GuildMembership Membership { get => (GuildMembership)_data.Membership; }

            public override string ToString()
            {
                return $"{PlayerName} (0x{PlayerDbId:X}) - {Membership}";
            }

            public GuildMemberInfo ToGuildMemberInfo()
            {
                return GuildMemberInfo.CreateBuilder()
                    .SetPlayerId(PlayerDbId)
                    .SetPlayerName(PlayerName)
                    .SetMembership(Membership)
                    .Build();
            }

            public bool SaveToDatabase()
            {
                if (PersistenceEnabled == false)
                    return true;

                return IDBManager.Instance.SaveGuildMember(_data);
            }

            public bool DeleteFromDatabase()
            {
                if (PersistenceEnabled == false)
                    return true;

                return IDBManager.Instance.DeleteGuildMember(_data);
            }

            private string GetPlayerName()
            {
                ulong playerDbId = PlayerDbId;

                // Doing lookups every time is somewhat suboptimal, but it's more straightforward than
                // keeping guild members in sync if a member's name changes. Reevaluate this if needed.
                if (PlayerNameCache.Instance.TryGetPlayerName(playerDbId, out string playerName) == false)
                    return $"0x{playerDbId:X}";

                return playerName;
            }
        }
    }
}
