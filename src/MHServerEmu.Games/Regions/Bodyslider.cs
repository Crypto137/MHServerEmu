using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.MetaGames;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Regions
{
    public static class Bodyslider
    {
        public static PrototypeId GetBodyslideTargetRef(Player player)
        {
            // Based on Bodyslider::GetBodyslideReturnToRegionName() from the client.

            // Check MetaGame overrides
            PrototypeId targetProtoRef = GetBodyslideRegionConnectionTargetOverride(player);

            // Check last town
            if (targetProtoRef == PrototypeId.Invalid)
            {
                // BodySliderTarget appears to be unused in 1.52.
                PrototypeId regionProtoRef = player.Properties[PropertyEnum.LastTownRegionForAccount];
                RegionPrototype regionProto = regionProtoRef.As<RegionPrototype>();
                if (regionProto != null)
                    targetProtoRef = regionProto.StartTarget;
            }

            // Use the fallback if everything else failed
            if (targetProtoRef == PrototypeId.Invalid)
                targetProtoRef = GameDatabase.GlobalsPrototype.DefaultStartTargetFallbackRegion;

            return targetProtoRef;
        }

        private static PrototypeId GetBodyslideRegionConnectionTargetOverride(Player player)
        {
            Region currentRegion = player.GetRegion();
            if (!Verify.IsNotNull(currentRegion)) return PrototypeId.Invalid;

            RegionPrototype currentRegionProto = currentRegion.Prototype;
            if (!Verify.IsNotNull(currentRegionProto)) return PrototypeId.Invalid;

            PrototypeId targetOverrideProtoRef = PrototypeId.Invalid;

            MetaGameTeam pvpTeam = player.GetPvPTeam();
            if (pvpTeam != null)
                targetOverrideProtoRef = currentRegion.Properties[PropertyEnum.RegionBodysliderTargetOverride, pvpTeam.ProtoRef];

            if (targetOverrideProtoRef == PrototypeId.Invalid)
            {
                PrototypeId metaGameTeamBase = GameDatabase.GlobalsPrototype.MetaGameTeamBase;
                targetOverrideProtoRef = currentRegion.Properties[PropertyEnum.RegionBodysliderTargetOverride, metaGameTeamBase];
            }

            return targetOverrideProtoRef;
        }
    }
}
