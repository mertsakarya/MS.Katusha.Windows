using System;
using System.Windows.Forms;
using MS.Katusha.Domain.Entities;
using MS.Katusha.SDK;
using MS.Katusha.SDK.Services;

namespace MS.Katusha.Windows
{
    public partial class TestForm : Form
    {
        private string username;
        private string password;
        private string server;
        private string bucketName;
        private S3FS bucket;
        private MSKatushaListService<Photo, ListViewItem> _listService;
        private MSKatushaListService<Conversation, ListViewItem> _conversationlistService;
        private MSKatushaServiceSettings _settings;
        private MSKatushaService _service;


        public TestForm()
        {
            InitializeComponent();

            username = "mertiko";
            password = "690514";
            server = MSKatushaWinFormsConfiguration.Servers[0];
            bucketName = MSKatushaWinFormsConfiguration.Buckets[0];
            bucket = new S3FS(bucketName);
            ProfileList.RetrieveVirtualItem += ProfileList_RetrieveVirtualItem;
            ProfileList.VirtualMode = true;
            _settings = new MSKatushaServiceSettings
                {
                    Username = username,
                    Password = password,
                    DataFolder = Application.LocalUserAppDataPath,
                    S3Fs = bucket,
                    BaseUrl = server
                };
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _listService = new MSKatushaListService<Photo, ListViewItem>("Photo", _settings, 128);
            _conversationlistService = new MSKatushaListService<Conversation, ListViewItem>("Conversation", _settings, 128);
            _service = new MSKatushaService(_settings);
            ProfileList.LargeImageList = _listService.ImageList;
            _listService.GetListEvent += ServiceOnGetListEvent;
            _conversationlistService.GetListEvent += ConversationServiceOnGetListEvent;
            _conversationlistService.GetItems(1, 1000);
            _listService.GetItems(1);
        }

        private void ConversationServiceOnGetListEvent(object sender, MSKatushaListManagerEventArgs<Conversation> e)
        {
            listBox1.DataSource = e.ApiList.Items;
        }

        private void ServiceOnGetListEvent(object sender, MSKatushaListManagerEventArgs<Photo> e)
        {
            if (e.ApiList.Items.Count > 0)
                MessageBox.Show(String.Format("Found {0} profiles to update.", e.ApiList.Items.Count), "FOUND UPDATES");
            ProfileList.VirtualListSize = e.TotalRaven;
        }

        private void ProfileList_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            var item = _listService.GetItemAt(e.ItemIndex, OnNewViewPhotoItem);
            if (item.Item.ImageList != null)
                item.Item.ImageIndex = item.Item.ImageList.Images.IndexOfKey(item.Index.ToString());
            e.Item = item.Item;
        }

        private ListViewItem OnNewViewPhotoItem(ImageList imageList, Photo p, int index)
        {
            var listViewItem = new ListViewItem
            {
                Tag = p,
                Text = p.FileName,
                ImageIndex = index,
            };
            var image = _service.GetImage(p.Guid);
            imageList.Images.Add(index.ToString(), image);
            listViewItem.ImageKey = index.ToString();
            return listViewItem;
        }

        private ListViewItem OnNewViewItem(ImageList imageList, Profile p, int index)
        {
            var listViewItem = new ListViewItem
            {
                Tag = p,
                Text = p.User.UserName + " / " + p.Name,
                ImageIndex = index,
                ToolTipText = p.User.Email
            };
            var image = _service.GetImage(p.ProfilePhotoGuid);
            imageList.Images.Add(index.ToString(), image);
            listViewItem.ImageKey = index.ToString();
            return listViewItem;
        }

    }
}
