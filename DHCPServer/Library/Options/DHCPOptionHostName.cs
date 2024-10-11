using System.IO;

namespace GitHub.JPMikkers.DHCP;

public class DHCPOptionHostName : DHCPOptionBase
{
    private string _hostName = string.Empty;

    #region IDHCPOption Members

    public string HostName
    {
        get
        {
            return _hostName;
        }
    }

    public override IDHCPOption FromStream(Stream s)
    {
        DHCPOptionHostName result = new DHCPOptionHostName();
        result._hostName = ParseHelper.ReadString(s);
        return result;
    }

    public override void ToStream(Stream s)
    {
        ParseHelper.WriteString(s, ZeroTerminatedStrings, _hostName);
    }

    #endregion

    public DHCPOptionHostName()
        : base(TDHCPOption.HostName)
    {
    }

    public DHCPOptionHostName(string hostName)
        : base(TDHCPOption.HostName)
    {
        _hostName = hostName;
    }

    public override string ToString()
    {
        return $"Option(name=[{OptionType}],value=[{_hostName}])";
    }
}
