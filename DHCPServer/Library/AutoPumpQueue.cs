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
using System.Text;
using System.Threading;

namespace CodePlex.JPMikkers.DHCP
{
    public class AutoPumpQueue<T>
    {
        public delegate void DataDelegate(AutoPumpQueue<T> sender, T data);

        private readonly object m_QueueSync = new object();
        private readonly object m_DispatchSync = new object();
        private readonly Queue<T> m_Queue;
        private DataDelegate m_DataDelegate;

        /// <summary>
        /// Constructor
        /// </summary>
        public AutoPumpQueue(DataDelegate dataDelegate)
        {
            m_Queue = new Queue<T>();
            m_DataDelegate = dataDelegate;
        }

        private void WaitCallback(object state)
        {
            lock (m_DispatchSync)    // ensures individual invokes are serialized
            {
                bool empty = false;
                T data = default(T);

                while (!empty)
                {
                    lock (m_QueueSync)
                    {
                        if (m_Queue.Count == 0)
                        {
                            // no data
                            empty = true;
                        }
                        else
                        {
                            // there are commands;
                            data = m_Queue.Dequeue();
                            empty = false;
                        }
                    }

                    if (!empty)
                    {
                        try
                        {
                            m_DataDelegate(this, data);
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }

        public void Enqueue(T data)
        {
            bool queueWasEmpty;

            lock (m_QueueSync)
            {
                queueWasEmpty = (m_Queue.Count == 0);
                m_Queue.Enqueue(data);
            }

            if (queueWasEmpty)
            {
                ThreadPool.QueueUserWorkItem(WaitCallback);
            }
        }
    }
}
