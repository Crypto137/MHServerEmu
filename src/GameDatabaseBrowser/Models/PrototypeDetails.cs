using MHServerEmu.Games.GameData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameDatabaseBrowser.Models
{
    public class PrototypeDetails
    {
        public string Name { get; set; }
        public string FullName { get; private set; }

        public PrototypeId PrototypeId => string.IsNullOrEmpty(FullName) ? 0 : GameDatabase.GetPrototypeRefByName(FullName);

        public List<PropertyDetails> Properties { get; private set; }

        public PrototypeDetails(string fullname, List<PropertyDetails> properties)
        {
            FullName = fullname;
            Name = fullname.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Last();
            Properties = properties;
        }
    }
}
