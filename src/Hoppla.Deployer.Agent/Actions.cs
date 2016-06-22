using Humanizer;
using Ionic.Zip;
using Microsoft.Web.Administration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hoppla.Deployer.Agent.Services;
using System.Net;
using System.Reflection;
using System.Net.Http;

namespace Hoppla.Deployer.Agent
{
    public abstract class ActionBase : ISequentialAction
    {
        private string _humanActionName;

        public ActionBase(string humanActionName = null)
        {
            if (humanActionName == null)
                _humanActionName = this.GetType().Name.Humanize().Replace(" action", "");
            else
                _humanActionName = humanActionName;
        }

        public virtual ActionExecutionResult Execute()
        {
            throw new NotImplementedException();
        }


        public string GetActionName()
        {
            return _humanActionName;
        }
    }

    public class ActionExecutionResult
    {
        public ActionExecutionResult(string actionName, bool success)
        {
            Success = success;
            ActionName = actionName;
        }

        public string DebugInformation { get; set; }
        public string Information { get; set; }
        public string ActionName { get; private set; }
        public bool Success { get; set; }
        public bool IsReleaseNote { get; set; }
        public Exception Exception { get; set; }
    }

    public interface ISequentialAction
    {
        ActionExecutionResult Execute();
        string GetActionName();

    }

    public class BackupCurrentReleaseDirectoryAction : ActionBase 
    {
        string _sourceDir;
        string _targetDir;
        string _deliveryObjectName;

        public BackupCurrentReleaseDirectoryAction(string deliveryObjectName, string sourceDir, string targetDir)
        {
            _sourceDir = sourceDir;
            _targetDir = targetDir;
            _deliveryObjectName = deliveryObjectName;
        }

        public override ActionExecutionResult Execute()
        {
             
            using (ZipFile zip = new ZipFile())
            {
                zip.AddDirectory(_sourceDir);
                var name = GetBackupNameForCurrentRelease(null);
                try
                {
                    zip.Save(_targetDir + "\\" + name);
                }
                catch
                {
                    name = GetBackupNameForCurrentRelease(name);
                    zip.Save(_targetDir + "\\" + name);
                }
            }
            return new ActionExecutionResult(base.GetActionName(), true);
        }

        public string GetBackupNameForCurrentRelease(string existingName = null)
        {
            var name = string.Format("{0} before deploy {1}", _deliveryObjectName, DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss"));
            return name + ".zip";
        }
    }

    public class DeleteDirectoryContentAction : ActionBase
    {
        string _targetDir;

        public DeleteDirectoryContentAction(string targetDir, string humanActionName = null)
            : base(humanActionName)
        {
            _targetDir = targetDir;
        }

        public override ActionExecutionResult Execute()
        {
            DirectoryInfo dirinfo = new DirectoryInfo(_targetDir);

            foreach (FileInfo file in dirinfo.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in dirinfo.GetDirectories())
            {
                dir.Delete(true);
            }
            return new ActionExecutionResult(base.GetActionName(), true);
        }
    }

    public class ExtractDirectoryAction : ActionBase
    {
        string _sourceFile;
        string _targetDir;

        public ExtractDirectoryAction(string sourceFile, string targetDir, string humanActionName = null)
            : base(humanActionName)
        {
            _sourceFile = sourceFile;
            _targetDir = targetDir;
        }

