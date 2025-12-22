using System;
using System.Linq;

namespace Baksteen.DHCPServer.ConfigFile;

[Serializable()]
public class OptionConfigurationDomainNameServer : OptionConfigurationAddresses
{
    public OptionConfigurationDomainNameServer()
    {
    }
}