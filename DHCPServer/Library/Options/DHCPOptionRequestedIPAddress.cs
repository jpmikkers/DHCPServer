using System.IO;
using System.Net;

namespace GitHub.JPMikkers.DHCP;

public class DHCPOptionRequestedIPAddress : DHCPOptionBase
{
    private IPAddress _IPAddress = IPAddress.None;

    #region IDHCPOption Members

    public IPAddress IPAddress
    {
        get
        {
            return _IPAddress;
        }
    }

    public override IDHCPOption FromStream(Stream s)
    {
        DHCPOptionRequestedIPAddress result = new DHCPOptionRequestedIPAddress();
        if(s.Length != 4) throw new IOException("Invalid DHCP option length");
        result._IPAddress = ParseHelper.ReadIPAddress(s);
        return result;
    }

    public override void ToStream(Stream s)
    {
        ParseHelper.WriteIPAddress(s, _IPAddress);
    }

    #endregion

    public DHCPOptionRequestedIPAddress()
        : base(TDHCPOption.RequestedIPAddress)
    {
    }

    public DHCPOptionRequestedIPAddress(IPAddress ipAddress)
        : base(TDHCPOption.RequestedIPAddress)
    {
        _IPAddress = ipAddress;
    }

    public override string ToString()
    {
        return $"Option(name=[{OptionType}],value=[{_IPAddress}])";
    }
}
