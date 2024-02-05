using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.JPMikkers.DHCP
{

    [Serializable]
    public class UDPSocketException : Exception
    {
        public required bool IsFatal { get; init; }

        public UDPSocketException() 
        { 
        }

        public UDPSocketException(string message) : base(message)
        { 
        }

        public UDPSocketException(string message, Exception inner) : base(message, inner) 
        { 
        }
    }


    public interface IUDPSocket : IDisposable
    {
        IPEndPoint LocalEndPoint { get; }

        Task Send(IPEndPoint endPoint, ReadOnlyMemory<byte> msg, CancellationToken cancellationToken);
        Task<(IPEndPoint,ReadOnlyMemory<byte>)> Receive(CancellationToken cancellationToken);
    }
}