using Google.ProtocolBuffers;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Dialog;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Common
{
    /// <summary>
    /// A <see cref="Dictionary{TKey, TValue}"/> of <see cref="PrototypeId"/> and <see cref="EntityTrackingFlag"/> that implements <see cref="ISerialize"/>.
    /// </summary>
    public class EntityTrackingContextMap : Dictionary<PrototypeId, EntityTrackingFlag>, ISerialize
    {
        // NOTE: Consider making this a wrapper around Dictionary rather than inherit from it.

        public bool Serialize(Archive archive)
        {
            bool success = true;

            ulong numEntries = (ulong)Count;
            success &= Serializer.Transfer(archive, ref numEntries);

            if (archive.IsPacking)
            {
                foreach (var kvp in this)
                {
                    PrototypeId contextRef = kvp.Key;
                    uint flags = (uint)kvp.Value;
                    success &= Serializer.Transfer(archive, ref contextRef);
                    success &= Serializer.Transfer(archive, ref flags);
                }
            }
            else
            {
                Clear();
                for (ulong i = 0; i < numEntries; i++)
                {
                    PrototypeId contextRef = PrototypeId.Invalid;
                    uint flags = 0;
                    success &= Serializer.Transfer(archive, ref contextRef);
                    success &= Serializer.Transfer(archive, ref flags);
                    Add(contextRef, (EntityTrackingFlag)flags);
                }
            }

            return success;
        }

        public void Decode(CodedInputStream stream)
        {
            Clear();
            ulong numEntries = stream.ReadRawVarint64();
            for (ulong i = 0; i < numEntries; i++)
            {
                PrototypeId context = stream.ReadPrototypeRef<Prototype>();
                uint flags = stream.ReadRawVarint32();
                Add(context, (EntityTrackingFlag)flags);
            }
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint64((ulong)Count);
            foreach (var kvp in this)
            {
                stream.WritePrototypeRef<Prototype>(kvp.Key);
                stream.WriteRawVarint32((uint)kvp.Value);
            }
        }

        // Gazillion::EntityTrackingContextMapInsert()
        public void Insert(PrototypeId contextRef, EntityTrackingFlag flag)
        {
            if (ContainsKey(contextRef))
                this[contextRef] |= flag;
            else
                Add(contextRef, flag);
        }
    }
}
