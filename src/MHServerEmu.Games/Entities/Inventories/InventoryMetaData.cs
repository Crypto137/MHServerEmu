using System.Text;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.Items;

namespace MHServerEmu.Games.Entities.Inventories
{
    // Serialized for NetMessageInventoryLoaded / NetMessageInventoryArchivedEntity

    public class InventoryMetaData : ISerialize
    {
        public InventoryMetaDataType Type { get; protected set; }

        public InventoryMetaData()
        {
            Type = InventoryMetaDataType.Parent;
        }

        public virtual bool Serialize(Archive archive)
        {
            return true;
        }

        public static bool SerializePtr(Archive archive, ref InventoryMetaData metaData)
        {
            if (archive.IsPacking)
            {
                byte type = 0;
                if (metaData != null) type = (byte)metaData.Type;
                Serializer.Transfer(archive, ref type);
            }
            else
            {
                byte type = 0;
                Serializer.Transfer(archive, ref type);

                switch ((InventoryMetaDataType)type)
                {
                    case InventoryMetaDataType.Parent:  metaData = new InventoryMetaData(); break;
                    case InventoryMetaDataType.Item:    metaData = new ItemMetaData(); break;
                }
            }

            metaData?.Serialize(archive);

            return true;
        }
    }

    public class ItemMetaData : InventoryMetaData
    {
        // See Item::GetInventoryMetaData()
        public ItemSpec ItemSpec;
        public float DamageRating;
        public float DefenseRating;
        public float DamageReductionPctFromGear;

        public ItemMetaData()
        {
            Type = InventoryMetaDataType.Item;
        }

        public override bool Serialize(Archive archive)
        {
            bool success = base.Serialize(archive);
            success &= Serializer.Transfer(archive, ref ItemSpec);
            success &= Serializer.Transfer(archive, ref DamageRating);
            success &= Serializer.Transfer(archive, ref DefenseRating);
            success &= Serializer.Transfer(archive, ref DamageReductionPctFromGear);
            return success;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(ItemSpec)}: {ItemSpec}");
            sb.AppendLine($"{nameof(DamageRating)}: {DamageRating}f");
            sb.AppendLine($"{nameof(DefenseRating)}: {DefenseRating}f");
            sb.AppendLine($"{nameof(DamageReductionPctFromGear)}: {DamageReductionPctFromGear}f");
            return sb.ToString();
        }
    }
}
