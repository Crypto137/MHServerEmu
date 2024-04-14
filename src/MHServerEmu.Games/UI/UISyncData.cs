using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.System;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.UI
{
    /// <summary>
    /// Base class for serializable UI widget data.
    /// </summary>
    public class UISyncData : ISerialize
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        protected readonly UIDataProvider _uiDataProvider;
        protected readonly PrototypeId _widgetRef;
        protected readonly PrototypeId _contextRef;

        protected readonly List<PrototypeId> _areaList = new();

        // Although these time fields are in the base UISyncData class, they seem to be used only in UIWidgetGenericFraction.
        // Potential TODO: Although it wouldn't be client-accurate, consider moving these and related methods to UIWidgetGenericFraction.
        protected long _timeStart;
        protected long _timeEnd;
        protected bool _timePaused;

        public UISyncData(UIDataProvider uiDataProvider, PrototypeId widgetRef, PrototypeId contextRef)
        {
            _uiDataProvider = uiDataProvider;
            _widgetRef = widgetRef;
            _contextRef = contextRef;
        }

        public virtual bool Serialize(Archive archive)
        {
            bool success = true;

            int numAreas = _areaList.Count;
            success &= Serializer.Transfer(archive, ref numAreas);

            if (archive.IsPacking)
            {
                for (int i = 0; i < _areaList.Count; i++)
                {
                    PrototypeId areaRef = _areaList[i];
                    success &= Serializer.Transfer(archive, ref areaRef);
                }
            }
            else
            {
                _areaList.Clear();
                
                for (int i = 0; i < numAreas; i++)
                {
                    PrototypeId areaRef = PrototypeId.Invalid;
                    success &= Serializer.Transfer(archive, ref areaRef);
                    _areaList.Add(areaRef);
                }
            }

            return success;
        }

        public virtual void Decode(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            _areaList.Clear();
            int numAreas = stream.ReadRawInt32();
            for (int i = 0; i < numAreas; i++)
                _areaList.Add(stream.ReadPrototypeRef<Prototype>());
        }

        public virtual void Encode(CodedOutputStream stream, BoolEncoder boolEncoder)
        {
            stream.WriteRawInt32(_areaList.Count);
            foreach (PrototypeId areaRef in _areaList)
                stream.WritePrototypeRef<Prototype>(areaRef);
        }

        public virtual void EncodeBools(BoolEncoder boolEncoder) { }

        public virtual void UpdateUI() { }

        public override string ToString()
        {
            StringBuilder sb = new();
            BuildString(sb);
            return sb.ToString();
        }

        /// <summary>
        /// Sets elapsed time in milliseconds for this widget. Mutually exclusive with <see cref="SetTimeRemaining(long)"/>.
        /// </summary>
        public void SetTimeElapsed(long timeElapsedMS)
        {
            if (_timeEnd != 0)
            {
                Logger.Warn("SetTimeElapsed(): _timeEnd != 0");
                _timeEnd = 0;
            }

            _timeStart = (long)Clock.GameTime.TotalMilliseconds - timeElapsedMS;
            UpdateUI();
        }

        /// <summary>
        /// Sets remaining time in milliseconds for this widget. Mutually exclusive with <see cref="SetTimeElapsed(long)"/>.
        /// </summary>
        public void SetTimeRemaining(long timeRemainingMS)
        {
            if (_timeStart != 0)
            {
                Logger.Warn("SetTimeRemaining(): _timeStart != 0");
                _timeStart = 0;
            }

            _timeEnd = (long)Clock.GameTime.TotalMilliseconds + timeRemainingMS;
            UpdateUI();
        }

        /// <summary>
        /// Sets time pause state for this widget.
        /// </summary>
        public void SetTimePaused(bool timePaused)
        {
            _timePaused = timePaused;
            UpdateUI();
        }

        protected virtual void BuildString(StringBuilder sb)
        {
            for (int i = 0; i < _areaList.Count; i++)
                sb.AppendLine($"{nameof(_areaList)}[{i}]: {_areaList[i]}");

            sb.AppendLine($"{nameof(_timeStart)}: {(_timeStart != 0 ? Clock.GameTimeMillisecondsToDateTime(_timeStart) : 0)}");
            sb.AppendLine($"{nameof(_timeEnd)}: {(_timeEnd != 0 ? Clock.GameTimeMillisecondsToDateTime(_timeEnd) : 0)}");
            sb.AppendLine($"{nameof(_timePaused)}: {_timePaused}");
        }
    }
}
