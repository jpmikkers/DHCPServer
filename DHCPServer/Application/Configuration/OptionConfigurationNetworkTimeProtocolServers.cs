using System;
using GitHub.JPMikkers.DHCP;
using System.Linq;

namespace DHCPServerApp
{
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
}