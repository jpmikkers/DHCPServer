using System;
using GitHub.JPMikkers.DHCP;
using System.Linq;

namespace DHCPServerApp
{
    [Serializable()]
    public class OptionConfigurationDomainNameServer : OptionConfigurationAddresses
    {
        public OptionConfigurationDomainNameServer()
        {
        }

        protected override IDHCPOption ConstructDHCPOption()
        {
            return new DHCPOptionDomainNameServer()
            {
                IPAddresses = Addresses
                    .Where(x => x.Address != null)
                    .Select(x => x.Address)
            };
        }
    }
}