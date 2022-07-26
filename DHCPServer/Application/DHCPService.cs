using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceProcess;

namespace DHCPServerApp
{
    public partial class DHCPService : ServiceBase
    {
        private readonly EventLog _eventLog;
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

            foreach(DHCPServerConfiguration config in _configuration)
            {
                _servers.Add(new DHCPServerResurrector(config, _eventLog));
            }
        }

        protected override void OnStop()
        {
            foreach(DHCPServerResurrector server in _servers)
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
