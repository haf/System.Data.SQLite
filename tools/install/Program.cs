using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace install
{
  static class Program
  {
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      new InstallDesigner().ShowDialog();
    }
  }
}