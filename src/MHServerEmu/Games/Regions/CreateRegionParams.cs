using Gazillion;

namespace MHServerEmu.Games.Regions
{
    public class CreateRegionParams
    {
        public uint Level { get; set; }
        public DifficultyTier DifficultyTier { get; set; }

        /* todo
            optional NetStructRegionOrigin  origin	= 2;
	        optional bool cheat = 3;
	        optional uint32 endlessLevel	= 5 [default = 0];
	        optional uint64 gameStateId	= 6;
	        optional uint64 matchNumber	= 7 [default = 0];
	        optional uint32 seed	= 8;
	        optional uint64 parentRegionId	= 9;
	        optional uint64 requiredItemProtoId	= 10;
	        optional uint64 requiredItemEntityId	= 11;
	        optional NetStructPortalInstance    accessPortal	= 12;
	        repeated uint64 affixes	= 13;
	        optional uint32 playerDeaths	= 14 [default = 0];
	        optional uint64 dangerRoomScenarioItemDbGuid	= 15 [default = 0];
	        optional uint64 itemRarity	= 16 [default = 0];
	        optional bytes  propertyBuffer	= 17;
	        optional uint64 dangerRoomScenarioR	= 18;
        */

        public CreateRegionParams(uint level, DifficultyTier difficultyTier)
        {
            Level = level;
            DifficultyTier = difficultyTier;
        }

        public NetStructCreateRegionParams ToNetStruct()
        {
            return NetStructCreateRegionParams.CreateBuilder()
                .SetLevel(Level)
                .SetDifficultyTierProtoId((ulong)DifficultyTier)
                .Build();
        }
    }
}
