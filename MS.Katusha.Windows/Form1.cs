using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MS.Katusha.SDK;

namespace MS.Katusha.Windows
{
    public partial class Form1 : Form
    {
        private MSKatushaService _service;
        private ApiProfileInfo profileInfo = null;

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
            FillList(Sex.Male, out total);
            label1.Text = total.ToString(CultureInfo.InvariantCulture);
        }

        public static Image FromURL(string Url)
        {
            try {
                var request = HttpWebRequest.Create(Url) as HttpWebRequest;
                var respone = request.GetResponse() as HttpWebResponse;
                return Image.FromStream(respone.GetResponseStream(), true);
            } catch {
                return new Bitmap(@"P:\GIT\MS.Katusha\MS.Katusha.Web\Images\4-00000000-0000-0000-0000-000000000000.jpg");
            }
        }

        private void FillList(Sex gender, out int total)
        {
            var list = _service.GetProfiles(gender, out total).ToArray();
            label1.Text = total.ToString(CultureInfo.InvariantCulture);
            var imageList = new ImageList();
            listBox1.Items.Clear();
            for (var i = 0; i < list.Length; i++) {
                var item = list[i];
                var listViewItem = new ListViewItem {Tag = item, Text = item.Name};
                listViewItem.ImageIndex = i;
                var bucket = "MS.Katusha" + ((comboBox1.Text=="http://localhost:10595/")?".Test":"");
                imageList.Images.Add(FromURL(String.Format("http://s3.amazonaws.com/{1}/Photos/4-{0}.jpg", item.ProfilePhotoGuid, bucket)));
                listBox1.Items.Add(listViewItem);
            }
            listBox1.SmallImageList = imageList;
            listBox1.LargeImageList = imageList;
            listBox1.StateImageList = imageList;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int total;
            FillList(Sex.Female, out total);
            label1.Text = total.ToString(CultureInfo.InvariantCulture);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            _service = new MSKatushaService(textBox2.Text, textBox3.Text, comboBox1.Text);
        }

        private void listBox1_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (listBox1.SelectedItems.Count > 0) {
                profileInfo = listBox1.SelectedItems[0].Tag as ApiProfileInfo;
                if (profileInfo == null) return;
                var guid = profileInfo.Guid;
                textBox1.Text = _service.GetProfile(guid);
                textBox4.Text = string.Format(@"C:\Users\mert.sakarya\Dropbox\MS.Katusha\ProfileBackups\{0}-{1}.json", profileInfo.Name, profileInfo.Guid);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            using(var file = new StreamWriter(textBox4.Text, false))
                file.Write(textBox1.Text);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (profileInfo != null && MessageBox.Show(String.Format("Delete {0}\r\nAre you sure?", profileInfo.Name), "Delete", MessageBoxButtons.YesNo) == DialogResult.Yes) {
                var result = _service.DeleteProfile(profileInfo.Guid);
                if (!String.IsNullOrWhiteSpace(result))
                    MessageBox.Show(result);
            }
        }
    }
}
