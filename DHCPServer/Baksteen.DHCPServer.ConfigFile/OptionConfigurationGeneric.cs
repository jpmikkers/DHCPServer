using System;

namespace Baksteen.DHCPServer.ConfigFile;

[Serializable()]
public class OptionConfigurationGeneric : OptionConfiguration
{
    public int Option;
    public string Data = string.Empty;

    public OptionConfigurationGeneric()
    {
    }
}