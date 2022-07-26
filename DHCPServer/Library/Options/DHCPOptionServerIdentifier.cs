using System.IO;
using System.Net;

namespace GitHub.JPMikkers.DHCP
{
    public class DHCPOptionServerIdentifier : DHCPOptionBase
    {
        private IPAddress _IPAddress;

        public IPAddress IPAddress
        {
            get
            {
                return _IPAddress;
            }
        }

        #region IDHCPOption Members

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionServerIdentifier result = new DHCPOptionServerIdentifier();
            if(s.Length != 4) throw new IOException("Invalid DHCP option length");
            result._IPAddress = ParseHelper.ReadIPAddress(s);
            return result;
        }

        public override void ToStream(Stream s)
        {
            ParseHelper.WriteIPAddress(s, _IPAddress);
        }

        #endregion

        public DHCPOptionServerIdentifier()
            : base(TDHCPOption.ServerIdentifier)
        {
        }

        public DHCPOptionServerIdentifier(IPAddress ipAddress)
            : base(TDHCPOption.ServerIdentifier)
        {
            _IPAddress = ipAddress;
        }

        public override string ToString()
        {
            return $"Option(name=[{OptionType}],value=[{_IPAddress}])";
        }
    }
}
