using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.GameData.Calligraphy.Attributes
{
    // We use these attributes as a replacement for the PrototypeFieldType enum value that is defined for each field in the client.
    // We need this to differentiate non-property mixins from RHStructs when using reflection to deserialize data.

    /// <summary>
    /// Indicates that a prototype field is a mixin prototype. As far as we currently know, these are used only in <see cref="AgentPrototype"/> and <see cref="ItemPrototype"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class MixinAttribute : Attribute { }

    /// <summary>
    /// Indicates that a prototype field is a list mixin prototype. As far as we currently know, these are used only in <see cref="PowerPrototype"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ListMixinAttribute : Attribute
    {
        public Type FieldType { get; }      // This property is used to indicate what prototype class type is expected in this list mixin

        public ListMixinAttribute(Type fieldType)
        {
            FieldType = fieldType;
        }
    }
}
