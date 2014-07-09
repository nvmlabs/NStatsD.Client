using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NStatsD;

namespace PerformanceTests
{
    class Program
    {
        static void Main(string[] args)
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
            Console.WriteLine("Done queuing messages... waiting for queue to drain");
            while (Client.Current.Connection.MessagesInSendQueue > 0)
            {
                Thread.Sleep(10);
            }
            Console.WriteLine("Done, press any key to exit");
            stopwatch.Stop();
            Console.WriteLine("Took {0} seconds to complete", stopwatch.Elapsed.TotalSeconds);
            Console.ReadKey();
            return;
        }
    }
}
