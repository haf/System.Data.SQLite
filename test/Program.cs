using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace test
{
    class Program
    {
        [STAThread()]
        static void Main(string[] args)
        {
            if (Environment.GetEnvironmentVariable("BREAK") != null)
            {
                Console.WriteLine(
                    "Attach a debugger to process {0} and press any key to continue.",
                    Process.GetCurrentProcess().Id);

                try
                {
                    Console.ReadKey(true); /* throw */
                }
                catch (InvalidOperationException) // Console.ReadKey
                {
                    // do nothing.
                }

                Debugger.Break();
            }

            bool autoRun = false;

            if ((args != null) && (args.Length > 0))
            {
                string arg = args[0];

                if (arg != null)
                {
                    arg = arg.TrimStart(new char[] { '-', '/' });

                    if (String.Equals(arg, "autoRun",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        autoRun = true;
                    }
                }
            }

            Application.Run(new TestCasesDialog(autoRun));
        }
    }
}
