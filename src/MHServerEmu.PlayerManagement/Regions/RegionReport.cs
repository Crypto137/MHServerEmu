using System.Text;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.PlayerManagement.Regions
{
    public readonly struct RegionReport : IDisposable, IHtmlDataStructure
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

        public void BuildHtml(StringBuilder sb)
        {
            ulong currentGameId = 0;
            bool isInSubList = false;
            foreach (Entry entry in Regions)
            {
                ulong gameId = entry.GameId;
                if (gameId != currentGameId)
                {
                    if (isInSubList)
                    {
                        HtmlBuilder.EndUnorderedList(sb);
                        isInSubList = false;
                    }

                    HtmlBuilder.AppendListItem(sb, $"Game [0x{gameId:X}]");
                    currentGameId = gameId;

                    HtmlBuilder.BeginUnorderedList(sb);
                    isInSubList = true;
                }

                HtmlBuilder.AppendListItem(sb, entry.ToString());
            }

            if (isInSubList)
            {
                HtmlBuilder.EndUnorderedList(sb);
                isInSubList = false;
            }
        }

        public readonly struct Entry : IComparable<Entry>
        {
            public ulong GameId { get; }
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
                Uptime = region.Uptime;
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
