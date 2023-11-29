using System.Diagnostics;
using System.Reflection;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData
{
    // We use C# types and reflection instead of class ids / class info and GRTTI

    public class PrototypeClassManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<string, Type> _prototypeNameToClassTypeDict = new();

        public int ClassCount { get => _prototypeNameToClassTypeDict.Count; }

        public PrototypeClassManager()
        {
            var stopwatch = Stopwatch.StartNew();

            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (PrototypeClassIsA(type, typeof(Prototype)) == false) continue;  // Skip non-prototype classes
                _prototypeNameToClassTypeDict.Add(type.Name, type);
            }

            stopwatch.Stop();
            Logger.Info($"Initialized {ClassCount} prototype classes in {stopwatch.ElapsedMilliseconds} ms");
        }

        public Type GetPrototypeClassTypeByName(string name)
        {
            if (_prototypeNameToClassTypeDict.TryGetValue(name, out Type type) == false)
            {
                Logger.Warn($"Prototype class {name} not found");
                return null;
            }

            return type;
        }

        public bool PrototypeClassIsA(Type classToCheck, Type parent)
        {
            return classToCheck == parent || classToCheck.IsSubclassOf(parent);
        }
    }
}
