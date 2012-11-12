using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Windows.Forms;
using MS.Katusha.Crawler;
using MS.Katusha.Domain.Entities;
using MS.Katusha.Domain.Service;
using MS.Katusha.Enumerations;
using MS.Katusha.SDK;

namespace MS.Katusha.Windows
{
    public partial class Form1 : Form
    {
        private MSKatushaService _service;
        private Profile _profile = null;
        private static readonly string KatushaFolder = GetDropboxFolder() + "\\MS.Katusha";
        private readonly ICrawler _crawler = new TravelGirlsCrawler();
        private static readonly string[] Folders = new[] {"TravelGirls", "TravelGirlsProcessed", "TravelGirlsProcessedEU", "TravelGirlsProcessedSite"};
        private IDictionary<long, Profile> _profiles = null;

        public Form1()
        {
            InitializeComponent();
            textBox2.Text = "mertiko";
            textBox3.Text = "690514";
            button7.Enabled = !checkBox2.Checked;
            comboBox1.Text = MSKatushaWinFormsConfiguration.Servers[0];
            comboBox1.Items.AddRange(MSKatushaWinFormsConfiguration.Servers);
            comboBox6.Text = MSKatushaWinFormsConfiguration.Servers[0];
            comboBox6.Items.AddRange(MSKatushaWinFormsConfiguration.Servers);
            comboBox3.Text = Folders[0];
            comboBox3.Items.AddRange(Folders);
            comboBox5.Text = Folders[1];
            comboBox5.Items.AddRange(Folders);
            comboBox7.Text = MSKatushaWinFormsConfiguration.Buckets[0];
            foreach (var bucket in MSKatushaWinFormsConfiguration.Buckets) {
                comboBox7.Items.Add(new S3FS(bucket));
            }
            
            comboBox7.SelectedIndex = 0;
            //button3_Click(null, null);
            FillFiles();
        }