        public override ActionExecutionResult Execute()
        {
            try
            {
                using (ZipFile zip = ZipFile.Read(_sourceFile))
                {
                    zip.ExtractAll(_targetDir);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return new ActionExecutionResult(base.GetActionName(), true);
        }
    }

    public class MoveDeliveryObjectToReleaseHistoryAction : ActionBase
    {
        string _sourcePath;
        string _targetDirPath;

        public MoveDeliveryObjectToReleaseHistoryAction(string sourcePath, string targetDirPath)
        {
            _sourcePath = sourcePath;
            _targetDirPath = targetDirPath;
        }

        public override ActionExecutionResult Execute()
        {
            try
            {
                _targetDirPath = _targetDirPath.TrimEnd('\\');
                var fileName = Path.GetFileNameWithoutExtension(_sourcePath);
                var targetPath = string.Format("{0}\\{1}_{2}.zip", _targetDirPath, fileName, DateTime.Now.ToString("HH_mm_ss"));
                File.Move(_sourcePath, targetPath);
            }
            catch (Exception)
            {
                throw;
            }
            return new ActionExecutionResult(base.GetActionName(), true);
        }
    }

    public class StopIISAction : ActionBase
    {
        string _siteName;

        public StopIISAction(string site)
        {
            _siteName = site;
        }

        public override ActionExecutionResult Execute()
        {
            var server = new ServerManager();
            var site = server.Sites.FirstOrDefault(s => s.Name == _siteName);
            if (site == null)
            {
                throw new ApplicationException("Could not find website!");
            }
            else
            {
                site.Stop();
                if (site.State != ObjectState.Stopped)
                {
                    throw new ApplicationException("Could not stop website!");
                }
            }
            return new ActionExecutionResult(base.GetActionName(), true);
        }
    }

    public class StartIISAction : ActionBase
    {
        string _siteName;

        public StartIISAction(string site)
        {
            _siteName = site;
        }

        public override ActionExecutionResult Execute()
        {
            var server = new ServerManager();
            var site = server.Sites.FirstOrDefault(s => s.Name == _siteName);
            if (site != null)
            {
                site.Start();
                if (site.State == ObjectState.Stopped)
                {
                    throw new ApplicationException("Could not start website!");
                }
            }
            else
            {
                throw new ApplicationException("Could not find website!");
            }
            return new ActionExecutionResult(base.GetActionName(), true);
        }
    }

    public class VerifyHttpResponse : ActionBase
    {
        string _uri;
        int _expectedResponseCode;
        
        public VerifyHttpResponse(string uri, int expectedResponseCode)
        {
            _uri = uri;
            _expectedResponseCode = expectedResponseCode;
        }

        public override ActionExecutionResult Execute()
        {
            try
            {
                HttpResponseMessage response = CallResource(_uri);
                Console.Write("VerifyHttpResponse");
                var statusCode = (int)response.StatusCode;

                if (statusCode != _expectedResponseCode)
                    throw new ApplicationException(string.Format("Expected statuscode: {0}, received {1}, on GET {2}", _expectedResponseCode, statusCode, _uri));

                return new ActionExecutionResult(base.GetActionName(), true) { DebugInformation = " > Verifying HTTP: StatusCode: " + statusCode + "." };
            }
            catch(Exception ex)
            {
                throw new ApplicationException(string.Format("Error on GET {0}", _uri), ex);
            }
        }

        static HttpResponseMessage CallResource(string uri)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = new TimeSpan(0, 10, 0);
                using (var response = client.GetAsync(uri))
                {
                    return response.Result;
                }
            }            
        }
    }

    public class ApplyTargetEnvironmentConfigAction : ActionBase
    {
        DeploymentTypeEnum _releaseType;
        TargetEnvironmentEnum _targetEnvironment;
        string _targetDir;
        string _entryPointAssemblyFileName;

        public ApplyTargetEnvironmentConfigAction(DeploymentTypeEnum releaseType, TargetEnvironmentEnum targetEnvironment, string targetDir, string entryPointAssemblyFileName)
        {
            _releaseType = releaseType;
            _targetEnvironment = targetEnvironment;
            _targetDir = targetDir;
            _entryPointAssemblyFileName = entryPointAssemblyFileName;
        }

        public override ActionExecutionResult Execute()
        {
            try
            {
                var environmentConfig = string.Format("{0}\\{1}.config", _targetDir, _targetEnvironment.ToString());

                if (File.Exists(environmentConfig))
                {
                    string destConfigFileName = "";
                    if (_releaseType == DeploymentTypeEnum.IISSite)
                    {
                        destConfigFileName = "Web.config";
                    }
                    else
                    {
                        destConfigFileName = _entryPointAssemblyFileName + ".config";
                    }
                    File.Copy(environmentConfig, string.Format("{0}\\{1}", _targetDir, destConfigFileName), true);
                }
                else
                    throw new Exception("Could not find environment specific config file.");

                foreach (var environmentString in Enum.GetValues(typeof(TargetEnvironmentEnum)))
                {
                    File.Delete(string.Format("{0}\\{1}.config", _targetDir, environmentString.ToString()));
                }

            }
            catch (Exception ex)
            {
                throw new ApplicationException("Could not apply correct target environment configuration.", ex);
            }
            return new ActionExecutionResult(base.GetActionName(), true);
        }
    }

    public class AfterDeploymentMethodAction : ActionBase
    {
        public AfterDeploymentMethodAction(DeploymentPackageConfiguration deploymentPackageConfiguration)
        {
            _deploymentPackageConfiguration = deploymentPackageConfiguration;
            _methodToExecute = "After";

            _entryPointAssemblyFilePath = _deploymentPackageConfiguration.TargetPath;
            if (_deploymentPackageConfiguration.DeploymentType == DeploymentTypeEnum.IISSite)
                _entryPointAssemblyFilePath += "\\bin";
            _entryPointAssemblyFilePath += "\\" + _deploymentPackageConfiguration.EntryPointAssemblyFileName;

            var defaultNamespace = _deploymentPackageConfiguration.Name.Replace("_", ".");
            _fullyQualifiedClassName = string.Format("{0}.Deployments.Deployment_{1}", defaultNamespace, _deploymentPackageConfiguration.Date.ToString("yyyyMMdd"));
        }

        DeploymentPackageConfiguration _deploymentPackageConfiguration;
        string _entryPointAssemblyFilePath;
        string _fullyQualifiedClassName;
        string _methodToExecute;

        public override ActionExecutionResult Execute()
        {
            var result = ExecuteMethodInAssembly(_entryPointAssemblyFilePath, _fullyQualifiedClassName, _methodToExecute);
            return new ActionExecutionResult(base.GetActionName(), true) { Information = result };
        }

