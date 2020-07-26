/*

Copyright (c) 2020 Jean-Paul Mikkers

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
using System.Xml.Serialization;
using System.IO;
using System.ComponentModel;

namespace DHCPServerApp
{
    [Serializable()]
    public class DHCPServerConfigurationList : BindingList<DHCPServerConfiguration>
    {
        private static XmlSerializer serializer = new XmlSerializer(typeof(DHCPServerConfigurationList));

        public static DHCPServerConfigurationList Read(string file)
        {
            DHCPServerConfigurationList result;

            if (File.Exists(file))
            {
                using (Stream s = File.OpenRead(file))
                {
                    result = (DHCPServerConfigurationList)serializer.Deserialize(s);
                }
            }
            else
            {
                result = new DHCPServerConfigurationList();
            }

            return result;
        }

        public void Write(string file)
        {
            string dirName = Path.GetDirectoryName(file);

            if(!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }

            using(Stream s = File.Open(file,FileMode.Create))
            {
                serializer.Serialize(s, this);
            }
        }
    }
}