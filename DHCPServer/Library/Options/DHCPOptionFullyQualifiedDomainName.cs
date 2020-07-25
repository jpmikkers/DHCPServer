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

namespace GitHub.JPMikkers.DHCP
{
    public class DHCPOptionFullyQualifiedDomainName : DHCPOptionBase
    {
        private byte[] m_Data;

        public byte[] Data
        {
            get { return m_Data; }
            set { m_Data = value; }
        }

        #region IDHCPOption Members

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionFullyQualifiedDomainName result = new DHCPOptionFullyQualifiedDomainName();
            result.m_Data = new byte[s.Length];
            s.Read(result.m_Data, 0, result.m_Data.Length);
            return result;
        }

        public override void ToStream(Stream s)
        {
            s.Write(m_Data, 0, m_Data.Length);
        }

        #endregion

        public DHCPOptionFullyQualifiedDomainName()
            : base(TDHCPOption.FullyQualifiedDomainName)
        {
            m_Data = new byte[0];
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, Utils.BytesToHexString(m_Data, " "));
        }
    }
}
