using System;
using System.Diagnostics;

namespace Demo
{
    class Program
    {
        static void Main()
        {
            var timer = Stopwatch.StartNew();

            Console.WriteLine("Writing to StatsD");
            NStatsD.Client.Current.Increment("test.increment");
            NStatsD.Client.Current.Decrement("test.decrement");
            NStatsD.Client.Current.Timing("test.increment", timer.ElapsedMilliseconds);
            NStatsD.Client.Current.Gauge("test.gauge", 25);
            Console.WriteLine("Done! Press any key to exit.");

            Console.Read();
        }
    }
}