        #region Crawler
        private void OnCrawlItemReady(ICrawler crawler, CrawlItemResult crawlItemResult)
        {
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


        private void checkBox2_CheckedChanged(object sender, EventArgs e) { button7.Enabled = !checkBox2.Checked; }

        private void button6_Click(object sender, EventArgs e)
        {
            var lastpageNo = (int)numericUpDown1.Value;
            listView1.Items.Clear();
            for (var i = lastpageNo; i > 0; i--) {
                _crawler.CrawlPageAsync(OnCrawlPageReady, comboBox2.Text, i.ToString(CultureInfo.InvariantCulture));
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView1.SelectedItems) {
                _crawler.CrawlItemAsync(OnCrawlItemReady, comboBox2.Text, item.Text);
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            var toFolder = KatushaFolder + "\\" + comboBox5.Text;
            if (!Directory.Exists(toFolder)) Directory.CreateDirectory(toFolder);
            foreach (ListViewItem fileName in listView2.SelectedItems) {
                var text = File.ReadAllText(fileName.Text);
                if (!String.IsNullOrWhiteSpace(text))
                    try {
                        var moveFile = false;
                        var userName = Path.GetFileNameWithoutExtension(fileName.Text);
                        var guid = _service.GetProfileGuid(userName);
                        if (guid == System.Guid.Empty) {
                            var result = _service.SetProfile(text);
                            textBox5.Text += String.Format("\r\n{0} {1}", result, fileName);
                            if (result == HttpStatusCode.OK) {
                                moveFile = true;
                            }
                        } else {
                            moveFile = true;
                            textBox5.Text += String.Format("\r\nUSER EXISTS {0}", userName);
                        }
                        if (moveFile) File.Move(fileName.Text, toFolder + "\\" + Path.GetFileName(fileName.Text));

                    } catch (Exception ex) {
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
            foreach (var file in files)
                listView2.Items.Add(new ListViewItem(file));
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            FillFiles();
        }
        #endregion
        
        private void ProcessList(IList<Profile> list)
        {
            textBox5.Text += "\r\n" + _service.Result;
            label1.Text = list.Count.ToString(CultureInfo.InvariantCulture);
            var imageList = new ImageList {ImageSize = new Size(80, 106), ColorDepth = ColorDepth.Depth32Bit};
            ProfileList.BeginUpdate();
            try {
                ProfileList.View = View.LargeIcon;
                ProfileList.Items.Clear();
                for (var i = 0; i < list.Count; i++) {
                    var item = list[i];
                    var listViewItem = new ListViewItem { Tag = item, Text = item.User.UserName + " / " + item.Name, ImageIndex = i, ToolTipText = item.User.Email };
                    var image = _service.GetImage(item.ProfilePhotoGuid, comboBox7.SelectedItem as S3FS);
                    imageList.Images.Add(image);
                    ProfileList.Items.Add(listViewItem);
                }
                //listBox1.SmallImageList = imageList;
                ProfileList.LargeImageList = imageList;
            } finally {
                ProfileList.EndUpdate();
            }
            //listBox1.StateImageList = imageList;
        }

        private void ConnectClick(object sender, EventArgs e)
        {
            _service = new MSKatushaService(textBox2.Text, textBox3.Text, comboBox1.Text, Application.LocalUserAppDataPath);
            var list = _service.GetProfiles();
            _profiles = new Dictionary<long, Profile>(list.Count);
            foreach(var profile in list)
                _profiles.Add(profile.Id, profile);
            ProcessList(list);
            dataGridView1.DataSource = list;
            ConnectButton.Text = "Re-Connect";
        }

        private void button4_Click(object sender, EventArgs e)
        {
            using(var file = new StreamWriter(textBox4.Text, false))
                file.Write(textBox1.Text);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (_profile != null && MessageBox.Show(String.Format("Delete {0}\r\nAre you sure?", _profile.Name), "Delete", MessageBoxButtons.YesNo) == DialogResult.Yes) {
                var result = _service.DeleteProfile(_profile.Guid);
                textBox5.Text += "\r\n" + _service.Result;
                if (!String.IsNullOrWhiteSpace(result))
                    MessageBox.Show(result);
            }
        }

        private Profile FindProfile(long id)
        {
            if(_profiles.ContainsKey(id))
                return _profiles[id];
            return null;
        }
        
        private void ProfileTabsSelectedIndexChanged(object sender, EventArgs e)
        {
            if (ProfileList.SelectedItems.Count <= 0) return;
            _profile = ProfileList.SelectedItems[0].Tag as Profile;
            DisplayForProfileTab();
        }

        private void DisplayForProfileTab()
        {
            if (_profile == null) return;

            DialogGridView.Rows.Clear();
            DialogGridView.DataSource = null;

            var guid = _profile.Guid;
            switch (ProfileTabs.SelectedIndex) {
                case 0:
                    var uri = new Uri(comboBox1.Text);
                    var url = String.Format("{0}Profiles/Show/{1}", uri.AbsoluteUri, guid);
                    textBox5.Text += "\r\n" + url;
                    webBrowser1.Navigate(url);
                    break;
                case 1:
                    textBox1.Text = "";
                    var str = _service.GetProfile(guid);
                    textBox1.Text = str;
                    textBox5.Text += "\r\n" + _service.Result;
                    break;
                case 2:
                    var list = _service.GetDialogs(_profile.Guid);
                    var dialogs = new BindingSource();// <WinDialog>(list.Count);
                    foreach(var item in list) {
                        var p = FindProfile(item.ProfileId);
                        if (p == null) continue;
                        var image = _service.GetImage(p.ProfilePhotoGuid, comboBox7.SelectedItem as S3FS, PhotoType.Icon);
                        var dialog = new WinDialog() {Image = image, Name = p.Name, ProfileId = item.ProfileId, Count = item.Count, LastReceived = item.LastReceivedDate, LastSent = item.LastSentDate, UnreadReceivedCount = item.UnreadReceivedCount, UnreadSentCount = item.UnreadSentCount};
                        dialogs.Add(dialog);
                    }
                    DialogsGridView.DataSource = dialogs;
                    var dataGridViewColumn = DialogsGridView.Columns["ProfileId"];
                    if (dataGridViewColumn != null) dataGridViewColumn.Visible = false;
                    dataGridViewColumn = DialogsGridView.Columns["Image"];
                    if (dataGridViewColumn != null) {
                        dataGridViewColumn.Width = 40;
                    }
                    break;
                case 4:
                    SetPhotos(_profile);
                    break;
            }

        }

        private void SetPhotos(Profile profile)
        {
            PhotoGridView.Rows.Clear();
            foreach(var photo in profile.Photos) {
                var row = new DataGridViewRow();
                row.Height = 106;
                var imageCell = new DataGridViewImageCell {Description = photo.FileName, Value = _service.GetImage(photo.Guid, comboBox7.SelectedItem as S3FS), };
                var statusCell = new DataGridViewCheckBoxCell {Value = (photo.Status == (byte)PhotoStatus.Ready)};
                row.Cells.Add(imageCell);
                row.Cells.Add(statusCell);
                row.Cells.Add(new DataGridViewTextBoxCell(){Value = photo.Guid});
                row.Tag = photo;
                PhotoGridView.Rows.Add(row);
            }
        }

        private void SearchButtonClick(object sender, EventArgs e)
        {
            var text = SearchTextBox.Text;
            var profiles = _service.GetProfiles(text, SearchComboBox.Text);
            ProfileList.Items.Clear();
            ProcessList(profiles);
            ConnectClick(null, null);
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) { _service.Explore(); }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            _service.ClearCache();
        }

        private void DialogsGridView_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (_profile == null) return;
            var row = DialogsGridView.Rows[e.RowIndex];

            var list = _service.GetDialog(_profile.Id, (long)row.Cells[0].Value);
            var dialogs = new BindingSource();// <WinDialog>(list.Count);
            foreach (var item in list) {

                var photoGuid = item.FromPhotoGuid;
                var name = item.FromName;
                var id = item.FromId;
                var guid = item.FromGuid;
                var image = _service.GetImage(photoGuid, comboBox7.SelectedItem as S3FS, PhotoType.Icon);
                var dialog = new { Guid = item.Guid, Image = image, Name = name, ProfileId = id, ProfileGuid = guid, Subject = item.Subject, Message = item.Message, ReadDate = item.ReadDate };
                dialogs.Add(dialog);
            }
            DialogGridView.DataSource = dialogs;
            var dataGridViewColumn = DialogGridView.Columns["ProfileId"];
            if (dataGridViewColumn != null) dataGridViewColumn.Visible = false;
            dataGridViewColumn = DialogGridView.Columns["ProfileGuid"];
            if (dataGridViewColumn != null) dataGridViewColumn.Visible = false;
            dataGridViewColumn = DialogGridView.Columns["Guid"];
            if (dataGridViewColumn != null) dataGridViewColumn.Visible = false;
            dataGridViewColumn = DialogGridView.Columns["Image"];
            if (dataGridViewColumn != null) {
                dataGridViewColumn.Width = 40;
            }
        }

        private void DialogGridView_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            if (_profile == null) return;
            var row = e.Row;
            var guid = (Guid) row.Cells["Guid"].Value;
            _service.DeleteMessage(guid);
        }

        private void DialogsGridView_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            if (_profile == null) return;
            var row = e.Row;
            var toId = (long)row.Cells["ProfileId"].Value;
            var toProfile = FindProfile(toId);
            if (toProfile == null) return;
            _service.DeleteDialog(_profile.Guid, toProfile.Guid);
        }

