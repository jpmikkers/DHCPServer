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
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.IO;

namespace GitHub.JPMikkers.DHCP
{
    public class Utils
    {
        public static TimeSpan InfiniteTimeSpan = TimeSpan.FromSeconds(UInt32.MaxValue);

        public static bool IsInfiniteTimeSpan(TimeSpan timeSpan)
        {
            return (timeSpan < TimeSpan.FromSeconds(0.0) || timeSpan >= InfiniteTimeSpan);
        }

        public static TimeSpan SanitizeTimeSpan(TimeSpan timeSpan)
        {
            return IsInfiniteTimeSpan(timeSpan) ? InfiniteTimeSpan : timeSpan;
        }

        public static bool ByteArraysAreEqual(byte[] array1, byte[] array2)
        {
            if (array1.Length != array2.Length)
            {
                return false;
            }

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static string BytesToHexString(byte[] data,string separator)
        {
            StringBuilder sb = new StringBuilder();
            for (int t = 0; t < data.Length; t++)
            {
                sb.AppendFormat("{0:X2}", data[t]);
                if(t<(data.Length-1))
                {
                    sb.Append(separator);
                }
            }
            return sb.ToString();
        }

        public static byte[] HexStringToBytes(string data)
        {
            int c;
            List<byte> result = new List<byte>();

            MemoryStream ms = new MemoryStream();
            StreamWriter sw = new StreamWriter(ms);
            sw.Write(data);
            sw.Flush();
            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);

            StringBuilder number = new StringBuilder();

            while((c=sr.Read())>0)
            {
               if( (c>='0' && c<='9') || (c>='a' && c<='f') || (c>='A' && c<='F'))
               {
                   number.Append((char) c);

                   if(number.Length>=2)
                   {
                       result.Add(Convert.ToByte(number.ToString(), 16));
                       number.Length = 0;
                   }
               }
            }
            return result.ToArray();
        }

        public static IPAddress GetSubnetMask(IPAddress address)
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (UnicastIPAddressInformation unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses)
                {
                    if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        if (address.Equals(unicastIPAddressInformation.Address))
                        {
                            // the following mask can be null.. return 255.255.255.0 in that case
                            return unicastIPAddressInformation.IPv4Mask ?? new IPAddress(new byte[] {255, 255, 255, 0});
                        }
                    }
                }
            }
            throw new ArgumentException(string.Format("Can't find subnetmask for IP address '{0}'", address));
        }

        public static IPAddress UInt32ToIPAddress(UInt32 address)
        {
            return new IPAddress(new byte[] { 
                (byte)((address>>24) & 0xFF) ,
                (byte)((address>>16) & 0xFF) , 
                (byte)((address>>8)  & 0xFF) , 
                (byte)( address & 0xFF)});
        }

        public static UInt32 IPAddressToUInt32(IPAddress address)
        {
            return
                (((UInt32)address.GetAddressBytes()[0]) << 24) |
                (((UInt32)address.GetAddressBytes()[1]) << 16) |
                (((UInt32)address.GetAddressBytes()[2]) << 8) |
                (((UInt32)address.GetAddressBytes()[3]));
        }

        public static string PrefixLines(string src,string prefix)
        {
            StringBuilder sb = new StringBuilder();
            MemoryStream ms = new MemoryStream();
            StreamWriter sw = new StreamWriter(ms);
            sw.Write(src);
            sw.Flush();
            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                sb.Append(prefix);
                sb.AppendLine(line);
            }
            return sb.ToString();
        }
    }
}
