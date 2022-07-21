/*

Copyright (c) 2010 Jean-Paul Mikkers

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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using GitHub.JPMikkers.DHCP;

namespace DHCPServerApp
{
    public class DHCPServerResurrector : IDisposable
    {
        private const int RetryTime = 30000;
        private readonly object _lock;
        private bool _disposed;
        private DHCPServerConfiguration _config;
        private EventLog _eventLog;

        private DHCPServer _server;
        private Timer _retryTimer;

        public DHCPServerResurrector(DHCPServerConfiguration config, EventLog eventLog)
        {
            _lock = new object();
            _disposed = false;
            _config = config;
            _eventLog = eventLog;
            _retryTimer = new Timer(new TimerCallback(Resurrect));
            Resurrect(null);
        }

        ~DHCPServerResurrector()
        {
            try
            {
                Dispose(false);
            }
            catch
            {
                // never let any exception escape the finalizer, or else your process will be killed.
            }
        }

        private void Resurrect(object state)
        {
            lock (_lock)
            {
                if (!_disposed)
                {
                    try
                    {
                        _server = new DHCPServer(Program.GetClientInfoPath(_config.Name,_config.Address));
                        _server.EndPoint = new IPEndPoint(IPAddress.Parse(_config.Address),67);
                        _server.SubnetMask = IPAddress.Parse(_config.NetMask);
                        _server.PoolStart = IPAddress.Parse(_config.PoolStart);
                        _server.PoolEnd = IPAddress.Parse(_config.PoolEnd);
                        _server.LeaseTime = (_config.LeaseTime>0) ? TimeSpan.FromSeconds(_config.LeaseTime) : Utils.InfiniteTimeSpan;
                        _server.OfferExpirationTime = TimeSpan.FromSeconds(Math.Max(1, _config.OfferTime));
                        _server.MinimumPacketSize = _config.MinimumPacketSize;

                        List <OptionItem> options = new List<OptionItem>();
                        foreach(OptionConfiguration optionConfiguration in _config.Options)
                        {
                            options.Add(optionConfiguration.ConstructOptionItem());
                        }
                        _server.Options = options;

                        List<ReservationItem> reservations = new List<ReservationItem>();
                        foreach (ReservationConfiguration reservationConfiguration in _config.Reservations)
                        {
                            reservations.Add(reservationConfiguration.ConstructReservationItem());
                        }
                        _server.Reservations = reservations;
                        
                        _server.OnStatusChange += server_OnStatusChange;
                        _server.OnTrace += server_OnTrace;
                        _server.Start();
                    }
                    catch (Exception)
                    {
                        CleanupAndRetry();
                    }
                }
            }
        }

        private void Log(EventLogEntryType entryType, string msg)
        {
            _eventLog.WriteEntry($"{_config.Name} : {msg}",entryType);
        }

        private void server_OnTrace(object sender, DHCPTraceEventArgs e)
        {
            Log(EventLogEntryType.Information,e.Message);
        }

        private void server_OnStatusChange(object sender, DHCPStopEventArgs e)
        {
            DHCPServer server = (DHCPServer)sender;

            if (server.Active)
            {
                //Log(EventLogEntryType.Information, string.Format("{0} transfers in progress", server.ActiveTransfers));
            }
            else
            {
                if (e.Reason != null)
                {
                    Log(EventLogEntryType.Error, $"Stopped, reason: {e.Reason}");
                }
                CleanupAndRetry();
            }
        }

        private void CleanupAndRetry()
        {
            lock (_lock)
            {
                if (!_disposed)
                {
                    // stop server
                    if (_server != null)
                    {
                        _server.OnStatusChange -= server_OnStatusChange;
                        _server.OnTrace -= server_OnTrace;
                        _server.Dispose();
                        _server = null;
                    }
                    // initiate retry timer
                    _retryTimer.Change(RetryTime, Timeout.Infinite);
                }
            }
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_lock)
                {
                    if (!_disposed)
                    {
                        _disposed = true;

                        _retryTimer.Change(Timeout.Infinite, Timeout.Infinite);
                        _retryTimer.Dispose();

                        if (_server != null)
                        {
                            _server.OnStatusChange -= server_OnStatusChange;
                            _server.Dispose();
                            _server.OnTrace -= server_OnTrace;
                            _server = null;
                        }
                    }
                }
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        #endregion
    }
}
