using System;
using GitHub.JPMikkers.DHCP;

namespace DHCPServerApp
{
    [Serializable()]
    public class OptionConfigurationTFTPServerName : OptionConfiguration
    {
        public string Name;

        public OptionConfigurationTFTPServerName()
        {
            Name = "";
        }

        protected override IDHCPOption ConstructDHCPOption()
        {
            return new DHCPOptionTFTPServerName(Name);
        }
    }
}