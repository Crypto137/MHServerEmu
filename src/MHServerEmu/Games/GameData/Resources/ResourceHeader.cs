namespace MHServerEmu.Games.GameData.Resources
{
    public readonly struct ResourceHeader
    {
        public uint Signature { get; }
        public uint Version { get; }
        public uint ClassId { get; }

        public ResourceHeader(BinaryReader reader)
        {
            Signature = reader.ReadUInt32();
            Version = reader.ReadUInt32();
            ClassId = reader.ReadUInt32();
        }
    }
}
