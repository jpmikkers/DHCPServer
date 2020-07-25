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
        private List<TDHCPOption> m_RequestList = new List<TDHCPOption>();

        #region IDHCPOption Members

        public List<TDHCPOption> RequestList
        {
            get
            {
                return m_RequestList;
            }
        }

        public override IDHCPOption FromStream(Stream s)
        {
            DHCPOptionParameterRequestList result = new DHCPOptionParameterRequestList();
            while(true)
            {
                int c = s.ReadByte();
                if(c<0) break;
                result.m_RequestList.Add((TDHCPOption)c);
            }
            return result;
        }

        public override void ToStream(Stream s)
        {
            foreach(TDHCPOption opt in m_RequestList)
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
            foreach(TDHCPOption opt in m_RequestList)
            {
                sb.Append(opt.ToString());
                sb.Append(",");
            }
            if(m_RequestList.Count>0) sb.Remove(sb.Length-1,1);
            return string.Format("Option(name=[{0}],value=[{1}])", OptionType, sb.ToString());
        }
    }
}
