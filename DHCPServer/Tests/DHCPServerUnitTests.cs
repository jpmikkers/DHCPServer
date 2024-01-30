using GitHub.JPMikkers.DHCP;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using System.Net;
using System.Net.Sockets;

namespace Tests;

[TestClass]
public class DHCPServerUnitTests
{
    [TestMethod]
    [Timeout(10000)]
    public async Task VerifyDiscoverOfferRequestAck()
    {
        var clientInfoPath = Path.Combine(Path.GetTempPath(), "dhcptest", "dhcptest.xml");
        if(File.Exists(clientInfoPath)) { File.Delete(clientInfoPath); }

        using var logFactory = LoggerFactory.Create(builder => builder.AddProvider(new DebugLoggerProvider()));
        var logger = logFactory.CreateLogger<DHCPServer>();
        var udpSocketFactory = new FakeUDPSocketFactory();
        using var dhcpServer = new DHCPServer(logger, clientInfoPath, udpSocketFactory);

        dhcpServer.EndPoint = new IPEndPoint(IPAddress.Parse("192.168.1.10"), 67);
        dhcpServer.SubnetMask = IPAddress.Parse("255.255.255.0");
        dhcpServer.PoolStart = IPAddress.Parse("192.168.1.20");
        dhcpServer.PoolEnd = IPAddress.Parse("192.168.1.30");

        byte[] clientHardwareAddress = [1, 2, 3, 4, 5];
        IPAddress expectedClientIPAddress = IPAddress.Parse("192.168.1.20");

        Assert.IsNull(udpSocketFactory.UdpSocket);
        dhcpServer.Start();
        Assert.IsNotNull(udpSocketFactory.UdpSocket);
        var socket = udpSocketFactory.UdpSocket!;

        Assert.AreEqual(dhcpServer.EndPoint, socket.LocalEndPoint);
        Assert.AreEqual(true, socket.DontFragment);
        Assert.AreEqual(0, dhcpServer.Clients.Count);   // initially we shouldn't have registered clients

        var client = new FakeClient(socket, dhcpServer.MinimumPacketSize);

        var mDiscover = new DHCPMessage()
        {
            Opcode = DHCPMessage.TOpcode.BootRequest,
            MessageType = TDHCPMessageType.DISCOVER,
            HardwareType = DHCPMessage.THardwareType.Ethernet,
            ClientHardwareAddress = clientHardwareAddress,
            NextServerIPAddress = IPAddress.Any,
            ClientIPAddress = IPAddress.Any,
            RelayAgentIPAddress = IPAddress.Any,
            YourIPAddress = IPAddress.Any,
            BroadCast = true,
            Hops = 0,
            XID = (uint)Random.Shared.Next(),
            Secs = 0,
            BootFileName = string.Empty,
            ServerHostName = string.Empty,
        };

        Console.WriteLine($"**** {nameof(mDiscover)}: c->s **** \r\n{mDiscover}\r\n");
        await client.SendBroadcast(mDiscover);
        var mOffer = await client.ReceiveBroadcast();
        Console.WriteLine($"**** {nameof(mOffer)}: s->c **** \r\n{mOffer}\r\n");

        Assert.AreEqual(mDiscover.XID, mOffer.XID);
        Assert.AreEqual(TDHCPMessageType.OFFER, mOffer.MessageType);
        Assert.AreEqual(0, mOffer.Hops);
        Assert.AreEqual(expectedClientIPAddress, mOffer.YourIPAddress);
        Assert.AreEqual(IPAddress.Any, mOffer.ClientIPAddress);
        Assert.AreEqual(IPAddress.Any, mOffer.RelayAgentIPAddress);
        Assert.AreEqual(IPAddress.Any, mOffer.NextServerIPAddress);
        CollectionAssert.AreEqual(mDiscover.ClientHardwareAddress, mOffer.ClientHardwareAddress);
        Assert.IsTrue(string.IsNullOrEmpty(mOffer.ServerHostName));
        Assert.IsTrue(string.IsNullOrEmpty(mOffer.BootFileName));

        var optServerIdentifier = AssertGetOption<DHCPOptionServerIdentifier>(mOffer);
        Assert.AreEqual(dhcpServer.EndPoint.Address, optServerIdentifier.IPAddress);

        var optIPAddressLeaseTime = AssertGetOption<DHCPOptionIPAddressLeaseTime>(mOffer);
        Assert.AreEqual(dhcpServer.LeaseTime, optIPAddressLeaseTime.LeaseTime);

        var mRequest = new DHCPMessage()
        {
            Opcode = DHCPMessage.TOpcode.BootRequest,
            MessageType = TDHCPMessageType.REQUEST,
            HardwareType = DHCPMessage.THardwareType.Ethernet,
            ClientHardwareAddress = clientHardwareAddress,
            NextServerIPAddress = IPAddress.Any,
            ClientIPAddress = IPAddress.Any,
            RelayAgentIPAddress = IPAddress.Any,
            YourIPAddress = IPAddress.Any,
            BroadCast = true,
            Hops = 0,
            XID = (uint)Random.Shared.Next(),
            Secs = 0,
            BootFileName = string.Empty,
            ServerHostName = string.Empty,
        };

        mRequest.Options.Add(new DHCPOptionRequestedIPAddress(mOffer.YourIPAddress));
        mRequest.Options.Add(new DHCPOptionServerIdentifier(dhcpServer.EndPoint.Address));

        Console.WriteLine($"**** {nameof(mRequest)} c->s ****\r\n{mRequest}\r\n");
        await client.SendBroadcast(mRequest);
        var mAck = await client.ReceiveBroadcast();
        Console.WriteLine($"**** {nameof(mAck)} s->c ****\r\n{mAck}\r\n");

        Assert.AreEqual(mRequest.XID, mAck.XID);
        Assert.AreEqual(TDHCPMessageType.ACK, mAck.MessageType);
        Assert.AreEqual(0, mAck.Hops);
        Assert.AreEqual(expectedClientIPAddress, mAck.YourIPAddress);
        Assert.AreEqual(IPAddress.Any, mAck.ClientIPAddress);
        Assert.AreEqual(IPAddress.Any, mAck.RelayAgentIPAddress);
        Assert.AreEqual(IPAddress.Any, mAck.NextServerIPAddress);
        CollectionAssert.AreEqual(mRequest.ClientHardwareAddress, mAck.ClientHardwareAddress);
        Assert.IsTrue(string.IsNullOrEmpty(mAck.ServerHostName));
        Assert.IsTrue(string.IsNullOrEmpty(mAck.BootFileName));

        optServerIdentifier = AssertGetOption<DHCPOptionServerIdentifier>(mAck);
        Assert.AreEqual(dhcpServer.EndPoint.Address, optServerIdentifier.IPAddress);

        optIPAddressLeaseTime = AssertGetOption<DHCPOptionIPAddressLeaseTime>(mAck);
        Assert.AreEqual(dhcpServer.LeaseTime, optIPAddressLeaseTime.LeaseTime);

        // ***** Check dhcpserver client administration *****

        Assert.AreEqual(1, dhcpServer.Clients.Count);
        var registeredClient = dhcpServer.Clients[0];

        Assert.AreEqual(expectedClientIPAddress, registeredClient.IPAddress);
        CollectionAssert.AreEqual(clientHardwareAddress, registeredClient.HardwareAddress);
        CollectionAssert.AreEqual(clientHardwareAddress, registeredClient.Identifier);
        Assert.AreEqual(DHCPClient.TState.Assigned, registeredClient.State);
        Assert.IsTrue(string.IsNullOrEmpty(registeredClient.HostName));
        Assert.IsTrue(registeredClient.LeaseStartTime < registeredClient.LeaseEndTime);
        Assert.AreEqual(dhcpServer.LeaseTime, registeredClient.LeaseDuration);
    }

    private static T AssertGetOption<T>(DHCPMessage mOffer) where T : DHCPOptionBase
    {
        var option = mOffer.FindOption<T>();
        Assert.IsNotNull(option);
        return option;
    }
}
