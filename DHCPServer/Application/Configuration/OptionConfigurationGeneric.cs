using GitHub.JPMikkers.DHCP;
using System;

namespace DHCPServerApp
{
    [Serializable()]
    public class OptionConfigurationGeneric : OptionConfiguration
    {
        public int Option;
        public string Data;

        public OptionConfigurationGeneric()
        {
        }

        protected override IDHCPOption ConstructDHCPOption()
        {
            return new DHCPOptionGeneric((TDHCPOption)Option, Utils.HexStringToBytes(Data));
        }
    }
}