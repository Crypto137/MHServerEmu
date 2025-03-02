using MHServerEmu.Core.Collisions;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Regions
{
    public class RegionSettings
    {
        public ulong InstanceAddress { get; set; }  // region id
        public PrototypeId RegionDataRef { get; set; }
        public Aabb Bounds { get; set; }
        public int Level { get; set; }
        public PrototypeId DifficultyTierRef { get; set; }
        public int EndlessLevel { get; set; }
        public ulong MatchNumber { get; set; }
        public int Seed { get; set; }
        public List<PrototypeId> Affixes { get; set; }
        public int PlayerDeaths { get; set; }
        public PropertyCollection Properties { get; set; }
        public ulong PlayerGuidParty { get; set; }

        public bool ApplyLevelOverride { get; set; }
        public bool GenerateLog { get; set; }
        public bool GenerateEntities { get; set; }
        public bool GenerateAreas { get; set; }
        public PrototypeId GameStateId { get; set; }
        public PrototypeId ItemRarity { get; set; }
        public ulong PortalId { get; set; }

        public RegionSettings() { }

        public RegionSettings(RegionContext regionContext)
        {
            RegionDataRef = regionContext.RegionDataRef;

            if (regionContext.Level != 0)
            {
                ApplyLevelOverride = true;
                Level = regionContext.Level;
            }

            DifficultyTierRef = regionContext.DifficultyTierRef;
            Seed = regionContext.Seed;
            Affixes = new(regionContext.Affixes);
            ItemRarity = regionContext.ItemRarity;
            EndlessLevel = regionContext.EndlessLevel;
            PlayerDeaths = regionContext.PlayerDeaths;
            PlayerGuidParty = regionContext.PlayerGuidParty;
            PortalId = regionContext.PortalId;

            if (regionContext.Properties.IsEmpty == false)
            {
                Properties = new();
                Properties.FlattenCopyFrom(regionContext.Properties, false);
            }
        }
    }

    public class RegionContext
    {
        public PrototypeId RegionDataRef;
        public PrototypeId DifficultyTierRef;
        public List<PrototypeId> Affixes;
        public PropertyCollection Properties;
        public int EndlessLevel;
        public int Level;
        public ulong PlayerGuidParty;
        public int Seed;
        public int PlayerDeaths;
        public PrototypeId ItemRarity;
        public ulong PortalId;

        public RegionContext() : this(PrototypeId.Invalid, PrototypeId.Invalid) { }

        public RegionContext(PrototypeId regionDataRef, PrototypeId difficultyTierRef)
        {
            RegionDataRef = regionDataRef;
            DifficultyTierRef = difficultyTierRef;
            Affixes = new();
            Properties = new();
            EndlessLevel = 0;
            Level = 0;
            PlayerGuidParty = 0;
            PortalId = 0;
        }

        public override string ToString()
        {
            return $"{RegionDataRef.GetNameFormatted()} (Level={Level} Difficulty={DifficultyTierRef.GetNameFormatted()})";
        }

        public void FromRegion(Region region)
        {
            var settings = region.Settings;
            if (settings.Properties != null) Properties.FlattenCopyFrom(settings.Properties, true);
            Properties.CopyPropertyRange(region.Properties, PropertyEnum.ScoringEventTimerAccumTimeMS);
            DifficultyTierRef = settings.DifficultyTierRef;
            PlayerGuidParty = settings.PlayerGuidParty;
            PortalId = settings.PortalId;
            EndlessLevel = settings.EndlessLevel + 1;
            ItemRarity = settings.ItemRarity;
            Affixes = new(settings.Affixes);
            Seed = settings.Seed;
        }

        public void ResetEndless()
        {
            Seed = 0;
            EndlessLevel = 0;
            Affixes.Clear();
            Properties.Clear();
        }

        public void ResetRegionSettings()
        {
            PortalId = 0;
            PlayerDeaths = 0;
            PlayerGuidParty = 0;
        }

        public void CopyScenarioProperties(PropertyCollection properties)
        {
            Properties.Clear();

            Properties.CopyProperty(properties, PropertyEnum.DifficultyTier);
            Properties.CopyProperty(properties, PropertyEnum.RegionAffixDifficulty);
            Properties.CopyProperty(properties, PropertyEnum.DangerRoomScenarioItemDbGuid);

            Properties.CopyProperty(properties, PropertyEnum.DifficultyIndex);
            Properties.CopyProperty(properties, PropertyEnum.DamageRegionMobToPlayer);
            Properties.CopyProperty(properties, PropertyEnum.DamageRegionPlayerToMob);

            ItemRarity = properties[PropertyEnum.ItemRarity];
            PlayerGuidParty = properties[PropertyEnum.RestrictedToPlayerGuidParty];

            if (properties.HasProperty(PropertyEnum.RegionAffix))
            {
                Affixes.Clear();
                foreach (var kvp in properties.IteratePropertyRange(PropertyEnum.RegionAffix))
                {
                    Property.FromParam(kvp.Key, 0, out PrototypeId affixRef);
                    Affixes.Add(affixRef);
                }
            }
        }
    }
}
