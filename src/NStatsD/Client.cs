using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NStatsD
{
    public sealed class Client : IDisposable
    {
        #region Thread Local Randomization - yields better results for concurrent apps (different seed values on different threads)

        private static int _seed = Environment.TickCount;
        private static readonly ThreadLocal<Random> Rng = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref _seed)));

        #endregion

        internal UdpClient UdpClient;
        private readonly string _prefix;
        private readonly bool _enabled;

        Client()
        {
            var host = GetAppSetting("NStatsD.Host", "localhost");
            var port = GetAppSetting("NStatsD.Port", 8125);
            
            _enabled = GetAppSetting("NStatsD.Enabled", true);
            _prefix = ValidatePrefix(GetAppSetting("NStatsD.Prefix", ""));

            UdpClient = new UdpClient(host, port);
        }

        public static Client Current
        {
            get { return CurrentClient.Instance.Value; }
        }

        static class CurrentClient
        {
            static CurrentClient() { }

            internal static readonly Lazy<Client> Instance = new Lazy<Client>(() => new Client(), true);
        }

        private static string ValidatePrefix(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                return prefix;

            if (prefix.EndsWith("."))
                return prefix;

            return string.Format("{0}.", prefix);
        }

        /// <summary>
        /// Sends timing statistics.
        /// </summary>
        /// <param name="stat">Name of statistic being updated.</param>
        /// <param name="time">The timing it took to complete.</param>
        /// <param name="sampleRate">Tells StatsD how often to sample this value. Defaults to 1 (send all values).</param>
        public void Timing(string stat, long time, double sampleRate = 1)
        {
            var data = new Dictionary<string, string> { { stat, string.Format("{0}|ms", time) } };

            Send(data, sampleRate);
        }

        /// <summary>
        /// Increments a counter
        /// </summary>
        /// <param name="stat">Name of statistic being updated.</param>
        /// <param name="sampleRate">Tells StatsD how often to sample this value. Defaults to 1 (send all values).</param>
        public void Increment(string stat, double sampleRate = 1)
        {
            UpdateStats(stat, 1, sampleRate);
        }

        /// <summary>
        /// Decrements a counter
        /// </summary>
        /// <param name="stat">Name of statistic being updated.</param>
        /// <param name="sampleRate">Tells StatsD how often to sample this value. Defaults to 1 (send all values).</param>
        public void Decrement(string stat, double sampleRate = 1)
        {
            UpdateStats(stat, -1, sampleRate);
        }

        /// <summary>
        /// Updates a counter by an arbitrary amount
        /// </summary>
        /// <param name="stat">Name of statistic being updated.</param>
        /// <param name="value">The value of the metric.</param>
        /// <param name="sampleRate">Tells StatsD how often to sample this value. Defaults to 1 (send all values).</param>
        public void Gauge(string stat, int value, double sampleRate = 1)
        {
            var data = new Dictionary<string, string> { { stat, string.Format("{0}|g", value) } };
            Send(data, sampleRate);
        }

        /// <summary>
        /// Updates a counter by an arbitrary amount
        /// </summary>
        /// <param name="stat">Name of statistic(s) being updated.</param>
        /// <param name="delta">The amount to adjust the counter</param>
        /// <param name="sampleRate">Tells StatsD how often to sample this value. Defaults to 1 (send all values).</param>
        public void UpdateStats(string stat, int delta = 1, double sampleRate = 1)
        {
            var dictionary = new Dictionary<string, string> { { stat, string.Format("{0}|c", delta) } };
            Send(dictionary, sampleRate);
        }

        private void Send(Dictionary<string, string> data, double sampleRate)
        {
            if (!_enabled)
                return;

            if (sampleRate < 1)
            {
                var nextRand = Rng.Value.NextDouble(); //offers superior randomization for concurrent systems
                if (nextRand <= sampleRate)
                {
                    var sampledData = data.Keys.ToDictionary(stat => stat,
                        stat => string.Format("{0}|@{1}", data[stat], sampleRate));
                    SendToStatsD(sampledData);
                }
            }
            else
            {
                SendToStatsD(data);
            }
        }

        private void SendToStatsD(Dictionary<string, string> sampledData)
        {
            foreach (var stat in sampledData.Keys)
            {
                var stringToSend = string.Format("{0}{1}:{2}", _prefix, stat, sampledData[stat]);
                var sendData = Encoding.ASCII.GetBytes(stringToSend);
                UdpClient.Send(sendData, sendData.Length);
            }
        }

        private static T GetAppSetting<T>(string key, T defaultValue)
        {
            var value = ConfigurationManager.AppSettings[key];
            try
            {
                if (value != null)
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
            }
            catch { }

            return defaultValue;
        }

        #region IDisposable

        public bool WasDisposed { get; private set; }

        public void Dispose(bool isDisposing)
        {
            if (isDisposing && !WasDisposed)
            {
                WasDisposed = true;
                try
                {
                    using (UdpClient) { }
                }
                catch { }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        ~Client()
        {
            GC.SuppressFinalize(this);
            Dispose();
        }

        #endregion
    }
}
