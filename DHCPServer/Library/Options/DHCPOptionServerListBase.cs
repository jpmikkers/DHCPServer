/*

Copyright (c) 2020 Jean-Paul Mikkers

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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace GitHub.JPMikkers.DHCP
{
    public abstract class DHCPOptionServerListBase : DHCPOptionBase
    {
        private List<IPAddress> _IPAddresses = new List<IPAddress>();

        public IEnumerable<IPAddress> IPAddresses
        {
            get
            {
                return _IPAddresses;
            }
            set
            {
                _IPAddresses = value.ToList();
            }
        }

        public abstract DHCPOptionServerListBase Create();

        #region IDHCPOption Members

        public override IDHCPOption FromStream(Stream s)
        {
            if (s.Length % 4 != 0) throw new IOException("Invalid DHCP option length");

            var result = Create();

            for(int t=0;t<s.Length;t+=4)
            {
                result._IPAddresses.Add(ParseHelper.ReadIPAddress(s));
            }

            return result;
        }

        public override void ToStream(Stream s)
        {
            foreach (var ipAddress in _IPAddresses)
            {
                ParseHelper.WriteIPAddress(s, ipAddress);
            }
        }

        #endregion

        protected DHCPOptionServerListBase(TDHCPOption optionType)
            : base(optionType)
        {
        }

        public override string ToString()
        {
            return $"Option(name=[{OptionType}],value=[{string.Join(",", _IPAddresses.Select(x => x.ToString()))}])";
        }
    }
}
