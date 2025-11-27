using Gazillion;
using MHServerEmu.Core.Serialization;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Regions.MatchQueues;
using MHServerEmu.Games.Social.Communities;

namespace MHServerEmu.Games.Network
{
    public static class MigrationUtility
    {
        // We have everything in a self-contained server, so we can get away with just storing our migration data in a runtime object.
        // We can potentially use a separate archive that would contain just the migration data.

        // TODO: Migrate summoned inventory.

        public static void Store(MigrationData migrationData, Entity entity)
        {
            // TODO: Migrate avatar properties (e.g. endurance).
            if (entity is not Player player)
                return;

            StoreProperties(migrationData.PlayerProperties, player.Properties);
            StoreWorldView(migrationData, player.PlayerConnection.WorldView);
            StoreMatchQueueStatus(migrationData, player.MatchQueueStatus);
            StoreCommunity(migrationData, player.Community);
        }

        public static void Restore(MigrationData migrationData, Entity entity)
        {
            // TODO: Migrate avatar properties (e.g. endurance).
            if (entity is not Player player)
                return;

            RestoreProperties(migrationData.PlayerProperties, player.Properties);
            RestoreWorldView(migrationData, player.PlayerConnection.WorldView);
            RestoreMatchQueueStatus(migrationData, player.MatchQueueStatus);
            RestoreCommunity(migrationData, player.Community);
        }

        private static void StoreProperties(List<KeyValuePair<ulong, ulong>> migrationDataList, PropertyCollection properties)
        {
            migrationDataList.Clear();

            PropertyEnum prevProperty = PropertyEnum.Invalid;
            PropertyInfoPrototype propInfoProto = null;
            foreach (var kvp in properties)
            {
                PropertyEnum propertyEnum = kvp.Key.Enum;
                if (propertyEnum != prevProperty)
                {
                    PropertyInfo propInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyEnum);
                    propInfoProto = propInfo.Prototype;
                    prevProperty = propertyEnum;
                }

                // Migrate properties that are not saved to the database, but are supposed to be replicated for transfer
                if (propInfoProto.ReplicateToDatabase == DatabasePolicy.None && propInfoProto.ReplicateForTransfer)
                    migrationDataList.Add(new(kvp.Key.Raw, kvp.Value));
            }
        }

        private static void RestoreProperties(List<KeyValuePair<ulong, ulong>> migrationDataList, PropertyCollection properties)
        {
            foreach (var kvp in migrationDataList)
            {
                PropertyId propertyId = new(kvp.Key);
                PropertyValue propertyValue = kvp.Value;
                properties[propertyId] = propertyValue;
            }
        }

        private static void StoreWorldView(MigrationData migrationData, WorldViewCache worldView)
        {
            List<(ulong, ulong)> worldViewData = migrationData.WorldView;
            worldViewData.Clear();

            foreach ((PrototypeId regionProtoRef, ulong regionId) in worldView)
                worldViewData.Add((regionId, (ulong)regionProtoRef));
        }

        private static void RestoreWorldView(MigrationData migrationData, WorldViewCache worldView)
        {
            worldView.Sync(migrationData.WorldView);
        }

        private static void StoreMatchQueueStatus(MigrationData migrationData, MatchQueueStatus matchQueueStatus)
        {
            // Do not serialize unless we have actual queue data.
            if (matchQueueStatus.Count == 0)
            {
                migrationData.MatchQueueStatus = null;
                return;
            }

            // We don't have the server migration mode properly implemented, so just use the client replication mode for now.
            using Archive archive = new(ArchiveSerializeType.Replication);
            matchQueueStatus.Serialize(archive);

            migrationData.MatchQueueStatus = archive.AccessAutoBuffer().ToArray();
        }

        private static void RestoreMatchQueueStatus(MigrationData migrationData, MatchQueueStatus matchQueueStatus)
        {
            // Do not deserialize unless we have actual queue data.
            if (migrationData.MatchQueueStatus == null)
                return;

            // We don't have the server migration mode properly implemented, so just use the client replication mode for now.
            using Archive archive = new(ArchiveSerializeType.Replication, migrationData.MatchQueueStatus);
            matchQueueStatus.Serialize(archive);
        }

        private static void StoreCommunity(MigrationData migrationData, Community community)
        {
            migrationData.CommunityStatus.Clear();
            foreach (CommunityMember member in community.IterateMembers())
            {
                if (member.IsOnline != CommunityMemberOnlineStatus.Online)
                    continue;

                var broadcast = CommunityMemberBroadcast.CreateBuilder()
                    .SetMemberPlayerDbId(member.DbId)
                    .SetCurrentRegionRefId((ulong)member.RegionRef)
                    .SetCurrentDifficultyRefId((ulong)member.DifficultyRef)
                    .SetIsOnline((int)member.IsOnline);

                AvatarSlotInfo slot = member.GetAvatarSlotInfo();
                if (slot != null)
                {
                    broadcast.AddSlots(CommunityMemberAvatarSlot.CreateBuilder()
                        .SetAvatarRefId((ulong)slot.AvatarRef)
                        .SetCostumeRefId((ulong)slot.CostumeRef)
                        .SetLevel((uint)slot.Level)
                        .SetPrestigeLevel((uint)slot.PrestigeLevel));
                }

                migrationData.CommunityStatus.Add(broadcast.Build());
            }
        }

        private static void RestoreCommunity(MigrationData migrationData, Community community)
        {
            foreach (CommunityMemberBroadcast broadcast in migrationData.CommunityStatus)
            {
                CommunityMember member = community.GetMember(broadcast.MemberPlayerDbId);
                if (member == null)
                    continue;

                member.ReceiveBroadcast(broadcast, false);
            }

            // Clear status in the migration data to allow the garbage colelctor to reclaim it asap
            migrationData.CommunityStatus.Clear();
        }
    }
}
