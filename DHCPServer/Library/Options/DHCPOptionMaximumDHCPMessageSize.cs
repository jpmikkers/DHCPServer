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
    public class DHCPOptionMaximumDHCPMessageSize : DHCPOptionBase
    {
        private ushort _maxSize;

        #region IDHCPOption Members

        public ushort MaxSize
        {
            get
            {
                return _maxSize;
            }
        }

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionMaximumDHCPMessageSize result = new DHCPOptionMaximumDHCPMessageSize();
            if (s.Length != 2) throw new IOException("Invalid DHCP option length");
            result._maxSize = ParseHelper.ReadUInt16(s);
            return result;
        }

        public override void ToStream(Stream s)
        {
            ParseHelper.WriteUInt16(s,_maxSize);
        }

        #endregion

        public DHCPOptionMaximumDHCPMessageSize()
            : base(TDHCPOption.MaximumDHCPMessageSize)
        {
        }

        public DHCPOptionMaximumDHCPMessageSize(ushort maxSize)
            : base(TDHCPOption.MaximumDHCPMessageSize)
        {
            _maxSize = maxSize;
        }

        public override string ToString()
        {
            return $"Option(name=[{OptionType}],value=[{_maxSize}])";
        }
    }
}
