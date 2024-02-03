using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmu.Common.Config.Containers
{
    public class CommandConfig : ConfigContainer
    {
        public string MarvelHeroesOmegax86ExeFolderPath { get; private set; }
        public string MarvelHeroesOmegaLogClientPath { get; private set; }

        public CommandConfig(IniFile configFile) : base(configFile) { }
    }
}
