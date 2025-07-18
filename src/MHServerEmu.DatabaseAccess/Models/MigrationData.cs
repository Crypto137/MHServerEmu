namespace MHServerEmu.DatabaseAccess.Models
{
    public class MigrationData
    {
        public bool SkipNextUpdate { get; set; }

        public bool IsFirstLoad { get; set; } = true;

        // TransferParams (TODO: change this to PlayerManager <-> GIS messages)
        public ulong DestTargetRegionProtoId { get; set; }
        public ulong DestTargetAreaProtoId { get; set; }
        public ulong DestTargetCellProtoId { get; set; }
        public ulong DestTargetEntityProtoId { get; set; }
        public bool HasDestTarget { get => DestTargetRegionProtoId != 0; }

        // Bodyslider (store everything here as ulong, PropertyCollection will sort it out game-side)
        public ulong BodySliderRegionId { get; set; }
        public ulong BodySliderRegionRef { get; set; }
        public ulong BodySliderDifficultyRef { get; set; }
        public ulong BodySliderRegionSeed { get; set; }
        public ulong BodySliderAreaRef { get; set; }
        public ulong BodySliderRegionPos { get; set; }

        // TODO: Summoned inventory

        public MigrationData() { }

        public void Reset()
        {
            SkipNextUpdate = false;

            IsFirstLoad = true;

            DestTargetRegionProtoId = 0;
            DestTargetAreaProtoId = 0;
            DestTargetCellProtoId = 0;
            DestTargetEntityProtoId = 0;

            BodySliderRegionId = 0;
            BodySliderRegionRef = 0;
            BodySliderDifficultyRef = 0;
            BodySliderRegionSeed = 0;
            BodySliderAreaRef = 0;
            BodySliderRegionPos = 0;
        }
    }
}
