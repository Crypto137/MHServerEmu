using Gazillion;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Loot.Specs
{
    public readonly struct AgentSpec
    {
        public PrototypeId AgentProtoRef { get; }
        public int AgentLevel { get; }
        public int CreditsAmount { get; }

        public AgentSpec(PrototypeId agentProtoRef, int agentLevel, int creditsAmount)
        {
            AgentProtoRef = agentProtoRef;
            AgentLevel = agentLevel;
            CreditsAmount = creditsAmount;
        }

        public NetStructAgentSpec ToProtobuf()
        {
            return NetStructAgentSpec.CreateBuilder()
                .SetAgentProtoRef((ulong)AgentProtoRef)
                .SetAgentLevel((uint)AgentLevel)
                .SetCreditsAmount((uint)CreditsAmount)
                .Build();
        }

        public override string ToString()
        {
            return $"agentProtoRef={AgentProtoRef.GetName()}, level={AgentLevel}, creditsAmount={CreditsAmount}";
        }
    }
}
