using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hoppla.Deployer.Agent
{
    public class EmailViewModel
    {
        public EmailViewModel(List<ActionBundleExecutionResult> results)
        {
            ActionBundleExecutionResults = results;
        }
        public List<ActionBundleExecutionResult> ActionBundleExecutionResults { get; set; }
        public int NumOfFailedBundles { get { return ActionBundleExecutionResults.Count(x => x.Success == false); } }
        public int NumSuccessBundles { get { return ActionBundleExecutionResults.Count(x => x.Success == true); } }
        public int NumOfBundles { get { return ActionBundleExecutionResults.Count; } }
        public DateTime DeployedFinished { get { return DateTime.Now; } }
        public string CssMedia { get { return "@media"; } }
        public string CssAtChar { get { return "@"; } }
    }
}
