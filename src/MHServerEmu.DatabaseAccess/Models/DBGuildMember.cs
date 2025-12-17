namespace MHServerEmu.DatabaseAccess.Models
{
    public class DBGuildMember
    {
        public long PlayerDbGuid { get; set; }
        public long GuildId { get; set; }
        public int Membership { get; set; }

        public DBGuildMember(long playerDbGuid, long guildId, int membership)
        {
            PlayerDbGuid = playerDbGuid;
            GuildId = guildId;
            Membership = membership;
        }

        public override string ToString()
        {
            return $"guildId={GuildId}, playerDbGuid=0x{PlayerDbGuid:X}, membership={Membership}";
        }
    }
}
