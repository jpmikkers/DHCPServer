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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Net.Configuration;
using System.Threading;
using System.Linq;

namespace CodePlex.JPMikkers.DHCP
{
    public class DHCPServer : IDHCPServer
    {
        private const int ClientInformationWriteRetries = 10;

        private object m_Sync = new object();
        private IPEndPoint m_EndPoint = new IPEndPoint(IPAddress.Loopback,67);
        private UDPSocket m_Socket;
        private IPAddress m_SubnetMask = IPAddress.Any;
        private IPAddress m_PoolStart = IPAddress.Any;
        private IPAddress m_PoolEnd = IPAddress.Broadcast;

        private string m_ClientInfoPath;
        private string m_HostName;
        private Dictionary<DHCPClient,DHCPClient> m_Clients = new Dictionary<DHCPClient, DHCPClient>();
        private Timer m_Timer;
        private TimeSpan m_OfferExpirationTime = TimeSpan.FromSeconds(30.0);
        private TimeSpan m_LeaseTime = TimeSpan.FromDays(1);
        private bool m_Active = false;
        private List<OptionItem> m_Options = new List<OptionItem>();
        private List<ReservationItem> m_Reservations = new List<ReservationItem>();
        private int m_MinimumPacketSize = 576;
        private AutoPumpQueue<int> m_UpdateClientInfoQueue;
        private Random m_Random = new Random();

        #region IDHCPServer Members

        public event EventHandler<DHCPTraceEventArgs> OnTrace = delegate(object sender, DHCPTraceEventArgs args) { };
        public event EventHandler<DHCPStopEventArgs> OnStatusChange = delegate(object sender, DHCPStopEventArgs args) { };

        public IPEndPoint EndPoint
        {
            get
            {
                return m_EndPoint;
            }
            set
            {
                m_EndPoint = value;
            }
        }

        public IPAddress SubnetMask
        {
            get
            {
                return m_SubnetMask;
            }
            set
            {
                m_SubnetMask = value;
            }
        }

        public IPAddress PoolStart
        {
            get
            {
                return m_PoolStart;
            }
            set
            {
                m_PoolStart = value;
            }
        }

        public IPAddress PoolEnd
        {
            get
            {
                return m_PoolEnd;
            }
            set
            {
                m_PoolEnd = value;
            }
        }

        public TimeSpan OfferExpirationTime
        {
            get
            {
                return m_OfferExpirationTime;
            }
            set
            {
                m_OfferExpirationTime = value;
            }
        }

        public TimeSpan LeaseTime
        {
            get
            {
                return m_LeaseTime;
            }
            set
            {
                // sanitize timespan.
                m_LeaseTime = Utils.SanitizeTimeSpan(value);
            }
        }

        public int MinimumPacketSize
        {
            get
            {
                return m_MinimumPacketSize;
            }
            set
            {
                m_MinimumPacketSize = Math.Max(value,312);
            }
        }


        public string HostName
        {
            get
            {
                return m_HostName;
            }
        }

        public IList<DHCPClient> Clients
        {
            get
            {
                lock(m_Clients)
                {
                    List<DHCPClient> clients = new List<DHCPClient>();
                    foreach(DHCPClient client in m_Clients.Values)
                    {
                        clients.Add(client.Clone());
                    }
                    return clients;
                }
            }
        }

        public bool Active
        {
            get
            {
                lock (m_Sync)
                {
                    return m_Active;
                }
            }
        }

        public List<OptionItem> Options
        {
            get
            {
                return m_Options;
            }
            set
            {
                m_Options = value;
            }
        }

        public List<ReservationItem> Reservations
        {
            get
            {
                return m_Reservations;
            }
            set
            {
                m_Reservations = value;
            }
        }

