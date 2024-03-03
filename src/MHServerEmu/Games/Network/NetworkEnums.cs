namespace MHServerEmu.Games.Network
{
    // AOI = Area of Interest

    [Flags]
    public enum AOINetworkPolicyValues
    {
        AOIChannelNone          = 0,        // eAOIChannel_None
        AOIChannelProximity     = 1 << 0,   // PropertyInfoPrototype.ReplicateToProximity
        AOIChannelParty         = 1 << 1,   // PropertyInfoPrototype.ReplicateToParty
        AOIChannelOwner         = 1 << 2,   // PropertyInfoPrototype.ReplicateToOwner
        AOIChannelTrader        = 1 << 3,   // PropertyInfoPrototype.ReplicateToTrader
        AOIChannel4             = 1 << 4,   // Doesn't seem to be set in any of our data
        AOIChannelDiscovery     = 1 << 5,   // PropertyInfoPrototype.ReplicateToDiscovered
        AOIChannelClientOnly    = 1 << 6,   // From PropertyInfoPrototype::Validate()
        AOIChannel7             = 1 << 7,   // The highest set channel flag we've seen

        // From ArchiveMessageHandler::ArchiveMessageHandles(), 0xEF (all channels except 4)
        // Appears in AddConditionArchive and MiniMapArchive
        DefaultPolicy           = AOIChannelProximity | AOIChannelParty | AOIChannelOwner | AOIChannelTrader
                                | AOIChannelDiscovery | AOIChannelClientOnly | AOIChannel7
    }
}
