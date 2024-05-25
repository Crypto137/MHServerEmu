using System.Text;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.MetaGames
{
    public class PvP : MetaGame
    {
        private ReplicatedVariable<int> _team1 = new();
        private ReplicatedVariable<int> _team2 = new();

        public PvP(Game game) : base(game) { }

        public override bool Serialize(Archive archive)
        {
            bool success = base.Serialize(archive);
            // if (archive.IsTransient)
            success &= Serializer.Transfer(archive, ref _team1);
            success &= Serializer.Transfer(archive, ref _team2);
            return success;
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            sb.AppendLine($"{nameof(_team1)}: {_team1}");
            sb.AppendLine($"{nameof(_team2)}: {_team2}");
        }
    }
}
