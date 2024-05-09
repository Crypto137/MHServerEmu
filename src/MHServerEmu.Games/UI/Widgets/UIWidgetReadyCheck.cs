using System.Text;
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

            public override string ToString()
            {
                return $"{PlayerName}={StateValue}";
            }
        }
    }
}
