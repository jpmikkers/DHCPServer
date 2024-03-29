using System;
using System.Collections.Generic;
using System.Net;

namespace DHCPServerApp
{
    [Serializable()]
    public class DHCPServerConfiguration
    {
        private string _name;
        private IPAddress _address;
        private IPAddress _netMask;
        private IPAddress _poolStart;
        private IPAddress _poolEnd;
        private int _leaseTime;
        private int _offerTime;
        private int _minimumPacketSize;
        private List<OptionConfiguration> _options;
        private List<ReservationConfiguration> _reservations;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Address
        {
            get
            {
                return _address.ToString();
            }
            set
            {
                _address = IPAddress.Parse(value);
            }
        }

        public string NetMask
        {
            get
            {
                return _netMask.ToString();
            }
            set
            {
                _netMask = IPAddress.Parse(value);
            }
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

        public int LeaseTime
        {
            get
            {
                return _leaseTime;
            }
            set
            {
                _leaseTime = value;
            }
        }

        public int OfferTime
        {
            get
            {
                return _offerTime;
            }
            set
            {
                _offerTime = value;
            }
        }

        public int MinimumPacketSize
        {
            get
            {
                return _minimumPacketSize;
            }
            set
            {
                _minimumPacketSize = value;
            }
        }

        public List<OptionConfiguration> Options
        {
            get
            {
                return _options;
            }
            set
            {
                _options = value;
            }
        }

        public List<ReservationConfiguration> Reservations
        {
            get
            {
                return _reservations;
            }
            set
            {
                _reservations = value;
            }
        }

        public DHCPServerConfiguration()
        {
            Name = "DHCP";
            Address = IPAddress.Loopback.ToString();
            NetMask = "255.255.255.0";
            PoolStart = "0.0.0.0";
            PoolEnd = "255.255.255.255";
            LeaseTime = (int)TimeSpan.FromDays(1.0).TotalSeconds;
            OfferTime = 30;
            MinimumPacketSize = 576;
            Options = new List<OptionConfiguration>();
            Reservations = new List<ReservationConfiguration>();
        }

        public DHCPServerConfiguration Clone()
        {
            return DeepCopier.Copy(this);
        }
    }
}