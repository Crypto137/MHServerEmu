namespace MHServerEmu.Games.GameData.Calligraphy
{
    /// <summary>
    /// Indicates that an enum has a corresponding Calligraphy asset type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Enum)]
    public class AssetEnumAttribute : Attribute
    {
        public int DefaultValue { get; }
        public string AssetBinding { get; }

        public AssetEnumAttribute(int defaultValue = 0, string assetBinding = null)
        {
            DefaultValue = defaultValue;
            AssetBinding = assetBinding;
        }
    }
}
