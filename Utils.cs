using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PicarX
{
    internal class Utils
    {
        public static void reset_mcu()
        {
            var mcu_reset = new Pin("MCURST", System.Device.Gpio.PinMode.Output);
            mcu_reset.off();
            Thread.Sleep(10);
            mcu_reset.on();
            Thread.Sleep(200);
        }
    }
}
