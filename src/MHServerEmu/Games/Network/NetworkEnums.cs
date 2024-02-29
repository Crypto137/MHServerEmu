namespace MHServerEmu.Games.Network
{
    // AOI = Area of Interest

    [Flags]
    public enum AoiNetworkPolicyValues
    {
        AoiChannelNone          = 0,        // eAOIChannel_None
        AoiChannelProximity     = 1 << 0,   // PropertyInfoPrototype.ReplicateToProximity
        AoiChannelParty         = 1 << 1,   // PropertyInfoPrototype.ReplicateToParty
        AoiChannelOwner         = 1 << 2,   // PropertyInfoPrototype.ReplicateToOwner
        AoiChannelTrader        = 1 << 3,   // PropertyInfoPrototype.ReplicateToTrader
        AoiChannel4             = 1 << 4,   // Doesn't seem to be set in any of our data
        AoiChannelDiscovery     = 1 << 5,   // PropertyInfoPrototype.ReplicateToDiscovered
        AoiChannelClientOnly    = 1 << 6,   // From PropertyInfoPrototype::Validate()
        AoiChannel7             = 1 << 7,   // The highest set channel flag we've seen

        // From ArchiveMessageHandler::ArchiveMessageHandles(), 0xEF (all channels except 4)
        // Appears in AddConditionArchive and MiniMapArchive
        DefaultPolicy           = AoiChannelProximity | AoiChannelParty | AoiChannelOwner | AoiChannelTrader
                                | AoiChannelDiscovery | AoiChannelClientOnly | AoiChannel7
    }
}
