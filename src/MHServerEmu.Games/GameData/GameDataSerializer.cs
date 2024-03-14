using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData
{
    /// <summary>
    /// Base class for prototype data serializers.
    /// </summary>
    public abstract class GameDataSerializer
    {
        public virtual void Deserialize(Prototype prototype, PrototypeId dataRef, Stream stream)
        {
            throw new NotImplementedException();
        }

        /* The client also has a Serialize() method here that we don't need.
        public void Serialize(Prototype prototype, Stream stream)
        {
            throw new NotImplementedException();
        }
        */
    }
}
