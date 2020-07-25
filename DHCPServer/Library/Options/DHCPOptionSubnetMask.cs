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
using System.IO;
using System.Net;

namespace GitHub.JPMikkers.DHCP
{
    public class DHCPOptionSubnetMask : DHCPOptionBase
    {
        private IPAddress m_SubnetMask;

        #region IDHCPOption Members

        public IPAddress SubnetMask
        {
            get
            {
                return m_SubnetMask;
            }
        }

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionSubnetMask result = new DHCPOptionSubnetMask();
            if (s.Length != 4) throw new IOException("Invalid DHCP option length");
            result.m_SubnetMask = ParseHelper.ReadIPAddress(s);
            return result;
        }

        public override void ToStream(Stream s)
        {
            ParseHelper.WriteIPAddress(s, m_SubnetMask);
        }

        #endregion

        public DHCPOptionSubnetMask()
            : base(TDHCPOption.SubnetMask)
        {
        }

        public DHCPOptionSubnetMask(IPAddress subnetMask)
            : base(TDHCPOption.SubnetMask)
        {
            m_SubnetMask = subnetMask;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, m_SubnetMask.ToString());
        }
    }
}
