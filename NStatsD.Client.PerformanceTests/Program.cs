using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NStatsD;

namespace PerformanceTests
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("We're going to write a ton of data to the console. 10 million iterations.");
            Console.WriteLine("Going!");
            var i = 0;
            while(i < 10000000)
            {
                Client.Current.Increment("performancetest.increment");
                i++;
            }
            Console.WriteLine("Done, press any key to exit");
            Console.ReadKey();
            return;
        }
    }
}
