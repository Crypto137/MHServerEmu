namespace MHServerEmu.DatabaseAccess.Models
{
    public class DBGuildMember
    {
        public long PlayerDbGuid { get; set; }
        public long GuildId { get; set; }
        public long Membership { get; set; }    // This needs to be long for our Dapper/System.Data.SQLite combo.

        public DBGuildMember(long playerDbGuid, long guildId, long membership)
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
