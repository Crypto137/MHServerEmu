namespace MHServerEmu.Games.UI
{
    /// <summary>
    /// Exposes a <see cref="UI.UIDataProvider"/> instance.
    /// </summary>
    public interface IUIDataProviderOwner
    {
        public UIDataProvider UIDataProvider { get; }
    }
}
