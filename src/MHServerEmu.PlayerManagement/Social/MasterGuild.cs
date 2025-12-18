using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.PlayerManagement.Social
{
    public class MasterGuild
    {
        // NOTE: All guild DB operations are currently synchronous.
        // May need some kind of async job queue for these, especially for potential non-SQLite backends.
        private readonly DBGuild _data;

        public ulong Id { get => (ulong)_data.Id; }
        public string Name { get => _data.Name; }
        public string Motd { get => _data.Motd; }
        public int MemberCount { get => _data.Members.Count; }

        public MasterGuild(DBGuild data, bool writeToDatabase)
        {
            _data = data;

            if (writeToDatabase)
            {
                /* Disabled for now
                IDBManager db = IDBManager.Instance;

                db.SaveGuild(_data);

                foreach (DBGuildMember member in _data.Members)
                    db.SaveGuildMember(member);
                */
            }
        }

        public override string ToString()
        {
            return _data.ToString();
        }
    }
}
