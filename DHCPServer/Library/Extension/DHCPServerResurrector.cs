/*

Copyright (c) 2010 Jean-Paul Mikkers

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using CodePlex.JPMikkers.DHCP;
using Library;

namespace DHCPServerApp
{
    public class DHCPServerResurrector : IDisposable
    {
        private const int RetryTime = 30000;
        private readonly object m_Lock;
        private bool m_Disposed;
        private DHCPServerConfiguration m_Config;
        private EventLog m_EventLog;

        private DHCPServer m_Server;
        private Timer m_RetryTimer;

        public DHCPServerResurrector(DHCPServerConfiguration config, EventLog eventLog)
        {
            m_Lock = new object();
            m_Disposed = false;
            m_Config = config;
            m_EventLog = eventLog;
            m_RetryTimer = new Timer(new TimerCallback(Resurrect));
            Resurrect(null);
        }

        ~DHCPServerResurrector()
        {
            try
            {
                Dispose(false);
            }
            catch
            {
                // never let any exception escape the finalizer, or else your process will be killed.
            }
        }

        private void Resurrect(object state)
        {
            lock (m_Lock)
            {
                if (!m_Disposed)
                {
                    try
                    {
                        m_Server = DHCPServer.FromConfig(m_Config);

                        m_Server.OnStatusChange += server_OnStatusChange;
                        m_Server.OnTrace += server_OnTrace;
                        m_Server.Start();
                    }
                    catch (Exception)
                    {
                        CleanupAndRetry();
                    }
                }
            }
        }


        private void Log(EventLogEntryType entryType, string msg)
        {
            m_EventLog.WriteEntry(string.Format("{0} : {1}",m_Config.Name,msg),entryType);
        }

        private void server_OnTrace(object sender, DHCPTraceEventArgs e)
        {
            Log(EventLogEntryType.Information,e.Message);
        }

        private void server_OnStatusChange(object sender, DHCPStopEventArgs e)
        {
            DHCPServer server = (DHCPServer)sender;

            if (server.Active)
            {
                //Log(EventLogEntryType.Information, string.Format("{0} transfers in progress", server.ActiveTransfers));
            }
            else
            {
                if (e.Reason != null)
                {
                    Log(EventLogEntryType.Error, string.Format("Stopped, reason: {0}", e.Reason));
                }
                CleanupAndRetry();
            }
        }

        private void CleanupAndRetry()
        {
            lock (m_Lock)
            {
                if (!m_Disposed)
                {
                    // stop server
                    if (m_Server != null)
                    {
                        m_Server.OnStatusChange -= server_OnStatusChange;
                        m_Server.OnTrace -= server_OnTrace;
                        m_Server.Dispose();
                        m_Server = null;
                    }
                    // initiate retry timer
                    m_RetryTimer.Change(RetryTime, Timeout.Infinite);
                }
            }
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (m_Lock)
                {
                    if (!m_Disposed)
                    {
                        m_Disposed = true;

                        m_RetryTimer.Change(Timeout.Infinite, Timeout.Infinite);
                        m_RetryTimer.Dispose();

                        if (m_Server != null)
                        {
                            m_Server.OnStatusChange -= server_OnStatusChange;
                            m_Server.Dispose();
                            m_Server.OnTrace -= server_OnTrace;
                            m_Server = null;
                        }
                    }
                }
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        #endregion
    }
}
