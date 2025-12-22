using System;

namespace Baksteen.DHCPServer.ConfigFile;

[Serializable()]
public class OptionConfigurationVendorSpecificInformation : OptionConfiguration
{
    public string Information;

    public OptionConfigurationVendorSpecificInformation()
    {
        Information = "";
    }
}