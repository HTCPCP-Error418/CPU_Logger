using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace CpuLoggerService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        private readonly ServiceProcessInstaller processInstaller;
        private readonly ServiceInstaller serviceInstaller;

        public ProjectInstaller()
        {
            processInstaller = new ServiceProcessInstaller();
            serviceInstaller = new ServiceInstaller();

            // Run under LocalSystem
            processInstaller.Account = ServiceAccount.LocalSystem;

            // Service details
            serviceInstaller.ServiceName = "CpuLoggerService";
            serviceInstaller.DisplayName = "CPU Logger Service";
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }
    }
}
