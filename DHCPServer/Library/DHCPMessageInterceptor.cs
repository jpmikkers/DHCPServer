namespace GitHub.JPMikkers.DHCP
{
    public interface IDHCPMessageInterceptor
    {
        void Apply(DHCPMessage sourceMsg, DHCPMessage targetMsg);
    }
}