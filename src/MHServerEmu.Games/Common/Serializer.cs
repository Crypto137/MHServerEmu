using Google.ProtocolBuffers;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Common
{
    /// <summary>
    /// Helper class for serializing data to and from <see cref="Archive"/> instances.
    /// </summary>
    public static class Serializer
    {
        #region Old

        /// <summary>
        /// Reads a prototype enum value for the specified class from the stream and converts it to a data ref.
        /// </summary>
        public static PrototypeId ReadPrototypeRef<T>(this CodedInputStream stream) where T : Prototype
        {
            return GameDatabase.DataDirectory.GetPrototypeFromEnumValue<T>((int)stream.ReadRawVarint64());
        }

        /// <summary>
        /// Converts a prototype data ref to an enum value for the specified class and writes it to the stream.
        /// </summary>
        public static void WritePrototypeRef<T>(this CodedOutputStream stream, PrototypeId prototypeId) where T : Prototype
        {
            stream.WriteRawVarint64((ulong)GameDatabase.DataDirectory.GetPrototypeEnumValue<T>(prototypeId));
        }

        #endregion

        // Basic types supported by archives

        public static bool Transfer(Archive archive, ref bool ioData) => archive.Transfer(ref ioData);
        public static bool Transfer(Archive archive, ref ushort ioData) => archive.Transfer(ref ioData);
        public static bool Transfer(Archive archive, ref int ioData) => archive.Transfer(ref ioData);
        public static bool Transfer(Archive archive, ref uint ioData) => archive.Transfer(ref ioData);
        public static bool Transfer(Archive archive, ref long ioData) => archive.Transfer(ref ioData);
        public static bool Transfer(Archive archive, ref ulong ioData) => archive.Transfer(ref ioData);
        public static bool Transfer(Archive archive, ref float ioData) => archive.Transfer(ref ioData);
        public static bool Transfer(Archive archive, ref ISerialize ioData) => archive.Transfer(ref ioData);
        public static bool Transfer(Archive archive, ref Vector3 ioData) => archive.Transfer(ref ioData);
        public static bool TransferFloatFixed(Archive archive, ref float ioData, int precision) => archive.TransferFloatFixed(ref ioData, precision);
        public static bool TransferVectorFixed(Archive archive, ref Vector3 ioData, int precision) => archive.TransferVectorFixed(ref ioData, precision);
        public static bool TransferOrientationFixed(Archive archive, ref Orientation ioData, bool yawOnly, int precision) => archive.TransferOrientationFixed(ref ioData, yawOnly, precision);

        // Data Refs

        public static bool Transfer(Archive archive, ref AssetId ioData)
        {
            bool success = true;

            // TODO: IsPersistent

            if (archive.IsPacking)
            {
                ulong dataId = (ulong)ioData;
                success &= Transfer(archive, ref dataId);
            }
            else
            {
                ulong dataId = 0;
                success &= Transfer(archive, ref dataId);
                ioData = (AssetId)dataId;
            }

            return success;
        }

        public static bool Transfer(Archive archive, ref PrototypeId ioData)
        {
            bool success = true;

            // TODO: IsPersistent

            if (archive.IsPacking)
            {
                uint enumValue = (uint)GameDatabase.DataDirectory.GetPrototypeEnumValue<Prototype>(ioData);
                success &= Transfer(archive, ref enumValue);
            }
            else
            {
                uint enumValue = 0;
                success &= Transfer(archive, ref enumValue);
                ioData = GameDatabase.DataDirectory.GetPrototypeFromEnumValue<Prototype>((int)enumValue);
            }

            return success;
        }

        public static bool TransferPrototypeEnum<T>(Archive archive, ref PrototypeId ioData) where T: Prototype
        {
            bool success = true;

            // TODO: IsPersistent

            if (archive.IsPacking)
            {
                uint enumValue = (uint)GameDatabase.DataDirectory.GetPrototypeEnumValue<T>(ioData);
                success &= Transfer(archive, ref enumValue);
            }
            else
            {
                uint enumValue = 0;
                success &= Transfer(archive, ref enumValue);
                ioData = GameDatabase.DataDirectory.GetPrototypeFromEnumValue<T>((int)enumValue);
            }

            return success;
        }

        // Properties
        public static bool Transfer(Archive archive, ref PropertyId ioData)
        {
            bool success = true;

            // Id is reversed so that it can be efficiently encoded into varint when all params are 0

            if (archive.IsPacking)
            {
                ulong id = ioData.Raw.ReverseBytes();
                success &= Transfer(archive, ref id);
            }
            else
            {
                ulong id = 0;
                success &= Transfer(archive, ref id);
                ioData = new(id.ReverseBytes());
            }

            return success;
        }

        public static bool Transfer(Archive archive, ref PropertyValue ioData)
        {
            bool success = true;

            if (archive.IsPacking)
            {
                ulong value = ioData;
                success &= Transfer(archive, ref value);
            }
            else
            {
                ulong value = 0;
                success &= Transfer(archive, ref value);
                ioData = value;
            }

            return success;
        }

        // Other

        public static bool Transfer(Archive archive, ref TimeSpan ioData)
        {
            bool success = true;

            if (archive.IsPacking)
            {
                long microseconds = ioData.Ticks / 10;
                success &= Transfer(archive, ref microseconds);
            }
            else
            {
                long microseconds = 0;
                success &= Transfer(archive, ref microseconds);
                ioData = new(microseconds * 10);
            }

            return success;
        }

        // Collections

        public static bool Transfer(Archive archive, ref ulong[] ioData)
        {
            bool success = true;

            if (archive.IsPacking)
            {
                ulong length = (ulong)ioData.Length;
                success &= Transfer(archive, ref length);
                for (int i = 0; i < ioData.Length; i++)
                {
                    ulong value = ioData[i];
                    success &= Transfer(archive, ref value);
                }
            }
            else
            {
                ulong length = 0;
                success &= Transfer(archive, ref length);
                ioData = new ulong[(int)length];
                for (int i = 0; i < ioData.Length; i++)
                {
                    ulong value = 0;
                    success &= Transfer(archive, ref value);
                    ioData[i] = value;
                }
            }

            return success;
        }

        public static bool Transfer(Archive archive, ref List<PrototypeId> ioData)
        {
            bool success = true;

            if (archive.IsPacking)
            {
                ulong count = (ulong)ioData.Count;
                success &= Transfer(archive, ref count);
                for (int i = 0; i < ioData.Count; i++)
                {
                    ulong value = (ulong)ioData[i];
                    success &= Transfer(archive, ref value);
                }
            }
            else
            {
                ulong count = 0;
                success &= Transfer(archive, ref count);
                ioData = new((int)count);
                for (ulong i = 0; i < count; i++)
                {
                    ulong value = 0;
                    success &= Transfer(archive, ref value);
                    ioData.Add((PrototypeId)value);
                }
            }

            return success;
        }


        // Class-specific
        
    }
}
