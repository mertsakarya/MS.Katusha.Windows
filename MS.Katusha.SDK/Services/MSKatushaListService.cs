using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using MS.Katusha.Domain.Entities.BaseEntities;
using MS.Katusha.SDK.Raven;
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
        public string Message { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public int Total { get; set; }
        public int NewCount { get; set; }
        public List<T> Items { get; set; }
    }

    public class MSKatushaListService<T, TL> : BaseMSKatushaService where T : BaseGuidModel
    {
        private readonly string _typeName;
        private readonly RavenStoreListManager<T> _ravenStoreListManager;
        private readonly int _cacheSize;
        private readonly Dictionary<int, MSKatushaCachedListItem<T, TL>> _dictionary;
        private readonly ImageList _imageList;
        public event MSKatushaListEventHandler<T> GetListEvent;

        public MSKatushaListService(string typeName, MSKatushaServiceSettings serviceSettings, int cacheSize = 64)  : base(serviceSettings)
        {
            _typeName = typeName;
            _ravenStoreListManager = new RavenStoreListManager<T>(DocumentStoreManager.GetInstance(DataFolder));
            _cacheSize = cacheSize;
            _dictionary = new Dictionary<int, MSKatushaCachedListItem<T, TL>>(_cacheSize);
            _imageList = new ImageList { ImageSize = new Size(80, 106), ColorDepth = ColorDepth.Depth32Bit };
        }

        public ImageList ImageList { get { return _imageList; } }

        public void GetItems()
        {
            var lastUpdateTime = _ravenStoreListManager.GetLastUpdate();
            var client = new RestClient(BaseUrl) { Authenticator = Authenticator };
            var request = new RestRequest("Api/Get" + _typeName + "sByTime/{key}", Method.GET) { RequestFormat = DataFormat.Json }
                .AddUrlSegment("key", "1")
                .AddParameter("date", lastUpdateTime.ToString("u"));
            var response = client.Execute<List<T>>(request);
            if (response.Data == null) { return; }
            var items = response.Data;
            if (items == null) { return; }
            if (items.Count > 0)
                _ravenStoreListManager.AddItems(items);
            if (GetListEvent == null) return;
            var total = _ravenStoreListManager.GetItemCount();
            GetListEvent(this, new MSKatushaListManagerEventArgs<T> { LastUpdateTime = lastUpdateTime, NewCount = items.Count, Message = String.Format("curl -u username:password {0}", response.ResponseUri), Total = total });
        }

        public void GetItems(int page,int pageSize = 128)
        {
            //var lastUpdateTime = _ravenStoreListManager.GetLastUpdate();
            //var client = new RestClient(BaseUrl) { Authenticator = Authenticator };
            //var request = new RestRequest("Api/Get" + _typeName + "sByTime/{key}", Method.GET) { RequestFormat = DataFormat.Json }
            //    .AddUrlSegment("key", page.ToString(CultureInfo.InvariantCulture))
            //    .AddParameter("date", lastUpdateTime.ToString("u"))
            //    .AddParameter("pageSize", 128);
            //var response = client.Execute<List<T>>(request);
            //if (response.Data == null) { return; }
            //var items = response.Data;
            //if (items == null) { return; }
            //if (items.Count > 0)
            //    _ravenStoreListManager.AddItems(items);
            //if (GetListEvent == null) return;
            //var total = _ravenStoreListManager.GetItemCount();
            //GetListEvent(this, new MSKatushaListManagerEventArgs<T> { LastUpdateTime = lastUpdateTime, NewCount = items.Count, Message = String.Format("curl -u username:password {0}", response.ResponseUri), Total = total });
            
            int total;
            var list = _ravenStoreListManager.GetItems(page, pageSize, out total);
            if (GetListEvent == null) return;
            GetListEvent(this, new MSKatushaListManagerEventArgs<T> { Items = list, LastUpdateTime = DateTime.MinValue, NewCount = list.Count, Message = "", Total = total });
        }

        public T GetItemDataAt(int index)
        {
            return _dictionary[index].Data;
        }

        public MSKatushaCachedListItem<T, TL> GetItemAt(int index, Func<ImageList, T, int, TL> newViewItem)
        {
            var msKatushaListItem = _dictionary.ContainsKey(index) ? _dictionary[index] : AddItem(index, newViewItem);
            msKatushaListItem.LastUpdate = DateTime.Now;
            return msKatushaListItem;
        }

        private MSKatushaCachedListItem<T, TL> AddItem(int index, Func<ImageList, T, int, TL> getViewItem)
        {
            if (_dictionary.Count == _cacheSize) RemoveOldest();
            if (getViewItem != null)
            {
                var items = _ravenStoreListManager.GetItems(index, index + 1);
                if (items.Count > 0)
                {
                    var item = items[0];
                    var listViewItem = getViewItem(_imageList, item, index);
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

    }
}
