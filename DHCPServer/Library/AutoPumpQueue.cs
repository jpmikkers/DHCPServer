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

namespace GitHub.JPMikkers.DHCP
{
    public class AutoPumpQueue<T>
    {
        public delegate void DataDelegate(AutoPumpQueue<T> sender, T data);

        private readonly object _queueSync = new object();
        private readonly object _dispatchSync = new object();
        private readonly Queue<T> _queue;
        private readonly DataDelegate _dataDelegate;

        /// <summary>
        /// Constructor
        /// </summary>
        public AutoPumpQueue(DataDelegate dataDelegate)
        {
            _queue = new Queue<T>();
            _dataDelegate = dataDelegate;
        }

        private void WaitCallback(object state)
        {
            lock (_dispatchSync)    // ensures individual invokes are serialized
            {
                bool empty = false;
                T data = default(T);

                while (!empty)
                {
                    lock (_queueSync)
                    {
                        if (_queue.Count == 0)
                        {
                            // no data
                            empty = true;
                        }
                        else
                        {
                            // there are commands;
                            data = _queue.Dequeue();
                            empty = false;
                        }
                    }

                    if (!empty)
                    {
                        try
                        {
                            _dataDelegate(this, data);
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

            lock (_queueSync)
            {
                queueWasEmpty = (_queue.Count == 0);
                _queue.Enqueue(data);
            }

            if (queueWasEmpty)
            {
                ThreadPool.QueueUserWorkItem(WaitCallback);
            }
        }
    }
}
