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
using System.Text;

namespace GitHub.JPMikkers.DHCP
{
    public class DHCPOptionParameterRequestList : DHCPOptionBase
    {
        private readonly List<TDHCPOption> _requestList = new List<TDHCPOption>();

        #region IDHCPOption Members

        public List<TDHCPOption> RequestList
        {
            get
            {
                return _requestList;
            }
        }

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionParameterRequestList result = new DHCPOptionParameterRequestList();
            while(true)
            {
                int c = s.ReadByte();
                if(c<0) break;
                result._requestList.Add((TDHCPOption)c);
            }
            return result;
        }

        public override void ToStream(Stream s)
        {
            foreach(TDHCPOption opt in _requestList)
            {
                s.WriteByte((byte)opt);
            }
        }

        #endregion

        public DHCPOptionParameterRequestList()
            : base(TDHCPOption.ParameterRequestList)
        {
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach(TDHCPOption opt in _requestList)
            {
                sb.Append(opt.ToString());
                sb.Append(",");
            }
            if(_requestList.Count>0) sb.Remove(sb.Length-1,1);
            return $"Option(name=[{OptionType}],value=[{sb}])";
        }
    }
}
