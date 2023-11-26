using System.Diagnostics;
using System.Reflection;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData
{
    public class PrototypeClassManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<string, Type> _prototypeNameToTypeDict = new();

        public PrototypeClassManager()
        {
            var stopwatch = Stopwatch.StartNew();

            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.IsSubclassOf(typeof(Prototype)) == false) continue;
                _prototypeNameToTypeDict.Add(type.Name, type);
            }

            stopwatch.Stop();
            Logger.Info($"Initialized in {stopwatch.ElapsedMilliseconds} ms");
        }

        public Type GetPrototypeTypeByName(string name)
        {
            if (_prototypeNameToTypeDict.TryGetValue(name, out Type type) == false)
                return null;

            return type;
        }

        public bool PrototypeClassIsA(Type classToCheck, Type parent)
        {
            return classToCheck.IsSubclassOf(parent);
        }
    }
}
