using Baksteen.DHCPServer.ConfigFile;
using GitHub.JPMikkers.DHCP;
using System.Net;

namespace ManagedDHCPService;

internal static class ReservationConfigurationConverter
{
    public static ReservationItem ConstructReservationItem(ReservationConfiguration reservationConfiguration)
    {
        return new ReservationItem()
        {
            HostName = reservationConfiguration.HostName,
            MacTaste = reservationConfiguration.MacTaste,
            PoolStart = IPAddress.Parse(reservationConfiguration.PoolStart),
            PoolEnd = IPAddress.Parse(reservationConfiguration.PoolEnd),
            Preempt = reservationConfiguration.Preempt,
        };
    }
}
