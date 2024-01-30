using GitHub.JPMikkers.DHCP;
using System.Net;
using static GitHub.JPMikkers.DHCP.UDPSocket;
using System.Threading.Channels;
using Baksteen.Async;

namespace Tests;

public class FakeUDPSocket : IUDPSocket
{
    private Channel<(IPEndPoint targetEndPoint, ArraySegment<byte> msg)> _serverToClientChannel;
    private TaskQueue _clientToServerChannel = new();

    public required IPEndPoint LocalEndPoint { get; init; }

    public bool SendPending => false;

    public required int PacketSize { get; init; }
    public required bool DontFragment { get; init; }
    public required OnReceiveDelegate OnReceive { get; init; }
    public required OnStopDelegate OnStop { get; init; }
    public required short Ttl { get; init; }

    public FakeUDPSocket()
    {
        _serverToClientChannel = Channel.CreateUnbounded<(IPEndPoint targetEndPoint, ArraySegment<byte> msg)>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = true,
            SingleReader = false,
            SingleWriter = false
        });
    }

    public void Send(IPEndPoint endPoint, ArraySegment<byte> msg)
    {
        Task.Run(async () => await _serverToClientChannel.Writer.WriteAsync((endPoint, msg))).Wait();
    }

    public void Dispose()
    {
    }

    public async Task ClientSend(IPEndPoint clientEndPoint, ArraySegment<byte> msg)
    {
        await _clientToServerChannel.Enqueue(() => {
            this.OnReceive?.Invoke(this, clientEndPoint, msg);
        });
    }

    public async Task<(IPEndPoint targetEndPoint, ArraySegment<byte> msg)> ClientReceive()
    {
        return await _serverToClientChannel.Reader.ReadAsync();
    }
}
