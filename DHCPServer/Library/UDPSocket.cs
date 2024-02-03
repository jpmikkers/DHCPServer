using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.JPMikkers.DHCP
{
    public class UDPSocket : IUDPSocket
    {
        // See: http://stackoverflow.com/questions/5199026/c-sharp-async-udp-listener-socketexception

        const uint IOC_IN = 0x80000000;
        const uint IOC_VENDOR = 0x18000000;
        const uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;

        #region private types, members

        private bool _disposed;                            // true => object is disposed

        private readonly bool _IPv6;                                // true => it's an IPv6 connection
        private readonly Socket _socket;                            // The active socket
        private readonly int _packetSize;                           // size of packets we'll try to receive

        private readonly IPEndPoint _localEndPoint;

        private CancellationTokenSource _cancellationTokenSource = new();

        #endregion

        public IPEndPoint LocalEndPoint
        {
            get
            {
                return _localEndPoint;
            }
        }

        #region constructors destructor
        public UDPSocket(IPEndPoint localEndPoint, int packetSize, bool dontFragment, short ttl)
        {
            _packetSize = packetSize;
            _disposed = false;

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
                _socket.IOControl((IOControlCode)SIO_UDP_CONNRESET, [0, 0, 0, 0], null);
            }
            catch(PlatformNotSupportedException)
            {
            }
        }

        public async Task<(IPEndPoint, ReadOnlyMemory<byte>)> Receive(CancellationToken cancellationToken)
        {
            //try
            //{
            var mem = new Memory<byte>(new byte[_packetSize]);

            Console.WriteLine("UDPSocket before receive");
            var result = await _socket.ReceiveFromAsync(mem, new IPEndPoint(IPAddress.Any, 0), cancellationToken);
            Console.WriteLine("UDPSocket after receive");

            if(result.RemoteEndPoint is IPEndPoint endpoint)
            {
                return (endpoint, mem[..result.ReceivedBytes]);
            }
            else
            {
                throw new InvalidOperationException("unexpected endpoint type");
            }
            //}
            //catch(Exception ex)
            //{
            //    Console.WriteLine($"UDPSocket receive exception {ex}");
            //}
        }


        //private async Task SocketTask(CancellationToken cancellationToken)
        //{
        //    while(!cancellationToken.IsCancellationRequested)
        //    {
        //        try
        //        {
        //            var mem = new Memory<byte>(new byte[_packetSize]);

        //            Console.WriteLine("UDPSocket before receive");
        //            var result = await _socket.ReceiveFromAsync(mem, new IPEndPoint(IPAddress.Any, 0), cancellationToken);
        //            Console.WriteLine("UDPSocket after receive");

        //            if(result.RemoteEndPoint is IPEndPoint endpoint)
        //            {
        //                await _onReceive(this, endpoint, mem[..result.ReceivedBytes]);
        //            }
        //        }
        //        catch(Exception ex)
        //        {
        //            Console.WriteLine($"UDPSocket receive exception {ex}");
        //        }
        //    }
        //}

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
        public async Task Send(IPEndPoint endPoint, ReadOnlyMemory<byte> msg, CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("UDPSocket before send");
                await _socket.SendToAsync(msg, endPoint, cancellationToken);
                Console.WriteLine("UDPSocket after send");
            }
            catch(Exception e)
            {
                Console.WriteLine($"UDPSocket send exception {e}");
            }

            //try
            //{
            //    lock(_sync)
            //    {
            //        if(!_disposed)
            //        {
            //            _sendFifo.Enqueue(new PacketBuffer(endPoint, msg));
            //            BeginSend();
            //        }
            //    }
            //}
            //catch(Exception e)
            //{
            //    Stop(e);
            //}
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

            if(notifyStop)
            {
                //_onStop(this, reason);
            }
        }

        /// <summary>
        /// Callback handler for the asynchronous Socket.BeginReceive() method
        /// </summary>
        /// <param name="ar">Represents the status of an asynchronous operation</param>
        //private void ReceiveDone(IAsyncResult ar)
        //{
        //    try
        //    {
        //        lock(_sync)
        //        {
        //            if(!_disposed)
        //            {
        //                try
        //                {
        //                    if(ar.AsyncState is PacketBuffer buf && buf.Data.Array is not null)
        //                    {
        //                        int packetSize;
        //                        try
        //                        {
        //                            packetSize = _socket.EndReceiveFrom(ar, ref buf.EndPoint);
        //                        }
        //                        finally
        //                        {
        //                            _receivePending--;
        //                        }
        //                        buf.Data = new ArraySegment<byte>(buf.Data.Array, 0, packetSize);
        //                        _receiveFifo.Enqueue(buf);
        //                        // BeginReceive should check state again because Stop() could have been called synchronously at NotifyReceive()
        //                        BeginReceive();
        //                    }
        //                }
        //                catch(SocketException e)
        //                {
        //                    switch(e.SocketErrorCode)
        //                    {
        //                        case SocketError.ConnectionReset:
        //                            // ConnectionReset is reported when the remote port wasn't listening.
        //                            // Since we're using UDP messaging we don't care about this -> continue receiving.
        //                            BeginReceive();
        //                            break;

        //                        case SocketError.MessageSize:
        //                            // someone tried to send a message bigger than m_MaxPacketSize
        //                            // discard it, and start receiving the next packet
        //                            BeginReceive();
        //                            break;

        //                        default:
        //                            // just assume anything else is fatal -> pass the exception.
        //                            throw;
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch(Exception e)
        //    {
        //        // it's only safe to Stop() the socket if this method wasn't called recursively (because in that case the lock will be taken!)
        //        // rethrow the exception until the stack unwinds to the top-level ReceiveDone.
        //        if(ar.CompletedSynchronously) throw; else Stop(e);
        //    }
        //}

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
