using System;
using System.Diagnostics;
using System.Threading;

namespace Blinkie
{
    class Program
    {
        static void Main(string[] args)
        {
            Debugger.Break();

            var var = Environment.GetEnvironmentVariable("TEST");

            while (true)
            {
                Console.WriteLine("Hello World!");
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }
    }
}
