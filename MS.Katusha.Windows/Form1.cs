using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using MS.Katusha.Crawler;
using MS.Katusha.SDK;

namespace MS.Katusha.Windows
{
    public partial class Form1 : Form
    {
        private MSKatushaService _service;
        private ApiProfileInfo profileInfo = null;
        private static readonly string KatushaFolder = GetDropboxFolder() + "\\MS.Katusha";
        private readonly ICrawler _crawler = new TravelGirlsCrawler();
        private static readonly string[] Servers = new[] {"https://mskatusha.apphb.com/", "https://mskatushaeu.apphb.com/", "http://localhost:10595/", "http://localhost/"};
        private static readonly string[] Folders = new[] {"TravelGirls", "TravelGirlsProcessed", "TravelGirlsProcessedEU", "TravelGirlsProcessedSite"};

        public Form1()
        {
            InitializeComponent();
            textBox2.Text = "mertiye";
            textBox3.Text = "690514";
            button3_Click(null, null);
            button7.Enabled = !checkBox2.Checked;
            comboBox1.Text = Servers[1];
            comboBox1.Items.AddRange(items: Servers);
            comboBox6.Text = Servers[1];
            comboBox6.Items.AddRange(items: Servers);
            comboBox3.Text = Folders[0];
            comboBox3.Items.AddRange(items: Folders);
            comboBox5.Text = Folders[1];
            comboBox5.Items.AddRange(items: Folders);
            FillFiles();
        }

        private void OnCrawlItemReady(ICrawler crawler, CrawlItemResult crawlItemResult) {
            var jsonString = crawlItemResult.Output;
            var filename = crawlItemResult.UniqueId;
            var folder = KatushaFolder + "\\" + comboBox4.Text;
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            using (var file = new StreamWriter(String.Format(folder + @"\{0}.json", filename))) {
                file.Write(jsonString);
            }
            textBox5.Text += "\r\n" + crawlItemResult.Uri;
        }

        private void OnCrawlPageReady(ICrawler crawler, CrawlPageResult crawlPageResult)
        {
            var taskResult = crawlPageResult.Items;
            if (taskResult != null)
                foreach (var item in taskResult) {
                    listView1.Items.Add(new ListViewItem(item.Key));
                    if (checkBox2.Checked) {
                        _crawler.CrawlItemAsync(OnCrawlItemReady, comboBox2.Text, item.Key);
                    }
                }
        }

    private static string GetDropboxFolder()
        {
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Dropbox\\host.db");
            var dbBase64Text = Convert.FromBase64String(System.IO.File.ReadAllLines(dbPath)[1]);
            var folderPath = System.Text.Encoding.ASCII.GetString(dbBase64Text);
            return folderPath;
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
                return new Bitmap(KatushaFolder + @"\Images\4-00000000-0000-0000-0000-000000000000.jpg");
            }
        }

        private void FillList(Sex gender, out int total)
        {
            var list = _service.GetProfiles(gender, out total).ToArray();
            textBox5.Text += "\r\n" + _service.Result;
            label1.Text = total.ToString(CultureInfo.InvariantCulture);
            var imageList = new ImageList();
            imageList.ImageSize = new Size(35,48);
            listBox1.BeginUpdate();
            try {
                listBox1.View = View.LargeIcon;
                listBox1.Items.Clear();
                for (var i = 0; i < list.Length; i++) {
                    var item = list[i];
                    var listViewItem = new ListViewItem {Tag = item, Text = item.UserName + " / " + item.Name};
                    listViewItem.ImageIndex = i;
                    listViewItem.ToolTipText = item.Email;
                    var bucket = "MS.Katusha" + ((comboBox1.Text == "http://localhost:10595/") ? ".Test" : "");
                    var image = FromURL(String.Format("http://s3.amazonaws.com/{1}/Photos/4-{0}.jpg", item.ProfilePhotoGuid, bucket));
                    imageList.Images.Add(image);
                    listBox1.Items.Add(listViewItem);
                }
                //listBox1.SmallImageList = imageList;
                listBox1.LargeImageList = imageList;
            } finally {
                listBox1.EndUpdate();
            }
            //listBox1.StateImageList = imageList;
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

        private void button4_Click(object sender, EventArgs e)
        {
            using(var file = new StreamWriter(textBox4.Text, false))
                file.Write(textBox1.Text);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (profileInfo != null && MessageBox.Show(String.Format("Delete {0}\r\nAre you sure?", profileInfo.Name), "Delete", MessageBoxButtons.YesNo) == DialogResult.Yes) {
                var result = _service.DeleteProfile(profileInfo.Guid);
                textBox5.Text += "\r\n" + _service.Result;
                if (!String.IsNullOrWhiteSpace(result))
                    MessageBox.Show(result);
            }
        }

        private void listBox1_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (listBox1.SelectedItems.Count > 0) {
                profileInfo = listBox1.SelectedItems[0].Tag as ApiProfileInfo;
                if (profileInfo == null) return;
                var guid = profileInfo.Guid;
                textBox1.Text = _service.GetProfile(guid);
                textBox5.Text += "\r\n" + _service.Result;
                textBox4.Text = string.Format(@"{0}\ProfileBackups\{1}.json", KatushaFolder, profileInfo.UserName);
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e) { button7.Enabled = !checkBox2.Checked; }

        private void button6_Click(object sender, EventArgs e)
        {
            var lastpageNo = (int) numericUpDown1.Value;
            listView1.Items.Clear();
            for (var i = lastpageNo; i > 0; i--) {
                _crawler.CrawlPageAsync(OnCrawlPageReady, comboBox2.Text, i.ToString(CultureInfo.InvariantCulture));
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            foreach(ListViewItem item in listView1.SelectedItems) {
                _crawler.CrawlItemAsync(OnCrawlItemReady, comboBox2.Text, item.Text);
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            var toFolder = KatushaFolder + "\\" + comboBox5.Text;
            if (!Directory.Exists(toFolder)) Directory.CreateDirectory(toFolder);
            foreach(ListViewItem fileName in listView2.SelectedItems) {
                var text = File.ReadAllText(fileName.Text);
                if(!String.IsNullOrWhiteSpace(text))
                    try {
                        var moveFile = false;
                        var userName = Path.GetFileNameWithoutExtension(fileName.Text);
                        var guid = _service.GetProfileGuid(userName);
                        if (guid == Guid.Empty) {
                            var result = _service.SetProfile(text);
                            textBox5.Text += String.Format("\r\n{0} {1}", result, fileName);
                            if (result == HttpStatusCode.OK) {
                                moveFile = true;
                            }
                        } else {
                            moveFile = true;
                            textBox5.Text += String.Format("\r\nUSER EXISTS {0}", userName);
                        }
                        if(moveFile) File.Move(fileName.Text, toFolder + "\\" + Path.GetFileName(fileName.Text));
                        
                    } catch(Exception ex) {
                        MessageBox.Show(ex.Message);
                    }
            }
        }
        private void FillFiles()
        {
            var fromFolder = KatushaFolder + "\\" + comboBox3.Text;
            if (!Directory.Exists(fromFolder)) Directory.CreateDirectory(fromFolder);
            var files = Directory.GetFiles(fromFolder, "*.json");
            listView2.Items.Clear();
            foreach(var file in files)
                listView2.Items.Add(new ListViewItem(file));
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            FillFiles();
        }
    }
}
