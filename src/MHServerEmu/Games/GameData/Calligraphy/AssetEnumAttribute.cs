namespace MHServerEmu.Games.GameData.Calligraphy
{
    /// <summary>
    /// Indicates that an enum has a corresponding Calligraphy asset type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Enum)]
    public class AssetEnumAttribute : Attribute
    {
        public string AssetBinding { get; }

        public AssetEnumAttribute(string assetBinding = null)
        {
            AssetBinding = assetBinding;
        }
    }
}
