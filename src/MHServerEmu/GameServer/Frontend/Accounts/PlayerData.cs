using System.Text.Json.Serialization;
using MHServerEmu.GameServer.Entities;
using MHServerEmu.GameServer.Regions;

namespace MHServerEmu.GameServer.Frontend.Accounts
{
    public class PlayerData
    {
        public ulong AccountId { get; }
        public string PlayerName { get; set; }
        public RegionPrototype Region { get; set; }
        public HardcodedAvatarEntity Avatar { get; set; }
        public ulong CostumeOverride { get; set; }

        public PlayerData(ulong accountId)
        {
            AccountId = accountId;
            PlayerName = "Player";
            Region = RegionPrototype.NPEAvengersTowerHUBRegion;
            Avatar = HardcodedAvatarEntity.BlackCat;
            CostumeOverride = 0;
        }

        [JsonConstructor]
        public PlayerData(ulong accountId, string playerName, RegionPrototype region, HardcodedAvatarEntity avatar, ulong costumeOverride)
        {
            AccountId = accountId;
            PlayerName = playerName;
            Region = region;
            Avatar = avatar;
            CostumeOverride = costumeOverride;
        }
    }
}
