using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;

namespace MHServerEmu.Games.MetaGames
{
    public class PvP : MetaGame
    {
        private ReplicatedVariable<int> _team1 = new();
        private ReplicatedVariable<int> _team2 = new();

        // new
        public PvP(Game game) : base(game) { }

        // old
        public PvP(EntityBaseData baseData, ByteString archiveData) : base(baseData, archiveData) { }

        public PvP(EntityBaseData baseData) : base(baseData) { }

        public override bool Serialize(Archive archive)
        {
            bool success = base.Serialize(archive);
            // if (archive.IsTransient)
            success &= Serializer.Transfer(archive, ref _team1);
            success &= Serializer.Transfer(archive, ref _team2);
            return success;
        }

        protected override void Decode(CodedInputStream stream)
        {
            base.Decode(stream);

            _team1.Decode(stream);
            _team2.Decode(stream);
        }

        public override void Encode(CodedOutputStream stream)
        {
            base.Encode(stream);

            _team1.Encode(stream);
            _team2.Encode(stream);
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            sb.AppendLine($"{nameof(_team1)}: {_team1}");
            sb.AppendLine($"{nameof(_team2)}: {_team2}");
        }
    }
}
