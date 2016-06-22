using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hoppla.Deployer.Agent
{
    public class StopIISActionFake : ActionBase
    {
        string _siteName;

        public StopIISActionFake(string site)
        {
            _siteName = site;
        }

        public override ActionExecutionResult Execute()
        {
            Console.WriteLine(String.Format("Fake stopping {0}.", _siteName));
            return new ActionExecutionResult(base.GetActionName(), true);
        }
    }

    public class StartIISActionFake : ActionBase
    {
        string _siteName;

        public StartIISActionFake(string site)
        {
            _siteName = site;
        }

        public override ActionExecutionResult Execute()
        {
            Console.WriteLine(String.Format("Fake starting {0}.", _siteName));
            return new ActionExecutionResult(base.GetActionName(), true);
        }
    }
}
