using Gazillion;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Social.Communities;

namespace MHServerEmu.Games.Network
{
    public static class MigrationUtility
    {
        // We have everything in a self-contained server, so we can get away with just storing our migration data in a runtime object.

        public static void StoreProperties(List<KeyValuePair<ulong, ulong>> migrationDataList, PropertyCollection properties)
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

        public static void RestoreProperties(List<KeyValuePair<ulong, ulong>> migrationDataList, PropertyCollection properties)
        {
            foreach (var kvp in migrationDataList)
            {
                PropertyId propertyId = new(kvp.Key);
                PropertyValue propertyValue = kvp.Value;
                properties[propertyId] = propertyValue;
            }
        }

        public static void StoreWorldView(MigrationData migrationData, WorldViewCache worldView)
        {
            List<(ulong, ulong)> worldViewData = migrationData.WorldView;
            worldViewData.Clear();

            foreach ((PrototypeId regionProtoRef, ulong regionId) in worldView)
                worldViewData.Add((regionId, (ulong)regionProtoRef));
        }

        public static void RestoreWorldView(MigrationData migrationData, WorldViewCache worldView)
        {
            worldView.Sync(migrationData.WorldView);
        }

        public static void StoreCommunity(MigrationData migrationData, Community community)
        {
            migrationData.CommunityStatus.Clear();
            foreach (CommunityMember member in community.IterateMembers())
            {
                if (member.IsOnline != CommunityMemberOnlineStatus.Online)
                    continue;

                var broadcast = CommunityMemberBroadcast.CreateBuilder()
                    .SetMemberPlayerDbId(member.DbId)
                    .SetCurrentRegionRefId((ulong)member.RegionRef)
                    .SetCurrentAvatarRefId((ulong)member.AvatarRef)
                    .SetCurrentCostumeRefId((ulong)member.CostumeRef)
                    .SetCurrentCharacterLevel((uint)member.CharacterLevel)
                    .SetCurrentPrestigeLevel((uint)member.PrestigeLevel)
                    .SetIsOnline((int)member.IsOnline);

                migrationData.CommunityStatus.Add(broadcast.Build());
            }
        }

        public static void RestoreCommunity(MigrationData migrationData, Community community)
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
