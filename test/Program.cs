/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

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

            string fileName = "test.db"; // NOTE: New default, was "Test.db3".
            bool autoRun = false;

            if (args != null)
            {
                int length = args.Length;

                for (int index = 0; index < length; index++)
                {
                    string arg = args[index];

                    if (arg != null)
                    {
                        arg = arg.TrimStart(new char[] { '-', '/' });

                        if (String.Equals(arg, "fileName",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            index++;

                            if (index < length)
                                fileName = args[index];
                        }
                        else if (String.Equals(arg, "autoRun",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            autoRun = true;
                        }
                    }
                }
            }

            Application.Run(new TestCasesDialog(fileName, autoRun));
        }
    }
}
