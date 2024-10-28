using System.Text;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.UI.Widgets
{
    public enum PlayerState
    {
        Pending,
        Ready,
        Fallback
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

        public void SetPlayerState(ulong playerGuid, string playerName, PlayerState state)
        {
            if (_playerReadyStateDict.TryGetValue(playerGuid, out PlayerReadyState playerReadyState) == false)
            {
                playerReadyState = new();
                _playerReadyStateDict.Add(playerGuid, playerReadyState);
            }

            playerReadyState.PlayerName = playerName;
            playerReadyState.State = state;
            UpdateUI();
        }

        public void ResetPlayerState(ulong playerGuid)
        {
            _playerReadyStateDict.Remove(playerGuid);
            UpdateUI();
        }

        public void UpdatePlayerState(ulong playerGuid, PlayerState state)
        {
            if (_playerReadyStateDict.TryGetValue(playerGuid, out PlayerReadyState playerReadyState))
                playerReadyState.State = state;

            UpdateUI();
        }

        class PlayerReadyState : ISerialize
        {
            public string PlayerName;
            public PlayerState State;

            public PlayerReadyState() { }

            public bool Serialize(Archive archive)
            {
                bool success = true;

                success &= Serializer.Transfer(archive, ref PlayerName);

                if (archive.IsPacking)
                {
                    int state = (int)State;
                    success &= Serializer.Transfer(archive, ref state);
                }
                else
                {
                    int state = 0;
                    success &= Serializer.Transfer(archive, ref state);
                    State = (PlayerState)state;
                }

                return success;
            }

            public override string ToString()
            {
                return $"{PlayerName}={State}";
            }
        }
    }
}
