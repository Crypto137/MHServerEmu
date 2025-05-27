using System.Text;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.UI
{
    /// <summary>
    /// Base class for serializable UI widget data.
    /// </summary>
    public class UISyncData : ISerialize
    {
        private const int UISyncDataTimeStartOffsetMS = 100;

        private static readonly Logger Logger = LogManager.CreateLogger();

        protected readonly UIDataProvider _uiDataProvider;
        protected readonly PrototypeId _widgetRef;
        protected readonly PrototypeId _contextRef;
        protected readonly HashSet<PrototypeId> _areaList = new();

        public PrototypeId WidgetRef { get => _widgetRef; }
        public PrototypeId ContextRef { get => _contextRef; }

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

        public virtual void Deallocate() { }

        public virtual bool Serialize(Archive archive)
        {
            bool success = true;

            int numAreas = _areaList.Count;
            success &= Serializer.Transfer(archive, ref numAreas);

            if (archive.IsPacking)
            {
                foreach (var areaSet in _areaList)
                {
                    PrototypeId areaRef = areaSet;
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

        public virtual void UpdateUI() 
        { 
            _uiDataProvider.OnUpdateUI(this); 
        }

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

            var game = Game.Current;

            // Client checks _timeStart against its own GameTime, so we need to add a time offset here to avoid UI issues
            var clientCurrentTime = (long)game.CurrentTime.TotalMilliseconds - UISyncDataTimeStartOffsetMS;

            _timeStart = clientCurrentTime - timeElapsedMS;
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

            _timeEnd = (long)Game.Current.CurrentTime.TotalMilliseconds + timeRemainingMS;
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
            int i = 0;
            foreach (var area in _areaList)
                sb.AppendLine($"{nameof(_areaList)}[{i++}]: {area}");

            sb.AppendLine($"{nameof(_timeStart)}: {(_timeStart != 0 ? Clock.GameTimeMillisecondsToDateTime(_timeStart) : 0)}");
            sb.AppendLine($"{nameof(_timeEnd)}: {(_timeEnd != 0 ? Clock.GameTimeMillisecondsToDateTime(_timeEnd) : 0)}");
            sb.AppendLine($"{nameof(_timePaused)}: {_timePaused}");
        }

        public void SetAreaContext(PrototypeId contextRef)
        {
            var contextProto = GameDatabase.GetPrototype<Prototype>(contextRef);
            if (contextProto is OpenMissionPrototype openProto)
            {
                if (openProto.ActiveInAreas.HasValue())
                    _areaList.Insert(openProto.ActiveInAreas);
                UpdateUI();
            }
            else if (contextProto is RegionPrototype)
            {
                HashSet<PrototypeId> areaList = HashSetPool<PrototypeId>.Instance.Get();
                RegionPrototype.GetAreasInGenerator(contextRef, areaList);
                _areaList.Insert(areaList);
                HashSetPool<PrototypeId>.Instance.Return(areaList);
                UpdateUI();
            }
        }

        public virtual void OnEntityTracked(WorldEntity worldEntity) { }
        public virtual void OnEntityLifecycle(WorldEntity worldEntity) { }
        public virtual void OnKnownEntityPropertyChanged(PropertyId id) { }
    }
}
