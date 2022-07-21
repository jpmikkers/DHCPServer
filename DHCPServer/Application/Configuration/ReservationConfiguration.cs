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
        private IPAddress _poolStart;
        private IPAddress _poolEnd;
        private bool _preempt;

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
                return _poolStart.ToString();
            }
            set
            {
                _poolStart = IPAddress.Parse(value);
            }
        }

        public string PoolEnd
        {
            get
            {
                return _poolEnd.ToString();
            }
            set
            {
                _poolEnd = IPAddress.Parse(value);
            }
        }

        [DefaultValue(false)]
        public bool Preempt
        {
            get
            {
                return _preempt;
            }
            set
            {
                _preempt = value;
            }
        }

        public ReservationItem ConstructReservationItem()
        {
            return new ReservationItem()
            { 
                HostName = this.HostName, 
                MacTaste = this.MacTaste, 
                PoolStart = _poolStart, 
                PoolEnd = _poolEnd,
                Preempt = _preempt,
            };
        }
    }
}