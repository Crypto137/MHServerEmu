using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Network
{
    public static class MigrationUtility
    {
        // We have everything in a self-contained server, so we can get away with just storing our migration data in a runtime object.

        public static void StoreTransferParams(MigrationData migrationData, TransferParams transferParams)
        {
            migrationData.TransferParams = transferParams.ToProtobuf();
        }

        public static void RestoreTransferParams(MigrationData migrationData, TransferParams transferParams)
        {
            transferParams.FromProtobuf(migrationData.TransferParams);
        }

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
    }
}
