using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Regions;
using System.Text;

namespace MHServerEmu.Games.Navi
{
    public class NaviSystem
    {
        public bool Log = true;
        public static readonly Logger Logger = LogManager.CreateLogger();
        public Region Region { get; private set; }
        public List<NaviErrorReport> ErrorLog { get; private set; } = new ();

        public bool Initialize(Region region)
        {
            Region = region;
            return true;
        }

        public void LogError(string msg)
        {
            NaviErrorReport errorReport = new()
            {
                Msg = msg
            };
            ErrorLog.Add(errorReport);
        }

        public void LogError(string msg, NaviEdge edge)
        {
            NaviErrorReport errorReport = new()
            {
                Msg = msg,
                Edge = edge
            };
            ErrorLog.Add(errorReport);
        }

        public void LogError(string msg, NaviPoint point)
        {
            NaviErrorReport errorReport = new()
            {
                Msg = msg,
                Point = point
            };
            ErrorLog.Add(errorReport);
        }

        public void ClearErrorLog()
        {
            ErrorLog.Clear();
        }

        public bool CheckErrorLog(bool clearErrorLog, string info = null)
        {
            bool hasErrors = HasErrors();
            if (Log && hasErrors)
            {
                var error = ErrorLog[0];

                Cell cell = null;
                if (Region != null)
                {
                    if (error.Point != null)
                        cell = Region.GetCellAtPosition(error.Point.Pos);
                    else if (error.Edge != null)
                        cell = Region.GetCellAtPosition(error.Edge.Midpoint());
                }
                StringBuilder sb = new();
                sb.AppendLine($"Navigation Error: {error.Msg}");
                sb.AppendLine($"Cell: {(cell != null ? cell.ToString() : "Unknown")}");
                if (error.Point != null)
                    sb.AppendLine($"Point: {error.Point}");
                if (error.Edge != null)
                    sb.AppendLine($"Edge: {error.Edge}");
                if (string.IsNullOrEmpty(info) == false)
                    sb.AppendLine($"Extra Info: {info}");
                Logger.Error(sb.ToString());
            }

            if (clearErrorLog) ClearErrorLog();
            return hasErrors;
        }

        public bool HasErrors()
        {
            return ErrorLog.Count > 0;
        }

    }

    public struct NaviErrorReport
    { 
        public string Msg;
        public NaviPoint Point;
        public NaviEdge Edge;
    }

    public readonly struct NaviSerialCheck : IDisposable
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly NaviCdt _naviCdt;

        public uint Serial { get; }

        public NaviSerialCheck(NaviCdt naviCdt)
        {
            _naviCdt = naviCdt;
            Serial = ++_naviCdt.Serial;
        }

        public void Dispose()
        {
            // This checks mimicks the destructor in the client.
            if (Serial != _naviCdt.Serial)
                Logger.Warn("Dispose(): Serial != _naviCdt.Serial");
        }
    }

    public class NavigationInfluence
    {
        public NaviPoint Point;
        public NaviTriangle Triangle;
    }
}
