using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GitHub.JPMikkers.DHCP
{
    public class DHCPServer : IDHCPServer
    {
        private const int ClientInformationWriteRetries = 10;

        private IPEndPoint _endPoint = new(IPAddress.Loopback, 67);
        private IUDPSocket _socket = default!;
        private IPAddress _subnetMask = IPAddress.Any;
        private IPAddress _poolStart = IPAddress.Any;
        private IPAddress _poolEnd = IPAddress.Broadcast;
        private readonly ILogger _logger;
        private readonly string? _clientInfoPath;
        private readonly IUDPSocketFactory _udpSocketFactory;
        private readonly string _hostName;
        private readonly ConcurrentDictionary<DHCPClient, DHCPClient> _clients = new();
        private TimeSpan _offerExpirationTime = TimeSpan.FromSeconds(30.0);
        private TimeSpan _leaseTime = TimeSpan.FromDays(1);
        private bool _active = false;
        private List<OptionItem> _options = new();
        private List<IDHCPMessageInterceptor> _interceptors = new();
        private List<ReservationItem> _reservations = new();
        private int _minimumPacketSize = 576;
        private readonly AutoPumpQueue<int> _updateClientInfoQueue;
        private readonly Random _random = new();
        private CancellationTokenSource _cancellationTokenSource = new();
        private Task? _mainTask;

        #region IDHCPServer Members

        public event EventHandler<DHCPStopEventArgs?> OnStatusChange = delegate (object? sender, DHCPStopEventArgs? args) { };

        public IPEndPoint EndPoint
        {
            get
            {
                return _endPoint;
            }
            set
            {
                _endPoint = value;
            }
        }

        public IPAddress SubnetMask
        {
            get
            {
                return _subnetMask;
            }
            set
            {
                _subnetMask = value;
            }
        }

        public IPAddress PoolStart
        {
            get
            {
                return _poolStart;
            }
            set
            {
                _poolStart = value;
            }
        }

        public IPAddress PoolEnd
        {
            get
            {
                return _poolEnd;
            }
            set
            {
                _poolEnd = value;
            }
        }

        public TimeSpan OfferExpirationTime
        {
            get
            {
                return _offerExpirationTime;
            }
            set
            {
                _offerExpirationTime = value;
            }
        }

        public TimeSpan LeaseTime
        {
            get
            {
                return _leaseTime;
            }
            set
            {
                // sanitize timespan.
                _leaseTime = Utils.SanitizeTimeSpan(value);
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
                _minimumPacketSize = Math.Max(value, 312);
            }
        }


        public string HostName
        {
            get
            {
                return _hostName;
            }
        }

        public IList<DHCPClient> Clients
        {
            get
            {
                return new List<DHCPClient>(_clients.Keys.Select(x => x.Clone()));
            }
        }

        public bool Active
        {
            get
            {
                return _active;
            }
        }

        public List<OptionItem> Options
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

        public List<IDHCPMessageInterceptor> Interceptors
        {
            get
            {
                return _interceptors;
            }
            set
            {
                _interceptors = value;
            }
        }

        public List<ReservationItem> Reservations
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

        private void OnUpdateClientInfo(AutoPumpQueue<int> sender, int data)
        {
            if(Active)
            {
                try
                {
                    if(_clientInfoPath != null)
                    {
                        DHCPClientInformation clientInformation = new();
                        clientInformation.Clients.AddRange(Clients);

                        for(int t = 0; t < ClientInformationWriteRetries; t++)
                        {
                            try
                            {
                                clientInformation.Write(_clientInfoPath);
                                break;
                            }
                            catch
                            {
                            }

                            if(t < ClientInformationWriteRetries)
                            {
                                Thread.Sleep(_random.Next(500, 1000));
                            }
                            else
                            {
                                Trace("Could not update client information, data might be stale");
                            }
                        }
                    }
                }
                catch(Exception e)
                {
                    Trace($"Exception in OnUpdateClientInfo : {e}");
                }
            }
        }

        public DHCPServer(ILogger logger, IUDPSocketFactory udpSocketFactory) : this(logger, null, udpSocketFactory)
        { }

        public DHCPServer(ILogger logger, string? clientInfoPath, IUDPSocketFactory udpSocketFactory)
        {
            _updateClientInfoQueue = new AutoPumpQueue<int>(OnUpdateClientInfo);
            _logger = logger;
            _clientInfoPath = clientInfoPath;
            _udpSocketFactory = udpSocketFactory;
            _hostName = System.Environment.MachineName;
        }

        public void Start()
        {
            try
            {
                var clientInformation = string.IsNullOrWhiteSpace(_clientInfoPath) ? new DHCPClientInformation() : DHCPClientInformation.Read(_clientInfoPath);

                foreach(DHCPClient client in clientInformation.Clients
                    .Where(c => c.State != DHCPClient.TState.Offered)   // Forget offered clients.
                    .Where(c => IsIPAddressInPoolRange(c.IPAddress)))   // Forget clients no longer in ip range.
                {
                    _ = _clients.TryAdd(client, client);
                }
            }
            catch(Exception)
            {
            }

            if(!_active)
            {
                try
                {
                    Trace($"Starting DHCP server '{_endPoint}'");
                    _active = true;
                    _socket = _udpSocketFactory.Create(_endPoint, 2048, true, 10);
                    _mainTask = Task.Run(async () => await MainTask(_cancellationTokenSource.Token));
                    Trace("DHCP Server start succeeded");
                }
                catch(Exception e)
                {
                    Trace($"DHCP Server start failed, reason '{e}'");
                    _active = false;
                    throw;
                }
            }

            HandleStatusChange(null);
        }

        public void Stop()
        {
            Stop(null);
        }

        #endregion

        #region Dispose pattern

        ~DHCPServer()
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

        protected void Dispose(bool disposing)
        {
            if(disposing)
            {
                Stop();
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        #endregion

        private void HandleStatusChange(DHCPStopEventArgs? data)
        {
            _updateClientInfoQueue.Enqueue(0);
            OnStatusChange(this, data);
        }

        internal void Trace(string msg)
        {
            _logger?.LogInformation(msg);
        }

        private void Stop(Exception? reason)
        {
            bool notify = false;

            if(_active)
            {
                Trace($"Stopping DHCP server '{_endPoint}'");
                _active = false;
                notify = true;
                _cancellationTokenSource.Cancel();

                if(_mainTask!= null)
                {
                    try
                    {
                        _mainTask.GetAwaiter().GetResult();
                    }
                    catch(Exception ex)
                    {
                        _logger?.LogError(ex, $"Exception during {nameof(Stop)}");
                    }
                    _mainTask = null;
                }

                _socket.Dispose();
                Trace("Stopped");
            }

            if(notify)
            {
                HandleStatusChange(new() { Reason = reason });
            }
        }

        private void CheckLeaseExpiration()
        {
            bool modified = false;

            foreach(var client in _clients.Keys.ToList())
            {
                if(client.State == DHCPClient.TState.Offered && (DateTime.Now - client.OfferedTime) > _offerExpirationTime)
                {
                    RemoveClient(client);
                    modified = true;
                }
                else if(client.State == DHCPClient.TState.Assigned && (DateTime.Now > client.LeaseEndTime))
                {
                    RemoveClient(client);
                    modified = true;
                }
            }

            if(modified)
            {
                HandleStatusChange(null);
            }
        }

        private void RemoveClient(DHCPClient client)
        {
            if(_clients.TryRemove(client, out _))
            {
                Trace($"Removed client '{client}' from client table");
            }
        }

        private async Task SendMessage(DHCPMessage msg, IPEndPoint endPoint)
        {
            Trace($"==== Sending response to {endPoint} ====");
            Trace(Utils.PrefixLines(msg.ToString(), "s->c "));

            try
            {
                MemoryStream m = new();
                msg.ToStream(m, _minimumPacketSize);
                await _socket.SendAsync(endPoint, m.ToArray(), _cancellationTokenSource.Token);
            }
            catch(Exception e)
            {
                // treat any send failures like a lost udp packet, we don't want any badly behaving DHCP clients to kill the server
                Trace($"{nameof(SendMessage)} failed: {e.Message}");
            }
        }

        private void AppendConfiguredOptions(DHCPMessage sourceMsg, DHCPMessage targetMsg)
        {
            foreach(OptionItem optionItem in _options)
            {
                if(optionItem.Mode == OptionMode.Force || sourceMsg.IsRequestedParameter(optionItem.Option.OptionType))
                {
                    if(targetMsg.GetOption(optionItem.Option.OptionType) == null)
                    {
                        targetMsg.Options.Add(optionItem.Option);
                    }
                }
            }

            foreach(IDHCPMessageInterceptor interceptor in _interceptors)
            {
                interceptor.Apply(sourceMsg, targetMsg);
            }
        }

        private async Task SendOFFER(DHCPMessage sourceMsg, IPAddress offeredAddress, TimeSpan leaseTime)
        {
            //Field      DHCPOFFER            
            //-----      ---------            
            //'op'       BOOTREPLY            
            //'htype'    (From "Assigned Numbers" RFC)
            //'hlen'     (Hardware address length in octets)
            //'hops'     0                    
            //'xid'      'xid' from client DHCPDISCOVER message              
            //'secs'     0                    
            //'ciaddr'   0                    
            //'yiaddr'   IP address offered to client            
            //'siaddr'   IP address of next bootstrap server     
            //'flags'    'flags' from client DHCPDISCOVER message              
            //'giaddr'   'giaddr' from client DHCPDISCOVER message              
            //'chaddr'   'chaddr' from client DHCPDISCOVER message              
            //'sname'    Server host name or options           
            //'file'     Client boot file name or options      
            //'options'  options              
            DHCPMessage response = new DHCPMessage();
            response.Opcode = DHCPMessage.TOpcode.BootReply;
            response.HardwareType = sourceMsg.HardwareType;
            response.Hops = 0;
            response.XID = sourceMsg.XID;
            response.Secs = 0;
            response.ClientIPAddress = IPAddress.Any;
            response.YourIPAddress = offeredAddress;
            response.NextServerIPAddress = IPAddress.Any;
            response.BroadCast = sourceMsg.BroadCast;
            response.RelayAgentIPAddress = sourceMsg.RelayAgentIPAddress;
            response.ClientHardwareAddress = sourceMsg.ClientHardwareAddress;
            response.MessageType = TDHCPMessageType.OFFER;

            //Option                    DHCPOFFER    
            //------                    ---------    
            //Requested IP address      MUST NOT     : ok
            //IP address lease time     MUST         : ok                                               
            //Use 'file'/'sname' fields MAY          
            //DHCP message type         DHCPOFFER    : ok
            //Parameter request list    MUST NOT     : ok
            //Message                   SHOULD       
            //Client identifier         MUST NOT     : ok
            //Vendor class identifier   MAY          
            //Server identifier         MUST         : ok
            //Maximum message size      MUST NOT     : ok
            //All others                MAY          

            response.Options.Add(new DHCPOptionIPAddressLeaseTime(leaseTime));
            response.Options.Add(new DHCPOptionServerIdentifier(_socket.LocalEndPoint.Address));
            if(sourceMsg.IsRequestedParameter(TDHCPOption.SubnetMask)) response.Options.Add(new DHCPOptionSubnetMask(this._subnetMask));
            AppendConfiguredOptions(sourceMsg, response);
            await SendOfferOrAck(sourceMsg, response);
        }

        private async Task SendNAK(DHCPMessage sourceMsg)
        {
            //Field      DHCPNAK
            //-----      -------
            //'op'       BOOTREPLY
            //'htype'    (From "Assigned Numbers" RFC)
            //'hlen'     (Hardware address length in octets)
            //'hops'     0
            //'xid'      'xid' from client DHCPREQUEST message
            //'secs'     0
            //'ciaddr'   0
            //'yiaddr'   0
            //'siaddr'   0
            //'flags'    'flags' from client DHCPREQUEST message
            //'giaddr'   'giaddr' from client DHCPREQUEST message
            //'chaddr'   'chaddr' from client DHCPREQUEST message
            //'sname'    (unused)
            //'file'     (unused)
            //'options'  
            DHCPMessage response = new DHCPMessage();
            response.Opcode = DHCPMessage.TOpcode.BootReply;
            response.HardwareType = sourceMsg.HardwareType;
            response.Hops = 0;
            response.XID = sourceMsg.XID;
            response.Secs = 0;
            response.ClientIPAddress = IPAddress.Any;
            response.YourIPAddress = IPAddress.Any;
            response.NextServerIPAddress = IPAddress.Any;
            response.BroadCast = sourceMsg.BroadCast;
            response.RelayAgentIPAddress = sourceMsg.RelayAgentIPAddress;
            response.ClientHardwareAddress = sourceMsg.ClientHardwareAddress;
            response.MessageType = TDHCPMessageType.NAK;
            response.Options.Add(new DHCPOptionServerIdentifier(_socket.LocalEndPoint.Address));
            if(sourceMsg.IsRequestedParameter(TDHCPOption.SubnetMask)) response.Options.Add(new DHCPOptionSubnetMask(this._subnetMask));

            if(!sourceMsg.RelayAgentIPAddress.Equals(IPAddress.Any))
            {
                // If the 'giaddr' field in a DHCP message from a client is non-zero,
                // the server sends any return messages to the 'DHCP server' port on the
                // BOOTP relay agent whose address appears in 'giaddr'.
                await SendMessage(response, new IPEndPoint(sourceMsg.RelayAgentIPAddress, 67));
            }
            else
            {
                // In all cases, when 'giaddr' is zero, the server broadcasts any DHCPNAK
                // messages to 0xffffffff.
                await SendMessage(response, new IPEndPoint(IPAddress.Broadcast, 68));
            }
        }

        private async Task SendACK(DHCPMessage sourceMsg, IPAddress assignedAddress, TimeSpan leaseTime)
        {
            //Field      DHCPACK             
            //-----      -------             
            //'op'       BOOTREPLY           
            //'htype'    (From "Assigned Numbers" RFC)
            //'hlen'     (Hardware address length in octets)
            //'hops'     0                   
            //'xid'      'xid' from client DHCPREQUEST message             
            //'secs'     0                   
            //'ciaddr'   'ciaddr' from DHCPREQUEST or 0
            //'yiaddr'   IP address assigned to client
            //'siaddr'   IP address of next bootstrap server
            //'flags'    'flags' from client DHCPREQUEST message             
            //'giaddr'   'giaddr' from client DHCPREQUEST message             
            //'chaddr'   'chaddr' from client DHCPREQUEST message             
            //'sname'    Server host name or options
            //'file'     Client boot file name or options
            //'options'  options
            DHCPMessage response = new DHCPMessage();
            response.Opcode = DHCPMessage.TOpcode.BootReply;
            response.HardwareType = sourceMsg.HardwareType;
            response.Hops = 0;
            response.XID = sourceMsg.XID;
            response.Secs = 0;
            response.ClientIPAddress = sourceMsg.ClientIPAddress;
            response.YourIPAddress = assignedAddress;
            response.NextServerIPAddress = IPAddress.Any;
            response.BroadCast = sourceMsg.BroadCast;
            response.RelayAgentIPAddress = sourceMsg.RelayAgentIPAddress;
            response.ClientHardwareAddress = sourceMsg.ClientHardwareAddress;
            response.MessageType = TDHCPMessageType.ACK;

            //Option                    DHCPACK            
            //------                    -------            
            //Requested IP address      MUST NOT           : ok
            //IP address lease time     MUST (DHCPREQUEST) : ok
            //Use 'file'/'sname' fields MAY                
            //DHCP message type         DHCPACK            : ok
            //Parameter request list    MUST NOT           : ok
            //Message                   SHOULD             
            //Client identifier         MUST NOT           : ok
            //Vendor class identifier   MAY                
            //Server identifier         MUST               : ok
            //Maximum message size      MUST NOT           : ok  
            //All others                MAY                

            response.Options.Add(new DHCPOptionIPAddressLeaseTime(leaseTime));
            response.Options.Add(new DHCPOptionServerIdentifier(_socket.LocalEndPoint.Address));
            if(sourceMsg.IsRequestedParameter(TDHCPOption.SubnetMask)) response.Options.Add(new DHCPOptionSubnetMask(this._subnetMask));
            AppendConfiguredOptions(sourceMsg, response);
            await SendOfferOrAck(sourceMsg, response);
        }

        private async Task SendINFORMACK(DHCPMessage sourceMsg)
        {
            // The server responds to a DHCPINFORM message by sending a DHCPACK
            // message directly to the address given in the 'ciaddr' field of the
            // DHCPINFORM message.  The server MUST NOT send a lease expiration time
            // to the client and SHOULD NOT fill in 'yiaddr'.  The server includes
            // other parameters in the DHCPACK message as defined in section 4.3.1.

            //Field      DHCPACK             
            //-----      -------             
            //'op'       BOOTREPLY           
            //'htype'    (From "Assigned Numbers" RFC)
            //'hlen'     (Hardware address length in octets)
            //'hops'     0                   
            //'xid'      'xid' from client DHCPREQUEST message             
            //'secs'     0                   
            //'ciaddr'   'ciaddr' from DHCPREQUEST or 0
            //'yiaddr'   IP address assigned to client
            //'siaddr'   IP address of next bootstrap server
            //'flags'    'flags' from client DHCPREQUEST message             
            //'giaddr'   'giaddr' from client DHCPREQUEST message             
            //'chaddr'   'chaddr' from client DHCPREQUEST message             
            //'sname'    Server host name or options
            //'file'     Client boot file name or options
            //'options'  options
            DHCPMessage response = new DHCPMessage();
            response.Opcode = DHCPMessage.TOpcode.BootReply;
            response.HardwareType = sourceMsg.HardwareType;
            response.Hops = 0;
            response.XID = sourceMsg.XID;
            response.Secs = 0;
            response.ClientIPAddress = sourceMsg.ClientIPAddress;
            response.YourIPAddress = IPAddress.Any;
            response.NextServerIPAddress = IPAddress.Any;
            response.BroadCast = sourceMsg.BroadCast;
            response.RelayAgentIPAddress = sourceMsg.RelayAgentIPAddress;
            response.ClientHardwareAddress = sourceMsg.ClientHardwareAddress;
            response.MessageType = TDHCPMessageType.ACK;

            //Option                    DHCPACK            
            //------                    -------            
            //Requested IP address      MUST NOT              : ok
            //IP address lease time     MUST NOT (DHCPINFORM) : ok
            //Use 'file'/'sname' fields MAY                
            //DHCP message type         DHCPACK               : ok
            //Parameter request list    MUST NOT              : ok
            //Message                   SHOULD             
            //Client identifier         MUST NOT              : ok
            //Vendor class identifier   MAY                
            //Server identifier         MUST                  : ok
            //Maximum message size      MUST NOT              : ok
            //All others                MAY                

            response.Options.Add(new DHCPOptionServerIdentifier(_socket.LocalEndPoint.Address));
            if(sourceMsg.IsRequestedParameter(TDHCPOption.SubnetMask)) response.Options.Add(new DHCPOptionSubnetMask(this._subnetMask));
            AppendConfiguredOptions(sourceMsg, response);
            await SendMessage(response, new IPEndPoint(sourceMsg.ClientIPAddress, 68));
        }

        private async Task SendOfferOrAck(DHCPMessage request, DHCPMessage response)
        {
            // RFC2131.txt, 4.1, paragraph 4

            // DHCP messages broadcast by a client prior to that client obtaining
            // its IP address must have the source address field in the IP header
            // set to 0.

            if(!request.RelayAgentIPAddress.Equals(IPAddress.Any))
            {
                // If the 'giaddr' field in a DHCP message from a client is non-zero,
                // the server sends any return messages to the 'DHCP server' port on the
                // BOOTP relay agent whose address appears in 'giaddr'.
                await SendMessage(response, new IPEndPoint(request.RelayAgentIPAddress, 67));
            }
            else
            {
                if(!request.ClientIPAddress.Equals(IPAddress.Any))
                {
                    // If the 'giaddr' field is zero and the 'ciaddr' field is nonzero, then the server
                    // unicasts DHCPOFFER and DHCPACK messages to the address in 'ciaddr'.
                    await SendMessage(response, new IPEndPoint(request.ClientIPAddress, 68));
                }
                else
                {
                    // If 'giaddr' is zero and 'ciaddr' is zero, and the broadcast bit is
                    // set, then the server broadcasts DHCPOFFER and DHCPACK messages to
                    // 0xffffffff. If the broadcast bit is not set and 'giaddr' is zero and
                    // 'ciaddr' is zero, then the server unicasts DHCPOFFER and DHCPACK
                    // messages to the client's hardware address and 'yiaddr' address.  
                    await SendMessage(response, new IPEndPoint(IPAddress.Broadcast, 68));
                }
            }
        }

        private bool ServerIdentifierPrecondition(DHCPMessage msg)
        {
            bool result = false;
            var dhcpOptionServerIdentifier = msg.FindOption<DHCPOptionServerIdentifier>();

            if(dhcpOptionServerIdentifier != null)
            {
                if(dhcpOptionServerIdentifier.IPAddress.Equals(EndPoint.Address))
                {
                    result = true;
                }
                else
                {
                    Trace($"Client sent message with non-matching server identifier '{dhcpOptionServerIdentifier.IPAddress}' -> ignored");
                }
            }
            else
            {
                Trace("Client sent message without filling required ServerIdentifier option -> ignored");
            }
            return result;
        }

        private bool IsIPAddressInRange(IPAddress address, IPAddress start, IPAddress end)
        {
            var adr32 = Utils.IPAddressToUInt32(address);
            return adr32 >= Utils.IPAddressToUInt32(SanitizeHostRange(start)) && adr32 <= Utils.IPAddressToUInt32(SanitizeHostRange(end));
        }

        /// <summary>
        /// Checks whether the given IP address falls within the known pool ranges.
        /// </summary>
        /// <param name="address">IP address to check</param>
        /// <returns>true when the ip address matches one of the known pool ranges</returns>
        private bool IsIPAddressInPoolRange(IPAddress address)
        {
            return IsIPAddressInRange(address, _poolStart, _poolEnd) || _reservations.Any(r => IsIPAddressInRange(address, r.PoolStart, r.PoolEnd));
        }

        private bool IPAddressIsInSubnet(IPAddress address)
        {
            return ((Utils.IPAddressToUInt32(address) & Utils.IPAddressToUInt32(_subnetMask)) == (Utils.IPAddressToUInt32(_endPoint.Address) & Utils.IPAddressToUInt32(_subnetMask)));
        }

        private bool IPAddressIsFree(IPAddress address, bool reuseReleased)
        {
            if(!IPAddressIsInSubnet(address)) return false;
            if(address.Equals(_endPoint.Address)) return false;
            foreach(DHCPClient client in _clients.Keys)
            {
                if(client.IPAddress.Equals(address))
                {
                    if(reuseReleased && client.State == DHCPClient.TState.Released)
                    {
                        client.IPAddress = IPAddress.Any;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private IPAddress SanitizeHostRange(IPAddress startend)
        {
            return Utils.UInt32ToIPAddress(
                (Utils.IPAddressToUInt32(_endPoint.Address) & Utils.IPAddressToUInt32(_subnetMask)) |
                (Utils.IPAddressToUInt32(startend) & ~Utils.IPAddressToUInt32(_subnetMask))
            );
        }

        private IPAddress AllocateIPAddress(DHCPMessage dhcpMessage)
        {
            var dhcpOptionRequestedIPAddress = dhcpMessage.FindOption<DHCPOptionRequestedIPAddress>();

            var reservation = _reservations.FirstOrDefault(x => x.Match(dhcpMessage));

            if(reservation != null)
            {
                // the client matches a reservation.. find the first free address in the reservation block
                for(UInt32 host = Utils.IPAddressToUInt32(SanitizeHostRange(reservation.PoolStart)); host <= Utils.IPAddressToUInt32(SanitizeHostRange(reservation.PoolEnd)); host++)
                {
                    IPAddress testIPAddress = Utils.UInt32ToIPAddress(host);
                    // I don't see the point of avoiding released addresses for reservations (yet)
                    if(IPAddressIsFree(testIPAddress, true))
                    {
                        return testIPAddress;
                    }
                    else if(reservation.Preempt)
                    {
                        // if Preempt is true, return the first address of the reservation range. Preempt should ONLY ever be used if the range is a single address, and you're 100% sure you'll 
                        // _always_ have just a single device in your network that matches the reservation MAC or name.
                        return testIPAddress;
                    }
                }
            }

            if(dhcpOptionRequestedIPAddress != null)
            {
                // there is a requested IP address. Is it in our subnet and free?
                if(IPAddressIsFree(dhcpOptionRequestedIPAddress.IPAddress, true))
                {
                    // yes, the requested address is ok
                    return dhcpOptionRequestedIPAddress.IPAddress;
                }
            }

            // first try to find a free address without reusing released ones
            for(UInt32 host = Utils.IPAddressToUInt32(SanitizeHostRange(_poolStart)); host <= Utils.IPAddressToUInt32(SanitizeHostRange(_poolEnd)); host++)
            {
                IPAddress testIPAddress = Utils.UInt32ToIPAddress(host);
                if(IPAddressIsFree(testIPAddress, false))
                {
                    return testIPAddress;
                }
            }

            // nothing found.. now start allocating released ones as well
            for(UInt32 host = Utils.IPAddressToUInt32(SanitizeHostRange(_poolStart)); host <= Utils.IPAddressToUInt32(SanitizeHostRange(_poolEnd)); host++)
            {
                IPAddress testIPAddress = Utils.UInt32ToIPAddress(host);
                if(IPAddressIsFree(testIPAddress, true))
                {
                    return testIPAddress;
                }
            }

            // still nothing: report failure
            return IPAddress.Any;
        }

        private async Task OfferClient(DHCPMessage dhcpMessage, DHCPClient client)
        {
            client.State = DHCPClient.TState.Offered;
            client.OfferedTime = DateTime.Now;
            _ = _clients.TryAdd(client, client);
            await SendOFFER(dhcpMessage, client.IPAddress, _leaseTime);
        }

        private async Task MainTask(CancellationToken cancellationToken)
        {
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            Task<(IPEndPoint,ReadOnlyMemory<byte>)>? receiveTask = default;
            Task? timerTask = default;

            while(!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    receiveTask ??= _socket.ReceiveAsync(cancellationToken);
                    timerTask ??= timer.WaitForNextTickAsync(cancellationToken).AsTask();

                    var completedTask = await Task.WhenAny(receiveTask, timerTask);
                    if(completedTask == receiveTask)
                    {
                        try
                        {
                            (var ipEndPoint, var data) = await receiveTask;
                            await OnReceive(ipEndPoint, data);
                        }
                        catch(UDPSocketException ex)
                        {
                            if(ex.IsFatal)
                            {
                                _logger?.LogError(ex, $"fatal exception in {nameof(MainTask)}");
                                throw;
                            }
                        }
                        finally
                        {
                            receiveTask = null;
                        }
                    }
                    else if(completedTask == timerTask)
                    {
                        try
                        {
                            await timerTask;
                            CheckLeaseExpiration();
                        }
                        finally
                        {
                            timerTask = null;
                        }
                    }
                }
                catch(OperationCanceledException)
                {
                    // Cancelled out next loop.
                }
            }
        }

        private DHCPClient? GetKnownClient(DHCPClient client)
        {
            return _clients.GetValueOrDefault(client);
        }

        private async Task OnReceive(IPEndPoint endPoint, ReadOnlyMemory<byte> data)
        {
            try
            {
                Trace("Incoming packet - parsing DHCP Message");

                // translate array segment into a DHCPMessage
                DHCPMessage dhcpMessage = DHCPMessage.FromStream(new MemoryStream(data.ToArray()));
                Trace(Utils.PrefixLines(dhcpMessage.ToString(), "c->s "));

                // only react to messages from client to server. Ignore other types.
                if(dhcpMessage.Opcode == DHCPMessage.TOpcode.BootRequest)
                {
                    DHCPClient client = DHCPClient.CreateFromMessage(dhcpMessage);
                    Trace($"Client {client} sent {dhcpMessage.MessageType}");
                    // is it a known client?
                    DHCPClient? knownClient = GetKnownClient(client);

                    switch(dhcpMessage.MessageType)
                    {
                        // DHCPDISCOVER - client to server
                        // broadcast to locate available servers
                        case TDHCPMessageType.DISCOVER:

                            if(knownClient != null)
                            {
                                Trace($"Client is known, in state {knownClient.State}");

                                if(knownClient.State == DHCPClient.TState.Offered || knownClient.State == DHCPClient.TState.Assigned)
                                {
                                    Trace("Client sent DISCOVER but we already offered, or assigned -> repeat offer with known address");
                                    await OfferClient(dhcpMessage, knownClient);
                                }
                                else
                                {
                                    Trace("Client is known but released");
                                    // client is known but released or dropped. Use the old address or allocate a new one
                                    if(knownClient.IPAddress.Equals(IPAddress.Any))
                                    {
                                        knownClient.IPAddress = AllocateIPAddress(dhcpMessage);
                                        if(!knownClient.IPAddress.Equals(IPAddress.Any))
                                        {
                                            await OfferClient(dhcpMessage, knownClient);
                                        }
                                        else
                                        {
                                            Trace("No more free addresses. Don't respond to discover");
                                        }
                                    }
                                    else
                                    {
                                        await OfferClient(dhcpMessage, knownClient);
                                    }
                                }
                            }
                            else
                            {
                                Trace("Client is not known yet");
                                // client is not known yet.
                                // allocate new address, add client to client table in Offered state
                                client.IPAddress = AllocateIPAddress(dhcpMessage);
                                // allocation ok ?
                                if(!client.IPAddress.Equals(IPAddress.Any))
                                {
                                    await OfferClient(dhcpMessage, client);
                                }
                                else
                                {
                                    Trace("No more free addresses. Don't respond to discover");
                                }
                            }
                            break;

                        // DHCPREQUEST - client to server
                        // Client message to servers either 
                        // (a) requesting offered parameters from one server and implicitly declining offers from all others.
                        // (b) confirming correctness of previously allocated address after e.g. system reboot, or
                        // (c) extending the lease on a particular network address
                        case TDHCPMessageType.REQUEST:

                            // is there a server identifier?
                            var dhcpOptionServerIdentifier = dhcpMessage.FindOption<DHCPOptionServerIdentifier>();
                            var dhcpOptionRequestedIPAddress = dhcpMessage.FindOption<DHCPOptionRequestedIPAddress>();

                            if(dhcpOptionServerIdentifier != null)
                            {
                                // there is a server identifier: the message is in response to a DHCPOFFER
                                if(dhcpOptionServerIdentifier.IPAddress.Equals(_endPoint.Address))
                                {
                                    // it's a response to OUR offer.
                                    // but did we actually offer one?
                                    if(knownClient != null && knownClient.State == DHCPClient.TState.Offered)
                                    {
                                        // yes.
                                        // the requested IP address MUST be filled in with the offered address
                                        if(dhcpOptionRequestedIPAddress != null)
                                        {
                                            if(knownClient.IPAddress.Equals(dhcpOptionRequestedIPAddress.IPAddress))
                                            {
                                                Trace("Client request matches offered address -> ACK");
                                                knownClient.State = DHCPClient.TState.Assigned;
                                                knownClient.LeaseStartTime = DateTime.Now;
                                                knownClient.LeaseDuration = _leaseTime;
                                                await SendACK(dhcpMessage, knownClient.IPAddress, knownClient.LeaseDuration);
                                            }
                                            else
                                            {
                                                Trace($"Client sent request for IP address '{dhcpOptionRequestedIPAddress.IPAddress}', but it does not match the offered address '{knownClient.IPAddress}' -> NAK");
                                                await SendNAK(dhcpMessage);
                                                RemoveClient(knownClient);
                                            }
                                        }
                                        else
                                        {
                                            Trace("Client sent request without filling the RequestedIPAddress option -> NAK");
                                            await SendNAK(dhcpMessage);
                                            RemoveClient(knownClient);
                                        }
                                    }
                                    else
                                    {
                                        // we don't have an outstanding offer!
                                        Trace("Client requested IP address from this server, but we didn't offer any. -> NAK");
                                        await SendNAK(dhcpMessage);
                                    }
                                }
                                else
                                {
                                    Trace($"Client requests IP address that was offered by another DHCP server at '{dhcpOptionServerIdentifier.IPAddress}' -> drop offer");
                                    // it's a response to another DHCP server.
                                    // if we sent an OFFER to this client earlier, remove it.
                                    if(knownClient != null)
                                    {
                                        RemoveClient(knownClient);
                                    }
                                }
                            }
                            else
                            {
                                // no server identifier: the message is a request to verify or extend an existing lease
                                // Received REQUEST without server identifier, client is INIT-REBOOT, RENEWING or REBINDING

                                Trace("Received REQUEST without server identifier, client state is INIT-REBOOT, RENEWING or REBINDING");

                                if(!dhcpMessage.ClientIPAddress.Equals(IPAddress.Any))
                                {
                                    Trace("REQUEST client IP is filled in -> client state is RENEWING or REBINDING");

                                    // see : http://www.tcpipguide.com/free/t_DHCPLeaseRenewalandRebindingProcesses-2.htm

                                    if(knownClient != null &&
                                        knownClient.State == DHCPClient.TState.Assigned &&
                                        knownClient.IPAddress.Equals(dhcpMessage.ClientIPAddress))
                                    {
                                        // known, assigned, and IP address matches administration. ACK
                                        knownClient.LeaseStartTime = DateTime.Now;
                                        knownClient.LeaseDuration = _leaseTime;
                                        await SendACK(dhcpMessage, dhcpMessage.ClientIPAddress, knownClient.LeaseDuration);
                                    }
                                    else
                                    {
                                        // not known, or known but in some other state. Just dump the old one.
                                        if(knownClient != null) RemoveClient(knownClient);

                                        // check if client IP address is marked free
                                        if(IPAddressIsFree(dhcpMessage.ClientIPAddress, false))
                                        {
                                            // it's free. send ACK
                                            client.IPAddress = dhcpMessage.ClientIPAddress;
                                            client.State = DHCPClient.TState.Assigned;
                                            client.LeaseStartTime = DateTime.Now;
                                            client.LeaseDuration = _leaseTime;
                                            _=_clients.TryAdd(client, client);
                                            await SendACK(dhcpMessage, dhcpMessage.ClientIPAddress, client.LeaseDuration);
                                        }
                                        else
                                        {
                                            Trace("Renewing client IP address already in use. Oops..");
                                        }
                                    }
                                }
                                else
                                {
                                    Trace("REQUEST client IP is empty -> client state is INIT-REBOOT");

                                    if(dhcpOptionRequestedIPAddress != null)
                                    {
                                        if(knownClient != null &&
                                            knownClient.State == DHCPClient.TState.Assigned)
                                        {
                                            if(knownClient.IPAddress.Equals(dhcpOptionRequestedIPAddress.IPAddress))
                                            {
                                                Trace("Client request matches cached address -> ACK");
                                                // known, assigned, and IP address matches administration. ACK
                                                knownClient.LeaseStartTime = DateTime.Now;
                                                knownClient.LeaseDuration = _leaseTime;
                                                await SendACK(dhcpMessage, dhcpOptionRequestedIPAddress.IPAddress, knownClient.LeaseDuration);
                                            }
                                            else
                                            {
                                                Trace($"Client sent request for IP address '{dhcpOptionRequestedIPAddress.IPAddress}', but it does not match cached address '{knownClient.IPAddress}' -> NAK");
                                                await SendNAK(dhcpMessage);
                                                RemoveClient(knownClient);
                                            }
                                        }
                                        else
                                        {
                                            // client not known, or known but in some other state.
                                            // send NAK so client will drop to INIT state where it can acquire a new lease.
                                            // see also: http://tcpipguide.com/free/t_DHCPGeneralOperationandClientFiniteStateMachine.htm
                                            Trace("Client attempted INIT-REBOOT REQUEST but server has no lease for this client -> NAK");
                                            await SendNAK(dhcpMessage);
                                            if(knownClient != null) RemoveClient(knownClient);
                                        }
                                    }
                                    else
                                    {
                                        Trace("Client sent apparent INIT-REBOOT REQUEST but with an empty 'RequestedIPAddress' option. Oops..");
                                    }
                                }
                            }
                            break;

                        case TDHCPMessageType.DECLINE:
                            // If the server receives a DHCPDECLINE message, the client has
                            // discovered through some other means that the suggested network
                            // address is already in use.  The server MUST mark the network address
                            // as not available and SHOULD notify the local system administrator of
                            // a possible configuration problem.
                            if(ServerIdentifierPrecondition(dhcpMessage))
                            {
                                if(knownClient != null)
                                {
                                    Trace("Found client in client table, removing.");
                                    RemoveClient(client);

                                    /*
                                        // the network address that should be marked as not available MUST be 
                                        // specified in the RequestedIPAddress option.                                        
                                        DHCPOptionRequestedIPAddress dhcpOptionRequestedIPAddress = (DHCPOptionRequestedIPAddress)dhcpMessage.GetOption(TDHCPOption.RequestedIPAddress);
                                        if(dhcpOptionRequestedIPAddress!=null)
                                        {
                                            if(dhcpOptionRequestedIPAddress.IPAddress.Equals(knownClient.IPAddress))
                                            {
                                                // TBD add IP address to exclusion list. or something.
                                            }
                                        }
                                        */
                                }
                                else
                                {
                                    Trace("Client not found in client table -> decline ignored.");
                                }
                            }
                            break;

                        case TDHCPMessageType.RELEASE:
                            // relinguishing network address and cancelling remaining lease.
                            // Upon receipt of a DHCPRELEASE message, the server marks the network
                            // address as not allocated.  The server SHOULD retain a record of the
                            // client's initialization parameters for possible reuse in response to
                            // subsequent requests from the client.
                            if(ServerIdentifierPrecondition(dhcpMessage))
                            {
                                if(knownClient != null /* && knownClient.State == DHCPClient.TState.Assigned */ )
                                {
                                    if(dhcpMessage.ClientIPAddress.Equals(knownClient.IPAddress))
                                    {
                                        Trace("Found client in client table, marking as released");
                                        knownClient.State = DHCPClient.TState.Released;
                                    }
                                    else
                                    {
                                        Trace("IP address in RELEASE doesn't match known client address. Mark this client as released with unknown IP");
                                        knownClient.IPAddress = IPAddress.Any;
                                        knownClient.State = DHCPClient.TState.Released;
                                    }
                                }
                                else
                                {
                                    Trace("Client not found in client table, release ignored.");
                                }
                            }
                            break;

                        // DHCPINFORM - client to server
                        // client asking for local configuration parameters, client already has externally configured
                        // network address.
                        case TDHCPMessageType.INFORM:
                            // The server responds to a DHCPINFORM message by sending a DHCPACK
                            // message directly to the address given in the 'ciaddr' field of the
                            // DHCPINFORM message.  The server MUST NOT send a lease expiration time
                            // to the client and SHOULD NOT fill in 'yiaddr'.  The server includes
                            // other parameters in the DHCPACK message as defined in section 4.3.1.
                            await SendINFORMACK(dhcpMessage);
                            break;

                        default:
                            Trace("Invalid message from client, ignored");
                            break;
                    }

                    HandleStatusChange(null);
                }
            }
            catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                System.Diagnostics.Debug.WriteLine(e.StackTrace);
            }
        }
    }
}
