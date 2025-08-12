using Gazillion;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Regions
{
    public class RegionSettings
    {
        public ulong InstanceAddress { get; set; }  // region id
        public PrototypeId RegionDataRef { get; set; }
        public Aabb Bounds { get; set; }

        public bool ApplyLevelOverride { get; set; }
        public bool GenerateLog { get; set; }
        public bool GenerateEntities { get; set; }
        public bool GenerateAreas { get; set; }

        // CreateRegionParams
        public int Level { get; set; }
        public NetStructRegionOrigin Origin { get; set; }
        public PrototypeId DifficultyTierRef { get; set; }
        public int EndlessLevel { get; set; }
        public PrototypeId GameStateId { get; set; }
        public ulong MatchNumber { get; set; }
        public int Seed { get; set; }
        public ulong ParentRegionId { get; set; }
        public PrototypeId RequiredItemProtoRef { get; set; }
        public ulong RequiredItemEntityId { get; set; }
        public NetStructPortalInstance AccessPortal { get; set; }
        public List<PrototypeId> Affixes { get; set; } = new();
        public int PlayerDeaths { get; set; }
        public ulong DangerRoomScenarioItemDbGuid { get; set; }
        public PropertyCollection Properties { get; set; }
        public PrototypeId ItemRarity { get; set; }
        public PrototypeId DangerRoomScenarioRef { get; set; }

        public ulong PortalEntityDbId { get => AccessPortal != null ? AccessPortal.EntityDbId : 0; }
        public ulong OwnerPlayerDbId { get => AccessPortal != null && AccessPortal.HasOwnerPlayerDbId ? AccessPortal.OwnerPlayerDbId : 0; }

        public RegionSettings() { }

        public RegionSettings(NetStructCreateRegionParams createRegionParams)
        {
            if (createRegionParams.Level != 0)
            {
                Level = (int)createRegionParams.Level;
                ApplyLevelOverride = true;
            }

            if (createRegionParams.HasOrigin)
                Origin = createRegionParams.Origin;

            if (createRegionParams.HasDifficultyTierProtoId)
                DifficultyTierRef = (PrototypeId)createRegionParams.DifficultyTierProtoId;

            if (createRegionParams.HasEndlessLevel)
                EndlessLevel = (int)createRegionParams.EndlessLevel;

            if (createRegionParams.HasGameStateId)
                GameStateId = (PrototypeId)createRegionParams.GameStateId;

            if (createRegionParams.HasMatchNumber)
                MatchNumber = createRegionParams.MatchNumber;

            if (createRegionParams.HasSeed)
                Seed = (int)createRegionParams.Seed;

            if (createRegionParams.HasParentRegionId)
                ParentRegionId = createRegionParams.ParentRegionId;

            if (createRegionParams.HasRequiredItemProtoId)
                RequiredItemProtoRef = (PrototypeId)createRegionParams.RequiredItemProtoId;

            if (createRegionParams.HasRequiredItemEntityId)
                RequiredItemEntityId = createRegionParams.RequiredItemEntityId;

            if (createRegionParams.HasAccessPortal)
                AccessPortal = createRegionParams.AccessPortal;

            int affixCount = createRegionParams.AffixesCount;
            if (affixCount > 0)
            {
                Affixes.EnsureCapacity(affixCount);
                for (int i = 0; i < affixCount; i++)
                    Affixes.Add((PrototypeId)createRegionParams.AffixesList[i]);
            }

            if (createRegionParams.HasPlayerDeaths)
                PlayerDeaths = (int)createRegionParams.PlayerDeaths;

            if (createRegionParams.HasDangerRoomScenarioItemDbGuid)
                DangerRoomScenarioItemDbGuid = createRegionParams.DangerRoomScenarioItemDbGuid;

            if (createRegionParams.HasItemRarity)
                ItemRarity = (PrototypeId)createRegionParams.ItemRarity;

            if (createRegionParams.HasPropertyBuffer)
            {
                Properties = new();
                using Archive archive = new(ArchiveSerializeType.Replication, createRegionParams.PropertyBuffer);
                Properties.Serialize(archive);
            }

            if (createRegionParams.HasDangerRoomScenarioR)
                DangerRoomScenarioRef = (PrototypeId)createRegionParams.DangerRoomScenarioR;
        }
    }
}
