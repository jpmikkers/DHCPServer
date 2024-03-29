﻿using GitHub.JPMikkers.DHCP;
using System;

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