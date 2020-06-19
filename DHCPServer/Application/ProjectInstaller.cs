using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Diagnostics;
using Library;

namespace DHCPServerApp
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        protected override void OnBeforeInstall(IDictionary savedState)
        {
            System.Diagnostics.Trace.WriteLine("Creating DHCP service log");

            try
            {
                //EventLogPermission eventLogPermission = new EventLogPermission(EventLogPermissionAccess.Administer, ".");
                //eventLogPermission.PermitOnly();

                Config.InitializeEventLog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(String.Format("Exception: {0}", ex));
            }

            Context.Parameters["assemblypath"] = string.Format("\"{0}\" {1}", Context.Parameters["assemblypath"], "/service");
            base.OnBeforeInstall(savedState);
        }

        protected override void OnBeforeUninstall(IDictionary savedState)
        {
            Context.Parameters["assemblypath"] = string.Format("\"{0}\" {1}", Context.Parameters["assemblypath"], "/service");
            base.OnBeforeUninstall(savedState);
        }

        protected override void OnAfterUninstall(IDictionary savedState)
        {
            System.Diagnostics.Trace.WriteLine("Removing DHCP service log");

            try
            {
                Config.DeleteEventLog();
            }
            catch(Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(String.Format("Exception: {0}", ex));
            }

            base.OnAfterUninstall(savedState);
        }
    }
}
