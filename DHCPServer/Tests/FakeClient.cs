using GitHub.JPMikkers.DHCP;
using System.Net;

namespace Tests;

public class FakeClient
{
    private readonly FakeUDPSocket _socket;
    private readonly int _minimumPacketSize;

    public FakeClient(FakeUDPSocket socket, int minimumPacketSize)
    {
        _socket = socket;
        _minimumPacketSize = minimumPacketSize;
    }

    private async Task SendClientMessage(IPEndPoint clientEndPoint, DHCPMessage message)
    {
        var ms = new MemoryStream();
        message.ToStream(ms, _minimumPacketSize);
        var raw = new ArraySegment<byte>(ms.ToArray());
        await _socket.ClientSend(clientEndPoint, raw);
    }

    public async Task SendBroadcast(DHCPMessage message)
    {
        Assert.AreEqual(DHCPMessage.TOpcode.BootRequest, message.Opcode);
        Assert.AreEqual(DHCPMessage.THardwareType.Ethernet, message.HardwareType);
        Assert.AreEqual(0, message.Hops);

        await SendClientMessage(new IPEndPoint(IPAddress.Broadcast, 68), message);
    }

    public async Task<DHCPMessage> ReceiveBroadcast()
    {
        var msg = await _socket.ClientReceive();

        Assert.AreEqual(msg.targetEndPoint.Address, IPAddress.Broadcast);
        Assert.AreEqual(msg.targetEndPoint.Port, 68);
        Assert.IsNotNull(msg.msg);

        var response = DHCPMessage.FromStream(new MemoryStream(msg.msg.Array!, msg.msg.Offset, msg.msg.Count, false, false));

        Assert.AreEqual(0, response.Hops);
        Assert.AreEqual(DHCPMessage.TOpcode.BootReply, response.Opcode);
        Assert.AreEqual(DHCPMessage.THardwareType.Ethernet, response.HardwareType);

        return response;
    }
}
