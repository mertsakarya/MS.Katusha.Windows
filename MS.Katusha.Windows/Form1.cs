using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Windows.Forms;
using MS.Katusha.Crawler;
using MS.Katusha.Domain.Entities;
using MS.Katusha.Enumerations;
using MS.Katusha.SDK;
using MS.Katusha.SDK.Services;

namespace MS.Katusha.Windows
{
    public delegate void SetControlPropertyCallback(Control control, string propertyName, object value);
    public partial class Form1 : Form
    {
        private MSKatushaService _service;
        private MSKatushaListService<Profile, ListViewItem> _profileListService;
        private MSKatushaListService<Photo, ListViewItem> _photoListService;
        private MSKatushaListService<Conversation, ListViewItem> _messageListService;
        private Profile _profile;
        private static readonly string KatushaFolder = GetDropboxFolder() + "\\MS.Katusha";
        private readonly ICrawler _crawler = new TravelGirlsCrawler();
        private static readonly string[] Folders = new[] {"TravelGirls", "TravelGirlsProcessed", "TravelGirlsProcessedEU", "TravelGirlsProcessedSite"};

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
            ProfileList.VirtualMode = true;
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
        
        private void ProcessList(int total)
        {
            ProfileList.LargeImageList = new ImageList { ImageSize = new Size(80, 106), ColorDepth = ColorDepth.Depth32Bit };
            ProfileList.VirtualListSize = total;
            label1.Text = total.ToString(CultureInfo.InvariantCulture);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            using(var file = new StreamWriter(textBox4.Text, false))
                file.Write(textBox1.Text);
        }

        private Profile FindProfile(long id)
        {
            return _service.GetProfile(id);
        }
        
        private void ProfileTabsSelectedIndexChanged(object sender, EventArgs e)
        {
            if (ProfileList.SelectedIndices.Count <= 0) return;
            _profile = _profileListService.GetItemDataAt(ProfileList.SelectedIndices[0]);
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
                        var image = _service.GetImage(p.ProfilePhotoGuid, PhotoType.Icon);
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
                var imageCell = new DataGridViewImageCell {Description = photo.FileName, Value = _service.GetImage(photo.Guid), };
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
            ProcessList(1);
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
                var image = _service.GetImage(photoGuid, PhotoType.Icon);
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
            if (ProfileList.SelectedIndices.Count <= 0) return;
            _profile = _profileListService.GetItemDataAt(ProfileList.SelectedIndices[0]);
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
                    ProfileList.SelectedIndices.Clear();
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
                var image = _service.GetImage(photo.Guid, PhotoType.Large);
                PhotoBox.Image = image;
            }
        }

        private void SetControlProperty(Control control, string propertyName, object value)
        {
            // InvokeRequired required compares the thread ID of the 
            // calling thread to the thread ID of the creating thread. 
            // If these threads are different, it returns true. 
            if (control.InvokeRequired)
            {
                var d = new SetControlPropertyCallback(SetControlProperty);
                this.Invoke(d, new object[] { control, propertyName, value });
            }
            else
            {
                var controlType = control.GetType();
                var propertyInfo = controlType.GetProperty(propertyName);
                propertyInfo.SetValue(control, value);
            }
        }

        private void ConnectClick(object sender, EventArgs e)
        {
            var serviceSettings = new MSKatushaServiceSettings
            {
                Username = textBox2.Text,
                Password = textBox3.Text,
                BaseUrl = comboBox1.Text,
                DataFolder = Application.LocalUserAppDataPath,
                S3Fs = comboBox7.SelectedItem as S3FS
            };
            _service = new MSKatushaService(serviceSettings);
            _profileListService = new MSKatushaListService<Profile, ListViewItem>("Profile", serviceSettings);
            _photoListService = new MSKatushaListService<Photo, ListViewItem>("Photo", serviceSettings);
            _messageListService = new MSKatushaListService<Conversation, ListViewItem>("Conversation", serviceSettings);
            ProfileList.LargeImageList = _profileListService.ImageList;
            PhotoList.LargeImageList = _photoListService.ImageList;
            _profileListService.GetListEvent += ProfileListServiceOnGetListEvent;
            _photoListService.GetListEvent += PhotoListServiceOnGetListEvent;
            _messageListService.GetListEvent += MessageListServiceOnGetListEvent;
            _profileListService.GetItems(1);
            _photoListService.GetItems(1);
            _messageListService.GetItems(1, 512);
        }

        private void PhotoListServiceOnGetListEvent(object sender, MSKatushaListManagerEventArgs<Photo> e)
        {
            //SetControlProperty(textBox5, "Text", textBox5.Text + "\r\n" + e.Uri);
            if (e.ApiList.Items.Count > 0)
                SetControlProperty(textBox5, "Text",
                                            textBox5.Text +
                                            String.Format("\r\nPhoto web request {1} / {2}. Found {0} items to update.", e.ApiList.Items.Count, e.ApiList.PageNo, (e.ApiList.Total / e.ApiList.PageSize)+ 1));
            SetControlProperty(PhotoList, "VirtualListSize", e.TotalRaven);
            SetControlProperty(label1, "Text", e.TotalRaven.ToString(CultureInfo.InvariantCulture));
        }

