using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace GoogleCalendarTaskTracker
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ConfigurationManager.ReadFromFile();

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
            catch(Exception ex)
            {
                File.WriteAllText("stack_dump.txt", ex.ToString());
                MessageBox.Show("A fatal error has occured. Please refer to your support contact for additional information about this error." + Environment.NewLine
                    + "A stack trace has been generated and saved to the directory that the program was executed from." + Environment.NewLine
                    + ex.ToString());
            }

            ConfigurationManager.SaveToFile();
        }
    }
}
