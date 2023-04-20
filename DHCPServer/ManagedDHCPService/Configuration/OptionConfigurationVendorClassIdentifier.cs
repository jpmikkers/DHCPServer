using GitHub.JPMikkers.DHCP;
using System;
using System.IO;

namespace ManagedDHCPService;

[Serializable()]
public class OptionConfigurationVendorClassIdentifier : OptionConfiguration
{
    public string DataAsString;
    public string DataAsHex;

    public OptionConfigurationVendorClassIdentifier()
    {
        DataAsString = "";
        DataAsHex = "";
    }

    protected override IDHCPOption ConstructDHCPOption()
    {
        byte[] data;

        if(string.IsNullOrEmpty(DataAsString))
        {
            data = Utils.HexStringToBytes(DataAsHex);
        }
        else
        {
            MemoryStream m = new MemoryStream();
            ParseHelper.WriteString(m, DataAsString);
            m.Flush();
            data = m.ToArray();
        }

        return new DHCPOptionVendorClassIdentifier(data);
    }
}