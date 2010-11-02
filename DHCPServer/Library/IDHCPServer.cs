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

        void Start();
        void Stop();
    }
}