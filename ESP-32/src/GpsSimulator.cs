using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace XMasDevice
{
    /// <summary>
    /// The GPS simulator.
    /// </summary>
    public class GpsSimulator
    {
        #region Delegate

        public delegate void PointCallbackDelegate (double latitude, double longitude);

        #endregion

        #region Fields

        private Thread                  thread      = null;
        private ManualResetEvent        syncClose   = null;
        private readonly ILogger        logger      = null;
        private readonly Random         Random      = new Random(DateTime.UtcNow.Second);
        private double                  latitude    = 41.82141979802636;
        private double                  longitude   = 12.45875158194143;

        private PointCallbackDelegate   pointCallback;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether this instance is running.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is running; otherwise, <c>false</c>.
        /// </value>
        public bool IsRunning { get; private set; } = false;

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the specified point callback.
        /// </summary>
        /// <param name="pointCallback">The point callback.</param>
        public void Start(PointCallbackDelegate pointCallback)
        {
            try
            {
                if (thread != null)
                {
                    Stop();
                    thread = null;
                }

                this.pointCallback = pointCallback;

                IsRunning  = true;
                thread     = new Thread(ThreadProc);
                syncClose  = new ManualResetEvent(false);

                thread.Start();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"{nameof(Start)} failed !");
            }
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            try
            {
                if (thread == null)
                    return;

                IsRunning = false;
                Thread.Sleep(0);
                syncClose.WaitOne(1000, true);

                if (thread.IsAlive)
                    thread.Abort();

                thread = null;
            }
            catch (ThreadAbortException ex)
            {
                logger.LogError(ex, $"{nameof(Stop)} failed !"); ;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"{nameof(Stop)} failed !");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Threads the proc.
        /// </summary>
        private void ThreadProc()
        {
            do
            {
                Thread.Sleep(1000);

                IncrementPosition();

                if (pointCallback != null)
                    pointCallback.Invoke(latitude, longitude);

            } while (IsRunning);

            syncClose.Set();
        }

        /// <summary>
        /// Increments the position.
        /// </summary>
        private void IncrementPosition()
        {
            int sec = DateTime.UtcNow.Second;
            
            if (sec % 2 == 0)
                latitude  += (Random.Next(100) * 0.00001);
            else
                longitude -= (Random.Next(100) * 0.00001);

            if (sec % 10 == 0)
                longitude += (Random.Next(200) * 0.00001);
        }

        #endregion
    }
}
