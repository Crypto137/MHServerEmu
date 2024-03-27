namespace MHServerEmu.Core.Serialization
{
    /// <summary>
    /// Exposes <see cref="Archive"/> serialization routine.
    /// </summary>
    public interface ISerialize
    {
        public bool Serialize(Archive archive);
    }
}
