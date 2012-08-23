using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
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
        public List<Guid> Profiles { get; set; }
        public int Total { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
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
            return response.Content;
        }

        public IList<Guid> GetProfiles(Sex gender, out int total, int page = 1)
        {
            var client = new RestClient(_baseUrl) { Authenticator = _authenticator };
            var request = new RestRequest("Api/Search/{page}", Method.POST) { RequestFormat = DataFormat.Json }
                .AddUrlSegment("page", page.ToString(CultureInfo.InvariantCulture))
                .AddBody(new { Gender = Enum.GetName(typeof(Sex), gender)});
            var result = client.Execute<ApiSearchResultModel>(request);
            total = result.Data.Total;
            return result.Data.Profiles;
        }

        public HttpStatusCode DeleteProfile(Guid guid)
        {
            var client = new RestClient(_baseUrl);
            var request = new RestRequest("Api/DeleteProfile/{guid}", Method.GET)
                .AddUrlSegment("guid", guid.ToString());
            var response = client.Execute(request);
            return response.StatusCode;
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
