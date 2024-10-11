using System.IO;
using System.Net;

namespace GitHub.JPMikkers.DHCP;

public class DHCPOptionSubnetMask : DHCPOptionBase
{
    private IPAddress _subnetMask = IPAddress.None;

    #region IDHCPOption Members

    public IPAddress SubnetMask
    {
        get
        {
            return _subnetMask;
        }
    }

    public override IDHCPOption FromStream(Stream s)
    {
        DHCPOptionSubnetMask result = new DHCPOptionSubnetMask();
        if(s.Length != 4) throw new IOException("Invalid DHCP option length");
        result._subnetMask = ParseHelper.ReadIPAddress(s);
        return result;
    }

    public override void ToStream(Stream s)
    {
        ParseHelper.WriteIPAddress(s, _subnetMask);
    }

    #endregion

    public DHCPOptionSubnetMask()
        : base(TDHCPOption.SubnetMask)
    {
    }

    public DHCPOptionSubnetMask(IPAddress subnetMask)
        : base(TDHCPOption.SubnetMask)
    {
        _subnetMask = subnetMask;
    }

    public override string ToString()
    {
        return $"Option(name=[{OptionType}],value=[{_subnetMask}])";
    }
}
