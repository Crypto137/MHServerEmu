namespace MHServerEmu.Games.Properties
{
    /// <summary>
    /// An inteface for implementing the Observer pattern for <see cref="PropertyCollection"/>.
    /// </summary>
    public interface IPropertyChangeWatcher
    {
        /// <summary>
        /// Subscribes for property updates in the provided <see cref="PropertyCollection"/>.
        /// </summary>
        public void Attach(PropertyCollection propertyCollection);

        /// <summary>
        /// Unsubscribes from property updates in the provided <see cref="PropertyCollection"/>/
        /// </summary>
        public void Detach(bool removeFromAttachedCollection);

        /// <summary>
        /// Handles property change in the attached <see cref="PropertyCollection"/>. 
        /// </summary>
        public void OnPropertyChange(PropertyId id, PropertyValue newValue, PropertyValue oldValue, SetPropertyFlags flags);
    }
}
