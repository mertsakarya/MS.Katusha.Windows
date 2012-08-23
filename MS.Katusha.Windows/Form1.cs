using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MS.Katusha.SDK;

namespace MS.Katusha.Windows
{
    public partial class Form1 : Form
    {
        private MSKatushaService _service;

        public Form1()
        {
            InitializeComponent();
            comboBox1.Text = "https://mskatusha.apphb.com/";
            textBox2.Text = "mertiye";
            textBox3.Text = "690514";
            button3_Click(null, null);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int total;
            var list = _service.GetProfiles(Sex.Male, out total);
            label1.Text = total.ToString();
            listBox1.Items.Clear();
            foreach(var item in list)
                listBox1.Items.Add(item.ToString());
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var guid = Guid.Parse(listBox1.SelectedItem.ToString());
            textBox1.Text = _service.GetProfile(guid);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int total;
            var list = _service.GetProfiles(Sex.Female, out total);
            label1.Text = total.ToString();
            listBox1.Items.Clear();
            foreach (var item in list)
                listBox1.Items.Add(item.ToString());
        }

        private void button3_Click(object sender, EventArgs e)
        {
            _service = new MSKatushaService(textBox2.Text, textBox3.Text, comboBox1.Text);

        }
    }
}
