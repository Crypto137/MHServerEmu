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

        public static void Store(MigrationData migrationData, Entity entity)
        {
            StoreProperties(migrationData, entity);

            if (entity is Player player)
            {
                StoreWorldView(migrationData, player.PlayerConnection.WorldView);
                StoreMatchQueueStatus(migrationData, player.MatchQueueStatus);
                StoreCommunity(migrationData, player.Community);
            }
            else if (entity is Agent agent)
            {
                foreach (WorldEntity summon in new SummonedEntityIterator(agent))
                    StoreProperties(migrationData, summon);
            }
        }

        public static void Restore(MigrationData migrationData, Entity entity)
        {
            RestoreProperties(migrationData, entity);

            if (entity is Player player)
            {
                RestoreWorldView(migrationData, player.PlayerConnection.WorldView);
                RestoreMatchQueueStatus(migrationData, player.MatchQueueStatus);
                RestoreCommunity(migrationData, player.Community);
            }
            else if (entity is Agent agent)
            {
                foreach (WorldEntity summon in new SummonedEntityIterator(agent))
                    RestoreProperties(migrationData, summon);
            }
        }

        private static void StoreProperties(MigrationData migrationData, Entity entity)
        {
            List<(ulong, ulong)> propertyList = migrationData.GetOrCreatePropertyList(entity.DatabaseUniqueId);
            propertyList.Clear();
            entity.Properties.GetPropertiesForMigration(propertyList);
        }

        private static void RestoreProperties(MigrationData migrationData, Entity entity)
        {
            PropertyCollection properties = entity.Properties;
            ulong entityDbId = entity.DatabaseUniqueId;

            List<(ulong, ulong)> propertyList = migrationData.GetOrCreatePropertyList(entityDbId);

            foreach (var kvp in propertyList)
            {
                PropertyId propertyId = new(kvp.Item1);
                PropertyValue propertyValue = kvp.Item2;
                properties[propertyId] = propertyValue;
            }

            // Clean up lists for runtime entities (e.g. summons).
            switch (entity.Prototype)
            {
                case PlayerPrototype:
                case AvatarPrototype:
                case AgentTeamUpPrototype:
                    break;

                default:
                    migrationData.RemovePropertyList(entityDbId);
                    break;
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
