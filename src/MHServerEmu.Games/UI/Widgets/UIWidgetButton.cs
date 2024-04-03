using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.UI.Widgets
{
    public class UIWidgetButton : UISyncData
    {
        // NOTE: This widget may be unfinished. The only place it seems to may have been used is
        // MetaStateShutdown for DangerRoom, but the TeleportButtonWidget field does not have
        // any data in any of the prototypes, and even the TeleportButtonWidget prototype itself
        // does not seem to be referenced anywhere else.
        //
        // It does sort of work, but the visuals for it get outside the bounds of the widget bar.

        private readonly List<CallbackBase> _callbackList = new();

        public UIWidgetButton(UIDataProvider uiDataProvider, PrototypeId widgetRef, PrototypeId contextRef) : base(uiDataProvider, widgetRef, contextRef) { }

        public override void Decode(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            base.Decode(stream, boolDecoder);

            uint numCallbacks = stream.ReadRawVarint32();
            for (uint i = 0; i < numCallbacks; i++)
                _callbackList.Add(new(stream.ReadRawVarint64()));
        }

        public override void Encode(CodedOutputStream stream, BoolEncoder boolEncoder)
        {
            base.Encode(stream, boolEncoder);

            stream.WriteRawVarint32((uint)_callbackList.Count);
            foreach (CallbackBase callback in _callbackList)
                stream.WriteRawVarint64(callback.PlayerGuid);
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            for (int i = 0; i < _callbackList.Count; i++)
                sb.AppendLine($"{nameof(_callbackList)}[{i}]: {_callbackList[i]}");
        }

        public void AddCallback(ulong playerGuid)
        {
            _callbackList.Add(new(playerGuid));
            UpdateUI();
        }

        class CallbackBase
        {
            public ulong PlayerGuid { get; }

            public CallbackBase(ulong playerGuid)
            {
                PlayerGuid = playerGuid;
            }

            public override string ToString()
            {
                return $"{nameof(PlayerGuid)}: 0x{PlayerGuid:X16}";
            }
        }
    }
}