        private void OnUpdateClientInfo(AutoPumpQueue<int> sender, int data)
        {
            if (Active)
            {
                try
                {
                    DHCPClientInformation clientInformation = new DHCPClientInformation();

                    foreach (DHCPClient client in Clients)
                    {
                        clientInformation.Clients.Add(client);
                    }

                    for (int t = 0; t < ClientInformationWriteRetries; t++)
                    {
                        try
                        {
                            clientInformation.Write(m_ClientInfoPath);
                            break;
                        }
                        catch
                        {
                        }

                        if (t < ClientInformationWriteRetries)
                        {
                            Thread.Sleep(m_Random.Next(500, 1000));
                        }
                        else
                        {
                            Trace("Could not update client information, data might be stale");
                        }
                    }
                }
                catch (Exception e)
                {
                    Trace(string.Format("Exception in OnUpdateClientInfo : {0}", e.ToString()));
                }
            }
        }

        public DHCPServer(string clientInfoPath)
        {
            m_UpdateClientInfoQueue = new AutoPumpQueue<int>(OnUpdateClientInfo);
            m_ClientInfoPath = clientInfoPath;
            m_HostName = System.Environment.MachineName;

            try
            {
                DHCPClientInformation clientInformation = DHCPClientInformation.Read(m_ClientInfoPath);

                foreach(DHCPClient client in clientInformation.Clients)
                {
                    // Forget about offered clients.
                    if (client.State != DHCPClient.TState.Offered)
                    {
                        m_Clients.Add(client, client);
                    }
                }
            }
            catch(Exception)
            {                
            }
        }

        public void Start()
        {
            lock (m_Sync)
            {
                if (!m_Active)
                {
                    try
                    {
                        Trace(string.Format("Starting DHCP server '{0}'", m_EndPoint));
                        m_Active = true;
                        m_Socket = new UDPSocket(m_EndPoint, 2048, true, 10, OnReceive, OnStop);
                        m_Timer = new Timer(new TimerCallback(OnTimer), null, TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0));
                        Trace("DHCP Server start succeeded");
                    }
                    catch (Exception e)
                    {
                        Trace(string.Format("DHCP Server start failed, reason '{0}'", e));
                        m_Active = false;
                        throw;
                    }
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
            if (disposing)
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

        private void HandleStatusChange(DHCPStopEventArgs data)
        {
            m_UpdateClientInfoQueue.Enqueue(0);
            OnStatusChange(this, data);            
        }

        internal void Trace(string msg)
        {
            DHCPTraceEventArgs data = new DHCPTraceEventArgs();
            data.Message = msg;
            OnTrace(this, data);
        }

        private void Stop(Exception reason)
        {
            bool notify = false;

            lock (m_Sync)
            {
                if (m_Active)
                {
                    Trace(string.Format("Stopping DHCP server '{0}'", m_EndPoint));
                    m_Active = false;
                    notify = true;
                    m_Socket.Dispose();
                    Trace("Stopped");
                }
            }

            if (notify)
            {
                DHCPStopEventArgs data = new DHCPStopEventArgs();
                data.Reason = reason;
                HandleStatusChange(data);
            }
        }

        private void OnTimer(object state)
        {
            bool modified = false;

            lock (m_Clients)
            {
                List<DHCPClient> clientsToRemove = new List<DHCPClient>();
                foreach (DHCPClient client in m_Clients.Keys)
                {
                    if (client.State == DHCPClient.TState.Offered && (DateTime.Now - client.OfferedTime) > m_OfferExpirationTime)
                    {
                        clientsToRemove.Add(client);
                    }
                    else if (client.State == DHCPClient.TState.Assigned && (DateTime.Now > client.LeaseEndTime))
                    {
                        // lease expired. remove client
                        clientsToRemove.Add(client);
                    }
                }

                foreach (DHCPClient client in clientsToRemove)
                {
                    m_Clients.Remove(client);
                    modified = true;
                }
            }

            if (modified)
            {
                HandleStatusChange(null);
            }
        }

        private void RemoveClient(DHCPClient client)
        {
            lock (m_Clients)
            {
                if (m_Clients.Remove(client))
                {
                    Trace(string.Format("Removed client '{0}' from client table", client));
                }
            }
        }

        private void SendMessage(DHCPMessage msg, IPEndPoint endPoint)
        {
            Trace(string.Format("==== Sending response to {0} ====", endPoint));
            Trace(Utils.PrefixLines(msg.ToString(), "s->c "));
            MemoryStream m = new MemoryStream();
            msg.ToStream(m, m_MinimumPacketSize);
            m_Socket.Send(endPoint, new ArraySegment<byte>(m.ToArray()));
        }

        private void AppendConfiguredOptions(DHCPMessage sourceMsg,DHCPMessage targetMsg)
        {
            foreach (OptionItem optionItem in m_Options)
            {
                if (optionItem.Mode == OptionMode.Force || sourceMsg.IsRequestedParameter(optionItem.Option.OptionType))
                {
                    if(targetMsg.GetOption(optionItem.Option.OptionType)==null)
                    {
                        targetMsg.Options.Add(optionItem.Option);
                    }
                }
            }
        }

        private void SendOFFER(DHCPMessage sourceMsg, IPAddress offeredAddress, TimeSpan leaseTime)
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
            response.Options.Add(new DHCPOptionServerIdentifier(((IPEndPoint)m_Socket.LocalEndPoint).Address));
            if (sourceMsg.IsRequestedParameter(TDHCPOption.SubnetMask)) response.Options.Add(new DHCPOptionSubnetMask(this.m_SubnetMask));
            AppendConfiguredOptions(sourceMsg, response);
            SendOfferOrAck(sourceMsg, response);
        }

        private void SendNAK(DHCPMessage sourceMsg)
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
            response.Options.Add(new DHCPOptionServerIdentifier(((IPEndPoint)m_Socket.LocalEndPoint).Address));
            if (sourceMsg.IsRequestedParameter(TDHCPOption.SubnetMask)) response.Options.Add(new DHCPOptionSubnetMask(this.m_SubnetMask));

            if (!sourceMsg.RelayAgentIPAddress.Equals(IPAddress.Any))
            {
                // If the 'giaddr' field in a DHCP message from a client is non-zero,
                // the server sends any return messages to the 'DHCP server' port on the
                // BOOTP relay agent whose address appears in 'giaddr'.
                SendMessage(response, new IPEndPoint(sourceMsg.RelayAgentIPAddress, 67));
            }
            else
            {
                // In all cases, when 'giaddr' is zero, the server broadcasts any DHCPNAK
                // messages to 0xffffffff.
                SendMessage(response, new IPEndPoint(IPAddress.Broadcast,68));
            }
        }

