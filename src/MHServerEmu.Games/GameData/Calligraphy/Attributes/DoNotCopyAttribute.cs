namespace MHServerEmu.Games.GameData.Calligraphy.Attributes
{
    /// <summary>
    /// Indicates that a property needs to be ignored when copying prototype fields.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DoNotCopyAttribute : Attribute { }
}
