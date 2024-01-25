using GitHub.JPMikkers.DHCP;
using System;

namespace ManagedDHCPService;

[Serializable()]
public class OptionConfigurationGeneric : OptionConfiguration
{
    public int Option;
    public string Data = string.Empty;

    public OptionConfigurationGeneric()
    {
    }

    protected override IDHCPOption ConstructDHCPOption()
    {
        return new DHCPOptionGeneric((TDHCPOption)Option, Utils.HexStringToBytes(Data));
    }
}