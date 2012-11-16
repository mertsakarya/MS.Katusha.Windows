using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using MS.Katusha.Domain.Entities;
using MS.Katusha.Domain.Raven.Entities;
using MS.Katusha.Enumerations;
using MS.Katusha.SDK.Raven;
using Newtonsoft.Json;
using RestSharp;
using Conversation = MS.Katusha.Domain.Raven.Entities.Conversation;

namespace MS.Katusha.SDK.Services
{
    public class MSKatushaService : BaseMSKatushaService
    {
        private readonly RavenStore _ravenStore;

        public MSKatushaService(MSKatushaServiceSettings serviceSettings)
            : base(serviceSettings)
        {
            _ravenStore = new RavenStore(DocumentStoreManager.GetInstance(DataFolder));

        }

        public Image GetImage(Guid guid, PhotoType photoType = PhotoType.Thumbnail)
        {
            //var photoType = PhotoType.Thumbnail;
            var path = DataFolder + "\\Images";
            var file = String.Format("{0}\\{1}-{2}.jpg", path, (byte)photoType, guid);
            if (!File.Exists(file)) {
                var s3 = S3Fs.FileSystem;
                var url = s3.GetPhotoUrl(guid, photoType);
                var webClient = new WebClient();
                webClient.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;" + ")");
                webClient.Headers["Accept"] = "/";
                try {
                    webClient.DownloadFile(url, file);
                } catch {
                    return GetImage(Guid.Empty);
                }
            }
            var f = new FileInfo(file);
            if (f.Length == 0)
            {
                f.Delete();
                return GetImage(Guid.Empty);
            }
            var image = Image.FromFile(file);
            return image;
        }

        public string GetProfile(Guid guid)
        {
            var client = new RestClient(BaseUrl) {Authenticator = Authenticator};
            var request = new RestRequest("Api/GetProfile/{guid}", Method.GET)
                .AddUrlSegment("guid", guid.ToString());
            var response = client.Execute(request);
            Result = String.Format("curl -u {0}:{1} {2}", Username, Password, response.ResponseUri);
            if(String.IsNullOrWhiteSpace(response.Content)) return "";
            string result;
            try {
                var obj = JsonConvert.DeserializeObject(response.Content);
                result = JsonConvert.SerializeObject(obj, Formatting.Indented);
            } catch {
                result = "";
            }
            return result;
        }

        public IList<Profile> GetProfiles(string text, string criteria)
        {
            return _ravenStore.GetProfiles(text, criteria);
        }

        public string DeleteProfile(Guid guid)
        {
            var client = new RestClient(BaseUrl) { Authenticator = Authenticator };
            var request = new RestRequest("Api/DeleteProfile/{guid}", Method.GET)
                .AddUrlSegment("guid", guid.ToString());
            var response = client.Execute(request);
            Result = String.Format("curl -u {0}:{1} {2}", Username, Password, response.ResponseUri);
            return response.Content;
        }

        public Guid GetProfileGuid(string key)
        {
            var client = new RestClient(BaseUrl) { Authenticator = Authenticator };
            var request = new RestRequest("Api/GetProfileGuid/{key}", Method.GET)
                .AddUrlSegment("key", key);
            var response = client.Execute(request);
            Result = String.Format("curl -u {0}:{1} {2}", Username, Password, response.ResponseUri);
            var content = response.Content;
            if (content == "{userGuid:''}") return Guid.Empty;
            var obj = JsonConvert.DeserializeObject<JsonObject>(content);
            var str = obj["userGuid"].ToString();
            if (String.IsNullOrWhiteSpace(str)) return Guid.Empty;
            return Guid.Parse(str);
        }

        public HttpStatusCode SetProfile(string profileJsonText)
        {
            var client = new RestClient(BaseUrl) { Authenticator = Authenticator };
            var request = new RestRequest("Api/SetProfile", Method.POST);
            request.AddHeader("Accept", "application/json");
            request.Parameters.Clear();
            request.AddParameter("application/json", profileJsonText, ParameterType.RequestBody);
            //"Api/SetProfile", Method.POST) { RequestFormat = DataFormat.Json }.AddBody(profileJsonText);
            var result = client.Execute(request);
            return result.StatusCode;
        }

        public IList<Dialog> GetDialogs(Guid guid)
        {
            var client = new RestClient(BaseUrl) { Authenticator = Authenticator };
            var request = new RestRequest("Api/GetDialogs/{key}", Method.GET) {RequestFormat = DataFormat.Json}
                .AddUrlSegment("key", guid.ToString());
            var response = client.Execute<List<Dialog>>(request);
            Result = String.Format("curl -u {0}:{1} {2}", Username, Password, response.ResponseUri);
            return response.Data ?? new List<Dialog>();
        }

        public Profile GetProfile(long id) { return _ravenStore.GetProfile(id); }

        public IList<Conversation> GetDialog(long fromId, long toId)
        {
            var client = new RestClient(BaseUrl) { Authenticator = Authenticator };
            var request = new RestRequest("Api/GetDialog/{from}/{to}", Method.GET) { RequestFormat = DataFormat.Json }
                .AddUrlSegment("from", fromId.ToString(CultureInfo.InvariantCulture))
                .AddUrlSegment("to", toId.ToString(CultureInfo.InvariantCulture));
            var response = client.Execute<List<Conversation>>(request);
            Result = String.Format("curl -u {0}:{1} {2}", Username, Password, response.ResponseUri);
            if (response.Data == null) {
                return new List<Conversation>();
            }
            return response.Data;
        }

        public string DeleteMessage(Guid guid) {
            var client = new RestClient(BaseUrl) { Authenticator = Authenticator };
            var request = new RestRequest("Api/DeleteMessage/{guid}", Method.GET)
                .AddUrlSegment("guid", guid.ToString());
            var response = client.Execute(request);
            Result = String.Format("curl -u {0}:{1} {2}", Username, Password, response.ResponseUri);
            return response.Content;
        }

        public string DeleteDialog(Guid from, Guid to)
        {
            var client = new RestClient(BaseUrl) { Authenticator = Authenticator };
            var request = new RestRequest("Api/DeleteDialog/{from}/{to}", Method.GET)
                .AddUrlSegment("from", from.ToString())
                .AddUrlSegment("to", to.ToString());
            var response = client.Execute(request);
            Result = String.Format("curl -u {0}:{1} {2}", Username, Password, response.ResponseUri);
            return response.Content;

        }

        public void Explore() { Process.Start("explorer.exe", DataFolder); }

        public void ClearCache()
        {
            var folder = new DirectoryInfo(DataFolder + "\\Profiles");
            folder.Delete(true);
            //folder = new DirectoryInfo(_dataFolder + "\\Images");
            //folder.Delete(true);
            _ravenStore.DeleteAll();
        }

    }

}
