using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GitHub.JPMikkers.DHCP;

public class DefaultUDPSocketFactory : IUDPSocketFactory
{
    private readonly ILogger _logger;

    public DefaultUDPSocketFactory(ILogger? logger)
    {
        // see https://blog.rsuter.com/logging-with-ilogger-recommendations-and-best-practices/
        _logger = logger ?? NullLogger.Instance; 
    }

    public IUDPSocket Create(IPEndPoint localEndPoint, int packetSize, bool dontFragment, short ttl)
    {
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            _logger.LogInformation("creating UDPSocket of type {SocketImpl}", nameof(UDPSocketLinux));
            return new UDPSocketLinux(localEndPoint, packetSize, dontFragment, ttl);
        }
        else
        {
            _logger.LogInformation("creating UDPSocket of type {SocketImpl}", nameof(UDPSocketWindows));
            return new UDPSocketWindows(localEndPoint, packetSize, dontFragment, ttl);
        }
    }
}
