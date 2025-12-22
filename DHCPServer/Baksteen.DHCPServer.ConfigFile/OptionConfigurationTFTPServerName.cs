using System;

namespace Baksteen.DHCPServer.ConfigFile;

[Serializable()]
public class OptionConfigurationTFTPServerName : OptionConfiguration
{
    public string Name;

    public OptionConfigurationTFTPServerName()
    {
        Name = "";
    }
}