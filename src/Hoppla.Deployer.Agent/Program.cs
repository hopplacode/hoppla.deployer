using Hoppla.Deployer.Agent.Services;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using RazorEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Hoppla.Deployer.Agent
{
    class Program
    {
        static void Main(string[] args)
        {
            ApplicationConfiguration applicationConfiguration = new ApplicationConfiguration();
            SetupApplication(applicationConfiguration);

            ServiceLocator serviceLocator = new ServiceLocator(applicationConfiguration);
            IEmailService emailService = serviceLocator.GetService<IEmailService>();
            IActionBundleFactory bundleFactory = serviceLocator.GetService<IActionBundleFactory>();
            ILog _log = serviceLocator.GetService<ILog>();

            List<ActionBundleExecutionResult> actionBundleExecutionResults = new List<ActionBundleExecutionResult>();
            try
            {
                _log.Info("Deployment starting.");

                List<DeploymentPackageConfiguration> objectsToDeploy = new List<DeploymentPackageConfiguration>();
                foreach (var deliveryObjectConfig in applicationConfiguration.GetDeploymentPackageConfigs())
                {
                    if (File.Exists(deliveryObjectConfig.ZipFilePath))
                    {
                        objectsToDeploy.Add(deliveryObjectConfig);
                    }
                }

                _log.Info(string.Format("Deploying {0} objects.", objectsToDeploy.Count));

                foreach (var deliveryObjectConfig in objectsToDeploy)
                {
                    ActionBundle bundle = bundleFactory.Create(deliveryObjectConfig);
                    ActionBundleExecutor actionBundleExecutor = new ActionBundleExecutor(bundle);
                    actionBundleExecutionResults.Add(actionBundleExecutor.Execute());
                }

                if (actionBundleExecutionResults.Any())
                {
                    var emailTemplate = File.ReadAllText("EmailTemplate.cshtml");
                    var emailBody = Razor.Parse(emailTemplate, new EmailViewModel(actionBundleExecutionResults));
                    emailService.SendMail(applicationConfiguration.ReportEmailFromAdress, applicationConfiguration.ReportEmailRecipientAdress, "Deployment Report", emailBody);

                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
            finally
            {
                _log.Info("Deployment finished.");

                foreach (var actionBundleExecutionResult in actionBundleExecutionResults)
                {
                    _log.Info(string.Format("Results from bundle {0}:", actionBundleExecutionResult.DeliveryObjectName));
                    
                    foreach (var actionExecutionResult in actionBundleExecutionResult.ActionExecutionResults)
                    {
                        if (actionExecutionResult.Success)
                        {
                            // success
                            if (!string.IsNullOrEmpty(actionExecutionResult.DebugInformation))
                                Console.WriteLine(actionExecutionResult.DebugInformation);

                            _log.Info(string.Format("* {0}: Success. {1}", actionExecutionResult.ActionName, actionExecutionResult.Information));
                        }
                        else
                        {
                            //fail
                            string logmessage = String.Format("* {0} ", actionExecutionResult.ActionName);
                            Console.WriteLine(logmessage + " Exception: " + actionExecutionResult.Exception.Message);
                            _log.Error(logmessage, actionExecutionResult.Exception);
                        }
                    }
                }



                _log.Info(string.Format("Deployed {0} successfully, {1} failed.", actionBundleExecutionResults.Count(x => x.Success == true), actionBundleExecutionResults.Count(x => x.Success == false)));
                _log.Info("*************************************************************************************");
            }


            if (applicationConfiguration.UseFakes)
            {
                Console.WriteLine("Finished.");
                Console.ReadKey();
            }
        }

        private static void SetupApplication(ApplicationConfiguration applicationConfiguration)
        {
            if (!Directory.Exists(applicationConfiguration.WorkingDirectory))
            {
                Directory.CreateDirectory(applicationConfiguration.WorkingDirectory);
            }

            if (!Directory.Exists(applicationConfiguration.MonitoredDeliveryPath))
            {
                Directory.CreateDirectory(applicationConfiguration.MonitoredDeliveryPath);
            }

            if (!Directory.Exists(applicationConfiguration.ReleaseBackupPath))
            {
                Directory.CreateDirectory(applicationConfiguration.ReleaseBackupPath);
            }

            if (!Directory.Exists(applicationConfiguration.ReleaseHistoryPath))
            {
                Directory.CreateDirectory(applicationConfiguration.ReleaseHistoryPath);
            }
        }
    }

    public class ServiceLocator
    {
        private IDictionary<object, object> services;

        internal ServiceLocator(ApplicationConfiguration appConfig)
        {
            services = new Dictionary<object, object>();

            if (appConfig.UseFakes)
            {
                //this.services.Add(typeof(IEmailService), new EmailServiceFake(appConfig.SMTP));
                this.services.Add(typeof(IEmailService), new EmailService(appConfig.SMTP));
                this.services.Add(typeof(IActionBundleFactory), new ActionBundleFactoryFake());
            }
            else
            {
                this.services.Add(typeof(IEmailService), new EmailService(appConfig.SMTP));
                this.services.Add(typeof(IActionBundleFactory), new ActionBundleFactory());
            }
            Logger.Setup(appConfig.LogFilePath);
            this.services.Add(typeof(ILog), LogManager.GetLogger(typeof(Program)));
        }

        public T GetService<T>()
        {
            try
            {
                return (T)services[typeof(T)];
            }
            catch (KeyNotFoundException)
            {
                throw new ApplicationException("finns inte");
            }
        }
    }

    public class Logger
    {
        public static void Setup(string logfilePath)
        {
            Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository();

            PatternLayout patternLayout = new PatternLayout();
            patternLayout.ConversionPattern = "%-5p %d{yyyy-MM-dd HH:mm:ss} – %m%n";
            patternLayout.ActivateOptions();

            RollingFileAppender roller = new RollingFileAppender();
            roller.AppendToFile = true;
            roller.File = logfilePath;
            roller.Layout = patternLayout;
            roller.MaxSizeRollBackups = 5;
            roller.MaximumFileSize = "10MB";
            roller.RollingStyle = RollingFileAppender.RollingMode.Size;
            roller.StaticLogFileName = true;
            roller.ActivateOptions();
            hierarchy.Root.AddAppender(roller);

            hierarchy.Root.Level = Level.Info;
            hierarchy.Configured = true;
        }
    }
}
