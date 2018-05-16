
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hoppla.Deployer.Agent
{
    public interface IActionBundle
    {
        void AddAction(ISequentialAction action);
        IEnumerable<ISequentialAction> GetActions();
        string DeliveryObjectName { get; }
        TargetEnvironmentEnum TargetEnvironment { get; }
    }

    public class ActionBundle : IActionBundle
    {
        List<ISequentialAction> _actions;
        public TargetEnvironmentEnum TargetEnvironment { get; set; }

        public ActionBundle(string deliveryObjectName, TargetEnvironmentEnum targetEnvironmentEnum)
        {
            DeliveryObjectName = deliveryObjectName;
            TargetEnvironment = targetEnvironmentEnum;
            _actions = new List<ISequentialAction>();
        }

        public void AddAction(ISequentialAction action)
        {
            _actions.Add(action);
        }

        public IEnumerable<ISequentialAction> GetActions()
        {
            return _actions;
        }

        public string DeliveryObjectName { get; private set; }

    }

    interface IActionBundleFactory
    {
        ActionBundle Create(DeploymentPackageConfiguration config);
    }

    public class ActionBundleFactory : IActionBundleFactory
    {
        public ActionBundle Create(DeploymentPackageConfiguration config)
        {
            ActionBundle bundle = new ActionBundle(config.Name, config.TargetEnvironment);

            switch (config.DeploymentType)
            {
                case DeploymentTypeEnum.IISSite:
                    {
                        var siteName = config.Settings["IISSiteName"].Value;
                        var verifyHttpResponseUri = config.Settings["VerifyHttpResponseUri"].Value;
                        bundle.AddAction(new StopIISAction(siteName));
                        bundle.AddAction(new BackupCurrentReleaseDirectoryAction(config.Name, config.TargetPath, config.ReleaseBackupPath));
                        bundle.AddAction(new DeleteDirectoryContentAction(config.TargetPath, "Delete current release"));
                        bundle.AddAction(new ExtractDirectoryAction(config.ZipFilePath, config.TargetPath, "Extract new release"));
                        bundle.AddAction(new ApplyTargetEnvironmentConfigAction(config.DeploymentType, config.TargetEnvironment, config.TargetPath, config.EntryPointAssemblyFileName));
                        bundle.AddAction(new MoveDeliveryObjectToReleaseHistoryAction(config.ZipFilePath, config.ReleaseHistoryPath));
                        bundle.AddAction(new StartIISAction(siteName));
                        bundle.AddAction(new VerifyHttpResponse(verifyHttpResponseUri, 401));
                        bundle.AddAction(new AfterDeploymentMethodAction(config));
                        bundle.AddAction(new ReleaseNotesAction(config));
                        break;
                    }
                case DeploymentTypeEnum.Executable:
                    {
                        bundle.AddAction(new BackupCurrentReleaseDirectoryAction(config.Name, config.TargetPath, config.ReleaseBackupPath));
                        bundle.AddAction(new DeleteDirectoryContentAction(config.TargetPath, "Delete current release"));
                        bundle.AddAction(new ExtractDirectoryAction(config.ZipFilePath, config.TargetPath, "Extract new release"));
                        bundle.AddAction(new ApplyTargetEnvironmentConfigAction(config.DeploymentType, config.TargetEnvironment, config.TargetPath, config.EntryPointAssemblyFileName));
                        bundle.AddAction(new MoveDeliveryObjectToReleaseHistoryAction(config.ZipFilePath, config.ReleaseHistoryPath));
                        bundle.AddAction(new AfterDeploymentMethodAction(config));
                        bundle.AddAction(new ReleaseNotesAction(config));
                        break;
                    }
                case DeploymentTypeEnum.WindowsService:
                    {
                        var serviceName = config.Settings["WindowsServiceName"].Value;
                        bundle.AddAction(new StopWindowsServiceAction(serviceName));
                        bundle.AddAction(new BackupCurrentReleaseDirectoryAction(config.Name, config.TargetPath, config.ReleaseBackupPath));
                        bundle.AddAction(new DeleteDirectoryContentAction(config.TargetPath, "Delete current release"));
                        bundle.AddAction(new ExtractDirectoryAction(config.ZipFilePath, config.TargetPath, "Extract new release"));
                        bundle.AddAction(new ApplyTargetEnvironmentConfigAction(config.DeploymentType, config.TargetEnvironment, config.TargetPath, config.EntryPointAssemblyFileName));
                        bundle.AddAction(new MoveDeliveryObjectToReleaseHistoryAction(config.ZipFilePath, config.ReleaseHistoryPath));
                        bundle.AddAction(new StartWindowsServiceAction(serviceName));
                        bundle.AddAction(new AfterDeploymentMethodAction(config));
                        bundle.AddAction(new ReleaseNotesAction(config));
                        break;
                    }
                default:
                    throw new ConfigurationException("Unknonw ReleaseType.");
            }

            return bundle;
        }
    }

    public class ActionBundleFactoryFake : IActionBundleFactory
    {
        public ActionBundle Create(DeploymentPackageConfiguration config)
        {
            ActionBundle bundle = new ActionBundle(config.Name, config.TargetEnvironment);

            switch (config.DeploymentType)
            {
                case DeploymentTypeEnum.IISSite:
                    {
                        var siteName = config.Settings["IISSiteName"].Value;
                        var verifyHttpResponseUri = config.Settings["VerifyHttpResponseUri"].Value;
                        bundle.AddAction(new StopIISActionFake(siteName));
                        //bundle.AddAction(new BackupCurrentReleaseDirectoryAction(config.Name, config.TargetPath, config.ReleaseBackupPath));
                        bundle.AddAction(new DeleteDirectoryContentAction(config.TargetPath, "Delete current release"));
                        bundle.AddAction(new ExtractDirectoryAction(config.ZipFilePath, config.TargetPath, "Extract new release"));
                        bundle.AddAction(new ApplyTargetEnvironmentConfigAction(config.DeploymentType, config.TargetEnvironment, config.TargetPath, config.EntryPointAssemblyFileName));
                        //bundle.AddAction(new MoveDeliveryObjectToReleaseHistoryAction(config.ZipFilePath, config.ReleaseHistoryPath));
                        bundle.AddAction(new StartIISAction(siteName));
                        bundle.AddAction(new AfterDeploymentMethodAction(config));
                        //bundle.AddAction(new VerifyHttpResponse(verifyHttpResponseUri, 401));
                        bundle.AddAction(new ReleaseNotesAction(config));
                        break;
                    }
                case DeploymentTypeEnum.Executable:
                    {
                        bundle.AddAction(new BackupCurrentReleaseDirectoryAction(config.Name, config.TargetPath, config.ReleaseBackupPath));
                        bundle.AddAction(new DeleteDirectoryContentAction(config.TargetPath, "Delete current release"));
                        bundle.AddAction(new ExtractDirectoryAction(config.ZipFilePath, config.TargetPath, "Extract new release"));
                        bundle.AddAction(new ApplyTargetEnvironmentConfigAction(config.DeploymentType, config.TargetEnvironment, config.TargetPath, config.EntryPointAssemblyFileName));
                        /**/bundle.AddAction(new MoveDeliveryObjectToReleaseHistoryAction(config.ZipFilePath, config.ReleaseHistoryPath));
                        bundle.AddAction(new AfterDeploymentMethodAction(config));
                        bundle.AddAction(new ReleaseNotesAction(config));
                        break;
                    }
                case DeploymentTypeEnum.WindowsService:
                    {
                        var serviceName = config.Settings["WindowsServiceName"].Value;
                        bundle.AddAction(new StopWindowsServiceAction(serviceName));
                        /**/bundle.AddAction(new BackupCurrentReleaseDirectoryAction(config.Name, config.TargetPath, config.ReleaseBackupPath));
                        bundle.AddAction(new DeleteDirectoryContentAction(config.TargetPath, "Delete current release"));
                        bundle.AddAction(new ExtractDirectoryAction(config.ZipFilePath, config.TargetPath, "Extract new release"));
                        bundle.AddAction(new ApplyTargetEnvironmentConfigAction(config.DeploymentType, config.TargetEnvironment, config.TargetPath, config.EntryPointAssemblyFileName));
                        /**/bundle.AddAction(new MoveDeliveryObjectToReleaseHistoryAction(config.ZipFilePath, config.ReleaseHistoryPath));
                        bundle.AddAction(new StartWindowsServiceAction(serviceName));
                        bundle.AddAction(new AfterDeploymentMethodAction(config));
                        bundle.AddAction(new ReleaseNotesAction(config));
                        break;
                    }
                default:
                    throw new ConfigurationException("Unknown ReleaseType.");
            }

            return bundle;
        }
    }
}
