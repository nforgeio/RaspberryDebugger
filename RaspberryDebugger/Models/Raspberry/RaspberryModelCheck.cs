using System.Linq;
using System.Collections.Generic;

namespace RaspberryDebugger.Models.Raspberry
{
    public class RaspberryModelCheck
    {
        public string ActualType { get; private set; }

        public List<string> Supported { get; }

        public RaspberryModelCheck()
        {
            Supported = new List<string>()
            {
                "Raspberry Pi 3 Model",
                "Raspberry Pi 4 Model",
                "Raspberry Pi Compute Module 4",
                "Raspberry Pi Zero 2"
            };
        }

        public bool IsNotSupported(string raspberryType)
        {
            ActualType = raspberryType;

            return !Supported.Any(raspberryType.StartsWith);
        }
    }
}
