using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace Baksteen.DHCPServer.ConfigFile;

public enum OptionConfigMode
{
    Default,
    Force
}


[Serializable()]
[XmlInclude(typeof(OptionConfigurationTFTPServerName))]
[XmlInclude(typeof(OptionConfigurationBootFileName))]
[XmlInclude(typeof(OptionConfigurationVendorSpecificInformation))]
[XmlInclude(typeof(OptionConfigurationGeneric))]
[XmlInclude(typeof(OptionConfigurationVendorClassIdentifier))]
[XmlInclude(typeof(OptionConfigurationRouter))]
[XmlInclude(typeof(OptionConfigurationNetworkTimeProtocolServers))]
[XmlInclude(typeof(OptionConfigurationDomainNameServer))]
public abstract class OptionConfiguration
{
    public OptionConfigMode Mode;

    [OptionalField]
    public bool ZeroTerminatedStrings;

    public OptionConfiguration()
    {
        Mode = OptionConfigMode.Default;
    }
}
