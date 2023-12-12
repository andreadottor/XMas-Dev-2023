
namespace XMasDevice
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections;
    using System.Threading;

    public class SyncQueue
    {
        #region Delegate

        public delegate bool DequeueCallback(object arg);

        #endregion

        #region Fields

        private Thread                  threadQueue  = null;
        private readonly Queue          queue        = null;
        private readonly AutoResetEvent syncDequeue  = null;
        private ManualResetEvent        syncClose    = null;
        private readonly ILogger        logger       = null;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>
        /// The count.
        /// </value>
        public int Count
        {
            get
            {
                lock (queue.SyncRoot)
                {
                    return (queue.Count);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is running.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is running; otherwise, <c>false</c>.
        /// </value>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Gets or sets the thread polling time.
        /// </summary>
        /// <value>
        /// The thread polling time.
        /// </value>
        public int ThreadPollingTime { get; set; } = 100;

        /// <summary>
        /// Gets the dequeue delegate.
        /// </summary>
        /// <value>
        /// The dequeue delegate.
        /// </value>
        public DequeueCallback DequeueDelegate { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncQueue"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="dequeueDelegate">The dequeue delegate.</param>
        public SyncQueue(ILogger logger, DequeueCallback dequeueDelegate)
        {
            queue       = new Queue();
            syncDequeue = new AutoResetEvent(false);

            this.logger          = logger;
            this.DequeueDelegate = dequeueDelegate;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            lock (queue.SyncRoot)
            {
                queue.Clear();
            }
        }

        /// <summary>
        /// Enqueues the specified object.
        /// </summary>
        /// <param name="obj">The object.</param>
        public void Enqueue(object obj)
        {
            lock (queue.SyncRoot)
            {
                queue.Enqueue(obj);
            }
            syncDequeue.Set();
        }

        /// <summary>
        /// Dequeues this instance.
        /// </summary>
        /// <returns></returns>
        public object Dequeue()
        {
            lock (queue.SyncRoot)
            {
                return queue.Dequeue();
            }
        }

        /// <summary>
        /// Peeks this instance.
        /// </summary>
        /// <returns></returns>
        public object Peek()
        {
            lock (queue.SyncRoot)
            {
                return queue.Peek();
            }
        }

        /// <summary>
        /// Starts the queue thread.
        /// </summary>
        public void StartQueueThread()
        {
            try
            {
                if (threadQueue != null)
                {
                    StopQueueThread();
                    threadQueue = null;
                }

                IsRunning       = true;
                threadQueue     = new Thread(QueueThreadProc);
                syncClose       = new ManualResetEvent(false);
                
                threadQueue.Start();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"{nameof(StartQueueThread)} failed !");
            }
        }

        /// <summary>
        /// Stops the queue thread.
        /// </summary>
        public void StopQueueThread()
        {
            try
            {
                if (threadQueue == null)
                    return;

                IsRunning = false;
                Thread.Sleep(0);
                syncClose.WaitOne(1000, true);

                if (threadQueue.IsAlive)
                    threadQueue.Abort();

                threadQueue = null;
            }
            catch (ThreadAbortException ex)
            {
                logger.LogError(ex, $"{nameof(StartQueueThread)} failed !"); ;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"{nameof(StartQueueThread)} failed !");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Queues the thread proc.
        /// </summary>
        private void QueueThreadProc()
        {
            bool signal = false;

            do
            {
                signal = syncDequeue.WaitOne(ThreadPollingTime, false);

                if (IsRunning == false)
                    break;

                if (!signal)
                    continue;

                while (Count > 0)
                {
                    if (IsRunning == false)
                        break;

                    object tmp = Peek();                        

                    try
                    {
                        if (DequeueDelegate != null)
                        {
                            if (DequeueDelegate(tmp))
                                Dequeue();
                        }
                        else
                            Dequeue();
                    }
                    catch(Exception ex) 
                    {
                        logger.LogError(ex, "Call dequeueCallback failed !");
                    }

                    Thread.Sleep(100);
                }

            } while (IsRunning);

            syncClose.Set();
        }

        #endregion
    }
}
