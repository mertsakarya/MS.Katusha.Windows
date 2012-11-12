using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using MS.Katusha.Domain.Entities;
using MS.Katusha.Domain.Raven.Entities;
using MS.Katusha.Domain.Service;
using MS.Katusha.Enumerations;
using Newtonsoft.Json;
using RestSharp;
using Conversation = MS.Katusha.Domain.Raven.Entities.Conversation;
using DialogResult = System.Windows.Forms.DialogResult;

namespace MS.Katusha.SDK
{
    //public enum Sex : byte
    //{
    //    Male = 1,
    //    Female = 2
    //}
    
    //public class ApiSearchResultModel
    //{
    //    public List<ApiProfileInfo> Profiles { get; set; }
    //    public int Total { get; set; }
    //    public int PageIndex { get; set; }
    //    public int PageSize { get; set; }
    //}

    //public class ApiProfileInfo
    //{
    //    public long Id { get; set; }
    //    public Guid Guid { get; set; }
    //    public string Name { get; set; }
    //    public Guid ProfilePhotoGuid { get; set; }
    //    public string UserName { get; set; }
    //    public string Email { get; set; }
    //    public DateTime LastUpdate { get; set; }
    //}
    public class MSKatushaWinFormsConfiguration
    {
        public static readonly string[] Servers = new[] { "http://www.mskatusha.com/", "https://mskatusha.apphb.com/", "https://mskatushaeu.apphb.com/", "http://localhost:10595/", "http://localhost/" };
        public static readonly string[] Buckets = new[] { "s.mskatusha.com", "MS.Katusha", "MS.Katusha.EU", "MS.Katusha.Test" };
    }

    public class MSKatushaService
    {
        private readonly string _username;
        private readonly string _password;
        private readonly string _dataFolder;
        private readonly string _baseUrl;
        private readonly HttpBasicAuthenticator _authenticator;
        public string Result = "";
        private RavenStore _ravenStore;

        public MSKatushaService(string username, string password, string baseUrl, string dataFolder)
        {
            _username = username;
            _password = password;
            _baseUrl = (!String.IsNullOrWhiteSpace(baseUrl)) ? baseUrl : "https://mskatusha.apphb.com/";

            var s3Folder = "\\" + new Uri(_baseUrl).Host;
            _dataFolder = dataFolder + s3Folder;
            if (!Directory.Exists(_dataFolder)) Directory.CreateDirectory(_dataFolder);
            if (!Directory.Exists(_dataFolder + "\\Images")) Directory.CreateDirectory(_dataFolder + "\\Images");
            if (!Directory.Exists(_dataFolder + "\\Profiles")) Directory.CreateDirectory(_dataFolder + "\\Profiles");
            if (!Directory.Exists(_dataFolder + "\\Data")) Directory.CreateDirectory(_dataFolder + "\\Data");
            _authenticator = new HttpBasicAuthenticator(_username, _password);
            _ravenStore = RavenStore.GetInstance(_dataFolder + "\\Data");
        }

        public Image GetImage(Guid guid, S3FS s3Fs)
        {
            if(s3Fs == null) throw new ArgumentNullException("S3FS");
            var path = _dataFolder + "\\Images";
            var file = path + "\\4-" + guid + ".jpg";
            if (!File.Exists(file)) {
                var s3 = s3Fs.FileSystem;
                var url = s3.GetPhotoUrl(guid, PhotoType.Icon);
                var webClient = new WebClient();
                webClient.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;" + ")");
                webClient.Headers["Accept"] = "/";
                try {
                    webClient.DownloadFile(url, file);
                } catch {
                    return GetImage(Guid.Empty, s3Fs);
                }
            }
            return Image.FromFile(file);
        }


