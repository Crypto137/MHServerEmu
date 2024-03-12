namespace MHServerEmu.Core.Config
{
    /// <summary>
    /// An attribute for ignoring properties when initializing config containers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ConfigIgnoreAttribute : Attribute { }
}
