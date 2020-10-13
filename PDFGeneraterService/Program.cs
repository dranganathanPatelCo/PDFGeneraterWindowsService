using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace PDFGeneraterService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static System.Timers.Timer aTimer;
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new PDFGeneraterService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
