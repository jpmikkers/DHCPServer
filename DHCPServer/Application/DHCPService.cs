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
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Configuration;
using System.Threading;
using GitHub.JPMikkers.DHCP;

namespace DHCPServerApp
{
    public partial class DHCPService : ServiceBase
    {
        private EventLog _eventLog;
        private DHCPServerConfigurationList _configuration;
        private List<DHCPServerResurrector> _servers;

        public DHCPService()
        {
            InitializeComponent();
            _eventLog = new EventLog(Program.CustomEventLog, ".", Program.CustomEventSource);
        }

        protected override void OnStart(string[] args)
        {
            _configuration = DHCPServerConfigurationList.Read(Program.GetConfigurationPath());
            _servers = new List<DHCPServerResurrector>();

            foreach (DHCPServerConfiguration config in _configuration)
            {
                _servers.Add(new DHCPServerResurrector(config, _eventLog));
            }
        }

        protected override void OnStop()
        {
            foreach (DHCPServerResurrector server in _servers)
            {
                server.Dispose();
            }
            _servers.Clear();
        }

        protected override void OnCustomCommand(int command)
        {
            base.OnCustomCommand(command);
        }
    }
}
