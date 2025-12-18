using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.PlayerManagement.Social
{
    public class MasterGuild
    {
        private readonly DBGuild _data;

        public ulong Id { get => (ulong)_data.Id; }
        public string Name { get => _data.Name; }
        public string Motd { get => _data.Motd; }
        public int MemberCount { get => _data.Members.Count; }

        public MasterGuild(DBGuild data)
        {
            _data = data;
        }

        public override string ToString()
        {
            return _data.ToString();
        }
    }
}
