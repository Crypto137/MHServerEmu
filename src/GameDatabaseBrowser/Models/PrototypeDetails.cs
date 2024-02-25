using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameDatabaseBrowser.Models
{
    public class PrototypeDetails
    {
        public string Name { get; private set; }
        public string FullName { get; private set; }
        public List<Property> Properties { get; private set; }

        public PrototypeDetails(string fullname, List<Property> properties)
        {
            FullName = fullname;
            Name = fullname.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Last();
            Properties = properties;
        }
    }
}
