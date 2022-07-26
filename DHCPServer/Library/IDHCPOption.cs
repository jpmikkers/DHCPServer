using System.IO;

namespace GitHub.JPMikkers.DHCP
{
    public interface IDHCPOption
    {
        bool ZeroTerminatedStrings { get; set; }
        TDHCPOption OptionType { get; }
        IDHCPOption FromStream(Stream s);
        void ToStream(Stream s);
    }
}
