using Baksteen.DHCPServer.ConfigFile;
using GitHub.JPMikkers.DHCP;
using System;
using System.IO;
using System.Linq;

namespace ManagedDHCPService;

internal static class OptionConfigurationConverter
{
    public static OptionItem ToOptionItem(OptionConfiguration config)
    {
        if(config == null) throw new ArgumentNullException(nameof(config));

        IDHCPOption option;

        switch(config)
        {
            case OptionConfigurationTFTPServerName tftp:
                option = new DHCPOptionTFTPServerName(tftp.Name);
                break;

            case OptionConfigurationBootFileName boot:
                option = new DHCPOptionBootFileName(boot.Name);
                break;

            case OptionConfigurationVendorSpecificInformation vendorInfo:
                option = new DHCPOptionVendorSpecificInformation(vendorInfo.Information);
                break;

            case OptionConfigurationGeneric generic:
                {
                    byte[] data = Utils.HexStringToBytes(generic.Data);
                    option = new DHCPOptionGeneric((TDHCPOption)generic.Option, data);
                }
                break;

            case OptionConfigurationVendorClassIdentifier vendorClass:
                {
                    byte[] data;
                    if(string.IsNullOrEmpty(vendorClass.DataAsString))
                    {
                        data = Utils.HexStringToBytes(vendorClass.DataAsHex);
                    }
                    else
                    {
                        using(MemoryStream m = new MemoryStream())
                        {
                            ParseHelper.WriteString(m, vendorClass.DataAsString);
                            m.Flush();
                            data = m.ToArray();
                        }
                    }
                    option = new DHCPOptionVendorClassIdentifier(data);
                }
                break;

            case OptionConfigurationRouter router:
                {
                    DHCPOptionRouter r = new DHCPOptionRouter();
                    r.IPAddresses = router.Addresses
                        .Where(x => x.Address != null)
                        .Select(x => x.Address);
                    option = r;
                }
                break;

            case OptionConfigurationNetworkTimeProtocolServers ntp:
                {
                    DHCPOptionNetworkTimeProtocolServers n = new DHCPOptionNetworkTimeProtocolServers();
                    n.IPAddresses = ntp.Addresses
                        .Where(x => x.Address != null)
                        .Select(x => x.Address);
                    option = n;
                }
                break;

            case OptionConfigurationDomainNameServer dns:
                {
                    DHCPOptionDomainNameServer d = new DHCPOptionDomainNameServer();
                    d.IPAddresses = dns.Addresses
                        .Where(x => x.Address != null)
                        .Select(x => x.Address);
                    option = d;
                }
                break;

            default:
                throw new NotSupportedException("Unknown OptionConfiguration type: " + config.GetType().FullName);
        }

        option.ZeroTerminatedStrings = config.ZeroTerminatedStrings;
        return new OptionItem(config.Mode == OptionConfigMode.Force ? OptionMode.Force : OptionMode.Default, option);
    }
}