using System.Text;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Powers
{
    public class Power
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private bool _isTeamUpPassivePowerWhileAway;

        public Game Game { get; }
        public PrototypeId PrototypeDataRef { get; }
        public PowerPrototype Prototype { get; }
        public WorldEntity Owner { get; private set; }

        public PowerCategoryType PowerCategory { get => Prototype != null ? Prototype.PowerCategory : PowerCategoryType.None; }

        public PropertyCollection Properties { get; } = new();
        public bool IsTravelPower { get => Prototype != null && Prototype.IsTravelPower; }
        public bool IsChannelingPower { get; internal set; }

        public Power(Game game, PrototypeId prototypeDataRef)
        {
            Game = game;
            PrototypeDataRef = prototypeDataRef;
            Prototype = prototypeDataRef.As<PowerPrototype>();
        }

        public bool Initialize(WorldEntity owner, bool isTeamUpPassivePowerWhileAway, PropertyCollection initializeProperties)
        {
            Owner = owner;
            _isTeamUpPassivePowerWhileAway = isTeamUpPassivePowerWhileAway;

            if (Prototype == null)
                return Logger.WarnReturn(false, $"Initialize(): Prototype == null");

            GeneratePowerProperties(Properties, Prototype, initializeProperties, Owner);
            // TODO: Power::createSituationalComponent()

            return true;
        }

        public void OnAssign()
        {
            // TODO
        }

        public static void GeneratePowerProperties(PropertyCollection primaryCollection, PowerPrototype prototype, PropertyCollection initializeProperties, WorldEntity owner)
        {
            // Start with a clean copy from the prototype
            if (prototype.Properties != null)
                primaryCollection.FlattenCopyFrom(prototype.Properties, true);

            // Add properties from the collection passed in the Initialize() method if we have one
            if (initializeProperties != null)
                primaryCollection.FlattenCopyFrom(initializeProperties, false);

            // Set properties for all keywords assigned in the prototype
            if (prototype.Keywords != null)
            {
                foreach (PrototypeId keywordRef in prototype.Keywords)
                    primaryCollection[PropertyEnum.HasPowerKeyword, keywordRef] = true;
            }

            if (prototype.EvalOnCreate != null)
            {
                // TODO
            }
            
            if (prototype.EvalPowerSynergies != null)
            {
                // TODO
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(PrototypeDataRef)}: {GameDatabase.GetPrototypeName(PrototypeDataRef)}");
            sb.AppendLine($"{nameof(Owner)}: {(Owner != null ? Owner.Id : 0)}");
            sb.AppendLine($"{nameof(Properties)}: {Properties}");
            return sb.ToString();
        }

        // Static accessors
        public static PowerCategoryType GetPowerCategory(PowerPrototype powerProto) => powerProto.PowerCategory;
        public static bool IsComboEffect(PowerPrototype powerProto) => GetPowerCategory(powerProto) == PowerCategoryType.ComboEffect;
        public static bool IsUltimatePower(PowerPrototype powerProto) => powerProto.IsUltimate;

        internal TimeSpan GetCooldownTimeRemaining()
        {
            throw new NotImplementedException();
        }
    }
}
