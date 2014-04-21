/*

Copyright (c) 2010 Jean-Paul Mikkers

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

*/
using System;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace CodePlex.JPMikkers.DHCP
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
        private static readonly Regex regex = new Regex(@"^(?<mac>([0-9a-fA-F][0-9a-fA-F][:\-\.]?)+)(?<netmask>/[0-9]+)?", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private string m_MacTaste;
        private byte[] m_Prefix;
        private int m_PrefixBits;

        public string MacTaste 
        {
            get { return m_MacTaste; }

            set
            {
                m_MacTaste = value;
                m_Prefix = null;
                m_PrefixBits = 0;

                if (!string.IsNullOrWhiteSpace(MacTaste))
                {
                    try
                    {
                        Match match = regex.Match(m_MacTaste);
                        if (match.Success && match.Groups["mac"].Success)
                        {
                            m_Prefix = Utils.HexStringToBytes(match.Groups["mac"].Value);
                            m_PrefixBits = m_Prefix.Length * 8;

                            if (match.Groups["netmask"].Success)
                            {
                                m_PrefixBits = Int32.Parse(match.Groups["netmask"].Value.Substring(1));
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

            if (!string.IsNullOrWhiteSpace(MacTaste) && m_Prefix!=null)
            {
                return MacMatch(client.HardwareAddress, m_Prefix, m_PrefixBits);
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