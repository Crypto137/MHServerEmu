namespace MHServerEmu.Games.Network
{
    // AOI = Area of Interest

    // Potential channel names from prototype data (order unknown):
    // ReplicateToProximity, ReplicateToParty, ReplicateToOwner, ReplicateToDiscovered, ReplicateToTrader

    [Flags]
    public enum AoiNetworkPolicyValues
    {
        AoiChannelNone  = 0,        // eAOIChannel_None
        AoiChannel0     = 1 << 0,
        AoiChannel1     = 1 << 1,
        AoiChannel2     = 1 << 2,
        AoiChannel3     = 1 << 3,
        AoiChannel4     = 1 << 4,   // Doesn't seem to be set in any of the dumped messages
        AoiChannel5     = 1 << 5,
        AoiChannel6     = 1 << 6,
        AoiChannel7     = 1 << 7    // The highest set channel flag we've seen (archiveData for NetMessageAddCondition)
    }
}
