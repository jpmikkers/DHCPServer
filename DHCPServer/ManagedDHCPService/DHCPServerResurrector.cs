using GitHub.JPMikkers.DHCP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace ManagedDHCPService;

public class DHCPServerResurrector : IDisposable
{
    private const int RetryTime = 30000;
    private readonly object _lock;
    private bool _disposed;
    private readonly DHCPServerConfiguration _config;
    private readonly ILogger _logger;
    private readonly string _clientInfoPath;
    private DHCPServer? _server;
    private readonly Timer _retryTimer;

    public DHCPServerResurrector(DHCPServerConfiguration config, ILogger eventLog, string clientInfoPath)
    {
        _lock = new object();
        _disposed = false;
        _config = config;
        _logger = eventLog;
        _clientInfoPath = clientInfoPath;
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

    private void Resurrect(object? state)
    {
        lock(_lock)
        {
            if(!_disposed)
            {
                try
                {
                    _server = new DHCPServer(_logger, _clientInfoPath);
                    _server.EndPoint = new IPEndPoint(IPAddress.Parse(_config.Address), 67);
                    _server.SubnetMask = IPAddress.Parse(_config.NetMask);
                    _server.PoolStart = IPAddress.Parse(_config.PoolStart);
                    _server.PoolEnd = IPAddress.Parse(_config.PoolEnd);
                    _server.LeaseTime = (_config.LeaseTime > 0) ? TimeSpan.FromSeconds(_config.LeaseTime) : Utils.InfiniteTimeSpan;
                    _server.OfferExpirationTime = TimeSpan.FromSeconds(Math.Max(1, _config.OfferTime));
                    _server.MinimumPacketSize = _config.MinimumPacketSize;

                    List<OptionItem> options = new List<OptionItem>();
                    foreach(OptionConfiguration optionConfiguration in _config.Options)
                    {
                        options.Add(optionConfiguration.ConstructOptionItem());
                    }
                    _server.Options = options;

                    List<ReservationItem> reservations = new List<ReservationItem>();
                    foreach(ReservationConfiguration reservationConfiguration in _config.Reservations)
                    {
                        reservations.Add(reservationConfiguration.ConstructReservationItem());
                    }
                    _server.Reservations = reservations;

                    _server.OnStatusChange += server_OnStatusChange;
                    _server.Start();
                }
                catch(Exception)
                {
                    CleanupAndRetry();
                }
            }
        }
    }

    private void server_OnStatusChange(object? sender, DHCPStopEventArgs? e)
    {
        if(sender is DHCPServer server)
        {
            if(server.Active)
            {
                //Log(EventLogEntryType.Information, string.Format("{0} transfers in progress", server.ActiveTransfers));
            }
            else
            {
                if(e!=null && e.Reason != null)
                {
                    //Log(EventLogEntryType.Error, $"Stopped, reason: {e.Reason}");
                    _logger.LogError($"{_config.Name} : Stopped, reason: {e.Reason}");
                }
                CleanupAndRetry();
            }
        }
    }

    private void CleanupAndRetry()
    {
        lock(_lock)
        {
            if(!_disposed)
            {
                // stop server
                if(_server != null)
                {
                    _server.OnStatusChange -= server_OnStatusChange;
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
        if(disposing)
        {
            lock(_lock)
            {
                if(!_disposed)
                {
                    _disposed = true;

                    _retryTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    _retryTimer.Dispose();

                    if(_server != null)
                    {
                        _server.OnStatusChange -= server_OnStatusChange;
                        _server.Dispose();
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
