using System.Collections.Generic;
using System.IO.Ports;

namespace ComfileTech.ComfilePi.CP_IO14_N
{
    /// <summary>
    /// Represents the CP-IO14-N IO board connected to the ComfilePi.
    /// </summary>
    public class CP_IO14_N : System.IDisposable
    {
        /// <summary>
        /// The singleton instance of this class.
        /// </summary>
        public static CP_IO14_N Instance { get; } = new CP_IO14_N();

        private CP_IO14_N()
        {
            // Create the digital inputs
            {
                var list = new List<DigitalInput>();
                for (int i = 6; i <= 7; i++)
                {
                    list.Add(new DigitalInput(i));
                }
                for (int i = 10; i <= 11; i++)
                {
                    list.Add(new DigitalInput(i));
                }
                for (int i = 16; i <= 17; i++)
                {
                    list.Add(new DigitalInput(i));
                }
                for (int i = 20; i <= 21; i++)
                {
                    list.Add(new DigitalInput(i));
                }
                DigitalInputs = list.AsReadOnly();
            }

            // Create the digital outputs
            {
                var list = new List<DigitalOutput>();
                for (int i = 22; i <= 27; i++)
                {
                    list.Add(new DigitalOutput(i));
                }
                DigitalOutputs = list.AsReadOnly();
            }

            // Create the serial port
            {
                var list = new List<SerialPort>();
                list.Add(new SerialPort("/dev/serial2"));
                SerialPorts = list.AsReadOnly();
            }
        }

        /// <summary>
        /// The digital inputs for the IO board.
        /// </summary>
        public IReadOnlyList<DigitalInput> DigitalInputs
        {
            get;
        }

        /// <summary>
        /// The digital outputs for the IO board.
        /// </summary>
        public IReadOnlyList<DigitalOutput> DigitalOutputs
        {
            get;
        }

        /// <summary>
        /// The serial ports for the IO board.
        /// </summary>
        public IReadOnlyList<SerialPort> SerialPorts
        {
            get;
        }

        /// <summary>
        /// The Linux I2C bus number for the IO board.
        /// </summary>
        public int I2cBusId
        {
            get;
        } = 1;

        /// <summary>
        /// The I2C slave address for the IO board.
        /// </summary>
        public int I2cSlaveAddress
        {
            get;
        } = 0x20;

        bool _disposed;

        /// <summary>
        /// Releases the GPIO and serial port resources used by the IO board.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            DigitalInput.DisposeGpioController();
            DigitalOutput.DisposeGpioController();

            foreach (var port in SerialPorts)
            {
                port.Dispose();
            }

            _disposed = true;
            System.GC.SuppressFinalize(this);
        }
    }
}