        private string ExecuteMethodInAssembly(string assemblyFile, string className, string methodToExecute)
        {
            try
            {
                Assembly a = Assembly.Load(File.ReadAllBytes(assemblyFile));
                // Get the type to use.
                Type myType = a.GetType(className);
                // Get the method to call.
                if (myType != null)
                {
                    MethodInfo myMethod = myType.GetMethod(methodToExecute);
                    if (myMethod != null)
                    {
                        // Create an instance. 
                        object obj = Activator.CreateInstance(myType);
                        // Execute the method.
                        return (string)myMethod.Invoke(obj, null);
                    }
                }               
            }
            catch (Exception ex)
            {
                throw new ApplicationException(string.Format("Could not execute {0} in class {1}.", methodToExecute, className), ex);
            }

            return string.Format("No actions to run.");
        }
    }


    public class ReleaseNotesAction : ActionBase
    {
        public ReleaseNotesAction(DeploymentPackageConfiguration deploymentPackageConfiguration)
        {
            _deploymentPackageConfiguration = deploymentPackageConfiguration;
            _methodToExecute = "ReleaseNotes";

            _entryPointAssemblyFilePath = _deploymentPackageConfiguration.TargetPath;
            if (_deploymentPackageConfiguration.DeploymentType == DeploymentTypeEnum.IISSite)
                _entryPointAssemblyFilePath += "\\bin";
            _entryPointAssemblyFilePath += "\\" + _deploymentPackageConfiguration.EntryPointAssemblyFileName;

            var defaultNamespace = _deploymentPackageConfiguration.Name.Replace("_", ".");
            _fullyQualifiedClassName = string.Format("{0}.Deployments.Deployment_{1}", defaultNamespace, _deploymentPackageConfiguration.Date.ToString("yyyyMMdd"));
        }

        DeploymentPackageConfiguration _deploymentPackageConfiguration;
        string _entryPointAssemblyFilePath;
        string _fullyQualifiedClassName;
        string _methodToExecute;

        public override ActionExecutionResult Execute()
        {
            var result = ExecuteMethodInAssembly(_entryPointAssemblyFilePath, _fullyQualifiedClassName, _methodToExecute);
            var reln = new ReleaseNotes(result);
            return new ActionExecutionResult(base.GetActionName(), true) { Information = reln.GetReleaseNoteAsHtml(), IsReleaseNote = true };
        }


        private class ReleaseNotes
        {
            public ReleaseNotes(string notes)
            {
                Additions = new List<string>();
                Removals = new List<string>();
                Fixes = new List<string>();


                foreach (var row in notes.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (row.StartsWith("+"))
                        Additions.Add(row.TrimStart('+').Trim());
                    else if (row.StartsWith("-"))
                        Removals.Add(row.TrimStart('-').Trim());
                    else if (row.StartsWith("*"))
                        Fixes.Add(row.TrimStart('*').Trim());
                }
            }

            List<string> Additions;
            List<string> Removals;
            List<string> Fixes;

            public IEnumerable<string> GetAdditions()
            {
                return Additions.AsEnumerable();
            }

            public IEnumerable<string> GetRemovals()
            {
                return Removals.AsEnumerable();
            }

            public IEnumerable<string> GetFixes()
            {
                return Fixes.AsEnumerable();
            }

            public string GetReleaseNoteAsHtml()
            {
                StringBuilder sb = new StringBuilder();                
                sb.Append(GetHtmlList("Added features", Additions));
                sb.Append(GetHtmlList("Removed features", Removals));
                sb.Append(GetHtmlList("Fixes", Fixes));               
                return sb.ToString();                
            }

            private string GetHtmlList(string header, IEnumerable<string> items)
            {
                
                if (items.Any())
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine(string.Format("<div style='font-size:90%;font-weight:bold'>{0}</div>", header));
                    sb.AppendLine("<ul style='font-size:85%'>");
                    foreach (var item in items)
                    {
                        sb.AppendLine("<li>" + item + "</li>");
                    }
                    sb.AppendLine("</ul>");
                    return sb.ToString();
                }
                return string.Empty;
            }
        }

        private string ExecuteMethodInAssembly(string assemblyFile, string className, string methodToExecute)
        {
            try
            {
                Assembly a = Assembly.Load(File.ReadAllBytes(assemblyFile));
                // Get the type to use.
                Type myType = a.GetType(className);
                // Get the method to call.
                if (myType != null)
                {
                    MethodInfo myMethod = myType.GetMethod(methodToExecute);
                    if (myMethod != null)
                    {
                        // Create an instance. 
                        object obj = Activator.CreateInstance(myType);
                        // Execute the method.
                        return (string)myMethod.Invoke(obj, null);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(string.Format("Could not execute {0} in class {1}.", methodToExecute, className), ex);
            }

            return string.Format("No actions to run.");
        }
    }


    public class PingAction : ActionBase
    {
        public override ActionExecutionResult Execute()
        {
            Console.WriteLine(DateTime.Now);
            return new ActionExecutionResult(base.GetActionName(), true);
        }
    }
}
