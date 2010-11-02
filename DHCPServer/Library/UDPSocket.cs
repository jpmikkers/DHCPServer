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
using System.Net;
using System.Net.Sockets;

namespace CodePlex.JPMikkers.DHCP
{
    public class UDPSocket : IDisposable
    {
        public delegate void OnReceiveDelegate(UDPSocket sender, IPEndPoint endPoint, ArraySegment<byte> data);
        public delegate void OnStopDelegate(UDPSocket sender, Exception reason);

        #region private types, members

        private bool m_Disposed;                            // true => object is disposed
        private readonly object m_Sync = new object();      // Synchronizing object

        private OnReceiveDelegate m_OnReceive;
        private OnStopDelegate m_OnStop;

        private bool m_IPv6;                                // true => it's an IPv6 connection
        private Socket m_Socket;                            // The active socket

        private Queue<PacketBuffer> m_SendFifo;             // queue of the outgoing packets
        private bool m_SendPending;                         // true => an asynchronous send is in progress
        private int m_ReceivePending;

        private AutoPumpQueue<PacketBuffer> m_ReceiveFifo;  // queue of the incoming packets
        private int m_PacketSize;                           // size of packets we'll try to receive

        private EndPoint m_LocalEndPoint;

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
                lock (m_Sync)
                {
                    return m_SendPending || m_SendFifo.Count > 0;
                }
            }
        }

        public EndPoint LocalEndPoint
        {
            get
            {
                return m_LocalEndPoint;
            }
        }

        #region constructors destructor
        public UDPSocket(IPEndPoint localEndPoint, int packetSize, bool dontFragment, short ttl, OnReceiveDelegate onReceive, OnStopDelegate onStop)
        {
            m_PacketSize = packetSize;
            m_Disposed = false;
            m_OnReceive = onReceive;
            m_OnStop = onStop;

            m_SendFifo = new Queue<PacketBuffer>();

            m_ReceiveFifo = new AutoPumpQueue<PacketBuffer>(
                delegate(AutoPumpQueue<PacketBuffer> sender, PacketBuffer data)
                {              
                    bool isDisposed = false;

                    lock (m_Sync)
                    {
                        isDisposed = m_Disposed;
                    }

                    if (!isDisposed)
                    {
                        m_OnReceive(this, (IPEndPoint)data.EndPoint, data.Data);
                    }
                }
            );

            m_SendPending = false;
            m_ReceivePending = 0;

            m_IPv6 = (localEndPoint.AddressFamily == AddressFamily.InterNetworkV6);
            m_Socket = new Socket(localEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            m_Socket.EnableBroadcast = true;
            m_Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            m_Socket.SendBufferSize = 65536;
            m_Socket.ReceiveBufferSize = 65536;
            if(!m_IPv6) m_Socket.DontFragment = dontFragment;
            if (ttl >= 0)
            {
                m_Socket.Ttl = ttl;
            }
            m_Socket.Bind(localEndPoint);
            m_LocalEndPoint = m_Socket.LocalEndPoint;
            BeginReceive();
        }

        ~UDPSocket()
        {
            Dispose(false);
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
                lock (m_Sync)
                {
                    if (!m_Disposed)
                    {
                        m_SendFifo.Enqueue(new PacketBuffer(endPoint, msg));
                        BeginSend();
                    }
                }
            }
            catch (Exception e)
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

        private void Stop(Exception reason)
        {
            bool notifyStop = false;

            lock (m_Sync)
            {
                if (!m_Disposed)
                {
                    notifyStop = true;
                    m_Disposed = true;

                    try
                    {
                        m_Socket.Shutdown(SocketShutdown.Both);
                        m_Socket.Close();
                    }
                    catch (Exception)
                    {
                        // socket tends to complain a lot during close. just eat those exceptions.
                    }
                }
            }

            if (notifyStop)
            {
                m_OnStop(this, reason);
            }
        }

        /// <summary>
        /// Start an asynchronous send of outgoing data
        /// </summary>
        private void BeginSend()
        {
            lock (m_Sync)
            {
                if (!m_Disposed && !m_SendPending)
                {
                    if (m_SendFifo.Count > 0)
                    {
                        m_SendPending = true;   // !! MUST BE DONE BEFORE CALLING BEGINSEND. Sometimes beginsend will call the SendDone routine synchronously!!
                        PacketBuffer sendPacket = m_SendFifo.Dequeue();

                        try
                        {
                            m_Socket.BeginSendTo(sendPacket.Data.Array, sendPacket.Data.Offset, sendPacket.Data.Count, SocketFlags.None, sendPacket.EndPoint, new AsyncCallback(SendDone), sendPacket);
                        }
                        catch (Exception)
                        {
                            // don't care about any exceptions here because the TFTP protocol will take care of retrying to send the packet
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("BeginSend while send pending?");
                }
            }
        }

        /// <summary>
        /// Callback handler for the asynchronous Socket.BeginSend() method
        /// </summary>
        /// <param name="ar">Represents the status of an asynchronous operation</param>
        private void SendDone(IAsyncResult ar)
        {
            lock (m_Sync)
            {
                if (!m_Disposed)
                {
                    try
                    {
                        m_Socket.EndSendTo(ar);
                    }
                    catch (Exception)
                    {
                        // don't care about any exceptions here because the TFTP protocol will take care of retrying to send the packet
                    }
                    m_SendPending = false;
                    BeginSend();
                }
            }
        }

        /// <summary>
        /// Start an asynchronous receive of incoming data.
        /// </summary>
        private void BeginReceive()
        {
            while (m_ReceivePending < 2)
            {
                m_ReceivePending++;
                PacketBuffer receivePacket = new PacketBuffer(new IPEndPoint(m_IPv6 ? IPAddress.IPv6Any : IPAddress.Any, 0), new ArraySegment<byte>(new byte[m_PacketSize], 0, m_PacketSize));
                m_Socket.BeginReceiveFrom(receivePacket.Data.Array, receivePacket.Data.Offset, receivePacket.Data.Count, SocketFlags.None, ref receivePacket.EndPoint, new AsyncCallback(ReceiveDone), receivePacket);
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
                lock (m_Sync)
                {
                    if (!m_Disposed)
                    {
                        try
                        {
                            PacketBuffer buf = (PacketBuffer)ar.AsyncState;
                            int packetSize = m_Socket.EndReceiveFrom(ar, ref buf.EndPoint);
                            m_ReceivePending--;
                            buf.Data = new ArraySegment<byte>(buf.Data.Array, 0, packetSize);
                            m_ReceiveFifo.Enqueue(buf);
                            // BeginReceive should check state again because Stop() could have been called synchronously at NotifyReceive()
                            BeginReceive();
                        }
                        catch (SocketException e)
                        {
                            switch (e.SocketErrorCode)
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
            catch (Exception e)
            {
                // it's only safe to Stop() the socket if this method wasn't called recursively (because in that case the lock will be taken!)
                // rethrow the exception until the stack unwinds to the top-level ReceiveDone.
                if (ar.CompletedSynchronously) throw; else Stop(e);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            Stop(null);
        }

        #endregion
    }
}
