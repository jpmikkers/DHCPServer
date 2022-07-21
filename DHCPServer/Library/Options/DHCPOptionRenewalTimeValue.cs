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
using System;
using System.IO;

namespace GitHub.JPMikkers.DHCP
{
    public class DHCPOptionRenewalTimeValue : DHCPOptionBase
    {
        private TimeSpan _timeSpan;

        #region IDHCPOption Members

        public TimeSpan TimeSpan
        {
            get
            {
                return _timeSpan;
            }
        }

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionRenewalTimeValue result = new DHCPOptionRenewalTimeValue();
            if (s.Length != 4) throw new IOException("Invalid DHCP option length");
            result._timeSpan = TimeSpan.FromSeconds(ParseHelper.ReadUInt32(s));
            return result;
        }

        public override void ToStream(Stream s)
        {
            ParseHelper.WriteUInt32(s, (uint)_timeSpan.TotalSeconds);
        }

        #endregion

        public DHCPOptionRenewalTimeValue()
            : base(TDHCPOption.RenewalTimeValue)
        {
        }

        public DHCPOptionRenewalTimeValue(TimeSpan timeSpan)
            : base(TDHCPOption.RenewalTimeValue)
        {
            _timeSpan = timeSpan;
        }

        public override string ToString()
        {
            return $"Option(name=[{OptionType}],value=[{_timeSpan}])";
        }
    }
}
