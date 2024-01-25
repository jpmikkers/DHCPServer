using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace GitHub.JPMikkers.DHCP
{
    public class UDPSocket : IDisposable
    {
        // See: http://stackoverflow.com/questions/5199026/c-sharp-async-udp-listener-socketexception

        const uint IOC_IN = 0x80000000;
        const uint IOC_VENDOR = 0x18000000;
        const uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;

        public delegate void OnReceiveDelegate(UDPSocket sender, IPEndPoint endPoint, ArraySegment<byte> data);
        public delegate void OnStopDelegate(UDPSocket sender, Exception? reason);

        #region private types, members

        private bool _disposed;                            // true => object is disposed
        private readonly object _sync = new object();      // Synchronizing object

        private readonly OnReceiveDelegate _onReceive;
        private readonly OnStopDelegate _onStop;

        private readonly bool _IPv6;                                // true => it's an IPv6 connection
        private readonly Socket _socket;                            // The active socket

        private readonly Queue<PacketBuffer> _sendFifo;             // queue of the outgoing packets
        private bool _sendPending;                         // true => an asynchronous send is in progress
        private int _receivePending;

        private readonly AutoPumpQueue<PacketBuffer> _receiveFifo;  // queue of the incoming packets
        private readonly int _packetSize;                           // size of packets we'll try to receive

        private readonly IPEndPoint _localEndPoint;

        private class PacketBuffer
        {
            public EndPoint EndPoint;
            public ArraySegment<byte> Data;

            public PacketBuffer(IPEndPoint endPoint, ArraySegment<byte> data)
            {
                this.EndPoint = endPoint;
                this.Data = data;
            }
        }

        #endregion

        public bool SendPending
        {
            get
            {
                lock(_sync)
                {
                    return _sendPending || _sendFifo.Count > 0;
                }
            }
        }

        public IPEndPoint LocalEndPoint
        {
            get
            {
                return _localEndPoint;
            }
        }

        #region constructors destructor
        public UDPSocket(IPEndPoint localEndPoint, int packetSize, bool dontFragment, short ttl, OnReceiveDelegate onReceive, OnStopDelegate onStop)
        {
            _packetSize = packetSize;
            _disposed = false;
            _onReceive = onReceive;
            _onStop = onStop;

            _sendFifo = new Queue<PacketBuffer>();

            _receiveFifo = new AutoPumpQueue<PacketBuffer>(
                (sender, data) =>
                {
                    bool isDisposed = false;

                    lock(_sync)
                    {
                        isDisposed = _disposed;
                    }

                    if(!isDisposed)
                    {
                        _onReceive(this, (IPEndPoint)data.EndPoint, data.Data);
                    }
                }
            );

            _sendPending = false;
            _receivePending = 0;

            _IPv6 = (localEndPoint.AddressFamily == AddressFamily.InterNetworkV6);
            _socket = new Socket(localEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            _socket.EnableBroadcast = true;
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            _socket.SendBufferSize = 65536;
            _socket.ReceiveBufferSize = 65536;
            if(!_IPv6) _socket.DontFragment = dontFragment;
            if(ttl >= 0)
            {
                _socket.Ttl = ttl;
            }
            _socket.Bind(localEndPoint);
            _localEndPoint = (_socket.LocalEndPoint as IPEndPoint) ?? localEndPoint;

            try
            {
                _socket.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
            }
            catch(PlatformNotSupportedException)
            {
            }

            BeginReceive();
        }

        ~UDPSocket()
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

        #endregion

        #region public methods, properties

        /// <summary>
        /// Sends a packet of bytes to the specified EndPoint using an UDP datagram.
        /// </summary>
        /// <param name="endPoint">Target for the data</param>
        /// <param name="msg">Data to send</param>
        public void Send(IPEndPoint endPoint, ArraySegment<byte> msg)
        {
            try
            {
                lock(_sync)
                {
                    if(!_disposed)
                    {
                        _sendFifo.Enqueue(new PacketBuffer(endPoint, msg));
                        BeginSend();
                    }
                }
            }
            catch(Exception e)
            {
                Stop(e);
            }
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Implements <see cref="IDisposable.Dispose"/>
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region private methods, properties

        private void Stop(Exception? reason)
        {
            bool notifyStop = false;

            lock(_sync)
            {
                if(!_disposed)
                {
                    notifyStop = true;
                    _disposed = true;

                    try
                    {
                        _socket.Shutdown(SocketShutdown.Both);
                        _socket.Close();
                    }
                    catch(Exception)
                    {
                        // socket tends to complain a lot during close. just eat those exceptions.
                    }
                }
            }

            if(notifyStop)
            {
                _onStop(this, reason);
            }
        }

        /// <summary>
        /// Start an asynchronous send of outgoing data
        /// </summary>
        private void BeginSend()
        {
            lock(_sync)
            {
                if(!_disposed && !_sendPending)
                {
                    if(_sendFifo.Count > 0)
                    {
                        _sendPending = true;   // !! MUST BE DONE BEFORE CALLING BEGINSEND. Sometimes beginsend will call the SendDone routine synchronously!!
                        PacketBuffer sendPacket = _sendFifo.Dequeue();

                        if(sendPacket.Data.Array is not null)
                        {
                            try
                            {
                                _socket.BeginSendTo(sendPacket.Data.Array, sendPacket.Data.Offset, sendPacket.Data.Count, SocketFlags.None, sendPacket.EndPoint, new AsyncCallback(SendDone), sendPacket);
                            }
                            catch(Exception)
                            {
                                // don't care about any exceptions here because the TFTP protocol will take care of retrying to send the packet
                            }
                        }
                    }
                }
                else
                {
                    //System.Diagnostics.Debug.WriteLine("BeginSend while send pending?");
                }
            }
        }

        /// <summary>
        /// Callback handler for the asynchronous Socket.BeginSend() method
        /// </summary>
        /// <param name="ar">Represents the status of an asynchronous operation</param>
        private void SendDone(IAsyncResult ar)
        {
            lock(_sync)
            {
                if(!_disposed)
                {
                    try
                    {
                        _socket.EndSendTo(ar);
                    }
                    catch(Exception)
                    {
                        // don't care about any exceptions here because the TFTP protocol will take care of retrying to send the packet
                    }
                    _sendPending = false;
                    BeginSend();
                }
            }
        }

        /// <summary>
        /// Start an asynchronous receive of incoming data.
        /// </summary>
        private void BeginReceive()
        {
            // just one pending receive for now. Anything more causes packet reordering at ReceiveDone (even on loopback connections) which doesn't feel right.
            while(_receivePending < 1)
            {
                _receivePending++;
                PacketBuffer receivePacket = new PacketBuffer(new IPEndPoint(_IPv6 ? IPAddress.IPv6Any : IPAddress.Any, 0), new ArraySegment<byte>(new byte[_packetSize], 0, _packetSize));
                if(receivePacket.Data.Array is not null)
                {
                    _socket.BeginReceiveFrom(receivePacket.Data.Array, receivePacket.Data.Offset, receivePacket.Data.Count, SocketFlags.None, ref receivePacket.EndPoint, new AsyncCallback(ReceiveDone), receivePacket);
                }
            }
        }

        /// <summary>
        /// Callback handler for the asynchronous Socket.BeginReceive() method
        /// </summary>
        /// <param name="ar">Represents the status of an asynchronous operation</param>
        private void ReceiveDone(IAsyncResult ar)
        {
            try
            {
                lock(_sync)
                {
                    if(!_disposed)
                    {
                        try
                        {
                            if(ar.AsyncState is PacketBuffer buf && buf.Data.Array is not null)
                            {
                                int packetSize;
                                try
                                {
                                    packetSize = _socket.EndReceiveFrom(ar, ref buf.EndPoint);
                                }
                                finally
                                {
                                    _receivePending--;
                                }
                                buf.Data = new ArraySegment<byte>(buf.Data.Array, 0, packetSize);
                                _receiveFifo.Enqueue(buf);
                                // BeginReceive should check state again because Stop() could have been called synchronously at NotifyReceive()
                                BeginReceive();
                            }
                        }
                        catch(SocketException e)
                        {
                            switch(e.SocketErrorCode)
                            {
                                case SocketError.ConnectionReset:
                                    // ConnectionReset is reported when the remote port wasn't listening.
                                    // Since we're using UDP messaging we don't care about this -> continue receiving.
                                    BeginReceive();
                                    break;

                                case SocketError.MessageSize:
                                    // someone tried to send a message bigger than m_MaxPacketSize
                                    // discard it, and start receiving the next packet
                                    BeginReceive();
                                    break;

                                default:
                                    // just assume anything else is fatal -> pass the exception.
                                    throw;
                            }
                        }
                    }
                }
            }
            catch(Exception e)
            {
                // it's only safe to Stop() the socket if this method wasn't called recursively (because in that case the lock will be taken!)
                // rethrow the exception until the stack unwinds to the top-level ReceiveDone.
                if(ar.CompletedSynchronously) throw; else Stop(e);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if(disposing)
            {
                Stop(null);
            }
        }

        #endregion
    }
}
