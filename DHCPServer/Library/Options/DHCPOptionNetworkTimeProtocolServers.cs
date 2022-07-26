namespace GitHub.JPMikkers.DHCP
{
    public class DHCPOptionNetworkTimeProtocolServers : DHCPOptionServerListBase
    {
        public override DHCPOptionServerListBase Create()
        {
            return new DHCPOptionNetworkTimeProtocolServers();
        }

        public DHCPOptionNetworkTimeProtocolServers() : base(TDHCPOption.NetworkTimeProtocolServers)
        {
        }
    }
}
