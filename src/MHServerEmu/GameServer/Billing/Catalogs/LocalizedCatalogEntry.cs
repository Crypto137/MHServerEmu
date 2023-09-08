using System.Text.Json.Serialization;
using Gazillion;

namespace MHServerEmu.GameServer.Billing.Catalogs
{
    public class LocalizedCatalogEntry
    {
        public string LanguageId { get; set; }
        public string Description { get; set; }
        public string Title { get; set; }
        public string ReleaseDate { get; set; }
        public long ItemPrice { get; set; }

        [JsonConstructor]
        public LocalizedCatalogEntry(string languageId, string description, string title, string releaseDate, long itemPrice)
        {
            LanguageId = languageId;
            Description = description;
            Title = title;
            ReleaseDate = releaseDate;
            ItemPrice = itemPrice;
        }

        public LocalizedCatalogEntry(MHLocalizedCatalogEntry localizedCatalogEntry)
        {
            LanguageId = localizedCatalogEntry.LanguageId;
            Description = localizedCatalogEntry.Description;
            Title = localizedCatalogEntry.Title;
            ReleaseDate = localizedCatalogEntry.ReleaseDate;
            ItemPrice = localizedCatalogEntry.ItemPrice;
        }
    }
}
