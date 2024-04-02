using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.UI
{
    public class UISyncData
    {
        protected readonly UIDataProvider _uiDataProvider;
        protected readonly PrototypeId _widgetRef;
        protected readonly PrototypeId _contextRef;

        protected PrototypeId[] _areas = Array.Empty<PrototypeId>();

        public UISyncData(UIDataProvider uiDataProvider, PrototypeId widgetRef, PrototypeId contextRef)
        {
            _uiDataProvider = uiDataProvider;
            _widgetRef = widgetRef;
            _contextRef = contextRef;
        }

        public virtual void Decode(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            _areas = new PrototypeId[stream.ReadRawInt32()];
            for (int i = 0; i < _areas.Length; i++)
                _areas[i] = stream.ReadPrototypeRef<Prototype>();
        }

        public virtual void Encode(CodedOutputStream stream, BoolEncoder boolEncoder)
        {
            stream.WriteRawInt32(_areas.Length);
            for (int i = 0; i < _areas.Length; i++)
                stream.WritePrototypeRef<Prototype>(_areas[i]);
        }

        public virtual void EncodeBools(BoolEncoder boolEncoder) { }

        public virtual void UpdateUI() { }

        public override string ToString()
        {
            StringBuilder sb = new();
            BuildString(sb);
            return sb.ToString();
        }

        protected virtual void BuildString(StringBuilder sb)
        {
            for (int i = 0; i < _areas.Length; i++)
                sb.AppendLine($"_areas[{i}]: {_areas[i]}");
        }
    }
}
