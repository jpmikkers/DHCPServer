namespace GitHub.JPMikkers.DHCP;

public class DHCPOptionRouter : DHCPOptionServerListBase
{
    public override DHCPOptionServerListBase Create()
    {
        return new DHCPOptionRouter();
    }

    public DHCPOptionRouter() : base(TDHCPOption.Router)
    {
    }
}
