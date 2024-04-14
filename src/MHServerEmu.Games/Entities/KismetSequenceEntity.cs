using Google.ProtocolBuffers;

namespace MHServerEmu.Games.Entities
{
    // KismetSequenceEntity doesn't contain any data of its own, but probably contains behavior
    public class KismetSequenceEntity : WorldEntity
    {
        // new
        public KismetSequenceEntity(Game game) : base(game) { }

        // old
        public KismetSequenceEntity(EntityBaseData baseData, ByteString archiveData) : base(baseData, archiveData) { }

        public KismetSequenceEntity(EntityBaseData baseData) : base(baseData) { }
    }

    public enum KismetSeqPrototypeId : ulong
    {
        RaftHeliPadQuinJetIdle = 16427676150442498085,
        RaftHeliPadQuinJetLanding = 12877373051438045540,
        RaftHeliPadQuinJetLandingStart = 320691797651954578,
        RaftHeliPadQuinJetDustoff = 16071663559488903554,
        RaftFunicularSequence = 9329157849119332306,
        RaftNPEJuggernautEscape = 4077223947200436384,
        RaftNPEElectroEscape = 6789899988464834386,
        RaftNPEGreenGoblin = 3581923635100194431,
        RaftTutorialCellBTurnOffAlarm = 15989927550533966647,
        RaftVenomTransformMoment = 4814459009646270785,
        SEQMonitorsOnSwitch01 = 10049226102883358615,

        Times01CaptainAmericaLanding = 4747023203178650586,
    }
}
