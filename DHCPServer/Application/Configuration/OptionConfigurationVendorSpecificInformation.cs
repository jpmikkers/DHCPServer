using GitHub.JPMikkers.DHCP;
using System;

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