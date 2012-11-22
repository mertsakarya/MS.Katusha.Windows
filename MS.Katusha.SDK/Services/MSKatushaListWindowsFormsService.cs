using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using MS.Katusha.Domain.Entities;
using MS.Katusha.Domain.Entities.BaseEntities;
using MS.Katusha.Domain.Service;
using MS.Katusha.SDK.Raven;
using Newtonsoft.Json;
using RestSharp;

namespace MS.Katusha.SDK.Services

{


    public delegate void MSKatushaListEventHandler<T>(object sender, MSKatushaListManagerEventArgs<T> e);

    public class MSKatushaCachedListItem<T, TL> where T : BaseGuidModel
    {
        public int Index { get; set; }
        public TL Item { get; set; }
        public T Data { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    public class MSKatushaListManagerEventArgs<T>
    {
        public DateTime LastUpdateTime { get; set; }
        public int TotalRaven { get; set; }
        public ApiList<T> ApiList { get; set; }
        public Uri Uri { get; set; }
        public string Message { get; set; }
    }

    public class MSKatushaListWindowsFormsService<T, TL> : BaseMSKatushaService where T : BaseGuidModel
    {
        private readonly string _typeName;
        private readonly Func<ImageList, T, int, TL> _newViewItem;
        private readonly RavenStoreListManager<T> _ravenStoreListManager;
        private readonly int _cacheSize;
        private readonly Dictionary<int, MSKatushaCachedListItem<T, TL>> _dictionary;
        private readonly ImageList _imageList;
        public event MSKatushaListEventHandler<T> GetListEvent;

        public MSKatushaListWindowsFormsService(string typeName, MSKatushaServiceSettings serviceSettings, Func<ImageList, T, int, TL> newViewItem, int cacheSize = 64)  : base(serviceSettings)
        {
            _typeName = typeName;
            _newViewItem = newViewItem;
            _ravenStoreListManager = new RavenStoreListManager<T>(DocumentStoreManager.GetInstance(DataFolder));
            _cacheSize = cacheSize;
            _dictionary = new Dictionary<int, MSKatushaCachedListItem<T, TL>>(_cacheSize);
            _imageList = new ImageList { ImageSize = new Size(80, 106), ColorDepth = ColorDepth.Depth32Bit };
        }

        public ImageList ImageList { get { return _imageList; } }

        public void GetItems(int page = 1, int pageSize = 128)
        {
            var lastUpdateTime = _ravenStoreListManager.GetLastUpdate();
            GetItems(lastUpdateTime, page, pageSize );
        }

        private void GetItems(DateTime lastUpdateTime, int page, int pageSize = 128)
        {
            var client = new RestClient(BaseUrl) { Authenticator = Authenticator };
            var request = new RestRequest("Api/Get" + _typeName + "sByTime/{key}", Method.GET) { RequestFormat = DataFormat.Json }
                .AddUrlSegment("key", page.ToString(CultureInfo.InvariantCulture))
                .AddParameter("date", lastUpdateTime.ToString("u"))
                .AddParameter("pageSize", pageSize.ToString(CultureInfo.InvariantCulture));
            client.ExecuteAsync(request, response => GetResponseAsync(response, lastUpdateTime) );
        }

        private void GetResponseAsync(IRestResponse response, DateTime lastUpdateTime)
        {
            var apiList = JsonConvert.DeserializeObject<ApiList<T>>(response.Content);
            if (apiList == null) return;
            if (apiList.Items.Count > 0)
                _ravenStoreListManager.AddItems(apiList.Items);
            if (GetListEvent == null) return;
            var total = _ravenStoreListManager.GetItemCount();
            GetListEvent(this, new MSKatushaListManagerEventArgs<T> { LastUpdateTime = lastUpdateTime, TotalRaven = total, Uri = response.ResponseUri, ApiList = apiList });
            if (apiList.Items.Count == apiList.PageSize)
                GetItems(lastUpdateTime, apiList.PageNo + 1, apiList.PageSize);
        }

        public MSKatushaCachedListItem<T, TL> GetItemAt(int index)
        {
            if (_newViewItem == null) return _dictionary[index];
            var msKatushaListItem = _dictionary.ContainsKey(index) ? _dictionary[index] : AddItem(index);
            msKatushaListItem.LastUpdate = DateTime.Now;
            return msKatushaListItem;
        }

        public string Delete(int index)
        {
            var data = GetItemAt(index).Data;
            var client = new RestClient(BaseUrl) { Authenticator = Authenticator };
            var request = new RestRequest("Api/Delete" + ((_typeName == "Conversation") ? "Message" : _typeName) + "/{guid}", Method.GET)
                .AddUrlSegment("guid", data.Guid.ToString());
            var response = client.Execute(request);
            Result = String.Format("curl -u {0}:{1} {2}", Username, Password, response.ResponseUri);
            _ravenStoreListManager.Delete(data.Id);
            return response.Content;
        }

        private MSKatushaCachedListItem<T, TL> AddItem(int index)
        {
            if (_dictionary.Count == _cacheSize) RemoveOldest();
            if (_newViewItem != null)
            {
                var items = _ravenStoreListManager.GetItems(index, index + 1);
                if (items.Count > 0)
                {
                    var item = items[0];

                    var listViewItem = _newViewItem(_imageList, item, index);
                    var cacheItem = new MSKatushaCachedListItem<T, TL> { Index = index, Item = listViewItem, Data = item };
                    _dictionary.Add(index, cacheItem);
                    return cacheItem;
                }
            }
            return null;
        }

        private void RemoveOldest()
        {
            var minDate = DateTime.MaxValue;
            var minId = -1;
            foreach (var profileCache in _dictionary)
            {
                var value = profileCache.Value;
                if (value.LastUpdate < minDate) minId = value.Index;
            }
            var image = _imageList.Images[minId.ToString(CultureInfo.InvariantCulture)];
            if (image != null) image.Dispose();
            _imageList.Images.RemoveByKey(minId.ToString(CultureInfo.InvariantCulture));
            _dictionary.Remove(minId);
        }

        public void UpdatePhotoStatus(Photo data)
        {
            var client = new RestClient(BaseUrl) { Authenticator = Authenticator };
            var request = new RestRequest("Api/UpdatePhotoStatus/{guid}", Method.GET)
                .AddUrlSegment("guid", data.Guid.ToString())
                .AddParameter("value", data.Status);
            var response = client.Execute(request);
            if (response.Content == "{status:'ok'}")
            {
                Result = String.Format("curl -u {0}:{1} {2}", Username, Password, response.ResponseUri);
                _ravenStoreListManager.Update(data);
            }
        }
    }
}
