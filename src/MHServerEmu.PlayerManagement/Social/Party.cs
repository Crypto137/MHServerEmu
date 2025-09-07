namespace MHServerEmu.PlayerManagement.Social
{
    /// <summary>
    /// The authoritative representation of a party on the server (as apposed to local parties in game instances).
    /// </summary>
    public class Party
    {
        // This class has the same name as game-side local representations of parties.
        // This is not ideal, but I'm not sure what else to call it without making it more confusing.

        public ulong Id { get; }

        public Party(ulong id)
        {
            Id = id;
        }
    }
}
