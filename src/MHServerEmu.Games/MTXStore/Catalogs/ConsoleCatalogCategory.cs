#if (GAME_VERSION_1_52 || GAME_VERSION_1_53) && (PLATFORM_TYPE_PS4 || PLATFORM_TYPE_XBOXONE)
using Gazillion;

namespace MHServerEmu.Games.MTXStore.Catalogs
{
    public class ConsoleCatalogCategory
    {
        public string Id { get; set; }
        public bool Visible { get; set; }
        public string DisplayName { get; set; }
        public int Ordinal { get; set; }

        public ConsoleCatalogCategory(string id, bool visible, string displayName, int ordinal)
        {
            Id = id;
            Visible = visible;
            DisplayName = displayName;
            Ordinal = ordinal;
        }

        public override string ToString()
        {
            return DisplayName;
        }

        public MHConsoleCatalogCategoryEntry ToNetStruct()
        {
            return MHConsoleCatalogCategoryEntry.CreateBuilder()
                .SetId(Id)
                .SetVisible(Visible)
                .AddLocalizedEntries(MHLocalizedStringCollection.CreateBuilder()
                    .SetLanguageId("en_us")
                    .AddTranslations(MHStringValue.CreateBuilder().SetKey("tid").SetText(DisplayName)))
                .SetTid(string.Empty)
#if GAME_VERSION_1_53
                .SetOrdinal(Ordinal)
#endif
                .Build();
        }
    }
}
#endif
