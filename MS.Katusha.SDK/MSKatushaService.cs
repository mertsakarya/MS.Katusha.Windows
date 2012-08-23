using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
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
        private HttpBasicAuthenticator _authenticator;

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
            if(String.IsNullOrWhiteSpace(response.Content)) return "";
            string result = "";
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
            var result = client.Execute<ApiSearchResultModel>(request);
            if(result.Data == null) {
                total = 0;
                return new List<ApiProfileInfo>();
            }
            total = result.Data.Total;
            return result.Data.Profiles;
        }

        public string DeleteProfile(Guid guid)
        {
            var client = new RestClient(_baseUrl) { Authenticator = _authenticator };
            var request = new RestRequest("Api/DeleteProfile/{guid}", Method.GET)
                .AddUrlSegment("guid", guid.ToString());
            var response = client.Execute(request);
            return response.Content;
        }

        public HttpStatusCode SetProfile(string profileJsonText)
        {
            var client = new RestClient(_baseUrl) { Authenticator = _authenticator };
            var request = new RestRequest("Api/SetProfile", Method.POST) { RequestFormat = DataFormat.Json }
                .AddBody(profileJsonText);
            var result = client.Execute(request);
            return result.StatusCode;

        }


    }
}
