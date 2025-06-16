using System;
using System.ServiceProcess;

namespace CpuLoggerService
{
    static class Program
    {
        static void Main()
        {
            ServiceBase[] servicesToRun;
            servicesToRun = new ServiceBase[]
            {
                new CpuLoggerService()
            };
            ServiceBase.Run(servicesToRun);
        }
    }
}
