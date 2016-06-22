using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hoppla.Deployer.Agent
{
    public class ConfigurationException : Exception
    {
        public ConfigurationException(string message)
            : base(message) { }
    }
}
