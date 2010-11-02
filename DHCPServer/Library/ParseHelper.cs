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
using System.IO;
using System.Net;
using System.Text;

namespace CodePlex.JPMikkers.DHCP
{
    public class ParseHelper
    {
        public static IPAddress ReadIPAddress(Stream s)
        {
            byte[] bytes = new byte[4];
            s.Read(bytes, 0, bytes.Length);
            return new IPAddress(bytes);
        }

        public static void WriteIPAddress(Stream s, IPAddress v)
        {
            byte[] bytes = v.GetAddressBytes();
            s.Write(bytes, 0, bytes.Length);
        }

        public static byte ReadUInt8(Stream s)
        {
            BinaryReader br = new BinaryReader(s);
            return br.ReadByte();
        }

        public static void WriteUInt8(Stream s, byte v)
        {
            BinaryWriter bw = new BinaryWriter(s);
            bw.Write(v);
        }

        public static ushort ReadUInt16(Stream s)
        {
            BinaryReader br = new BinaryReader(s);
            return (ushort)IPAddress.NetworkToHostOrder((short)br.ReadUInt16());
        }

        public static void WriteUInt16(Stream s, ushort v)
        {
            BinaryWriter bw = new BinaryWriter(s);
            bw.Write((ushort)IPAddress.HostToNetworkOrder((short)v));
        }

        public static uint ReadUInt32(Stream s)
        {
            BinaryReader br = new BinaryReader(s);
            return (uint)IPAddress.NetworkToHostOrder((int)br.ReadUInt32());
        }

        public static void WriteUInt32(Stream s, uint v)
        {
            BinaryWriter bw = new BinaryWriter(s);
            bw.Write((uint)IPAddress.HostToNetworkOrder((int)v));
        }

        public static string ReadZString(Stream s)
        {
            StringBuilder sb = new StringBuilder();
            int c = s.ReadByte();
            while (c>0)
            {
                sb.Append((char)c);
                c = s.ReadByte();
            }
            return sb.ToString();
        }

        public static void WriteZString(Stream s, string msg)
        {
            TextWriter tw = new StreamWriter(s, Encoding.ASCII);
            tw.Write(msg);
            tw.Flush();
            s.WriteByte(0);
        }

        public static void WriteZString(Stream s, string msg, int length)
        {
            if (msg.Length >= length)
            {
                msg = msg.Substring(0, length - 1);
            }

            TextWriter tw = new StreamWriter(s, Encoding.ASCII);
            tw.Write(msg);
            tw.Flush();

            // write terminating and padding zero's
            for (int t = msg.Length; t < length; t++)
            {
                s.WriteByte(0);
            }
        }

        public static string ReadString(Stream s, int maxLength)
        {
            StringBuilder sb = new StringBuilder();
            int c = s.ReadByte();
            while (c > 0 && sb.Length < maxLength)
            {
                sb.Append((char)c);
                c = s.ReadByte();
            }
            return sb.ToString();
        }

        public static string ReadString(Stream s)
        {
            return ReadString(s, 16*1024);
        }

        public static void WriteString(Stream s, string msg)
        {
            TextWriter tw = new StreamWriter(s, Encoding.ASCII);
            tw.Write(msg);
            tw.Flush();
        }
    }
}
