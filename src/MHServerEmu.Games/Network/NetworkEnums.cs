namespace MHServerEmu.Games.Network
{
    // AOI = Area of Interest

    [Flags]
    public enum AOINetworkPolicyValues
    {
        AOIChannelNone          = 0,
        AOIChannelProximity     = 1 << 0,
        AOIChannelParty         = 1 << 1,
        AOIChannelOwner         = 1 << 2,
        AOIChannelTrader        = 1 << 3,
        AOIChannel4             = 1 << 4,   // Doesn't seem to be set in any of our data
        AOIChannelDiscovery     = 1 << 5,
        AOIChannelClientOnly    = 1 << 6,
        AOIChannelClientIndependent  = 1 << 7,   // Missiles

        // From the constructor for ArchiveMessageHandler, 0xEF (all channels except 4)
        // Appears in AddConditionArchive and MiniMapArchive
        AllChannels             = AOIChannelProximity | AOIChannelParty | AOIChannelOwner | AOIChannelTrader
                                | AOIChannelDiscovery | AOIChannelClientOnly | AOIChannelClientIndependent
    }

    public enum InterestTrackOperation
    {
        Invalid = -1,
        Add = 0,
        Remove = 1,
        Modify = 2
    }
}
