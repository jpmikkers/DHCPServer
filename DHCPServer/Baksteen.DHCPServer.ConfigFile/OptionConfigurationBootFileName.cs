using System;

namespace Baksteen.DHCPServer.ConfigFile;

[Serializable()]
public class OptionConfigurationBootFileName : OptionConfiguration
{
    public string Name;

    public OptionConfigurationBootFileName()
    {
        Name = "";
    }
}