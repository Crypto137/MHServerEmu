using System.Text;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.MetaGames
{
    public class PvP : MetaGame
    {
        private RepInt _team1;
        private RepInt _team2;

        public PvP(Game game) : base(game) { }

        public override bool Serialize(Archive archive)
        {
            bool success = base.Serialize(archive);
            // if (archive.IsTransient)
            success &= Serializer.Transfer(archive, ref _team1);
            success &= Serializer.Transfer(archive, ref _team2);
            return success;
        }

        protected override void BindReplicatedFields()
        {
            base.BindReplicatedFields();

            _team1.Bind(this, AOINetworkPolicyValues.AOIChannelProximity);
            _team2.Bind(this, AOINetworkPolicyValues.AOIChannelProximity);
        }

        protected override void UnbindReplicatedFields()
        {
            base.UnbindReplicatedFields();

            _team1.Unbind();
            _team2.Unbind();
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            sb.AppendLine($"{nameof(_team1)}: {_team1}");
            sb.AppendLine($"{nameof(_team2)}: {_team2}");
        }
    }
}
