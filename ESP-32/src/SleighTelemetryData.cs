using System;

namespace XMasDevice
{
    /// <summary>
    /// The SleighTelemetryData record.
    /// </summary>
    public class SleighTelemetryData
    {
        /// <summary>
        /// Gets or sets the date.
        /// </summary>
        /// <value>
        /// The date.
        /// </value>
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the latitude.
        /// </summary>
        /// <value>
        /// The latitude.
        /// </value>
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude.
        /// </summary>
        /// <value>
        /// The longitude.
        /// </value>
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets the gyro x.
        /// </summary>
        /// <value>
        /// The gyro x.
        /// </value>
        public double GyroX { get; set; }

        /// <summary>
        /// Gets or sets the gyro y.
        /// </summary>
        /// <value>
        /// The gyro y.
        /// </value>
        public double GyroY { get; set; }

        /// <summary>
        /// Gets or sets the gyro z.
        /// </summary>
        /// <value>
        /// The gyro z.
        /// </value>
        public double GyroZ { get; set; }

        /// <summary>
        /// Gets or sets the gifts delivered.
        /// </summary>
        /// <value>
        /// The gifts delivered.
        /// </value>
        public int GiftsDelivered { get; set; }
    }
}