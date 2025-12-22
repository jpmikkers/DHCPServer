using System;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;

namespace Baksteen.DHCPServer.ConfigFile;

[Serializable()]
[XmlRoot("DHCPServerConfigurationList")]
public class DHCPServerConfigurationList : List<DHCPServerConfiguration>
{
    private static readonly XmlSerializer s_serializer = new(typeof(DHCPServerConfigurationList));

    public static DHCPServerConfigurationList Read(string file)
    {
        DHCPServerConfigurationList result;

        if(File.Exists(file))
        {
            using(Stream s = File.OpenRead(file))
            {
                result = (s_serializer.Deserialize(s) as DHCPServerConfigurationList) ?? [];
            }
        }
        else
        {
            result = [];
        }

        return result;
    }

    public void Write(string file)
    {
        string? dirName = Path.GetDirectoryName(file);

        if(!string.IsNullOrEmpty(dirName) && !Directory.Exists(dirName))
        {
            Directory.CreateDirectory(dirName);
        }

        using(Stream s = File.Open(file, FileMode.Create))
        {
            s_serializer.Serialize(s, this);
        }
    }
}