using Iot.Device.Imu;
using Microsoft.Extensions.Logging;
using nanoFramework.Hardware.Esp32;
using nanoFramework.Json;
using nanoFramework.Logging.Debug;
using nanoFramework.Networking;
using System;
using System.Device.Gpio;
using System.Device.I2c;
using System.Diagnostics;
using System.Threading;

namespace XMasDevice
{
    /// <summary>
    /// The application.
    /// </summary>
    public class Application
    {
        #region Constants

        private readonly double A  = 0.962;
        private readonly double DT = 0.020;
        private readonly double PI = 3.14159265358979323846;


        private const int ReadyLedPin       = Gpio.IO23;
        private const int GiftButtonPin     = Gpio.IO15;

        private const int GyroscopeDataPin  = Gpio.IO21;
        private const int GyroscopeClockPin = Gpio.IO22;

        #endregion

        #region Fields

        private static Application      instance;

        private bool                    isWiFiConnected;
        private bool                    isReady;    
        private readonly DebugLogger    logger;
        private GpioPin                 giftButton;
        private GpioPin                 readyLed;
        private readonly GpioController gpioController;
        private System.Numerics.Vector3 gyroData;
        private System.Numerics.Vector3 accData;
        private double                  pitch;
        private double                  roll;
        private double                  yaw;    
        private int                     giftCount;    
        private Mpu6050                 gyro;
        private int                     gyroXAngle;
        private int                     gyroYAngle;
        private int                     gyroZAngle;
        private readonly GpsSimulator   gpsSimulator;
        private double                  latitude;
        private double                  longitude;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>
        /// The instance.
        /// </value>
        public static Application Instance
        {
            get
            {
                if (instance == null)
                    instance = new Application();

                return instance;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is ready.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is ready; otherwise, <c>false</c>.
        /// </value>
        public bool IsReady => isReady;

        #endregion

        #region Constructors

        public Application()
        {
            logger         = new DebugLogger(nameof(Application));
            gpioController = new GpioController();
            gpsSimulator   = new GpsSimulator();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Runs this instance.
        /// </summary>
        public void Run()
        {
            try
            {
                logger.LogInformation(">>> Application.Run <<<");

                InitializeGpio();

                ConfigureI2C();

                ConnectToWiFi();

                if (isWiFiConnected)
                {
                    AzureClient.Instance.CreateDeviceClient();
                    isReady = AzureClient.Instance.IsConnect;
                }

                SetReadyLed(AzureClient.Instance.IsConnect);

                gpsSimulator.Start((latitude, longitude) =>
                {
                    this.latitude  = latitude;
                    this.longitude = longitude;

                    SendTelemetry();
                });

                int index = 0;

                while (true)
                {
                    if (++index % 5000 == 0)
                    {
                        index = 0;

                        if (isReady != AzureClient.Instance.IsConnect)
                        {
                            isReady = AzureClient.Instance.IsConnect;
                            SetReadyLed(isReady);
                        }
                    }

                    CalculateGyroData();

                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            finally
            {
                logger.LogInformation("<<< Application.Run");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Connects to wi fi.
        /// </summary>
        private void ConnectToWiFi()
        {
            CancellationTokenSource cs = new(30000);

            isWiFiConnected = WifiNetworkHelper.ConnectDhcp(Constants.WiFiSSD,
                                                            Constants.WiFiPassword,
                                                            requiresDateTime: true,
                                                            token: cs.Token);

            logger.LogInformation($"ConnectToWiFi: {isWiFiConnected}");

            // wait to internal syncronize time & so on...
            //
            if (isWiFiConnected)
                Thread.Sleep(500);
        }

        /// <summary>
        /// Configures the i2c.
        /// </summary>
        private void ConfigureI2C()
        {
            Configuration.SetPinFunction(21, DeviceFunction.I2C1_DATA);
            Configuration.SetPinFunction(22, DeviceFunction.I2C1_CLOCK);

            I2cConnectionSettings mpui2CConnectionSettingmpus = new(1, Mpu6050.DefaultI2cAddress);
            gyro = new Mpu6050(I2cDevice.Create(mpui2CConnectionSettingmpus));
            gyro.FifoModes = FifoModes.GyroscopeY | FifoModes.GyroscopeX;

            logger.LogInformation("ConfigureI2C done");
        }

        /// <summary>
        /// Sends the telemetry.
        /// </summary>
        private void SendTelemetry()
        {
            if (!isWiFiConnected || !AzureClient.Instance.IsConnect)
            {
                logger.LogInformation("Skip event for connection broken reason");
                return;
            }

            SleighTelemetryData telemetry = new()
            {
                Date           = DateTime.UtcNow,
                Latitude       = latitude,
                Longitude      = longitude,
                GyroX          = gyroXAngle,
                GyroY          = gyroYAngle,
                GyroZ          = gyroZAngle,
                GiftsDelivered = giftCount
            };

            string message = JsonConvert.SerializeObject(telemetry);
            AzureClient.Instance.EnqueueMessage(message);
        }

        /// <summary>
        /// Initializes the gpio.
        /// </summary>
        private void InitializeGpio()
        {
            giftButton = gpioController.OpenPin(GiftButtonPin, PinMode.InputPullUp);
            giftButton.ValueChanged += OnButtonValueChanged;

            readyLed = gpioController.OpenPin(ReadyLedPin, PinMode.Output);

            logger.LogInformation("InitializeGpio done");
        }

        /// <summary>
        /// Sets the ready led.
        /// </summary>
        /// <param name="active">if set to <c>true</c> [active].</param>
        private void SetReadyLed(bool active)
        {
            readyLed.Write(active ? PinValue.High : PinValue.Low);
        }

        /// <summary>
        /// Called when [button value changed].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="PinValueChangedEventArgs"/> instance containing the event data.</param>
        private void OnButtonValueChanged(object sender, PinValueChangedEventArgs e)
        {
            try
            {
                if (e.ChangeType ==  PinEventTypes.Falling)
                {
                    giftCount++;
                    logger.LogInformation($"GiftCount: {giftCount}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }
        }

        /// <summary>
        /// Calculates the gyro.
        /// </summary>
        private void CalculateGyroData()
        {
            // https://forum.arduino.cc/t/converting-raw-data-from-mpu-6050-to-yaw-pitch-and-roll/465354/8
            // http://www.starlino.com/imu_guide.html
            // https://ozeki.hu/p_3010-how-to-setup-a-gyroscope-on-raspberry-pi.html

            gyroData = gyro.GetGyroscopeReading();
            accData  = gyro.GetAccelerometer();

            double rollangle  = Math.Atan2(accData.Y, accData.Z) * 180 / PI;
            double pitchangle = Math.Atan2(accData.X, Math.Sqrt(accData.Y * accData.Y + accData.Z * accData.Z)) * 180 / PI;
            roll  = A * (roll  + gyroData.X * DT) + (1 - A) * rollangle;
            pitch = A * (pitch + gyroData.Y * DT) + (1 - A) * pitchangle;
            yaw   = gyroData.Z;

            // logger.LogInformation($"roll {roll} - pitch {pitch} - yaw {yaw}");

            gyroXAngle = (int)(roll  - 16); // quite value
            gyroYAngle = (int)(pitch - 3);  // quite value
            gyroZAngle = 0;

            if (gyroYAngle < -45)
                gyroYAngle = -45;
            if (gyroYAngle > 45)
                gyroYAngle = 45;

            if (gyroXAngle < -45)
                gyroXAngle = -45;
            if (gyroXAngle > 45)
                gyroXAngle = 45;

            if (Math.Abs(gyroXAngle) < 5) gyroXAngle = 0;
            if (Math.Abs(gyroYAngle) < 5) gyroXAngle = 0;

            // if (gyroYAngle != 0 || gyroXAngle != 0)
            //    logger.LogInformation($"Y {gyroYAngle} - X {gyroXAngle} - Z {gyroZAngle}");
        }

        #endregion
    }
}
