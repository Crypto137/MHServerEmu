namespace MHServerEmu.Games.Entities
{
    // KismetSequenceEntity doesn't contain any data of its own, but probably contains behavior
    public class KismetSequenceEntity : WorldEntity
    {
        public KismetSequenceEntity(Game game) : base(game) { }

        public override void OnEnteredWorld(EntitySettings settings)
        {
            base.OnEnteredWorld(settings);

            Region?.DiscoverEntity(this, false);
        }

        public override void OnExitedWorld()
        {
            Region?.UndiscoverEntity(this, true);

            base.OnExitedWorld();
        }
    }
}
