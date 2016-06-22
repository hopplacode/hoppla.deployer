using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hoppla.Deployer.Agent
{
    public class ApplicationConfiguration
    {
        public ApplicationConfiguration()
        {
            WorkingDirectory = ConfigurationManager.AppSettings["WorkingDirectory"];
            UseFakes = bool.Parse(ConfigurationManager.AppSettings["UseFakes"]);
            LogFilePath = ConfigurationManager.AppSettings["LogFilePath"];
            ReportEmailRecipientAdress = ConfigurationManager.AppSettings["ReportEmailRecipientAdress"];
            ReportEmailFromAdress = ConfigurationManager.AppSettings["ReportEmailFromAdress"];

            // Global configs
            ReleaseHistoryPath = string.Format("{0}\\{1}", WorkingDirectory, "ReleaseHistory");
            ReleaseBackupPath = string.Format("{0}\\{1}", WorkingDirectory, "ReleaseBackup");
            MonitoredDeliveryPath = string.Format("{0}\\{1}", WorkingDirectory, "MonitoredDelivery");

            if (ConfigurationManager.AppSettings["SMTP"] != null)
                SMTP = ConfigurationManager.AppSettings["SMTP"];
            else
                throw new ConfigurationException(string.Format("Configuration error for key SMTP. Specified in App.config"));

        }

        public List<DeploymentPackageConfiguration> GetDeploymentPackageConfigs()
        {
            //scan for ReleaseObject config files
            var filesInCurrentDirectory = Directory.GetFiles(Directory.GetCurrentDirectory() + "/DeploymentPackageConfigs");
            var configFiles = filesInCurrentDirectory.Where(x => x.ToString().EndsWith(".config"));


            var packagesInMonitoredPath = Directory.GetFiles(MonitoredDeliveryPath);

            var packageConfigs = new List<DeploymentPackageConfiguration>();
            //read each config file
            foreach (var configFileFullPath in configFiles)
            {
                var deploymentPackageConfigFileNameWithoutFileExtention = Path.GetFileNameWithoutExtension(configFileFullPath);
                Configuration packageConfiguration;
                ExeConfigurationFileMap configFile = new ExeConfigurationFileMap();
                configFile.ExeConfigFilename = configFileFullPath;
                packageConfiguration = ConfigurationManager.OpenMappedExeConfiguration(configFile, ConfigurationUserLevel.None);

                var deploymentPackageFilePath = packagesInMonitoredPath.FirstOrDefault(x => x.Contains("Release." + deploymentPackageConfigFileNameWithoutFileExtention));

                if (deploymentPackageFilePath != null)
                {
                    var packageConfig = new DeploymentPackageConfiguration(packageConfiguration.AppSettings.Settings, ReleaseBackupPath, deploymentPackageFilePath, ReleaseHistoryPath);

                    packageConfigs.Add(packageConfig);
                }
            }
            return packageConfigs;
        }

        public string SMTP { get; private set; }
        public string WorkingDirectory { get; private set; }
        public string ReleaseHistoryPath { get; private set; }
        public string ReleaseBackupPath { get; private set; }
        public string MonitoredDeliveryPath { get; private set; }
        public bool UseFakes { get; set; }
        public string LogFilePath { get; set; }
        public string ReportEmailRecipientAdress { get; set; }
        public string ReportEmailFromAdress { get; set; }
    }

    public class DeploymentPackageConfiguration
    {
        public DeploymentPackageConfiguration(KeyValueConfigurationCollection settings, string releaseBackupPath, string deploymentPackageFilePath, string releaseHistoryPath)
        {
            ReleaseBackupPath = releaseBackupPath;
            Settings = settings;
            ZipFilePath = deploymentPackageFilePath;
            ReleaseHistoryPath = releaseHistoryPath;

            DeploymentTypeEnum typeEnum;
            if (settings["DeploymentType"] != null && Enum.TryParse(settings["DeploymentType"].Value, out typeEnum))
                DeploymentType = typeEnum;
            else
                throw new ConfigurationException(string.Format("Configuration error for key DeploymentPackageType. Specified in {0}.config", Name));

            TargetEnvironmentEnum targetEnvironment;
            if (settings["TargetEnvironment"] != null && Enum.TryParse(settings["TargetEnvironment"].Value, out targetEnvironment))
                TargetEnvironment = targetEnvironment;
            else
                throw new ConfigurationException(string.Format("Configuration error for key TargetEnvironment. Specified in {0}.config", Name));

            if (settings["TargetPath"] != null)
                TargetPath = settings["TargetPath"].Value;
            else
                throw new ConfigurationException(string.Format("Configuration error for key TargetPath. Specified in {0}.config", Name));

            try
            {
                Name = Path.GetFileNameWithoutExtension(ZipFilePath).Split('.')[1];

                var tempEntryPointAssemblyFileName = Name.Replace("_", ".");
                if (DeploymentType == DeploymentTypeEnum.IISSite)
                    tempEntryPointAssemblyFileName += ".dll";
                else
                    tempEntryPointAssemblyFileName += ".exe";
                EntryPointAssemblyFileName = tempEntryPointAssemblyFileName;

                var datePart = Path.GetFileNameWithoutExtension(ZipFilePath).Split('.')[2];
                try
                {
                    Date = DateTime.ParseExact(datePart, "yyyyMMdd", null);
                }
                catch
                {
                    throw new ConfigurationException("Could not parse date part of deploymentpackage.");
                }

                if (string.IsNullOrEmpty(Name))
                {
                    throw new ConfigurationException("Could not parse name part of deploymentpackage.");
                }
            }
            catch 
            {
                throw new ConfigurationException("Could not parse filename of deploymentpackage.");
            }
            

        }
        
        public string ReleaseBackupPath { get; private set; }
        public string ReleaseHistoryPath { get; private set; }
        public string Name { get; private set; }
        public KeyValueConfigurationCollection Settings { get; private set; }
        public DeploymentTypeEnum DeploymentType { get; private set; }
        public TargetEnvironmentEnum TargetEnvironment { get; private set; }
        public string TargetPath { get; private set; }
        public string ZipFilePath { get; private set; }
        public string EntryPointAssemblyFileName { get; private set; }
        public DateTime Date { get; private set; }
    }
}
