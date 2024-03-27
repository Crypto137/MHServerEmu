using Google.ProtocolBuffers;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Common
{
    /// <summary>
    /// Helper class for serializing data to and from <see cref="Archive"/> instances.
    /// </summary>
    public static class Serializer
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

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
        // TODO: Find a good way to make this more DRY without sacrificing performance

        #region Arrays

        public static bool Transfer(Archive archive, ref ulong[] ioData)
        {
            bool success = true;

            if (archive.IsPacking)
            {
                ulong numElements = (ulong)ioData.Length;
                success &= Transfer(archive, ref numElements);
                for (int i = 0; i < ioData.Length; i++)
                {
                    ulong value = ioData[i];
                    success &= Transfer(archive, ref value);
                }
            }
            else
            {
                Array.Clear(ioData);

                ulong numElements = 0;
                success &= Transfer(archive, ref numElements);

                if (ioData.Length < (int)numElements)
                    Logger.Warn($"Transfer(): Array length {ioData} is not enough to hold {numElements} elements");
                
                for (int i = 0; i < ioData.Length; i++)
                {
                    ulong value = 0;
                    success &= Transfer(archive, ref value);
                    ioData[i] = value;
                }

                // Elements outside the range of the provided array are discarded
                for (ulong i = (ulong)ioData.Length; i < numElements; i++)
                {
                    ulong value = 0;
                    success &= Transfer(archive, ref value);
                }
            }

            return success;
        }

        public static bool Transfer(Archive archive, ref long[] ioData)
        {
            bool success = true;

            if (archive.IsPacking)
            {
                ulong numElements = (ulong)ioData.Length;
                success &= Transfer(archive, ref numElements);
                for (int i = 0; i < ioData.Length; i++)
                {
                    long value = ioData[i];
                    success &= Transfer(archive, ref value);
                }
            }
            else
            {
                Array.Clear(ioData);

                ulong numElements = 0;
                success &= Transfer(archive, ref numElements);

                if (ioData.Length < (int)numElements)
                    Logger.Warn($"Transfer(): Array length {ioData} is not enough to hold {numElements} elements");

                for (int i = 0; i < ioData.Length; i++)
                {
                    long value = 0;
                    success &= Transfer(archive, ref value);
                    ioData[i] = value;
                }

                // Elements outside the range of the provided array are discarded
                for (ulong i = (ulong)ioData.Length; i < numElements; i++)
                {
                    long value = 0;
                    success &= Transfer(archive, ref value);
                }
            }

            return success;
        }

        public static bool Transfer(Archive archive, ref PrototypeId[] ioData)
        {
            bool success = true;

            if (archive.IsPacking)
            {
                ulong numElements = (ulong)ioData.Length;
                success &= Transfer(archive, ref numElements);
                for (int i = 0; i < ioData.Length; i++)
                {
                    PrototypeId value = ioData[i];
                    success &= Transfer(archive, ref value);
                }
            }
            else
            {
                Array.Clear(ioData);

                ulong numElements = 0;
                success &= Transfer(archive, ref numElements);

                if (ioData.Length < (int)numElements)
                    Logger.Warn($"Transfer(): Array length {ioData} is not enough to hold {numElements} elements");

                for (int i = 0; i < ioData.Length; i++)
                {
                    PrototypeId value = PrototypeId.Invalid;
                    success &= Transfer(archive, ref value);
                    ioData[i] = value;
                }

                // Elements outside the range of the provided array are discarded
                for (ulong i = (ulong)ioData.Length; i < numElements; i++)
                {
                    ulong value = 0;
                    success &= Transfer(archive, ref value);
                }
            }

            return success;
        }

        public static bool Transfer<T>(Archive archive, ref T[] ioData) where T: ISerialize, new()
        {
            bool success = true;

            if (archive.IsPacking)
            {
                ulong numElements = (ulong)ioData.Length;
                success &= Transfer(archive, ref numElements);
                for (int i = 0; i < ioData.Length; i++)
                {
                    ISerialize value = ioData[i];
                    success &= Transfer(archive, ref value);
                }
            }
            else
            {
                Array.Clear(ioData);

                ulong numElements = 0;
                success &= Transfer(archive, ref numElements);

                if (ioData.Length < (int)numElements)
                    Logger.Warn($"Transfer(): Array length {ioData} is not enough to hold {numElements} elements");

                for (int i = 0; i < ioData.Length; i++)
                {
                    ISerialize value = new T();
                    success &= Transfer(archive, ref value);
                    ioData[i] = (T)value;
                }

                // Elements outside the range of the provided array are discarded
                for (ulong i = (ulong)ioData.Length; i < numElements; i++)
                {
                    ISerialize value = new T();
                    success &= Transfer(archive, ref value);
                }
            }

            return success;
        }

        #endregion

        #region Lists

        public static bool Transfer(Archive archive, ref List<ulong> ioData)
        {
            bool success = true;

            if (archive.IsPacking)
            {
                ulong numElements = (ulong)ioData.Count;
                success &= Transfer(archive, ref numElements);
                foreach (ulong data in ioData)
                {
                    ulong value = data;
                    success &= Transfer(archive, ref value);
                }
            }
            else
            {
                ioData.Clear();

                ulong numElements = 0;
                success &= Transfer(archive, ref numElements);

                for (ulong i = 0; i < numElements; i++)
                {
                    ulong value = 0;
                    success &= Transfer(archive, ref value);
                    ioData.Add(value);
                }
            }

            return success;
        }

        public static bool Transfer(Archive archive, ref List<PrototypeId> ioData)
        {
            bool success = true;

            if (archive.IsPacking)
            {
                ulong numElements = (ulong)ioData.Count;
                success &= Transfer(archive, ref numElements);
                foreach (PrototypeId prototypeRef in ioData)
                {
                    PrototypeId value = prototypeRef;
                    success &= Transfer(archive, ref value);
                }
            }
            else
            {
                ioData.Clear();

                ulong numElements = 0;
                success &= Transfer(archive, ref numElements);

                for (ulong i = 0; i < numElements; i++)
                {
                    PrototypeId value = 0;
                    success &= Transfer(archive, ref value);
                    ioData.Add(value);
                }
            }

            return success;
        }

        public static bool Transfer(Archive archive, ref List<PrototypeGuid> ioData)
        {
            bool success = true;

            if (archive.IsPacking)
            {
                ulong numElements = (ulong)ioData.Count;
                success &= Transfer(archive, ref numElements);
                foreach (PrototypeGuid prototypeGuid in ioData)
                {
                    ulong value = (ulong)prototypeGuid;
                    success &= Transfer(archive, ref value);
                }
            }
            else
            {
                ioData.Clear();

                ulong numElements = 0;
                success &= Transfer(archive, ref numElements);

                for (ulong i = 0; i < numElements; i++)
                {
                    ulong value = 0;
                    success &= Transfer(archive, ref value);
                    ioData.Add((PrototypeGuid)value);
                }
            }

            return success;
        }

        public static bool Transfer<T>(Archive archive, ref List<T> ioData) where T: ISerialize, new()
        {
            bool success = true;

            if (archive.IsPacking)
            {
                ulong numElements = (ulong)ioData.Count;
                success &= Transfer(archive, ref numElements);
                foreach (T data in ioData)
                {
                    ISerialize value = data;
                    success &= Transfer(archive, ref value);
                }
            }
            else
            {
                ioData.Clear();

                ulong numElements = 0;
                success &= Transfer(archive, ref numElements);

                for (ulong i = 0; i < numElements; i++)
                {
                    ISerialize value = new T();
                    success &= Transfer(archive, ref value);
                    ioData.Add((T)value);
                }
            }

            return success;
        }

        #endregion

        #region Dictionaries

        public static bool Transfer<T>(Archive archive, ref Dictionary<PrototypeId, T> ioData) where T: ISerialize, new()
        {
            bool success = true;

            if (archive.IsPacking)
            {
                ulong numElements = (ulong)ioData.Count;
                success &= Transfer(archive, ref numElements);
                foreach (var kvp in ioData)
                {
                    PrototypeId key = kvp.Key;
                    success &= Transfer(archive, ref key);
                    ISerialize value = kvp.Value;
                    success &= Transfer(archive, ref value);
                }
            }
            else
            {
                ioData.Clear();

                ulong numElements = 0;
                success &= Transfer(archive, ref numElements);

                for (ulong i = 0; i < numElements; i++)
                {
                    PrototypeId key = 0;
                    success &= Transfer(archive, ref key);
                    ISerialize value = new T();
                    success &= Transfer(archive, ref value);

                    ioData.Add(key, (T)value);
                }
            }

            return success;
        }

        public static bool Transfer<T>(Archive archive, ref SortedDictionary<uint, T> ioData) where T : ISerialize, new()
        {
            bool success = true;

            if (archive.IsPacking)
            {
                ulong numElements = (ulong)ioData.Count;
                success &= Transfer(archive, ref numElements);

                foreach (var kvp in ioData)
                {
                    uint key = kvp.Key;
                    success &= Transfer(archive, ref key);
                    ISerialize value = kvp.Value;
                    success &= Transfer(archive, ref value);
                }
            }
            else
            {
                ioData.Clear();

                ulong numElements = 0;
                success &= Transfer(archive, ref numElements);

                for (ulong i = 0; i < numElements; i++)
                {
                    uint key = 0;
                    success &= Transfer(archive, ref key);
                    ISerialize value = new T();
                    success &= Transfer(archive, ref value);
                    ioData.Add(key, (T)value);
                }
            }

            return success;
        }

        public static bool Transfer<T>(Archive archive, ref SortedDictionary<PrototypeGuid, T> ioData) where T : ISerialize, new()
        {
            bool success = true;

            if (archive.IsPacking)
            {
                ulong numElements = (ulong)ioData.Count;
                success &= Transfer(archive, ref numElements);

                foreach (var kvp in ioData)
                {
                    ulong key = (ulong)kvp.Key;
                    success &= Transfer(archive, ref key);
                    ISerialize value = kvp.Value;
                    success &= Transfer(archive, ref value);
                }
            }
            else
            {
                ioData.Clear();

                ulong numElements = 0;
                success &= Transfer(archive, ref numElements);

                for (ulong i = 0; i < numElements; i++)
                {
                    ulong key = 0;
                    success &= Transfer(archive, ref key);
                    ISerialize value = new T();
                    success &= Transfer(archive, ref value);
                    ioData.Add((PrototypeGuid)key, (T)value);
                }
            }

            return success;
        }

        public static bool Transfer(Archive archive, ref SortedDictionary<PrototypeId, bool> ioData)
        {
            bool success = true;

            if (archive.IsPacking)
            {
                ulong numElements = (ulong)ioData.Count;
                success &= Transfer(archive, ref numElements);

                foreach (var kvp in ioData)
                {
                    PrototypeId key = kvp.Key;
                    success &= Transfer(archive, ref key);
                    bool value = kvp.Value;
                    success &= Transfer(archive, ref value);
                }
            }
            else
            {
                ioData.Clear();

                ulong numElements = 0;
                success &= Transfer(archive, ref numElements);

                for (ulong i = 0; i < numElements; i++)
                {
                    PrototypeId key = PrototypeId.Invalid;
                    success &= Transfer(archive, ref key);
                    bool value = false;
                    success &= Transfer(archive, ref value);
                    ioData.Add(key, value);
                }
            }

            return success;
        }

        public static bool Transfer(Archive archive, ref SortedDictionary<EquipmentInvUISlot, PrototypeId> ioData)
        {
            bool success = true;

            if (archive.IsPacking)
            {
                ulong numElements = (ulong)ioData.Count;
                success &= Transfer(archive, ref numElements);

                foreach (var kvp in ioData)
                {
                    ulong key = (ulong)kvp.Key;
                    success &= Transfer(archive, ref key);
                    PrototypeId value = kvp.Value;
                    success &= Transfer(archive, ref value);
                }
            }
            else
            {
                ioData.Clear();

                ulong numElements = 0;
                success &= Transfer(archive, ref numElements);

                for (ulong i = 0; i < numElements; i++)
                {
                    ulong key = 0;
                    success &= Transfer(archive, ref key);
                    PrototypeId value = PrototypeId.Invalid;
                    success &= Transfer(archive, ref value);
                    ioData.Add((EquipmentInvUISlot)key, value);
                }
            }

            return success;
        }

        #endregion

        // Class-specific
        public static bool Transfer(Archive archive, ref SortedSet<AvailableBadges> ioData)
        {
            bool success = true;

            if (archive.IsPacking)
            {
                ulong numElements = (ulong)ioData.Count;
                success &= Transfer(archive, ref numElements);

                foreach (AvailableBadges data in ioData)
                {
                    uint value = (uint)data;
                    success &= Transfer(archive, ref value);
                }
            }
            else
            {
                ioData.Clear();

                ulong numBadges = 0;
                success &= Transfer(archive, ref numBadges);

                for (ulong i = 0; i < numBadges; i++)
                {
                    uint value = 0;
                    success &= Transfer(archive, ref value);
                    ioData.Add((AvailableBadges)value);
                }
            }

            return success;
        }
    }
}
