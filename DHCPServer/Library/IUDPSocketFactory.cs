using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static GitHub.JPMikkers.DHCP.UDPSocket;

namespace GitHub.JPMikkers.DHCP
{
    public interface IUDPSocketFactory
    {
        IUDPSocket Create(IPEndPoint localEndPoint, int packetSize, bool dontFragment, short ttl, OnReceiveDelegate onReceive, OnStopDelegate onStop);
    }
}
