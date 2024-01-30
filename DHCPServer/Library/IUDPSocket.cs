using System;
using System.Net;

namespace GitHub.JPMikkers.DHCP
{
    public interface IUDPSocket : IDisposable
    {
        IPEndPoint LocalEndPoint { get; }
        bool SendPending { get; }

        void Send(IPEndPoint endPoint, ArraySegment<byte> msg);
    }
}