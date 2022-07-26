using System;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace GitHub.JPMikkers.DHCP
{
    public class DHCPTraceEventArgs : EventArgs
    {
        private string m_Message;
        public string Message { get { return m_Message; } set { m_Message = value; } }
    }

    public class DHCPStopEventArgs : EventArgs
    {
        private Exception m_Reason;
        public Exception Reason { get { return m_Reason; } set { m_Reason = value; } }
    }

    public enum OptionMode
    {
        Default,
        Force
    }

    public struct OptionItem
    {
        public OptionMode Mode;
        public IDHCPOption Option;

        public OptionItem(OptionMode mode, IDHCPOption option)
        {
            this.Mode = mode;
            this.Option = option;
        }
    }

    public class ReservationItem
    {
        private static readonly Regex s_regex = new Regex(@"^(?<mac>([0-9a-fA-F][0-9a-fA-F][:\-\.]?)+)(?<netmask>/[0-9]+)?", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private string _macTaste;
        private byte[] _prefix;
        private int _prefixBits;

        public string MacTaste 
        {
            get { return _macTaste; }

            set
            {
                _macTaste = value;
                _prefix = null;
                _prefixBits = 0;

                if (!string.IsNullOrWhiteSpace(MacTaste))
                {
                    try
                    {
                        Match match = s_regex.Match(_macTaste);
                        if (match.Success && match.Groups["mac"].Success)
                        {
                            _prefix = Utils.HexStringToBytes(match.Groups["mac"].Value);
                            _prefixBits = _prefix.Length * 8;

                            if (match.Groups["netmask"].Success)
                            {
                                _prefixBits = Int32.Parse(match.Groups["netmask"].Value.Substring(1));
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        public string HostName { get; set; }
        public IPAddress PoolStart { get; set; }
        public IPAddress PoolEnd { get; set; }
        public bool Preempt { get; set; }

        private static bool MacMatch(byte[] mac, byte[] prefix, int bits)
        {
            // prefix should have more bits than masklength
            if (((bits + 7) >> 3) > prefix.Length) return false;
            // prefix should be shorter or equal to mac address
            if (prefix.Length > mac.Length) return false;
            for (int t = 0; t < (bits - 7); t += 8)
            {
                if (mac[t >> 3] != prefix[t >> 3]) return false;
            }

            if ((bits & 7) > 0)
            {
                byte bitMask = (byte)(0xFF00 >> (bits & 7));
                if ((mac[bits >> 3] & bitMask) != (prefix[bits >> 3] & bitMask)) return false;
            }
            return true;
        }

        public bool Match(DHCPMessage message)
        {
            var client = DHCPClient.CreateFromMessage(message);

            if (!string.IsNullOrWhiteSpace(MacTaste) && _prefix!=null)
            {
                return MacMatch(client.HardwareAddress, _prefix, _prefixBits);
            }
            else if (!string.IsNullOrWhiteSpace(HostName))
            {
                if (!string.IsNullOrWhiteSpace(client.HostName))
                {
                    if (client.HostName.StartsWith(HostName,true,CultureInfo.InvariantCulture))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }

    public interface IDHCPServer : IDisposable
    {
        event EventHandler<DHCPTraceEventArgs> OnTrace;
        event EventHandler<DHCPStopEventArgs> OnStatusChange;

        IPEndPoint EndPoint { get; set; }
        IPAddress SubnetMask { get; set; }
        IPAddress PoolStart { get; set; }
        IPAddress PoolEnd { get; set; }

        TimeSpan OfferExpirationTime { get; set; }
        TimeSpan LeaseTime { get; set; }
        IList<DHCPClient> Clients { get; }
        string HostName { get; }
        bool Active { get; }
        List<OptionItem> Options { get; set; }
        List<ReservationItem> Reservations { get; set; }

        void Start();
        void Stop();
    }
}