namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// Represents a <see cref="IFrontendClient"/> session.
    /// </summary>
    public interface IFrontendSession
    {
        public ulong Id { get; }
        public object Account { get; }  // Not having this be strongly typed is not ideal, but it allows us to avoid coupling Core and DatabaseAccess.
        public string Locale { get; }
    }
}
