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
    public class DHCPOptionOptionOverload : DHCPOptionBase
    {
        private byte m_Overload;

        #region IDHCPOption Members

        public byte Overload
        {
            get
            {
                return m_Overload;
            }
        }

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionOptionOverload result = new DHCPOptionOptionOverload();
            if (s.Length != 1) throw new IOException("Invalid DHCP option length");
            result.m_Overload = (byte)s.ReadByte();
            return result;
        }

        public override void ToStream(Stream s)
        {
            s.WriteByte(m_Overload);
        }

        #endregion

        public DHCPOptionOptionOverload()
            : base(TDHCPOption.OptionOverload)
        {
        }

        public DHCPOptionOptionOverload(byte overload)
            : base(TDHCPOption.OptionOverload)
        {
            m_Overload = overload;
        }

        public override string ToString()
        {
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, m_Overload);
        }
    }
}
