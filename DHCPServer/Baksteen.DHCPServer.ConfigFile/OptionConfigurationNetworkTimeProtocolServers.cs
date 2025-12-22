using System;
using System.Linq;

namespace Baksteen.DHCPServer.ConfigFile;

[Serializable()]
public class OptionConfigurationNetworkTimeProtocolServers : OptionConfigurationAddresses
{
    public OptionConfigurationNetworkTimeProtocolServers()
    {
    }
}