        private void SendACK(DHCPMessage sourceMsg, IPAddress assignedAddress, TimeSpan leaseTime)
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
            response.Options.Add(new DHCPOptionServerIdentifier(((IPEndPoint)m_Socket.LocalEndPoint).Address));
            if (sourceMsg.IsRequestedParameter(TDHCPOption.SubnetMask)) response.Options.Add(new DHCPOptionSubnetMask(this.m_SubnetMask));
            AppendConfiguredOptions(sourceMsg, response);
            SendOfferOrAck(sourceMsg, response);
        }

        private void SendINFORMACK(DHCPMessage sourceMsg)
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

            response.Options.Add(new DHCPOptionServerIdentifier(((IPEndPoint)m_Socket.LocalEndPoint).Address));
            if (sourceMsg.IsRequestedParameter(TDHCPOption.SubnetMask)) response.Options.Add(new DHCPOptionSubnetMask(this.m_SubnetMask));
            AppendConfiguredOptions(sourceMsg, response);
            SendMessage(response, new IPEndPoint(sourceMsg.ClientIPAddress, 68));
        }

        private void SendOfferOrAck(DHCPMessage request, DHCPMessage response)
        {
            // RFC2131.txt, 4.1, paragraph 4

            // DHCP messages broadcast by a client prior to that client obtaining
            // its IP address must have the source address field in the IP header
            // set to 0.

            if (!request.RelayAgentIPAddress.Equals(IPAddress.Any))
            {
                // If the 'giaddr' field in a DHCP message from a client is non-zero,
                // the server sends any return messages to the 'DHCP server' port on the
                // BOOTP relay agent whose address appears in 'giaddr'.
                SendMessage(response, new IPEndPoint(request.RelayAgentIPAddress, 67));
            }
            else
            {
                if (!request.ClientIPAddress.Equals(IPAddress.Any))
                {
                    // If the 'giaddr' field is zero and the 'ciaddr' field is nonzero, then the server
                    // unicasts DHCPOFFER and DHCPACK messages to the address in 'ciaddr'.
                    SendMessage(response, new IPEndPoint(request.ClientIPAddress, 68));
                }
                else
                {
                    // If 'giaddr' is zero and 'ciaddr' is zero, and the broadcast bit is
                    // set, then the server broadcasts DHCPOFFER and DHCPACK messages to
                    // 0xffffffff. If the broadcast bit is not set and 'giaddr' is zero and
                    // 'ciaddr' is zero, then the server unicasts DHCPOFFER and DHCPACK
                    // messages to the client's hardware address and 'yiaddr' address.  
                    SendMessage(response, new IPEndPoint(IPAddress.Broadcast, 68));
                }
            }
        }

        private bool ServerIdentifierPrecondition(DHCPMessage msg)
        {
            bool result = false;
            DHCPOptionServerIdentifier dhcpOptionServerIdentifier = (DHCPOptionServerIdentifier)msg.GetOption(TDHCPOption.ServerIdentifier);

            if (dhcpOptionServerIdentifier != null)
            {
                if (dhcpOptionServerIdentifier.IPAddress.Equals(EndPoint.Address))
                {
                    result = true;
                }
                else
                {
                    Trace(string.Format("Client sent message with non-matching server identifier '{0}' -> ignored", dhcpOptionServerIdentifier.IPAddress));
                }
            }
            else
            {
                Trace("Client sent message without filling required ServerIdentifier option -> ignored");
            }
            return result;
        }

        private bool IPAddressIsInSubnet(IPAddress address)
        {
            return ((Utils.IPAddressToUInt32(address) & Utils.IPAddressToUInt32(m_SubnetMask)) == (Utils.IPAddressToUInt32(m_EndPoint.Address) & Utils.IPAddressToUInt32(m_SubnetMask)));
        }

        private bool IPAddressIsFree(IPAddress address, bool reuseReleased)
        {
            if (!IPAddressIsInSubnet(address)) return false;
            if (address.Equals(m_EndPoint.Address)) return false;
            foreach(DHCPClient client in m_Clients.Keys)
            {
                if(client.IPAddress.Equals(address))
                {
                    if (reuseReleased && client.State == DHCPClient.TState.Released)
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
                (Utils.IPAddressToUInt32(m_EndPoint.Address) & Utils.IPAddressToUInt32(m_SubnetMask)) |
                (Utils.IPAddressToUInt32(startend) & ~Utils.IPAddressToUInt32(m_SubnetMask))
            );
        }

        private IPAddress AllocateIPAddress(DHCPMessage dhcpMessage)
        {
            DHCPOptionRequestedIPAddress dhcpOptionRequestedIPAddress = (DHCPOptionRequestedIPAddress)dhcpMessage.GetOption(TDHCPOption.RequestedIPAddress);

            var reservation = m_Reservations.FirstOrDefault(x => x.Match(dhcpMessage));

            if (reservation != null)
            {
                // the client matches a reservation.. find the first free address in the reservation block
                for (UInt32 host = Utils.IPAddressToUInt32(SanitizeHostRange(reservation.PoolStart)); host <= Utils.IPAddressToUInt32(SanitizeHostRange(reservation.PoolEnd)); host++)
                {
                    IPAddress testIPAddress = Utils.UInt32ToIPAddress(host);
                    // I don't see the point of avoiding released addresses for reservations (yet)
                    if (IPAddressIsFree(testIPAddress, true))
                    {
                        return testIPAddress;
                    }
                }
            }

            if(dhcpOptionRequestedIPAddress!=null)
            {
                // there is a requested IP address. Is it in our subnet and free?
                if(IPAddressIsFree(dhcpOptionRequestedIPAddress.IPAddress,true))
                {
                    // yes, the requested address is ok
                    return dhcpOptionRequestedIPAddress.IPAddress;
                }
            }

            // first try to find a free address without reusing released ones
            for(UInt32 host = Utils.IPAddressToUInt32(SanitizeHostRange(m_PoolStart)); host <= Utils.IPAddressToUInt32(SanitizeHostRange(m_PoolEnd)); host++)
            {
                IPAddress testIPAddress = Utils.UInt32ToIPAddress(host);
                if(IPAddressIsFree(testIPAddress,false))
                {
                    return testIPAddress;
                }
            }

            // nothing found.. now start allocating released ones as well
            for (UInt32 host = Utils.IPAddressToUInt32(SanitizeHostRange(m_PoolStart)); host <= Utils.IPAddressToUInt32(SanitizeHostRange(m_PoolEnd)); host++)
            {
                IPAddress testIPAddress = Utils.UInt32ToIPAddress(host);
                if (IPAddressIsFree(testIPAddress, true))
                {
                    return testIPAddress;
                }
            }

            // still nothing: report failure
            return IPAddress.Any;
        }

        private void OfferClient(DHCPMessage dhcpMessage, DHCPClient client)
        {
            lock (m_Clients)
            {
                client.State = DHCPClient.TState.Offered;
                client.OfferedTime = DateTime.Now;
                if (!m_Clients.ContainsKey(client)) m_Clients.Add(client, client);
                SendOFFER(dhcpMessage, client.IPAddress, m_LeaseTime);
            }
        }

        private void OnReceive(UDPSocket sender, IPEndPoint endPoint, ArraySegment<byte> data)
        {
            try
            {
                Trace("Incoming packet - parsing DHCP Message");

                // translate array segment into a DHCPMessage
                DHCPMessage dhcpMessage = DHCPMessage.FromStream(new MemoryStream(data.Array, data.Offset, data.Count, false, false));
                Trace(Utils.PrefixLines(dhcpMessage.ToString(),"c->s "));

                // only react to messages from client to server. Ignore other types.
                if (dhcpMessage.Opcode == DHCPMessage.TOpcode.BootRequest)
                {
                    DHCPClient client = DHCPClient.CreateFromMessage(dhcpMessage);
                    Trace(string.Format("Client {0} sent {1}", client, dhcpMessage.MessageType));

                    switch (dhcpMessage.MessageType)
                    {
                        // DHCPDISCOVER - client to server
                        // broadcast to locate available servers
                        case TDHCPMessageType.DISCOVER:
                            lock(m_Clients)
                            {
                                // is it a known client?
                                DHCPClient knownClient = m_Clients.ContainsKey(client) ? m_Clients[client] : null;
                                
                                if(knownClient!=null)
                                {
                                    Trace(string.Format("Client is known, in state {0}",knownClient.State));

                                    if (knownClient.State == DHCPClient.TState.Offered || knownClient.State == DHCPClient.TState.Assigned)
                                    {
                                        Trace("Client sent DISCOVER but we already offered, or assigned -> repeat offer with known address");
                                        OfferClient(dhcpMessage, knownClient);
                                    }
                                    else
                                    {
                                        Trace("Client is known but released");
                                        // client is known but released or dropped. Use the old address or allocate a new one
                                        if (knownClient.IPAddress.Equals(IPAddress.Any))
                                        {
                                            knownClient.IPAddress = AllocateIPAddress(dhcpMessage);
                                            if (!knownClient.IPAddress.Equals(IPAddress.Any))
                                            {
                                                OfferClient(dhcpMessage, knownClient);
                                            }
                                            else
                                            {
                                                Trace("No more free addresses. Don't respond to discover");                                                
                                            }
                                        }
                                        else
                                        {
                                            OfferClient(dhcpMessage, knownClient);
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
                                    if (!client.IPAddress.Equals(IPAddress.Any))
                                    {
                                        OfferClient(dhcpMessage, client);
                                    }
                                    else
                                    {
                                        Trace("No more free addresses. Don't respond to discover");
                                    }
                                }
                            }
                            break;

                        // DHCPREQUEST - client to server
                        // Client message to servers either 
                        // (a) requesting offered parameters from one server and implicitly declining offers from all others.
                        // (b) confirming correctness of previously allocated address after e.g. system reboot, or
                        // (c) extending the lease on a particular network address
                        case TDHCPMessageType.REQUEST:
                            lock (m_Clients)
                            {
                                // is it a known client?
                                DHCPClient knownClient = m_Clients.ContainsKey(client) ? m_Clients[client] : null;

                                // is there a server identifier?
                                DHCPOptionServerIdentifier dhcpOptionServerIdentifier = (DHCPOptionServerIdentifier) dhcpMessage.GetOption(TDHCPOption.ServerIdentifier);
                                DHCPOptionRequestedIPAddress dhcpOptionRequestedIPAddress = (DHCPOptionRequestedIPAddress) dhcpMessage.GetOption(TDHCPOption.RequestedIPAddress);

                                if (dhcpOptionServerIdentifier != null)
                                {
                                    // there is a server identifier: the message is in response to a DHCPOFFER
                                    if(dhcpOptionServerIdentifier.IPAddress.Equals(m_EndPoint.Address))
                                    {         
                                        // it's a response to OUR offer.
                                        // but did we actually offer one?
                                        if( knownClient!=null && knownClient.State == DHCPClient.TState.Offered)
                                        {
                                            // yes.
                                            // the requested IP address MUST be filled in with the offered address
                                            if(dhcpOptionRequestedIPAddress!=null)
                                            {
                                                if(knownClient.IPAddress.Equals(dhcpOptionRequestedIPAddress.IPAddress))
                                                {
                                                    Trace("Client request matches offered address -> ACK");
                                                    knownClient.State = DHCPClient.TState.Assigned;
                                                    knownClient.LeaseStartTime = DateTime.Now;
                                                    knownClient.LeaseDuration = m_LeaseTime;
                                                    SendACK(dhcpMessage, knownClient.IPAddress, knownClient.LeaseDuration);
                                                }
                                                else
                                                {
                                                    Trace(
                                                        string.Format(
                                                            "Client sent request for IP address '{0}', but it does not match the offered address '{1}' -> NAK",
                                                            dhcpOptionRequestedIPAddress.IPAddress,
                                                            knownClient.IPAddress));
                                                    SendNAK(dhcpMessage);
                                                    RemoveClient(knownClient);
                                                }
                                            }                   
                                            else
                                            {
                                                Trace("Client sent request without filling the RequestedIPAddress option -> NAK");
                                                SendNAK(dhcpMessage);
                                                RemoveClient(knownClient);
                                            }
                                        }
                                        else
                                        {          
                                            // we don't have an outstanding offer!
                                            Trace("Client requested IP address from this server, but we didn't offer any. -> NAK");
                                            SendNAK(dhcpMessage);
                                        }
                                    }
                                    else
                                    {
                                        Trace(
                                            string.Format(
                                                "Client requests IP address that was offered by another DHCP server at '{0}' -> drop offer",
                                                dhcpOptionServerIdentifier.IPAddress));
                                        // it's a response to another DHCP server.
                                        // if we sent an OFFER to this client earlier, remove it.
                                        if(knownClient!=null)
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

                                    if (!dhcpMessage.ClientIPAddress.Equals(IPAddress.Any))
                                    {
                                        Trace("REQUEST client IP is filled in -> client state is RENEWING or REBINDING");

                                        // see : http://www.tcpipguide.com/free/t_DHCPLeaseRenewalandRebindingProcesses-2.htm

                                        if (knownClient!=null && 
                                            knownClient.State == DHCPClient.TState.Assigned && 
                                            knownClient.IPAddress.Equals(dhcpMessage.ClientIPAddress))
                                        {
                                            // known, assigned, and IP address matches administration. ACK
                                            knownClient.LeaseStartTime = DateTime.Now;
                                            knownClient.LeaseDuration = m_LeaseTime;
                                            SendACK(dhcpMessage, dhcpMessage.ClientIPAddress, knownClient.LeaseDuration);
                                        }
                                        else
                                        {
                                            // not known, or known but in some other state. Just dump the old one.
                                            if(knownClient!=null) RemoveClient(knownClient);

                                            // check if client IP address is marked free
                                            if (IPAddressIsFree(dhcpMessage.ClientIPAddress, false))
                                            {
                                                // it's free. send ACK
                                                client.IPAddress = dhcpMessage.ClientIPAddress;
                                                client.State = DHCPClient.TState.Assigned;
                                                client.LeaseStartTime = DateTime.Now;
                                                client.LeaseDuration = m_LeaseTime;
                                                m_Clients.Add(client, client);
                                                SendACK(dhcpMessage, dhcpMessage.ClientIPAddress, knownClient.LeaseDuration);
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

                                        if (dhcpOptionRequestedIPAddress != null)
                                        {
                                            if (knownClient != null &&
                                                knownClient.State == DHCPClient.TState.Assigned)
                                            {
                                                if (knownClient.IPAddress.Equals(dhcpOptionRequestedIPAddress.IPAddress))
                                                {
                                                    Trace("Client request matches cached address -> ACK");
                                                    // known, assigned, and IP address matches administration. ACK
                                                    knownClient.LeaseStartTime = DateTime.Now;
                                                    knownClient.LeaseDuration = m_LeaseTime;
                                                    SendACK(dhcpMessage, dhcpOptionRequestedIPAddress.IPAddress, knownClient.LeaseDuration);
                                                }
                                                else
                                                {
                                                    Trace(string.Format("Client sent request for IP address '{0}', but it does not match cached address '{1}' -> NAK", dhcpOptionRequestedIPAddress.IPAddress, knownClient.IPAddress));
                                                    SendNAK(dhcpMessage);
                                                    RemoveClient(knownClient);
                                                }
                                            }
                                            else
                                            {
                                                // client not known, or known but in some other state. Just dump the old one.
                                                if (knownClient != null) RemoveClient(knownClient);
                                                Trace("Client attempted INIT-REBOOT REQUEST but server has no administration for this client -> silently ignoring this client");
                                            }
                                        }
                                        else
                                        {
                                            Trace("Client sent apparent INIT-REBOOT REQUEST but with an empty 'RequestedIPAddress' option. Oops..");
                                        }
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
                            lock(m_Clients)
                            {
                                if (ServerIdentifierPrecondition(dhcpMessage))
                                {
                                    // is it a known client?
                                    DHCPClient knownClient = m_Clients.ContainsKey(client) ? m_Clients[client] : null;

                                    if(knownClient!=null)
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
                            }
                            break;

                        case TDHCPMessageType.RELEASE:
                            // relinguishing network address and cancelling remaining lease.
                            // Upon receipt of a DHCPRELEASE message, the server marks the network
                            // address as not allocated.  The server SHOULD retain a record of the
                            // client's initialization parameters for possible reuse in response to
                            // subsequent requests from the client.
                            lock(m_Clients)
                            {
                                if (ServerIdentifierPrecondition(dhcpMessage))
                                {
                                    // is it a known client?
                                    DHCPClient knownClient = m_Clients.ContainsKey(client) ? m_Clients[client] : null;

                                    if ( knownClient!=null /* && knownClient.State == DHCPClient.TState.Assigned */ )
                                    {
                                        if (dhcpMessage.ClientIPAddress.Equals(knownClient.IPAddress))
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
                            SendINFORMACK(dhcpMessage);
                            break;

                        default:
                            Trace(string.Format("Invalid message from client, ignored"));
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

        private void OnStop(UDPSocket sender, Exception reason)
        {
            Stop(reason);
        }
    }
}
