using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Entities.Persistence
{
    public static class PlayerVersioning
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static bool Apply(Player player)
        {
            ArchiveVersion loadedVersion = player.LastSerializedArchiveVersion;

            while (loadedVersion < ArchiveVersion.Current)
            {
                bool success = true;

                switch (loadedVersion)
                {
                    case ArchiveVersion.Initial:
                        success |= V2_ClearProperties(player);
                        success |= V2_MoveToTutorial(player);
                        break;
                }

                if (success == false)
                    return Logger.WarnReturn(false, $"ApplyVersioning(): Failed to apply versioning to loaded version {loadedVersion}");

                Logger.Trace($"Applied versioning to archive version {loadedVersion} for player [{player}]");
                loadedVersion++;
            }

            return true;
        }

        #region V2

        private static bool V2_ClearProperties(Player player)
        {
            player.Properties.RemovePropertyRange(PropertyEnum.Waypoint);
            player.Properties.RemovePropertyRange(PropertyEnum.UISystemLock);
            return true;
        }

        private static bool V2_MoveToTutorial(Player player)
        {
            // Reset avatar to the default starting one
            PrototypeId startingAvatarProtoRef = GameDatabase.GlobalsPrototype.DefaultStartingAvatarPrototype;
            Avatar currentAvatar = player.CurrentAvatar;

            if (currentAvatar == null || currentAvatar.PrototypeDataRef != startingAvatarProtoRef)
            {
                Inventory avatarInPlay = player.GetInventory(InventoryConvenienceLabel.AvatarInPlay);
                if (avatarInPlay == null) return Logger.WarnReturn(false, "V2_MoveToTutorial(): avatarInPlay == null");

                Avatar startingAvatar = player.GetAvatar(startingAvatarProtoRef);
                if (startingAvatar == null) return Logger.WarnReturn(false, "V2_MoveToTutorial(): startingAvatar == null");

                InventoryResult result = startingAvatar.ChangeInventoryLocation(avatarInPlay, 0);
                if (result != InventoryResult.Success) return Logger.WarnReturn(false, $"V2_MoveToTutorial(): Failed to swap avatar to {startingAvatarProtoRef.GetName()}");
            }

            // Move to the default starting region
            PrototypeId startingRegionTargetProtoRef = GameDatabase.GlobalsPrototype.DefaultStartTargetStartingRegion;
            TransferParams transferParams = player.PlayerConnection.TransferParams;

            if (transferParams.DestTargetProtoRef != startingRegionTargetProtoRef)
                transferParams.SetTarget(startingRegionTargetProtoRef);

            return true;
        }

        #endregion
    }
}
