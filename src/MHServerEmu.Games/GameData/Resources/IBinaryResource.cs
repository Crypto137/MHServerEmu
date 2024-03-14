namespace MHServerEmu.Games.GameData.Resources
{
    /// <summary>
    /// Provides a custom prototype deserialization routine.
    /// </summary>
    public interface IBinaryResource
    {
        /// <summary>
        /// Deserializes this binary resource using its custom routine.
        /// </summary>
        public void Deserialize(BinaryReader reader);
    }
}
