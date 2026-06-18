using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;

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
                ConfigureSerialPortPins();
                var list = new List<SerialPort>
                {
                    new SerialPort("/dev/serial3")
                };
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

        static void ConfigureSerialPortPins()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return;
            }

            ConfigureSerialPortPin(4, "UART2_TX", "TXD2");
            ConfigureSerialPortPin(5, "UART2_RX", "RXD2");
        }

        static void ConfigureSerialPortPin(int gpio, params string[] functionNames)
        {
            var altFunction = GetPinctrlAltFunction(gpio, functionNames);
            RunPinctrl("set", gpio.ToString(), $"a{altFunction}", "pn");
        }

        static int GetPinctrlAltFunction(int gpio, params string[] functionNames)
        {
            var output = RunPinctrl("funcs", gpio.ToString());
            var parts = output
                .Split(',')
                .Select(part => part.Trim())
                .ToArray();

            if (parts.Length < 3)
            {
                throw new InvalidOperationException($"Unexpected pinctrl funcs output for GPIO{gpio}: {output.Trim()}");
            }

            for (int i = 2; i < parts.Length; i++)
            {
                if (functionNames.Any(name => string.Equals(parts[i], name, StringComparison.OrdinalIgnoreCase)))
                {
                    return i - 2;
                }
            }

            throw new InvalidOperationException($"GPIO{gpio} does not advertise any of the expected UART functions: {string.Join(", ", functionNames)}");
        }

        static string RunPinctrl(params string[] arguments)
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "pinctrl",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            foreach (var argument in arguments)
            {
                process.StartInfo.ArgumentList.Add(argument);
            }

            try
            {
                process.Start();
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is FileNotFoundException)
            {
                throw new InvalidOperationException("Unable to run pinctrl to configure UART GPIO pins. Make sure Raspberry Pi OS pinctrl is installed.", ex);
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"pinctrl {string.Join(" ", arguments)} failed with exit code {process.ExitCode}: {error.Trim()}");
            }

            return output;
        }

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
