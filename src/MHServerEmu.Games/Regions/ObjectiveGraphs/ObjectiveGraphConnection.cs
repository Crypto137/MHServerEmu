namespace MHServerEmu.Games.Regions.ObjectiveGraphs
{
    public readonly struct ObjectiveGraphConnection
    {
        public ObjectiveGraphNode Node0 { get; }
        public ObjectiveGraphNode Node1 { get; }
        public float Distance { get; }

        public ObjectiveGraphConnection(ObjectiveGraphNode node0, ObjectiveGraphNode node1, float distance)
        {
            // The original implementation sorts by pointer here
            if (node0.InstanceNumber >= node1.InstanceNumber)
            {
                Node0 = node1;
                Node1 = node0;
            }
            else
            {
                Node0 = node0;
                Node1 = node1;
            }

            Distance = distance;
        }
    }
}
