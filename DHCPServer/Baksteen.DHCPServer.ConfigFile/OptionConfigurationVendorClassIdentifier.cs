using System;
using System.IO;

namespace Baksteen.DHCPServer.ConfigFile;

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
}