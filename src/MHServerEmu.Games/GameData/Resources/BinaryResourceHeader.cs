namespace MHServerEmu.Games.GameData.Resources
{
    /// <summary>
    /// A header shared by all resource prototype files.
    /// </summary>
    public readonly struct BinaryResourceHeader
    {
        public byte CookerVersion { get; }          // 16
        public byte Endianness { get; }             // 1 for little endian
        public ushort Field2 { get; }               // wtf is this
        public uint PrototypeDataVersion { get; }
        public uint ClassHash { get; }

        public BinaryResourceHeader(BinaryReader reader)
        {
            CookerVersion = reader.ReadByte();
            Endianness = reader.ReadByte();
            Field2 = reader.ReadUInt16();
            PrototypeDataVersion = reader.ReadUInt32();
            ClassHash = reader.ReadUInt32();
        }
    }
}
