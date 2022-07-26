using System;
using GitHub.JPMikkers.DHCP;

namespace DHCPServerApp
{
    [Serializable()]
    public class OptionConfigurationVendorSpecificInformation : OptionConfiguration
    {
        public string Information;

        public OptionConfigurationVendorSpecificInformation()
        {
            Information = "";
        }

        protected override IDHCPOption ConstructDHCPOption()
        {
            return new DHCPOptionVendorSpecificInformation(Information);
        }
    }
}