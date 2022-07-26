using System;
using GitHub.JPMikkers.DHCP;

namespace DHCPServerApp
{
    [Serializable()]
    public class OptionConfigurationBootFileName : OptionConfiguration
    {
        public string Name;

        public OptionConfigurationBootFileName()
        {
            Name = "";
        }

        protected override IDHCPOption ConstructDHCPOption()
        {
            return new DHCPOptionBootFileName(Name);
        }
    }
}