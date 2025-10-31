using System.Text.Json.Serialization;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.PlayerManagement.Regions
{
    public readonly struct RegionReport : IDisposable
    {
        public List<Entry> Regions { get; }

        public RegionReport()
        {
            Regions = ListPool<Entry>.Instance.Get();
        }

        public void Initialize(WorldManager worldManager)
        {
            foreach (RegionHandle region in worldManager)
            {
                if (region.State == RegionHandleState.Shutdown)
                    continue;

                Entry entry = new(region);
                Regions.Add(entry);
            }

            Regions.Sort();
        }

        public void Dispose()
        {
            ListPool<Entry>.Instance.Return(Regions);
        }

        public readonly struct Entry : IComparable<Entry>
        {
            [JsonNumberHandling(JsonNumberHandling.WriteAsString)]
            public ulong GameId { get; }
            [JsonNumberHandling(JsonNumberHandling.WriteAsString)]
            public ulong RegionId { get; }
            public string Name { get; }
            public string DifficultyTier { get; }
            public TimeSpan Uptime { get; }

            public Entry(RegionHandle region)
            {
                GameId = region.Game.Id;
                RegionId = region.Id;
                Name = region.RegionProtoRef.GetNameFormatted();
                DifficultyTier = region.DifficultyTierProtoRef.GetNameFormatted();
                Uptime = TimeSpan.FromSeconds((long)region.Uptime.TotalSeconds);
            }

            public override string ToString()
            {
                return $"[0x{RegionId:X}] {Name} ({DifficultyTier}) - {Uptime:dd\\:hh\\:mm\\:ss}";
            }

            public int CompareTo(Entry other)
            {
                int compare = GameId.CompareTo(other.GameId);
                if (compare != 0)
                    return compare;

                return RegionId.CompareTo(other.RegionId);
            }
        }
    }
}
