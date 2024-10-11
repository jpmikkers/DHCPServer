namespace GitHub.JPMikkers.DHCP;

public class DHCPOptionDomainNameServer : DHCPOptionServerListBase
{
    public override DHCPOptionServerListBase Create()
    {
        return new DHCPOptionDomainNameServer();
    }

    public DHCPOptionDomainNameServer() : base(TDHCPOption.DomainNameServer)
    {
    }
}
