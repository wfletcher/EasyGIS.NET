using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace egis
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            MainForm f = new MainForm();            
            if (args.Length == 1 && !string.IsNullOrEmpty(args[0]))
            {
                if (args[0].EndsWith(".egp", StringComparison.OrdinalIgnoreCase))
                {
                    f.OpenProject(args[0]);
                }
                else if (args[0].EndsWith(".shp", StringComparison.OrdinalIgnoreCase) ||
                    args[0].EndsWith(".shpx", StringComparison.OrdinalIgnoreCase))
                {
                    f.OpenShapeFile(args[0]);
                }
            }

            Application.Run(f);

        }
        
    }
}