        private void PhotoList_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            var item = _photoListService.GetItemAt(e.ItemIndex, OnNewPhotoViewItem);
            if (item.Item.ImageList != null)
                item.Item.ImageIndex = item.Item.ImageList.Images.IndexOfKey(item.Index.ToString());
            e.Item = item.Item;
        }

        private ListViewItem OnNewPhotoViewItem(ImageList imageList, Photo p, int index)
        {
            var profile = FindProfile(p.ProfileId);
            var text = (profile != null) ? profile.User.UserName + " / " + profile.Name : p.FileName;
            var listViewItem = new ListViewItem
            {
                Tag = p,
                Text = text,
                ImageIndex = index,
                ToolTipText = p.FileName
            };
            var image = _service.GetImage(p.Guid);
            imageList.Images.Add(index.ToString(CultureInfo.InvariantCulture), image);
            listViewItem.ImageKey = index.ToString(CultureInfo.InvariantCulture);
            return listViewItem;
        }

        private void ProfileListServiceOnGetListEvent(object sender, MSKatushaListManagerEventArgs<Profile> e)
        {
            //SetControlProperty(textBox5,"Text", textBox5.Text + "\r\n" + e.Uri);
            if (e.ApiList.Items.Count > 0) 
                SetControlProperty(textBox5, "Text",
                                            textBox5.Text +
                                            String.Format("\r\nProfile web request {1} / {2}. Found {0} items to update.", e.ApiList.Items.Count, e.ApiList.PageNo, (e.ApiList.Total / e.ApiList.PageSize)+ 1));
            SetControlProperty(ProfileList, "VirtualListSize", e.TotalRaven);
            SetControlProperty(label1, "Text", e.TotalRaven.ToString(CultureInfo.InvariantCulture));
        }

        private void ProfileList_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            var item = _profileListService.GetItemAt(e.ItemIndex, OnNewViewItem);
            if (item.Item.ImageList != null)
                item.Item.ImageIndex = item.Item.ImageList.Images.IndexOfKey(item.Index.ToString());
            e.Item = item.Item;
        }

        private ListViewItem OnNewViewItem(ImageList imageList, Profile p, int index)
        {
            var listViewItem = new ListViewItem
                {
                    Tag = p, Text = p.User.UserName + " / " + p.Name, ImageIndex = index, ToolTipText = p.User.Email
                };
            var image = _service.GetImage(p.ProfilePhotoGuid);
            imageList.Images.Add(index.ToString(), image);
            listViewItem.ImageKey = index.ToString();
            return listViewItem;
        }

        private void MessageListServiceOnGetListEvent(object sender, MSKatushaListManagerEventArgs<Conversation> e)
        {
            //SetControlProperty(textBox5, "Text", textBox5.Text + "\r\n" + e.Uri);
            if (e.ApiList.Items.Count > 0)
                SetControlProperty(textBox5, "Text",
                                            textBox5.Text +
                                            String.Format("\r\nMessage web request {1} / {2}. Found {0} items to update.", e.ApiList.Items.Count, e.ApiList.PageNo, (e.ApiList.Total / e.ApiList.PageSize) + 1));
            SetControlProperty(MessageView, "VirtualListSize", e.TotalRaven);
            SetControlProperty(label1, "Text", e.TotalRaven.ToString(CultureInfo.InvariantCulture));
        }

        private void MessageView_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            var item = _messageListService.GetItemAt(e.ItemIndex, OnNewMessageViewItem);
            e.Item = item.Item;
        }

        private ListViewItem OnNewMessageViewItem(ImageList imageList, Conversation p, int index)
        {
            p.From = p.From ?? FindProfile(p.FromId);
            p.To = p.To ?? FindProfile(p.ToId);
            var listViewItem = new ListViewItem {
                Tag = p,
                Text = p.CreationDate.ToString("u"),
                ImageIndex = index,
                ToolTipText = p.Subject
            };
           var isRead = (p.ReadDate > new DateTime(1900, 1, 1));
           if(!isRead) listViewItem.Font = new Font(listViewItem.Font, FontStyle.Bold);
            listViewItem.SubItems.Add(new ListViewItem.ListViewSubItem(listViewItem, (p.From != null) ? p.From.Name: p.FromId.ToString(CultureInfo.InvariantCulture)));
            listViewItem.SubItems.Add(new ListViewItem.ListViewSubItem(listViewItem, (p.To != null) ? p.To.Name: p.ToId.ToString(CultureInfo.InvariantCulture)));
            listViewItem.SubItems.Add(new ListViewItem.ListViewSubItem(listViewItem, p.Subject));
            listViewItem.SubItems.Add(new ListViewItem.ListViewSubItem(listViewItem, p.Message));
            listViewItem.SubItems.Add(new ListViewItem.ListViewSubItem(listViewItem, (isRead) ? p.ReadDate.ToString("u") : ""));
            return listViewItem;
        }

        
        private void ProfileList_SearchForVirtualItem(object sender, SearchForVirtualItemEventArgs e)
        {

        }

        private void MessageView_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && MessageBox.Show(String.Format("Delete {0}\r\nAre you sure?", _profile.Name), "Delete", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                foreach (int index in MessageView.SelectedIndices)
                {
                    _messageListService.Delete(index);
                    MessageView.VirtualListSize--;
                }
            }
        }

        private void ProfileList_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && MessageBox.Show(String.Format("Delete {0}\r\nAre you sure?", _profile.Name), "Delete", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                foreach (int index in ProfileList.SelectedIndices)
                {
                    _profileListService.Delete(index);
                    //ProfileList.RedrawItems(ProfileList.D)
                    ProfileList.VirtualListSize--;
                }
            }
        }
    }

}
