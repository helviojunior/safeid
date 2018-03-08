using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace IAM.Queue
{
    public class QueueManager<T>
    {
        public delegate void Processor(T item, Object state);
        public delegate void ProcessorError(T item, Object state, Exception exception);
        public delegate Object StartThread(Int32 threadIndex);
        public delegate void ThreadStop(Int32 threadIndex, Object state);
        public event StartThread OnThreadStart;
        public event ThreadStop OnThreadStop;
        public event ProcessorError OnError;

        private QueueList<T>[] _queue;
        private Thread[] _tList;
        private Boolean _executing = false;
        private Processor _processor = null;
        private Int32 _index = 0;

        public Int32 QueueCount
        {
            get
            {
                Int32 count = 0;
                try
                {
                    for (Int32 i = 0; i < _queue.Length; i++)
                        if (_queue[i] != null)
                            count += _queue[i].Count;
                }
                catch { }
                return count;
            }
        }

        public Int32 ThreadCount
        {
            get
            {
                Int32 count = 0;
                try
                {
                    for (Int32 i = 0; i < _queue.Length; i++)
                        if (_queue[i] != null)
                            count++;
                }
                catch { }
                return count;
            }
        }

        public Int32 ExecutingCount
        {
            get
            {
                Int32 count = 0;
                try
                {
                    for (Int32 i = 0; i < _tList.Length; i++)
                        if (_tList[i] != null)
                            count++;
                }
                catch { }
                return count;
            }
        }

        public String QueueCount2
        {
            get
            {
                String count = "";
                try
                {
                    for (Int32 i = 0; i < _queue.Length; i++)
                        if (_queue[i] != null)
                            count += String.Format("Queue {0:000}: {1}\r\n", i, _queue[i].Count);
                }
                catch { }
                return count;
            }
        }


        public QueueManager(Int32 theadsCount, Processor processor)
        {

            if (processor == null)
                throw new Exception("Processor can not be null");

            if (theadsCount < 0)
                throw new Exception("TheadsCount can not be negative");

            _processor = processor;

            _queue = new QueueList<T>[theadsCount];
            for (Int32 i = 0; i < _queue.Length; i++)
                _queue[i] = new QueueList<T>();
        }

        public void Start()
        {
            _executing = true;

            _tList = new Thread[_queue.Length];

            for (Int32 i = 0; i < _queue.Length; i++)
            {
                _tList[i] = new Thread(new ParameterizedThreadStart(ProcQueue));
                _tList[i].Start(i);
            }

            //Aguarda para início das threads
            Thread.Sleep(500);
        }

        public void Stop()
        {
            _executing = false;
        }


        public void StopAndWait()
        {
            this.Stop();

            Boolean exists = false;
            do
            {
                exists = false;
                for (Int32 i = 0; i < _queue.Length; i++)
                    if ((_queue[i] != null) && (_queue[i].Count > 0))
                    {
                        exists = true;
                        break;
                    }
            } while (exists);
        }


        public void AddItem(T item)
        {
            Int32 cnt = 0;
            Int32 iQueue = _index;
            lock (_queue)
            {
                do
                {
                    _index++;
                    if (_index > _queue.Length - 1) _index = 0;

                    iQueue = _index;
                } while (_queue[_index] == null && cnt < _queue.Length);
            }

            if (_queue[iQueue] == null)
                throw new Exception("All queue objects is null");

            _queue[iQueue].Add(item);

        }

        private void ProcQueue(Object oIndex)
        {
            Int32 index = (Int32)oIndex;
            Object state = null;

            try
            {
                if (OnThreadStart != null)
                    try
                    {
                        state = OnThreadStart(index);
                    }
                    catch (Exception ex)
                    {
                        _queue[index] = null;
                        _tList[index] = null;
                        //throw new Exception("Error on start thread processor", ex);
                        return;
                    }

                while ((_queue[index] != null) && (_executing || _queue[index].Count > 0))
                {
                    T queueItem = (T)((Object)null);

                    while ((queueItem = _queue[index].nextItem) != null)
                        try
                        {
                            if (_processor != null) _processor(queueItem, state);
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                if (OnError != null)
                                    OnError(queueItem, state, ex);
                            }
                            catch { }
                        }

                    Thread.Sleep(100);
                }
            }
            finally
            {
                _queue[index] = null;

                if (OnThreadStop != null)
                    OnThreadStop(index, state);

                state = null;

                _tList[index] = null;
                Thread.CurrentThread.Abort();
            }
        }
    }

    public class QueueList<T>
    {
        private List<T> _logItems;

        public Int32 Count { get { return _logItems.Count; } }

        public QueueList()
        {
            _logItems = new List<T>();
        }

        public void Clear()
        {
            _logItems.Clear();
        }

        public void Add(T queueItem)
        {
            lock (_logItems)
            {
                _logItems.Add(queueItem);
            }
        }

        public T nextItem
        {
            get
            {
                try
                {
                    if (_logItems.Count == 0)
                        return (T)((Object)null);

                    T item = (T)((Object)null);
                    lock (_logItems)
                    {
                        item = _logItems[0];
                        _logItems.RemoveAt(0);
                    }
                    return item;
                }
                catch
                {
                    return (T)((Object)null);
                }
            }
        }
    }
}
