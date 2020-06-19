using CodePlex.JPMikkers.DHCP;
using DHCPServerApp;
using NUnit.Framework;
using System.Linq;
using System.Threading;

namespace Library.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            Config.InitializeEventLog();
            var configurations = DHCPServerConfigurationList.Read(Config.GetConfigurationPath());
            var config = configurations.Single();
            var dhcpServer = DHCPServer.FromConfig(config);
            dhcpServer.Start();
            Thread.Sleep(100000000);
        }
    }
}