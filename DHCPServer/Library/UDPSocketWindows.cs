using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.JPMikkers.DHCP;

public class UDPSocketWindows : IUDPSocket
{
    // See: http://stackoverflow.com/questions/5199026/c-sharp-async-udp-listener-socketexception

    const uint IOC_IN = 0x80000000;
    const uint IOC_VENDOR = 0x18000000;
    const uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;

    private bool _disposed;                            // true => object is disposed

    private readonly bool _IPv6;                                // true => it's an IPv6 connection
    private readonly Socket _socket;                            // The active socket
    private readonly int _maxPacketSize;                        // size of packets we'll try to receive

    private readonly IPEndPoint _localEndPoint;

    public IPEndPoint LocalEndPoint
    {
        get
        {
            return _localEndPoint;
        }
    }

    public UDPSocketWindows(IPEndPoint localEndPoint, int maxPacketSize, bool dontFragment, short ttl)
    {
        _maxPacketSize = maxPacketSize;
        _disposed = false;

        _IPv6 = (localEndPoint.AddressFamily == AddressFamily.InterNetworkV6);
        _socket = new Socket(localEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        _socket.EnableBroadcast = true;
        _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
        _socket.SendBufferSize = 65536;
        _socket.ReceiveBufferSize = 65536;
        if(!_IPv6) _socket.DontFragment = dontFragment;
        if(ttl >= 0)
        {
            _socket.Ttl = ttl;
        }
        _socket.Bind(localEndPoint);
        _localEndPoint = (_socket.LocalEndPoint as IPEndPoint) ?? localEndPoint;

        try
        {
            _socket.IOControl((IOControlCode)SIO_UDP_CONNRESET, [0, 0, 0, 0], null);
        }
        catch(PlatformNotSupportedException)
        {
        }
    }

    public async Task<(IPEndPoint, ReadOnlyMemory<byte>)> ReceiveAsync(CancellationToken cancellationToken)
    {
        try
        {
            var mem = new Memory<byte>(new byte[_maxPacketSize]);
            var result = await _socket.ReceiveFromAsync(mem, new IPEndPoint(IPAddress.Any, 0), cancellationToken);

            if(result.RemoteEndPoint is IPEndPoint endpoint)
            {
                return (endpoint, mem[..result.ReceivedBytes]);
            }

            throw new InvalidCastException("unexpected endpoint type");
        }
        catch(OperationCanceledException)
        {
            throw;
        }
        catch(SocketException ex) when(ex.SocketErrorCode is SocketError.MessageSize or SocketError.ConnectionReset)
        {
            // MessageSize is reported when someone tried to send a message bigger than _maxPacketSize. Discard it, and start receiving the next packet.
            // ConnectionReset is reported when the remote port wasn't listening. Since we're using UDP messaging we don't care about this -> continue receiving.
            throw new UDPSocketException($"{nameof(ReceiveAsync)} error: {ex.Message}", ex) { IsFatal = false };
        }
        catch(Exception ex)
        {
            // everything else is fatal
            throw new UDPSocketException($"{nameof(ReceiveAsync)} error: {ex.Message}", ex) { IsFatal = true };
        }
    }

    /// <summary>
    /// Sends a packet of bytes to the specified EndPoint using an UDP datagram.
    /// </summary>
    /// <param name="endPoint">Target for the data</param>
    /// <param name="msg">Data to send</param>
    /// <param name="cancellationToken">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    public async Task SendAsync(IPEndPoint endPoint, ReadOnlyMemory<byte> msg, CancellationToken cancellationToken)
    {
        try
        {
            await _socket.SendToAsync(msg, endPoint, cancellationToken);
        }
        catch(OperationCanceledException)
        {
            throw;
        }
        catch(Exception ex)
        {
            throw new UDPSocketException(nameof(SendAsync), ex) { IsFatal = true };
        }
    }

    ~UDPSocketWindows()
    {
        try
        {
            Dispose(false);
        }
        catch
        {
            // never let any exception escape the finalizer, or else your process will be killed.
        }
    }

    /// <summary>
    /// Implements <see cref="IDisposable.Dispose"/>
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if(disposing)
        {
            if(!_disposed)
            {
                _disposed = true;

                try
                {
                    _socket.Shutdown(SocketShutdown.Both);
                    _socket.Close();
                }
                catch(Exception)
                {
                    // socket tends to complain a lot during close. just eat those exceptions.
                }
            }
        }
    }
}
