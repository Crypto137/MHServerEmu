using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.DatabaseAccess
{
    /// <summary>
    /// Provides access to a <see cref="DBAccount"/>.
    /// </summary>
    public interface IDBAccountOwner
    {
        public DBAccount Account { get; }
    }
}
