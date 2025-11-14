using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties.Evals;
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
        private readonly Dictionary<ulong, ScoreTableRowPlayer> _playerRows;

        public ScoreTable(MetaGame metagame)
        {
            MetaGame = metagame;
            PlayerSchema = new();
            RegionSchema = new();
            _playerRows = [];
        }

        public void Initialize(PrototypeId scoreSchemaRegion, PrototypeId scoreSchemaPlayer)
        {
            _playerSchemaSize = BuildSchema(PlayerSchema, scoreSchemaPlayer, DataType.Player);

            if (scoreSchemaRegion != PrototypeId.Invalid) // not used in game
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

        public void AddNewPlayer(Player player)
        {
            ulong guid = player.DatabaseUniqueId;
            if (_playerRows.ContainsKey(guid)) return;
           
            var row = new ScoreTableRowPlayer(_playerSchemaSize);
            _playerRows.Add(guid, row);

            var team = MetaGame.GetTeamByPlayer(player);
            row.Team = team != null ? team.ProtoRef : PrototypeId.Invalid;

            var avatar = player.CurrentAvatar;
            var avatarRef = avatar != null ? avatar.PrototypeDataRef : PrototypeId.Invalid;
            row.Avatar = avatarRef;

            var message = NetMessagePvPScorePlayerNewId.CreateBuilder()
                .SetPvpEntityId(MetaGame.Id)
                .SetPlayerDbGuid(guid)
                .SetPlayerName(player.GetName())
                .SetTeamProtoId((ulong)row.Team)
                .SetAvatarProtoId((ulong)avatarRef)
                .Build();

            SendMessageToInterested(message);

            for (int category = 0; category < PlayerSchema.Count; category++)
                InitPlayerScoreByCategory(player, category);

            var updMessage = NetMessagePvPScorePlayerUpdate.CreateBuilder()
                .SetPvpEntityId(MetaGame.Id)
                .SetPlayerDbGuid(guid);

            for (int category = 0; category < row.CategoriesNum; category++) 
            {
                var scoreValue = row.GetValueByCategory(category);
                if (scoreValue == null) continue;
                updMessage.AddUpdates(GetUpdates(category, scoreValue));
            }

            SendMessageToInterested(updMessage.Build());

            SendPlayersScoreToPlayer(player);            
        }

        private void SendPlayersScoreToPlayer(Player updatePlayer)
        {
            foreach (var player in new PlayerIterator(MetaGame.Region))
            {
                ulong guid = player.DatabaseUniqueId;
                var avatar = player.CurrentAvatar;
                var avatarRef = avatar != null ? avatar.PrototypeDataRef : PrototypeId.Invalid;
                if (_playerRows.TryGetValue(guid, out var row) == false) continue;

                if (row == null) return;

                var message = NetMessagePvPScorePlayerNewId.CreateBuilder()
                    .SetPvpEntityId(MetaGame.Id)
                    .SetPlayerDbGuid(guid)
                    .SetPlayerName(player.GetName())
                    .SetTeamProtoId((ulong)row.Team)
                    .SetAvatarProtoId((ulong)avatarRef)
                    .Build();

                updatePlayer.SendMessage(message);

                var updMessage = NetMessagePvPScorePlayerUpdate.CreateBuilder()
                    .SetPvpEntityId(MetaGame.Id)
                    .SetPlayerDbGuid(guid);

                for (int category = 0; category < row.CategoriesNum; category++)
                {
                    var scoreValue = row.GetValueByCategory(category);
                    if (scoreValue == null) continue;
                    updMessage.AddUpdates(GetUpdates(category, scoreValue));
                }

                updatePlayer.SendMessage(updMessage.Build());
            }
        }

        private void SendMessageToInterested(IMessage message)
        {
            MetaGame.Game.NetworkManager.SendMessageToInterested(message, MetaGame, AOINetworkPolicyValues.AOIChannelProximity);
        }

        private void InitPlayerScoreByCategory(Player player, int category)
        {
            var schema = PlayerSchema[category];
            if (schema == null) return;
            int value = schema.GetEvalValue(player);
            SetPlayerScoreValue(player, value, category);
        }

        public void SetPlayerScoreValue(Player player, int value, int category)
        {
            var scoreValue = GetScoreValueByCategory(player, category);
            if (scoreValue == null) return;

            scoreValue.IntValue = value;
            SendPvPScorePlayerUpdateValue(player, category, scoreValue);
        }

        public void UpdatePlayerScoreValue(Player player, int value, int category)
        {
            var scoreValue = GetScoreValueByCategory(player, category);
            if (scoreValue == null) return;

            scoreValue.IntValue += value;
            SendPvPScorePlayerUpdateValue(player, category, scoreValue);
        }

        private void SendPvPScorePlayerUpdateValue(Player player, int category, ScoreTableValue scoreValue)
        {
            var message = NetMessagePvPScorePlayerUpdate.CreateBuilder()
                .SetPvpEntityId(MetaGame.Id)
                .SetPlayerDbGuid(player.DatabaseUniqueId)
                .AddUpdates(GetUpdates(category, scoreValue))
                .Build();

            SendMessageToInterested(message);
        }

        private static NetMessagePvPScoreScoreUpdateEntry GetUpdates(int category, ScoreTableValue scoreValue)
        {
            var updates = NetMessagePvPScoreScoreUpdateEntry.CreateBuilder()
                .SetCategory((uint)category);

            if (scoreValue.Type == ScoreTableValueType.Int)
                updates.SetIvalue(scoreValue.IntValue);
            else if (scoreValue.Type == ScoreTableValueType.Float)
                updates.SetFvalue(scoreValue.FloatValue);

            return updates.Build();
        }

        public bool TryGetPlayerScore(Player player, int category, out int score)
        {
            score = 0;
            var scoreValue = GetScoreValueByCategory(player, category);
            if (scoreValue == null) return false;

            score = scoreValue.IntValue;
            return true;
        }

        private ScoreTableValue GetScoreValueByCategory(Player player, int category)
        {
            if (player != null && category < _playerSchemaSize)
            {
                var guid = player.DatabaseUniqueId;
                if (_playerRows.TryGetValue(guid, out var row)) 
                    return row.GetValueByCategory(category);
            }
            else if (_regionTableRow != null && category < _regionSchemaSize)
            {
                return _regionTableRow.GetValueByCategory(category);
            }
            return null;
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
        //private Event<AdjustHealthGameEvent>.Action _adjustHealthAction;
        //private Event<EntityDeadGameEvent>.Action _entityDeadAction;

        public ScoreTableType(ScoreTable table, ScoreTable.DataType type, int index)
        {
            _table = table;
            _type = type;
            _index = index;
        }

        public void Initialize(ScoreTableSchemaEntryPrototype proto)
        {
            if (proto == null) return;
            Prototype = proto;
            Name = proto.Name;

            /* Not used in game
            
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

            if (proto.EvalAuto != null && _type == ScoreTable.DataType.Region)
            {
                // only HoloSimScoreSchemaRegion, XDefenseRegionScoreSchema
            }
            */
        }

        public int GetEvalValue(Player player)
        {
            if (Prototype.EvalOnPlayerAdd == null || player == null) return 0;

            using var evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.Game = player.Game;
            evalContext.SetVar_EntityPtr(EvalContext.Default, player);
            evalContext.SetVar_EntityPtr(EvalContext.Entity, player.CurrentAvatar);
            return Eval.RunInt(Prototype.EvalOnPlayerAdd, evalContext);
        }
    }

    public class ScoreTableRow
    {
        private readonly ScoreTableValue[] _scores;

        public string Name;
        public int CategoriesNum { get => _scores.Length; }

        public ScoreTableRow(int size)
        {
            _scores = new ScoreTableValue[size];
            for (int i = 0; i < size; i++)
                _scores[i] = new ScoreTableValue(); 
        }

        public ScoreTableValue GetValueByCategory(int category)
        {
            if (category < 0 || category >= _scores.Length) return null;
            return _scores[category];
        }
    }

    public class ScoreTableRowPlayer(int size) : ScoreTableRow(size)
    {
        public PrototypeId Team { get; set; }
        public PrototypeId Avatar { get; set; }
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