        public string GetProfile(Guid guid)
        {
            var client = new RestClient(_baseUrl) {Authenticator = _authenticator};
            var request = new RestRequest("Api/GetProfile/{guid}", Method.GET)
                .AddUrlSegment("guid", guid.ToString());
            var response = client.Execute(request);
            Result = String.Format("curl -u {0}:{1} {2}", _username, _password, response.ResponseUri);
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

        public IList<ApiProfileInfo> GetProfiles(Sex gender, out int total, int page = 1)
        {
            var client = new RestClient(_baseUrl) { Authenticator = _authenticator };
            var request = new RestRequest("Api/Search/{page}", Method.POST) { RequestFormat = DataFormat.Json }
                .AddUrlSegment("page", page.ToString(CultureInfo.InvariantCulture))
                .AddBody(new { Gender = Enum.GetName(typeof(Sex), gender)});
            var response = client.Execute<ApiSearchResult>(request);
            Result = String.Format("curl -u {0}:{1} {2}", _username, _password, response.ResponseUri);
            if (response.Data == null) {
                total = 0;
                return new List<ApiProfileInfo>();
            }
            total = response.Data.Total;
            return response.Data.Profiles;
        }

        public IList<Profile> GetProfiles()
        {
            var lastUpdateTime = _ravenStore.LastUpdate("Profiles");

            var client = new RestClient(_baseUrl) { Authenticator = _authenticator };
            var request = new RestRequest("Api/GetProfilesByTime/{key}", Method.GET) { RequestFormat = DataFormat.Json }
                .AddUrlSegment("key", "1")
                .AddParameter("date", lastUpdateTime.ToString("u"));
            var response = client.Execute<List<Profile>>(request);
            Result = String.Format("curl -u {0}:{1} {2}", _username, _password, response.ResponseUri);
            if (response.Data == null) {
                return new List<Profile>();
            }
            var profilesFolder = _dataFolder + "\\Profiles";
            var profiles = response.Data;
            if (profiles == null) {
                return new List<Profile>();
            }
            if (profiles.Count > 0) {
                var dialogResult = MessageBox.Show(String.Format("Found {0} profiles to update.\r\n\r\nDo you want to update cache? [Yes]", profiles.Count), "FOUND UPDATE", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                if (dialogResult == DialogResult.Yes) {
                    var maxDate = new DateTime(1900, 1, 1);
                    foreach (var profile in profiles) {
                        _ravenStore.AddProfile(profile);
                        var file = profilesFolder + "\\" + profile.Guid + ".json";
                        WriteFile(file, JsonConvert.SerializeObject(profile));
                        if (profile.ModifiedDate > maxDate) maxDate = profile.ModifiedDate;
                    }
                    _ravenStore.LastUpdate("Profiles", maxDate.AddSeconds(1));
                }
            }
            return _ravenStore.GetProfiles();
        }

        public string DeleteProfile(Guid guid)
        {
            var client = new RestClient(_baseUrl) { Authenticator = _authenticator };
            var request = new RestRequest("Api/DeleteProfile/{guid}", Method.GET)
                .AddUrlSegment("guid", guid.ToString());
            var response = client.Execute(request);
            Result = String.Format("curl -u {0}:{1} {2}", _username, _password, response.ResponseUri);
            return response.Content;
        }

        public Guid GetProfileGuid(string key)
        {
            var client = new RestClient(_baseUrl) { Authenticator = _authenticator };
            var request = new RestRequest("Api/GetProfileGuid/{key}", Method.GET)
                .AddUrlSegment("key", key);
            var response = client.Execute(request);
            Result = String.Format("curl -u {0}:{1} {2}", _username, _password, response.ResponseUri);
            var content = response.Content;
            if (content == "{userGuid:''}") return Guid.Empty;
            var obj = JsonConvert.DeserializeObject<JsonObject>(content);
            var str = obj["userGuid"].ToString();
            if (String.IsNullOrWhiteSpace(str)) return Guid.Empty;
            return Guid.Parse(str);
        }

        public HttpStatusCode SetProfile(string profileJsonText)
        {
            var client = new RestClient(_baseUrl) { Authenticator = _authenticator };
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
            var client = new RestClient(_baseUrl) { Authenticator = _authenticator };
            var request = new RestRequest("Api/GetDialogs/{key}", Method.GET) {RequestFormat = DataFormat.Json}
                .AddUrlSegment("key", guid.ToString());
            var response = client.Execute<List<Dialog>>(request);
            Result = String.Format("curl -u {0}:{1} {2}", _username, _password, response.ResponseUri);
            if (response.Data == null) {
                return new List<Dialog>();
            }
            return response.Data;
        }

        private string ReadFile(string fileName)
        {
            using (var streamReadr = new StreamReader(fileName, Encoding.UTF8))
                return streamReadr.ReadToEnd();
        }
        private void WriteFile(string fileName, string text)
        {
            using (var stream = new StreamWriter(fileName))
                stream.Write(text);
        }

        public IList<Profile> GetProfiles(string text, string criteria)
        {
            return _ravenStore.GetProfiles(text, criteria);
        }

        public void Explore() { Process.Start("explorer.exe", _dataFolder); }

        public IList<Conversation> GetDialog(long fromId, long toId)
        {
            var client = new RestClient(_baseUrl) { Authenticator = _authenticator };
            var request = new RestRequest("Api/GetDialog/{from}/{to}", Method.GET) { RequestFormat = DataFormat.Json }
                .AddUrlSegment("from", fromId.ToString(CultureInfo.InvariantCulture))
                .AddUrlSegment("to", toId.ToString(CultureInfo.InvariantCulture));
            var response = client.Execute<List<Conversation>>(request);
            Result = String.Format("curl -u {0}:{1} {2}", _username, _password, response.ResponseUri);
            if (response.Data == null) {
                return new List<Conversation>();
            }
            return response.Data;
        }

        public string DeleteMessage(Guid guid) {
            var client = new RestClient(_baseUrl) { Authenticator = _authenticator };
            var request = new RestRequest("Api/DeleteMessage/{guid}", Method.GET)
                .AddUrlSegment("guid", guid.ToString());
            var response = client.Execute(request);
            Result = String.Format("curl -u {0}:{1} {2}", _username, _password, response.ResponseUri);
            return response.Content;
        }

        public string DeleteDialog(Guid from, Guid to)
        {
            var client = new RestClient(_baseUrl) { Authenticator = _authenticator };
            var request = new RestRequest("Api/DeleteDialog/{from}/{to}", Method.GET)
                .AddUrlSegment("from", from.ToString())
                .AddUrlSegment("to", to.ToString());
            var response = client.Execute(request);
            Result = String.Format("curl -u {0}:{1} {2}", _username, _password, response.ResponseUri);
            return response.Content;

        }
    }
}
