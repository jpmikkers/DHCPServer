using GitHub.JPMikkers.DHCP;
using System.Net;

namespace Tests;

public class FakeUDPSocketFactory : IUDPSocketFactory
{
    public FakeUDPSocket? UdpSocket { get; set; }

    public IUDPSocket Create(IPEndPoint localEndPoint, int packetSize, bool dontFragment, short ttl)
    {
        UdpSocket = new FakeUDPSocket() 
        { 
            LocalEndPoint = localEndPoint,
            PacketSize = packetSize,
            DontFragment = dontFragment, 
            Ttl = ttl, 
        };
        return UdpSocket;
    }
}
