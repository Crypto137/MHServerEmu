namespace MHServerEmu.Games.Locales
{
    /// <summary>
    /// A singleton that manages <see cref="Locale"/> instances.
    /// </summary>
    public class LocaleManager
    {
        public static LocaleManager Instance { get; } = new();

        public Locale CurrentLocale { get; private set; }

        private LocaleManager() { }

        public bool Initialize()
        {
            CurrentLocale = new();
            return true;
        }
    }
}
