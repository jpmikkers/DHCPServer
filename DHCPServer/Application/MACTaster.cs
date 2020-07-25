using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using GitHub.JPMikkers.DHCP;

namespace DHCPServerApp
{
    public class MACTaster
    {
        private static readonly Regex regex = new Regex(@"^(?<mac>([0-9a-fA-F][0-9a-fA-F][:\-\.]?)+)(?<netmask>/[0-9]+)?\s*(?<id>\w*)\s*(?<comment>#.*)?", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private List<PrefixItem> m_PrefixItems = new List<PrefixItem>();

        private struct PrefixItem
        {
            public byte[] Prefix;
            public int PrefixBits;
            public string Id;

            public PrefixItem(byte[] prefix,int bits,string id)
            {
                this.Prefix = prefix;
                this.PrefixBits = bits;
                this.Id = id;
            }
        }

        public MACTaster(string configFile)
        {
            try
            {
                using (StreamReader sr = new StreamReader(configFile))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        try
                        {
                            Match match = regex.Match(line);
                            if (match.Success && match.Groups["mac"].Success && match.Groups["id"].Success)
                            {
                                byte[] prefix = Utils.HexStringToBytes(match.Groups["mac"].Value);
                                string id = match.Groups["id"].Value;
                                int prefixBits = prefix.Length*8;

                                if (match.Groups["netmask"].Success)
                                {
                                    prefixBits = Int32.Parse(match.Groups["netmask"].Value.Substring(1));
                                }

                                m_PrefixItems.Add(new PrefixItem(prefix, prefixBits, id));
                            }
                        }
                        catch
                        {                            
                        }
                    }
                }
            }
            catch
            {                
            }
        }

        private bool MacMatch(byte[] mac,byte[] prefix,int bits)
        {
            // prefix should have more bits than masklength
            if (((bits + 7) >> 3) > prefix.Length) return false;
            // prefix should be shorter or equal to mac address
            if (prefix.Length > mac.Length) return false;
            for(int t=0;t<(bits-7);t+=8)
            {
                if (mac[t >> 3] != prefix[t >> 3]) return false;
            }

            if ((bits & 7) > 0)
            {
                byte bitMask = (byte) (0xFF00 >> (bits & 7));
                if ((mac[bits >> 3] & bitMask) != (prefix[bits >> 3] & bitMask)) return false;
            }
            return true;
        }


        public string Taste(byte[] macAddress)
        {
            foreach(PrefixItem prefixItem in m_PrefixItems)
            {
                if(MacMatch(macAddress,prefixItem.Prefix,prefixItem.PrefixBits))
                {
                    return prefixItem.Id;
                }
            }
            return "Unknown";
        }
    }
}
