using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ComfileTech.ComfilePi.CP_IO14_N.Demo
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            var uiContext = SynchronizationContext.Current;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var model = File.ReadAllText("/proc/device-tree/model").Trim();
                if (!model.Contains("Compute Module 5 "))
                {
                    MessageBox.Show("This application should only be run on a CPi-G series panel PC.", Text);
                    Environment.Exit(0);
                }

                // Workaround for fullscreen on Raspberry Pi OS Bookworm with Wayland and LabWC
                LocationChanged += (s, e) => { Location = new Point(0, 0); };
            }

            // On Linux, bind the digital input lamps to the IO board's digital inputs
            var lamps = _digitalInputPanel.Controls.OfType<Lamp>().ToArray();

            int index = 0;
            foreach (var input in CP_IO14_N.Instance.DigitalInputs)
            {
                var lamp = lamps[index];
                lamp.State = input.State;
                lamp.Text = input.Number.ToString();

                input.StateChanged += di =>
                {
                    if (uiContext != null)
                    {
                        uiContext.Post(_ =>
                        {
                            if (!IsDisposed && !lamp.IsDisposed)
                            {
                                lamp.State = di.State;
                            }
                        }, null);
                    }
                    else if (!IsDisposed && !lamp.IsDisposed)
                    {
                        lamp.State = di.State;
                    }
                };

                index++;
            }

            // On Linux, bind the digital output buttons to the IO board's digital outputs
            var buttons = _digitalOutputPanel.Controls.OfType<Button>().ToArray();

            index = 0;
            foreach (var output in CP_IO14_N.Instance.DigitalOutputs)
            {
                var button = buttons[index];
                button.State = output.State;
                button.Text = output.Number.ToString();

                button.StateChanged += (s, e) =>
                {
                    output.State = button.State;
                };

                index++;
            }
        }

        private void _repositoryUrl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (sender is LinkLabel linkLabel)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = linkLabel.Text,
                    UseShellExecute = true
                });
            }
        }

        async Task SerialTestAsync(SerialPort port, Label label)
        {
            SetTesting(label, string.Empty);
            var progress = new Progress<string>(text => SetTesting(label, text));

            try
            {
                await Task.Run(() => RunSerialTest(port, progress));
                SetPass(label);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                SetFail(label);
            }
        }

        static void RunSerialTest(SerialPort port, IProgress<string> progress)
        {
            var baudrates = new int[]
            {
                9600,
                19200,
                38400,
                57600,
                115200,
                230400,
                460800
            };

            var parities = new List<Parity>();
            parities.AddRange(
            [
                Parity.None,
                Parity.Odd,
                Parity.Even
            ]);

            lock (port)
            {
                try
                {
                    port.ReadTimeout = 500;
                    port.WriteTimeout = 500;
                    port.ReadBufferSize = 2048;
                    port.WriteBufferSize = 2048;

                    if (!port.IsOpen)
                    {
                        port.Open();
                    }

                    byte[] txBytes = new byte[256];
                    for (int i = 0; i < txBytes.Length; i++)
                    {
                        txBytes[i] = (byte)i;
                    }

                    foreach (var baud in baudrates)
                    {
                        foreach (var parity in parities)
                        {
                            progress.Report($"Testing {baud},{parity}");

                            port.BaudRate = baud;
                            port.Parity = parity;
                            port.DiscardInBuffer();
                            port.DiscardOutBuffer();
                            port.Write(txBytes, 0, txBytes.Length);

                            byte[] rxBytes = new byte[256];
                            int bc = 0;
                            while (bc != txBytes.Length)
                            {
                                bc += port.Read(rxBytes, bc, txBytes.Length - bc);
                            }

                            if (!rxBytes.SequenceEqual(txBytes))
                            {
                                throw new Exception("Data mismatch");
                            }
                        }
                    }
                }
                finally
                {
                    if (port.IsOpen)
                    {
                        port.Close();
                    }
                }
            }
        }



        static void SetTesting(Label label, string text)
        {
            label.Text = text;
            label.ForeColor = Color.White;
            label.Update();
        }

        static void SetPass(Label label)
        {
            label.Text = "PASS";
            label.ForeColor = Color.FromArgb(128, 255, 128);
            label.Update();
        }

        static void SetFail(Label label)
        {
            label.Text = "FAIL";
            label.ForeColor = Color.FromArgb(255, 128, 128);
            label.Update();
        }

        private void _closeButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private async void _serial2Button_Click(object sender, EventArgs e)
        {
            _serialButton.Enabled = false;
            try
            {
                await SerialTestAsync(CP_IO14_N.Instance.SerialPorts[0], _serialResult);
            }
            finally
            {
                if (!IsDisposed)
                {
                    _serialButton.Enabled = true;
                }
            }
        }


    }
}
