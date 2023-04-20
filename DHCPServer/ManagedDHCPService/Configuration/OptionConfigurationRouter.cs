using GitHub.JPMikkers.DHCP;
using System;
using System.Linq;

namespace ManagedDHCPService;

[Serializable()]
public class OptionConfigurationRouter : OptionConfigurationAddresses
{
    public OptionConfigurationRouter()
    {
    }

    protected override IDHCPOption ConstructDHCPOption()
    {
        return new DHCPOptionRouter()
        {
            IPAddresses = Addresses
                .Where(x => x.Address != null)
                .Select(x => x.Address)
        };
    }
}