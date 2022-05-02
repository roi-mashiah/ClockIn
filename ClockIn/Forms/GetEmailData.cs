using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClockIn.Forms
{
    public partial class GetEmailData : Form
    {
        private string _password;

        public string Password
        {
            get { return _password; }
            set { _password = value; }
        }

        public GetEmailData()
        {
            InitializeComponent();
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                button1.PerformClick();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _password = textBox2.Text;
            this.Close();
        }
    }
}
