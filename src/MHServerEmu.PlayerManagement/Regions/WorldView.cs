namespace MHServerEmu.PlayerManagement.Regions
{
    public class WorldView
    {
        // NOTE: This is based on the PlayerMgrToGameServer protocol extracted from 1.53 builds.

        // WorldView is a class that represents a collection of region instances (both public and private)
        // bound to a player. This is what allows a player to access their private instances, as well as
        // consistently return to the same public instances. When in party, everyone should use the world view
        // of the leader.

        // TODO: Implement this on the PlayerManager side

        // TODO: Implement some method of short-term persistence between sessions (e.g. so your world view doesn't reset when you relog).
    }
}
