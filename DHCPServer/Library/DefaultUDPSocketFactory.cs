using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GitHub.JPMikkers.DHCP
{
    public class DefaultUDPSocketFactory : IUDPSocketFactory
    {
        public IUDPSocket Create(IPEndPoint localEndPoint, int packetSize, bool dontFragment, short ttl)
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new UDPSocketLinux(localEndPoint, packetSize, dontFragment, ttl);
            }
            else
            {
                return new UDPSocketWindows(localEndPoint, packetSize, dontFragment, ttl);
            }
        }
    }
}
