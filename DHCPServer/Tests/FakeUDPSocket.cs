using GitHub.JPMikkers.DHCP;
using System.Net;
using System.Threading.Channels;
using Baksteen.Async;

namespace Tests;

public class FakeUDPSocket : IUDPSocket
{
    private Channel<(IPEndPoint targetEndPoint, ReadOnlyMemory<byte> msg)> _serverToClientChannel;
    private Channel<(IPEndPoint clientEndPoint, ReadOnlyMemory<byte> msg)> _clientToServerChannel;

    public required IPEndPoint LocalEndPoint { get; init; }

    public bool SendPending => false;

    public required int PacketSize { get; init; }
    public required bool DontFragment { get; init; }
    public required short Ttl { get; init; }

    public FakeUDPSocket()
    {
        _serverToClientChannel = Channel.CreateUnbounded<(IPEndPoint targetEndPoint, ReadOnlyMemory<byte> msg)>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = true,
            SingleReader = false,
            SingleWriter = false
        });

        _clientToServerChannel = Channel.CreateUnbounded<(IPEndPoint clientEndPoint, ReadOnlyMemory<byte> msg)>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = true,
            SingleReader = false,
            SingleWriter = false
        });
    }

    public async Task Send(IPEndPoint endPoint, ReadOnlyMemory<byte> msg, CancellationToken cancellationToken)
    {
        await _serverToClientChannel.Writer.WriteAsync((endPoint, msg),cancellationToken);
    }

    public void Dispose()
    {
    }

    public async Task ClientSend(IPEndPoint clientEndPoint, ArraySegment<byte> msg)
    {
        await _clientToServerChannel.Writer.WriteAsync((clientEndPoint, msg));
    }

    public async Task<(IPEndPoint targetEndPoint, ReadOnlyMemory<byte> msg)> ClientReceive()
    {
        return await _serverToClientChannel.Reader.ReadAsync();
    }

    public async Task<(IPEndPoint, ReadOnlyMemory<byte>)> Receive(CancellationToken cancellationToken)
    {
        return await _clientToServerChannel.Reader.ReadAsync(cancellationToken);
    }
}
