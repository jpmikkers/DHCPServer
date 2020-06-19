using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Reflection;
using System.Windows.Forms;
using System.Diagnostics;
using System.Security;
using System.Security.Principal;
using System.IO;
using CodePlex.JPMikkers.DHCP;

namespace DHCPServerApp
{
    static class Program
    {

        private const string Switch_Install = "/install";
        private const string Switch_Uninstall = "/uninstall";
        private const string Switch_Service = "/service";

       

     

        public static bool HasAdministrativeRight()
        {
            WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static bool RunElevated(string fileName,string args)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo();
            processInfo.Verb = "runas";
            processInfo.FileName = fileName;
            processInfo.Arguments = args;
            try
            {
                Process.Start(processInfo);
                return true;
            }
            catch (Exception)
            {
                //Do nothing. Probably the user canceled the UAC window
            }
            return false;
        }

        public static bool RunElevated(string args)
        {
            return RunElevated(System.Windows.Forms.Application.ExecutablePath, args);
        }

        private static void Install()
        {
            if (!HasAdministrativeRight())
            {
                RunElevated(Switch_Install);
                return;
            }
            else
            {
                System.Diagnostics.Trace.WriteLine("Installing DHCP service");

                try
                {
                    System.Configuration.Install.AssemblyInstaller Installer = new System.Configuration.Install.AssemblyInstaller(Assembly.GetExecutingAssembly(), new string[] { });
                    Installer.UseNewContext = true;
                    Installer.Install(null);
                    Installer.Commit(null);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(String.Format("Exception: {0}", ex));
                }
            }
        }

        private static void Uninstall()
        {
            if (!HasAdministrativeRight())
            {
                RunElevated(Switch_Uninstall);
                return;
            }
            else
            {
                System.Diagnostics.Trace.WriteLine("Uninstalling DHCP service");

                try
                {
                    System.Configuration.Install.AssemblyInstaller Installer = new System.Configuration.Install.AssemblyInstaller(Assembly.GetExecutingAssembly(), new string[] { });
                    Installer.UseNewContext = true;
                    Installer.Uninstall(null);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(String.Format("Exception: {0}", ex));
                }
            }
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0].ToLower() == Switch_Service)
            {
                ServiceBase.Run(new ServiceBase[] { new DHCPService() });
            }
            else
            {
                System.Windows.Forms.Application.EnableVisualStyles();
                System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

                if (args.Length == 0)
                {
                    ServiceController serviceController = ServiceController.GetServices().FirstOrDefault(x => x.ServiceName == "DHCPServer");

                    if (serviceController == null)
                    {
                        if (MessageBox.Show("Service has not been installed yet, install?", "DHCP Server", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            Install();
                        }
                    }
                    else
                    {
                        System.Windows.Forms.Application.Run(new FormMain(serviceController));
                    }
                }
                else
                {
                    switch (args[0].ToLower())
                    {
                        case Switch_Install:
                            Install();
                            break;

                        case Switch_Uninstall:
                            Uninstall();
                            break;
                    }
                }
            }
        }
    }
}
