using Microsoft.Extensions.Logging;
using nanoFramework.Azure.Devices.Client;
using nanoFramework.Logging.Debug;
using System.Security.Cryptography.X509Certificates;


namespace XMasDevice
{
    /// <summary>
    /// The Azure client.
    /// </summary>
    public class AzureClient
    {
        #region Fields

        private readonly DebugLogger logger;
        private DeviceClient         client;
        private static AzureClient   instance;
        private readonly SyncQueue   queue;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>
        /// The instance.
        /// </value>
        public static AzureClient Instance
        {
            get
            {
                if (instance == null)
                    instance = new AzureClient();
                return instance;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is connect.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is connect; otherwise, <c>false</c>.
        /// </value>
        public bool IsConnect => client != null && client.IsConnected;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureClient"/> class.
        /// </summary>
        public AzureClient()
        {
            logger = new DebugLogger(nameof(AzureClient));
            queue  = new SyncQueue(logger, OnDequeueItem);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates the device client.
        /// </summary>
        public void CreateDeviceClient()
        {
            try
            {
                logger.LogInformation("CreateDeviceClient is starting...");


                client = new DeviceClient(Constants.IoTBrokerAddress, 
                                            Constants.DeviceID, 
                                            Constants.SasKey, 
                                            azureCert: new X509Certificate(Resources.GetString(Resources.StringResources.AzureRootCerts)));
                // add callbacks
                //
                client.StatusUpdated += OnStatusUpdated;
                client.AddMethodCallback(GetStatus);

                // Open it and continue like for the previous sections
                //
                bool res = client.Open();

                if (!res)
                {
                    logger.LogError($"can't open the device client");
                    return;
                }

                logger.LogInformation("CreateDeviceClient done");
            }
            catch (System.Exception ex)
            {
                logger.LogError(ex.ToString());
            }
        }

        /// <summary>
        /// Disposes the device client.
        /// </summary>
        public void DisposeDeviceClient()
        {
            if (client == null)
                return;

            queue.StopQueueThread();

            client.Close();
            client.Dispose();
            client = null;
        }

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public bool SendMessage(string message)
        {
            if (client == null || !client.IsConnected)
                return false;

            client.SendMessage(message);

            return true; // fire & forget !
        }

        /// <summary>
        /// Enqueues the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public bool EnqueueMessage(string message)
        {
            if (client == null || !client.IsConnected)
                return false;

            if (!queue.IsRunning)
                queue.StartQueueThread();

            queue.Enqueue(message);

            return true;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Called when [status updated].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="StatusUpdatedEventArgs"/> instance containing the event data.</param>
        private void OnStatusUpdated(object sender, StatusUpdatedEventArgs e)
        {
            try
            {
                logger.LogInformation($"OnStatusUpdated");

                if (e.IoTHubStatus != null)
                {
                    logger.LogInformation($"IoTHub Status changed: {e.IoTHubStatus.Status}");
                    if (e.IoTHubStatus.Message != null)
                        logger.LogInformation($"IoTHub Status message: {e.IoTHubStatus.Message}");
                }

                // You may want to reconnect or use a similar retry mechanism
                //
                if (e.IoTHubStatus.Status == Status.Disconnected)
                {
                    logger.LogInformation("OnStatusUpdated -> IoTHub Stoppped !");
                }
            }
            catch (System.Exception ex)
            {
                logger.LogError(ex.ToString());
            }
        }

        /// <summary>
        /// Gets the status.
        /// </summary>
        /// <param name="rid">The rid.</param>
        /// <param name="payload">The payload.</param>
        /// <returns></returns>
        private string GetStatus(int rid, string payload)
        {
            string ret = string.Empty;

            try
            {
                logger.LogInformation($"GetStatus -> rid: {rid} - payload: {payload}");

                ret = $"{{\"status\":{Application.Instance.IsReady.ToString().ToLower()}}}";
            }
            catch (System.Exception ex)
            {
                logger.LogError(ex.ToString());
            }

            return ret;
        }

        /// <summary>
        /// Called when [dequeue item].
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        private bool OnDequeueItem(object message)
        {
            return SendMessage(message as string);
        }


        #endregion
    }
}
