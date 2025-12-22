using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Baksteen.DHCPServer.ConfigFile;

[Serializable()]
public abstract class OptionConfigurationAddresses : OptionConfiguration
{
    [XmlArrayItem("Address")]
    public List<XmlSerializableIPAddress> Addresses { get; set; }

    public OptionConfigurationAddresses()
    {
        Addresses = new List<XmlSerializableIPAddress>();
    }
}