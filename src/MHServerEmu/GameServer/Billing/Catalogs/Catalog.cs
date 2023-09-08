using System.Text.Json.Serialization;
using Gazillion;

namespace MHServerEmu.GameServer.Billing.Catalogs
{
    public class Catalog
    {
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
        }
    }
}
