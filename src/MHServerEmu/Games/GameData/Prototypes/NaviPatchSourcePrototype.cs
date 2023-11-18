namespace MHServerEmu.Games.GameData.Prototypes
{
    public class NaviPatchSourcePrototype : Prototype
    {
        // PatchFragments "Skipping writing field %s in class %s because it has eFlagDontCook set"
        public uint NaviPatchCrc { get; }
        public NaviPatchPrototype NaviPatch { get; }
        public NaviPatchPrototype PropPatch { get; }
        public float PlayableArea { get; }
        public float SpawnableArea { get; }

        public NaviPatchSourcePrototype(BinaryReader reader)
        {
            NaviPatchCrc = reader.ReadUInt32();
            NaviPatch = new(reader);
            PropPatch = new(reader);
            PlayableArea = reader.ReadSingle();
            SpawnableArea = reader.ReadSingle();
        }
    }
}
