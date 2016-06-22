using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hoppla.Deployer.Agent.Services;

namespace Hoppla.Deployer.Agent
{
    public static class ActionExecutor
    {
        public static ActionExecutionResult Invoke<T>(T sequentialAction) where T : ISequentialAction
        {
            try
            {
                ActionExecutionResult result = sequentialAction.Execute();
                return result;
            }
            catch (Exception ex)
            {
                ActionExecutionResult result = new ActionExecutionResult(sequentialAction.GetActionName(), false);
                result.Exception = ex;
                result.Information = ex.Message;
                return result;
            }
        }
    }

    public class ActionBundleExecutor
    {
        ILog _log;
        IActionBundle _actionBundle;

        public ActionBundleExecutor(IActionBundle actionBundle)
        {
            _actionBundle = actionBundle;
            if (actionBundle == null || !actionBundle.GetActions().Any())
                throw new ConfigurationException("Bundle contains no actions.");
        }

        public ActionBundleExecutionResult Execute()
        {
            ActionBundleExecutionResult actionBundleExecutionResult = new ActionBundleExecutionResult(_actionBundle.DeliveryObjectName, _actionBundle.TargetEnvironment);

            foreach (var action in _actionBundle.GetActions())
            {
                ActionExecutionResult actionExecutionResult = ActionExecutor.Invoke(action);                 
                actionBundleExecutionResult.ActionExecutionResults.Add(actionExecutionResult);
                if (!actionExecutionResult.Success)
                    break;
            }
            actionBundleExecutionResult.Finished = DateTime.Now;
            return actionBundleExecutionResult;
        }
    }

    public class ActionBundleExecutionResult
    {
        public ActionBundleExecutionResult(string deliveryObjectName, TargetEnvironmentEnum targetEnvironment)
        {
            DeliveryObjectName = deliveryObjectName;
            ActionExecutionResults = new List<ActionExecutionResult>();
            Started = DateTime.Now;
            TargetEnvironment = targetEnvironment;
        }

        public TargetEnvironmentEnum TargetEnvironment { get; private set; }
        public string DeliveryObjectName { get; private set; }
        public DateTime Started { get; private set; }
        public DateTime Finished { get; set; }
        public List<ActionExecutionResult> ActionExecutionResults { get; set; }
        public bool Success { get { return ActionExecutionResults.All(x => x.Success == true); } }
    }


}
