using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.Threading;

namespace Blinkie
{
    class Program
    {
        static void Main(string[] args)
        {
            var var = Environment.GetEnvironmentVariable("TEST");      

            using (var gpio = new GpioController(PinNumberingScheme.Logical))
            {
                var interval = TimeSpan.FromSeconds(0.5);
                var pin      = 5;

                gpio.OpenPin(pin, PinMode.Output);

                while (true)
                {
                    Console.WriteLine("Hello World!");

                    gpio.Write(pin, PinValue.High);
                    Thread.Sleep(interval);
                    gpio.Write(pin, PinValue.Low);
                    Thread.Sleep(interval);
                }
            }
        }
    }
}

