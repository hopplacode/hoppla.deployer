using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hoppla.Deployer.Agent
{
    public enum DeploymentTypeEnum
    {
        IISSite, Executable, WindowsService
    }

    public enum TargetEnvironmentEnum
    {
        Dev, AcceptanceTest, Production
    }
}
