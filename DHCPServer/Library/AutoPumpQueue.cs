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
