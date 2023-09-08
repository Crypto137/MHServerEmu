using System.Text.Json.Serialization;
using Gazillion;

namespace MHServerEmu.GameServer.Billing.Catalogs
{
    public class Catalog
    {
        private Dictionary<long, CatalogEntry> _entryDict;

        public long TimestampSeconds { get; set; }
        public long TimestampMicroseconds { get; set; }
        public CatalogEntry[] Entries { get; set; }
        public LocalizedCatalogUrls[] Urls { get; set; }
        public bool ClientMustDownloadImages { get; set; }

        [JsonConstructor]
        public Catalog(long timestampSeconds, long timestampMicroseconds, CatalogEntry[] entries, LocalizedCatalogUrls[] urls, bool clientMustDownloadImages)
        {
            TimestampSeconds = timestampSeconds;
            TimestampMicroseconds = timestampMicroseconds;
            Entries = entries;
            Urls = urls;
            ClientMustDownloadImages = clientMustDownloadImages;

            GenerateEntryDict();
        }

        public Catalog(NetMessageCatalogItems catalogItems)
        {
            TimestampSeconds = catalogItems.TimestampSeconds;
            TimestampMicroseconds = catalogItems.TimestampMicroseconds;

            Entries = new CatalogEntry[catalogItems.EntriesCount];
            for (int i = 0; i < Entries.Length; i++)
                Entries[i] = new(catalogItems.EntriesList[i]);

            Urls = new LocalizedCatalogUrls[catalogItems.UrlsCount];
            for (int i = 0; i < Urls.Length; i++)
                Urls[i] = new(catalogItems.UrlsList[i]);

            ClientMustDownloadImages = catalogItems.Clientmustdownloadimages;

            GenerateEntryDict();
        }

        public CatalogEntry GetEntry(long skuId)
        {
            if (_entryDict.TryGetValue(skuId, out CatalogEntry entry))
                return entry;
            else
                return null;
        }
        public NetMessageCatalogItems ToNetMessageCatalogItems(bool clientMustDownloadImages)
        {
            DateTimeOffset timestamp = (DateTimeOffset)DateTime.UtcNow;

            var entries = new MarvelHeroesCatalogEntry[Entries.Length];
            for (int i = 0; i < entries.Length; i++)
                entries[i] = Entries[i].ToNetStruct();

            var urls = new MHLocalizedCatalogUrls[Urls.Length];
            for (int i = 0; i < urls.Length; i++)
                urls[i] = Urls[i].ToNetStruct();

            return NetMessageCatalogItems.CreateBuilder()
                .SetTimestampSeconds(timestamp.ToUnixTimeSeconds())
                .SetTimestampMicroseconds(timestamp.Millisecond * 1000)
                .AddRangeEntries(entries)
                .AddRangeUrls(urls)
                .SetClientmustdownloadimages(clientMustDownloadImages)
                .Build();
        }

        private void GenerateEntryDict()
        {
            _entryDict = new(Entries.Length);
            foreach (CatalogEntry entry in Entries)
                _entryDict.Add(entry.SkuId, entry);
        }
    }
}
