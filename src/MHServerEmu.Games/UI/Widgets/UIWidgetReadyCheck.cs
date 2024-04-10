using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.UI.Widgets
{
    public enum PlayerReadyStateValue
    {
        Pending,
        Ready
    }

    public class UIWidgetReadyCheck : UISyncData
    {
        private Dictionary<ulong, PlayerReadyState> _playerReadyStateDict = new();

        public UIWidgetReadyCheck(UIDataProvider uiDataProvider, PrototypeId widgetRef, PrototypeId contextRef) : base(uiDataProvider, widgetRef, contextRef) { }

        public override bool Serialize(Archive archive)
        {
            bool success = base.Serialize(archive);

            success &= Serializer.Transfer(archive, ref _playerReadyStateDict);
            
            // UIWidgetReadyCheck::calculateNumberReady()
            
            return success;
        }

        public override void Decode(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            base.Decode(stream, boolDecoder);

            _playerReadyStateDict.Clear();
            ulong numStates = stream.ReadRawVarint64();
            for (ulong i = 0; i < numStates; i++)
            {
                ulong key = stream.ReadRawVarint64();
                PlayerReadyState readyState = new();
                readyState.Decode(stream);
                _playerReadyStateDict.Add(key, readyState);
            }
        }

        public override void Encode(CodedOutputStream stream, BoolEncoder boolEncoder)
        {
            base.Encode(stream, boolEncoder);

            stream.WriteRawVarint64((ulong)_playerReadyStateDict.Count);
            foreach (var kvp in _playerReadyStateDict)
            {
                stream.WriteRawVarint64(kvp.Key);
                kvp.Value.Encode(stream);
            }
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            foreach (var kvp in _playerReadyStateDict)
                sb.AppendLine($"{nameof(_playerReadyStateDict)}[{kvp.Key}]: {kvp.Value}");
        }

        public void SetPlayerReadyState(ulong key, string playerName, PlayerReadyStateValue stateValue)
        {
            if (_playerReadyStateDict.TryGetValue(key, out PlayerReadyState playerReadyState) == false)
            {
                playerReadyState = new();
                _playerReadyStateDict.Add(key, playerReadyState);
            }

            playerReadyState.PlayerName = playerName;
            playerReadyState.StateValue = stateValue;
            UpdateUI();
        }

        class PlayerReadyState : ISerialize
        {
            public string PlayerName;
            public PlayerReadyStateValue StateValue;

            public PlayerReadyState() { }

            public bool Serialize(Archive archive)
            {
                bool success = true;

                success &= Serializer.Transfer(archive, ref PlayerName);

                if (archive.IsPacking)
                {
                    int stateValue = (int)StateValue;
                    success &= Serializer.Transfer(archive, ref stateValue);
                }
                else
                {
                    int stateValue = 0;
                    success &= Serializer.Transfer(archive, ref stateValue);
                    StateValue = (PlayerReadyStateValue)stateValue;
                }

                return success;
            }

            public void Decode(CodedInputStream stream)
            {
                PlayerName = stream.ReadRawString();
                StateValue = (PlayerReadyStateValue)stream.ReadRawInt32();
            }

            public void Encode(CodedOutputStream stream)
            {
                stream.WriteRawString(PlayerName);
                stream.WriteRawInt32((int)StateValue);
            }

            public override string ToString()
            {
                return $"{PlayerName}={StateValue}";
            }
        }
    }
}
