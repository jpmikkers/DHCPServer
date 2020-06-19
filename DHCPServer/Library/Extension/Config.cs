using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Library
{
    public class Config
    {
        public const string CustomEventLog = "DHCPServerLog";
        public const string CustomEventSource = "DHCPServerSource";

        public static string GetMacTastePath()
        {
            string configurationPath = GetConfigurationPath();
            return Path.Combine(Path.GetDirectoryName(GetConfigurationPath()), "mactaste.cfg");
        }
        public static string GetConfigurationPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "JPMikkers\\DHCP Server\\Configuration.xml");
        }

        public static string GetClientInfoPath(string serverName, string serverAddress)
        {
            string configurationPath = GetConfigurationPath();
            return Path.Combine(Path.GetDirectoryName(GetConfigurationPath()), string.Format("{0}_{1}.xml", serverName, serverAddress.Replace('.', '_')));
        }

        public static void DeleteEventLog()
        {
            if (EventLog.SourceExists(Config.CustomEventSource))
            {
                EventLog.DeleteEventSource(Config.CustomEventSource);
            }

            if (EventLog.Exists(Config.CustomEventLog))
            {
                EventLog.Delete(Config.CustomEventLog);
            }
        }
        public static void InitializeEventLog()
        {
            if (!EventLog.SourceExists(Config.CustomEventSource))
            {
                EventLog.CreateEventSource(Config.CustomEventSource, Config.CustomEventLog);
            }
            // write something to the event log, or else the EventLog component in the UI
            // won't fire the updating events. I know, it sucks.
            EventLog tmp = new EventLog(Config.CustomEventLog, ".", Config.CustomEventSource);
            tmp.WriteEntry("Installation complete");
            //tmp.MaximumKilobytes = 16000;   // value MUST be a factor of 64
            //tmp.ModifyOverflowPolicy(OverflowAction.OverwriteAsNeeded, 7);
            tmp.Close();
        }
    }
}
