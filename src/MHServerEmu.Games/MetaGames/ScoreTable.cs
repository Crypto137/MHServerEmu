using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.MetaGames
{
    public class ScoreTable
    {
        public enum DataType
        {
            Region,
            Player
        }

        public MetaGame MetaGame { get; }
        public ScoreTableSchema PlayerSchema { get; private set; }
        public ScoreTableSchema RegionSchema { get; private set; }

        private int _playerSchemaSize;
        private int _regionSchemaSize;
        private ScoreTableRow _regionTableRow;

        public ScoreTable(MetaGame metagame)
        {
            MetaGame = metagame;
            PlayerSchema = new();
            RegionSchema = new();
        }

        public void Initialize(PrototypeId scoreSchemaRegion, PrototypeId scoreSchemaPlayer)
        {
            _playerSchemaSize = BuildSchema(PlayerSchema, scoreSchemaPlayer, DataType.Player);
            if (scoreSchemaRegion != PrototypeId.Invalid)
            {
                _regionSchemaSize = BuildSchema(RegionSchema, scoreSchemaPlayer, DataType.Region);
                _regionTableRow = new(_regionSchemaSize);
            }
        }

        private int BuildSchema(ScoreTableSchema schema, PrototypeId schemaRef, DataType type)
        {
            if (schemaRef == PrototypeId.Invalid) return 0;
            var schemaProto = GameDatabase.GetPrototype<ScoreTableSchemaPrototype>(schemaRef);
            if (schemaProto == null) return 0;

            schema.Clear();
            int index = 0;

            if (schemaProto?.Schema.HasValue() == true)
                foreach(var entryProto in schemaProto.Schema)
                {
                    var scoreTableType = new ScoreTableType(this, type, index++);
                    scoreTableType.Initialize(entryProto);
                    schema.Add(scoreTableType);
                }

            return schema.Count;
        }
    }

    public class ScoreTableSchema : List<ScoreTableType> { }

    public class ScoreTableType
    {
        public LocaleStringId Name { get; private set; }
        public ScoreTableSchemaEntryPrototype Prototype { get; private set; }

        private ScoreTable _table;
        private ScoreTable.DataType _type;
        private int _index;
        private Event<AdjustHealthGameEvent>.Action _adjustHealthAction;
        private Event<EntityDeadGameEvent>.Action _entityDeadAction;

        public ScoreTableType(ScoreTable table, ScoreTable.DataType type, int index)
        {
            _table = table;
            _type = type;
            _index = index;
            _adjustHealthAction = OnAdjustHealth;
            _entityDeadAction = OnEntityDead;
        }

        private void OnEntityDead(in EntityDeadGameEvent evt)
        {
            // TODO
        }

        private void OnAdjustHealth(in AdjustHealthGameEvent evt)
        {
            // TODO
        }

        public void Initialize(ScoreTableSchemaEntryPrototype proto)
        {
            if (proto == null) return;
            Prototype = proto;
            Name = proto.Name;

            var metaGame = _table.MetaGame;
            var region = metaGame.Region;
            if (region == null) return;

            if (proto.OnEntityDeathFilter != null
                || proto.Event == ScoreTableValueEvent.Deaths
                || proto.Event == ScoreTableValueEvent.PlayerAssists
                || proto.Event == ScoreTableValueEvent.PlayerKills)
                region.EntityDeadEvent.AddActionBack(_entityDeadAction);

            if (proto.Event == ScoreTableValueEvent.DamageTaken
                || proto.Event == ScoreTableValueEvent.DamageDealt
                || proto.Event == ScoreTableValueEvent.PlayerDamageDealt)
                region.AdjustHealthEvent.AddActionBack(_adjustHealthAction);

            // TODO proto.EvalAuto
        }
    }

    public class ScoreTableRow
    {
        public string Name;

        private List<ScoreTableValue> _scores = new();

        public ScoreTableRow(int size)
        {
            _scores.Capacity = size;
        }

        public int GetCategoriesNum() => _scores.Count;
    }

    public class ScoreTableValue
    {
        public ScoreTableValueType Type;
        public int IntValue { get; set; }
        public float FloatValue { get; set; }

        public ScoreTableValue()
        {
            Type = ScoreTableValueType.Int;
        }
    }
}
