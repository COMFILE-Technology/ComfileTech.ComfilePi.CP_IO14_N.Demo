using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ComfileTech.ComfilePi.CP_IO14_N;

namespace ComfileTech.ComfilePi.CP_IO14_N.Demo
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                Application.Run(new Form1());
            }
            finally
            {
                CP_IO14_N.Instance.Dispose();
            }
        }
    }
}