        private void ProfileList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ProfileList.SelectedItems.Count <= 0) return;
            _profile = ProfileList.SelectedItems[0].Tag as Profile;
            if (_profile == null) return;
            DisplayForProfileTab();
            textBox4.Text = string.Format(@"{0}\ProfileBackups\{1}.json", KatushaFolder, _profile.User.UserName);
        }

        private void DialogsGridView_CellClick(object sender, DataGridViewCellEventArgs e) {
            GetDataGridProfile(DialogsGridView, e.RowIndex, e.ColumnIndex);
        }

        private void DialogGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            GetDataGridProfile(DialogGridView, e.RowIndex, e.ColumnIndex);
        }

        private void GetDataGridProfile(DataGridView gridView, int rowIndex, int columnIndex)
        {
            long profileId = 0;
            if (rowIndex > 0 && columnIndex > 0) {
                var row = gridView.Rows[rowIndex];
                var column = gridView.Columns[columnIndex];
                if (column.Name == "Image") profileId = (long) row.Cells["ProfileId"].Value;
                if (profileId > 0) {
                    _profile = FindProfile(profileId);
                    ProfileList.SelectedItems.Clear();
                    foreach (ListViewItem item in ProfileList.Items) {
                        var p = item.Tag as Profile;
                        if (p == null || p.Id != profileId) continue;
                        item.Selected = true;
                        item.EnsureVisible();
                        ProfileTabs.SelectTab(0);
                        item.Focused = true;
                        _profile = p;
                        DisplayForProfileTab();
                        break;
                    }
                }
            } 
        }

        private void PhotoGridView_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            var photo = PhotoGridView.Rows[e.RowIndex].Tag as Photo;
            if (photo != null) {
                var image = _service.GetImage(photo.Guid, comboBox7.SelectedItem as S3FS, PhotoType.Large);
                PhotoBox.Image = image;
            }
        }

    }

}
