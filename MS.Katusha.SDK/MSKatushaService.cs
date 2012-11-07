using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using MS.Katusha.Domain.Raven.Entities;
using Newtonsoft.Json;
using RestSharp;

namespace MS.Katusha.SDK
{
    public enum Sex : byte
    {
        Male = 1,
        Female = 2
    }
    
    public class ApiSearchResultModel
    {
        public List<ApiProfileInfo> Profiles { get; set; }
        public int Total { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
    }

    public class ApiProfileInfo
    {
        public long Id { get; set; }
        public Guid Guid { get; set; }
        public string Name { get; set; }
        public Guid ProfilePhotoGuid { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
    }

    public class MSKatushaService
    {
        private readonly string _username;
        private readonly string _password;
        private readonly string _baseUrl;
        private readonly HttpBasicAuthenticator _authenticator;
        public string Result = "";

        public MSKatushaService(string username, string password, string baseUrl = "")
        {
            _username = username;
            _password = password;
            _baseUrl = (!String.IsNullOrWhiteSpace(baseUrl)) ? baseUrl : "https://mskatusha.apphb.com/";
            _authenticator = new HttpBasicAuthenticator(_username, _password);
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
            var response = client.Execute<ApiSearchResultModel>(request);
            Result = String.Format("curl -u {0}:{1} {2}", _username, _password, response.ResponseUri);
            if (response.Data == null) {
                total = 0;
                return new List<ApiProfileInfo>();
            }
            total = response.Data.Total;
            return response.Data.Profiles;
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

    }
}
