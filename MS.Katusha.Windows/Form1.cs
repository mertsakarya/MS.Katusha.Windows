using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using MS.Katusha.Configuration;
using MS.Katusha.Crawler;
using MS.Katusha.Enumerations;
using MS.Katusha.SDK;
using Newtonsoft.Json;
using Sex = MS.Katusha.SDK.Sex;

namespace MS.Katusha.Windows
{
    public partial class Form1 : Form
    {
        private MSKatushaService _service;
        private ApiProfileInfo profileInfo = null;
        private static readonly string KatushaFolder = GetDropboxFolder() + "\\MS.Katusha";
        private readonly ICrawler _crawler = new TravelGirlsCrawler();
        private static readonly string[] Servers = new[] {"http://www.mskatusha.com/", "https://mskatusha.apphb.com/", "https://mskatushaeu.apphb.com/", "http://localhost:10595/", "http://localhost/"};
        private static readonly string[] Buckets = new[] { "s.mskatusha.com", "MS.Katusha", "MS.Katusha.EU", "MS.Katusha.Test" };
        private static readonly string[] Folders = new[] {"TravelGirls", "TravelGirlsProcessed", "TravelGirlsProcessedEU", "TravelGirlsProcessedSite"};


        public Form1()
        {
            InitializeComponent();
            textBox2.Text = "mertiye";
            textBox3.Text = "690514";
            button3_Click(null, null);
            button7.Enabled = !checkBox2.Checked;
            comboBox1.Text = Servers[0];
            comboBox1.Items.AddRange(Servers);
            comboBox6.Text = Servers[0];
            comboBox6.Items.AddRange(Servers);
            comboBox3.Text = Folders[0];
            comboBox3.Items.AddRange(Folders);
            comboBox5.Text = Folders[1];
            comboBox5.Items.AddRange(Folders);
            comboBox7.Text = Buckets[0];
            foreach (var bucket in Buckets) {
                comboBox7.Items.Add(new S3FS(bucket));
            }
            comboBox7.SelectedIndex = 0;
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
            var dbBase64Text = Convert.FromBase64String(File.ReadAllLines(dbPath)[1]);
            var folderPath = System.Text.Encoding.ASCII.GetString(dbBase64Text);
            return folderPath;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int total;
            FillList(Sex.Male, out total);
            label1.Text = total.ToString(CultureInfo.InvariantCulture);
        }

        public static Image FromUrl(string Url)
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
            var list = _service.GetProfiles(Sex.Female, out total).ToList();
            list.AddRange(_service.GetProfiles(Sex.Male, out total).ToList());

            ProcessList(total, list);
        }

        private void ProcessList(int total, IList<ApiProfileInfo> list)
        {
            textBox5.Text += "\r\n" + _service.Result;
            label1.Text = total.ToString(CultureInfo.InvariantCulture);
            var imageList = new ImageList();
            imageList.ImageSize = new Size(35, 48);
            listBox1.BeginUpdate();
            try {
                listBox1.View = View.LargeIcon;
                listBox1.Items.Clear();
                for (var i = 0; i < list.Count; i++) {
                    var item = list[i];
                    var listViewItem = new ListViewItem {Tag = item, Text = item.UserName + " / " + item.Name, ImageIndex = i, ToolTipText = item.Email};
                    var image = GetImage(item.ProfilePhotoGuid);
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

        private Image GetImage(Guid guid)
        {
            const string ua = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;" + ")";
            var path = Application.LocalUserAppDataPath + "\\Images";
            var file = path + "\\4-" + guid + ".jpg";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            if (!File.Exists(file)) {
                var s3 = (comboBox7.SelectedItem as S3FS).FileSystem;
                var url = s3.GetPhotoUrl(guid, PhotoType.Icon);
                var webClient = new WebClient();
                webClient.Headers.Add(HttpRequestHeader.UserAgent, ua);
                webClient.Headers["Accept"] = "/";
                try {
                    webClient.DownloadFile(url, file);
                } catch {
                    return GetImage(Guid.Empty);
                }
            }
            return Image.FromFile(file);
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
            if (listBox1.SelectedItems.Count <= 0) return;
            var uri = new Uri(comboBox1.Text);
            profileInfo = listBox1.SelectedItems[0].Tag as ApiProfileInfo;
            if (profileInfo == null) return;
            var guid = profileInfo.Guid;
            switch (tabControl3.SelectedIndex) {
                case 0:
                    webBrowser1.Navigate(String.Format("{0}/Profiles/Show/{1}", uri.AbsoluteUri, guid.ToString()));
                    break;
                case 1:
                    textBox1.Text = _service.GetProfile(guid);
                    break;
                case 2:
                    var list = _service.GetDialogs(profileInfo.Guid);
                    var dialogs = new BindingSource();// <WinDialog>(list.Count);
                    foreach(var item in list) {
                        var profile = FindProfile(item.ProfileId);
                        if(profile != null) {
                            var image = GetImage(profile.ProfilePhotoGuid);
                            var dialog = new WinDialog() {Image = image, Name = profile.Name, ProfileId = item.ProfileId, Count = item.Count, LastReceived = item.LastReceivedDate, LastSent = item.LastSentDate, UnreadReceivedCount = item.UnreadReceivedCount, UnreadSentCount = item.UnreadSentCount};
                            dialogs.Add(dialog);
                        }
                    }
                    dataGridView1.DataSource = dialogs;
                    break;
            }
            textBox5.Text += "\r\n" + _service.Result;
            textBox4.Text = string.Format(@"{0}\ProfileBackups\{1}.json", KatushaFolder, profileInfo.UserName);
        }

        private ApiProfileInfo FindProfile(long id)
        {
            foreach(ListViewItem item in listBox1.Items) {
                var apiProfileInfo = item.Tag as ApiProfileInfo;
                if (apiProfileInfo.Id == id) return apiProfileInfo;
            }
            return null;
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

        private void textBox6_KeyPress(object sender, KeyPressEventArgs e)
        {
            var text = textBox6.Text;
            if(text.Length > 2)
                foreach (ListViewItem item in listBox1.Items) {
                    var profile = item.Tag as ApiProfileInfo;
                    if (profile == null) continue;
                    var searchVal = profile.Email + " | " + profile.Name + " | " + profile.UserName;
                    if (searchVal.IndexOf(text, StringComparison.Ordinal) >= 0)
                        item.Selected = true;
                }
        }
    }

}
