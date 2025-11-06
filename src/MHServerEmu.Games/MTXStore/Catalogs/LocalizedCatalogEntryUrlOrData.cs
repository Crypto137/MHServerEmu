using System.Text.Json.Serialization;
using Gazillion;

namespace MHServerEmu.Games.MTXStore.Catalogs
{
    public class LocalizedCatalogEntryUrlOrData
    {
        public string LanguageId { get; set; }
        public string Url { get; set; }
        public byte[] ImageData { get; set; }

        [JsonConstructor]
        public LocalizedCatalogEntryUrlOrData(string languageId, string url, byte[] imageData)
        {
            LanguageId = languageId;
            Url = url;
            ImageData = imageData;
        }

        public LocalizedCatalogEntryUrlOrData(MHLocalizedCatalogEntryUrlOrData localizedCatalogEntryUrlOrData)
        {
            LanguageId = localizedCatalogEntryUrlOrData.LanguageId;
            Url = localizedCatalogEntryUrlOrData.Url;
            ImageData = localizedCatalogEntryUrlOrData.Imagedata.ToByteArray();
        }

        public MHLocalizedCatalogEntryUrlOrData ToNetStruct()
        {
            // imagedata is unused in our dump.
            return MHLocalizedCatalogEntryUrlOrData.CreateBuilder()
                .SetLanguageId(LanguageId)
                .SetUrl(Url)
                //.SetImagedata(ByteString.CopyFrom(ImageData))
                .Build();
        }
    }
}
