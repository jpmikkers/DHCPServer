using GitHub.JPMikkers.DHCP;
using System;
using System.Linq;

namespace ManagedDHCPService;

[Serializable()]
public class OptionConfigurationNetworkTimeProtocolServers : OptionConfigurationAddresses
{
    public OptionConfigurationNetworkTimeProtocolServers()
    {
    }

    protected override IDHCPOption ConstructDHCPOption()
    {
        return new DHCPOptionNetworkTimeProtocolServers()
        {
            IPAddresses = Addresses
                .Where(x => x.Address != null)
                .Select(x => x.Address)
        };
    }
}