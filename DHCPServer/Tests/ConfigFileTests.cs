using Baksteen.DHCPServer.ConfigFile;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;

namespace Tests;

[TestClass]
public class ConfigFileTests
{
    [TestMethod]
    public void ReadDHCPServerConfigurationFromXml()
    {
        // Arrange
        string testXml = """
            <?xml version="1.0" encoding="utf-8"?>
            <DHCPServerConfigurationList xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
                <DHCPServerConfiguration>
                    <Name>TestServer</Name>
                    <Address>192.168.1.1</Address>
                    <NetMask>255.255.255.0</NetMask>
                    <PoolStart>192.168.1.10</PoolStart>
                    <PoolEnd>192.168.1.254</PoolEnd>
                    <LeaseTime>86400</LeaseTime>
                    <OfferTime>30</OfferTime>
                    <MinimumPacketSize>576</MinimumPacketSize>
                    <Options/>
                    <Reservations/>
                </DHCPServerConfiguration>
            </DHCPServerConfigurationList>
            """;

        string tempFilePath = Path.Combine(Path.GetTempPath(), "test_dhcp_config.xml");

        try
        {
            // Act
            File.WriteAllText(tempFilePath, testXml);
            DHCPServerConfigurationList configList = DHCPServerConfigurationList.Read(tempFilePath);

            // Assert
            Assert.IsNotNull(configList);
            Assert.HasCount(1, configList);

            DHCPServerConfiguration config = configList[0];
            Assert.AreEqual("TestServer", config.Name);
            Assert.AreEqual("192.168.1.1", config.Address);
            Assert.AreEqual("255.255.255.0", config.NetMask);
            Assert.AreEqual("192.168.1.10", config.PoolStart);
            Assert.AreEqual("192.168.1.254", config.PoolEnd);
            Assert.AreEqual(86400, config.LeaseTime);
            Assert.AreEqual(30, config.OfferTime);
            Assert.AreEqual(576, config.MinimumPacketSize);
        }
        finally
        {
            // Cleanup
            if(File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [TestMethod]
    public void ReadMultipleDHCPServerConfigurationsFromXml()
    {
        // Arrange
        string testXml = """
            <?xml version="1.0" encoding="utf-8"?>
            <DHCPServerConfigurationList xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
                <DHCPServerConfiguration>
                    <Name>Server1</Name>
                    <Address>192.168.1.1</Address>
                    <NetMask>255.255.255.0</NetMask>
                    <PoolStart>192.168.1.10</PoolStart>
                    <PoolEnd>192.168.1.100</PoolEnd>
                    <LeaseTime>86400</LeaseTime>
                    <OfferTime>30</OfferTime>
                    <MinimumPacketSize>576</MinimumPacketSize>
                    <Options/>
                    <Reservations/>
                </DHCPServerConfiguration>
                <DHCPServerConfiguration>
                    <Name>Server2</Name>
                    <Address>192.168.2.1</Address>
                    <NetMask>255.255.255.0</NetMask>
                    <PoolStart>192.168.2.10</PoolStart>
                    <PoolEnd>192.168.2.100</PoolEnd>
                    <LeaseTime>43200</LeaseTime>
                    <OfferTime>60</OfferTime>
                    <MinimumPacketSize>1024</MinimumPacketSize>
                    <Options/>
                    <Reservations/>
                </DHCPServerConfiguration>
            </DHCPServerConfigurationList>
            """;

        string tempFilePath = Path.Combine(Path.GetTempPath(), "test_dhcp_config_multiple.xml");

        try
        {
            // Act
            File.WriteAllText(tempFilePath, testXml);
            DHCPServerConfigurationList configList = DHCPServerConfigurationList.Read(tempFilePath);

            // Assert
            Assert.IsNotNull(configList);
            Assert.HasCount(2, configList);

            DHCPServerConfiguration config1 = configList[0];
            Assert.AreEqual("Server1", config1.Name);
            Assert.AreEqual("192.168.1.1", config1.Address);

            DHCPServerConfiguration config2 = configList[1];
            Assert.AreEqual("Server2", config2.Name);
            Assert.AreEqual("192.168.2.1", config2.Address);
            Assert.AreEqual(43200, config2.LeaseTime);
        }
        finally
        {
            // Cleanup
            if(File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [TestMethod]
    public void ReadNonExistentXmlFileReturnsEmptyList()
    {
        // Arrange
        string nonExistentFilePath = Path.Combine(Path.GetTempPath(), "non_existent_dhcp_config.xml");

        // Cleanup any leftover file
        if(File.Exists(nonExistentFilePath))
        {
            File.Delete(nonExistentFilePath);
        }

        try
        {
            // Act
            DHCPServerConfigurationList configList = DHCPServerConfigurationList.Read(nonExistentFilePath);

            // Assert
            Assert.IsNotNull(configList);
            Assert.HasCount(0, configList);
        }
        finally
        {
            // Cleanup
            if(File.Exists(nonExistentFilePath))
            {
                File.Delete(nonExistentFilePath);
            }
        }
    }
}