using System;
using System.Windows.Forms;

namespace test
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
    }

		public void WriteLine(string str)
		{
			textBox1.Text += str + "\r\n";
		}

    public void Write(string str)
    {
      textBox1.Text += str;
    }

    private void menuItem1_Click(object sender, EventArgs e)
    {
      Close();
    }
	}
}