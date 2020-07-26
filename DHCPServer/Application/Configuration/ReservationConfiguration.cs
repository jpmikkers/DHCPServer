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
using System.Net;
using System.ComponentModel;
using GitHub.JPMikkers.DHCP;

namespace DHCPServerApp
{
    [Serializable()]
    public class ReservationConfiguration
    {
        private IPAddress m_PoolStart;
        private IPAddress m_PoolEnd;
        private bool m_Preempt;

        public string MacTaste
        {
            get;
            set;
        }

        public string HostName
        {
            get;
            set;
        }

        public string PoolStart
        {
            get
            {
                return m_PoolStart.ToString();
            }
            set
            {
                m_PoolStart = IPAddress.Parse(value);
            }
        }

        public string PoolEnd
        {
            get
            {
                return m_PoolEnd.ToString();
            }
            set
            {
                m_PoolEnd = IPAddress.Parse(value);
            }
        }

        [DefaultValue(false)]
        public bool Preempt
        {
            get
            {
                return m_Preempt;
            }
            set
            {
                m_Preempt = value;
            }
        }

        public ReservationItem ConstructReservationItem()
        {
            return new ReservationItem()
            { 
                HostName = this.HostName, 
                MacTaste = this.MacTaste, 
                PoolStart = m_PoolStart, 
                PoolEnd = m_PoolEnd,
                Preempt = m_Preempt,
            };
        }
    }
}