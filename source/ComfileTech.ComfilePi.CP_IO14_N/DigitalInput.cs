using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Runtime.InteropServices;
using System.Text;

namespace ComfileTech.ComfilePi.CP_IO14_N
{
    /// <summary>
    /// A digital input pin on the IO board.
    /// </summary>
    public class DigitalInput : DigitalInputOutput<DigitalInput>
    {
        internal DigitalInput(int number)
            : base(number, PinMode.Input)
        { }

        internal static void DisposeGpioController()
        {
            DisposeGpioControllerCore();
        }
    }
}
