using System;
using System.Diagnostics;
using NStatsD;

namespace PerformanceTests
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("We're going to write a ton of data to the console. 100k iterations.");
            Console.WriteLine("Going!");
            var stopwatch = Stopwatch.StartNew();
            var i = 0;
            while(i < 100000)
            {
                Client.Current.Increment("performancetest.increment");
                i++;
            }
            Console.WriteLine("Done sending messages... waiting for queue to drain");
            Console.WriteLine("Done, press any key to exit");
            stopwatch.Stop();
            Console.WriteLine("Took {0} seconds to complete", stopwatch.Elapsed.TotalSeconds);
            Console.ReadKey();
        }
    }
}
