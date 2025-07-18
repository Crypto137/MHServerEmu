using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Network
{
    public static class MigrationUtility
    {
        // We have everything in a self-contained server, so we can get away with just storing our migration data in a runtime object.

        public static void StoreTransferParams(MigrationData migrationData, TransferParams transferParams)
        {
            migrationData.DestTargetRegionProtoId = (ulong)transferParams.DestTargetRegionProtoRef;
            migrationData.DestTargetAreaProtoId = (ulong)transferParams.DestTargetAreaProtoRef;
            migrationData.DestTargetCellProtoId = (ulong)transferParams.DestTargetCellProtoRef;
            migrationData.DestTargetEntityProtoId = (ulong)transferParams.DestTargetEntityProtoRef;
        }

        public static void RestoreTransferParams(MigrationData migrationData, TransferParams transferParams)
        {
            PrototypeId regionProtoRef = (PrototypeId)migrationData.DestTargetRegionProtoId;
            PrototypeId areaProtoRef = (PrototypeId)migrationData.DestTargetAreaProtoId;
            PrototypeId cellProtoRef = (PrototypeId)migrationData.DestTargetCellProtoId;
            PrototypeId entityProtoRef = (PrototypeId)migrationData.DestTargetEntityProtoId;

            transferParams.SetTarget(regionProtoRef, areaProtoRef, cellProtoRef, entityProtoRef);
        }

        public static void StoreBodyslideProperties(MigrationData migrationData, Player player)
        {
            migrationData.BodySliderRegionId = player.Properties[PropertyEnum.BodySliderRegionId];
            migrationData.BodySliderRegionRef = player.Properties[PropertyEnum.BodySliderRegionRef];
            migrationData.BodySliderDifficultyRef = player.Properties[PropertyEnum.BodySliderDifficultyRef];
            migrationData.BodySliderRegionSeed = player.Properties[PropertyEnum.BodySliderRegionSeed];
            migrationData.BodySliderAreaRef = player.Properties[PropertyEnum.BodySliderAreaRef];
            migrationData.BodySliderRegionPos = player.Properties[PropertyEnum.BodySliderRegionPos];
        }

        public static void RestoreBodyslideProperties(MigrationData migrationData, Player player)
        {
            player.Properties[PropertyEnum.BodySliderRegionId] = migrationData.BodySliderRegionId;
            player.Properties[PropertyEnum.BodySliderRegionRef] = migrationData.BodySliderRegionRef;
            player.Properties[PropertyEnum.BodySliderDifficultyRef] = migrationData.BodySliderDifficultyRef;
            player.Properties[PropertyEnum.BodySliderRegionSeed] = migrationData.BodySliderRegionSeed;
            player.Properties[PropertyEnum.BodySliderAreaRef] = migrationData.BodySliderAreaRef;
            player.Properties[PropertyEnum.BodySliderRegionPos] = migrationData.BodySliderRegionPos;
        }
    }
}
