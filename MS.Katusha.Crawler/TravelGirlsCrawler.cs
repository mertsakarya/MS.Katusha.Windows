using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MS.Katusha.Domain.Service;
using MS.Katusha.Enumerations;
using Newtonsoft.Json;

namespace MS.Katusha.Crawler
{
    public class TravelGirlsCrawler : ICrawler
    {
        private const string BaseUrl = "http://www.travelgirls.com";
        private static readonly string KatushaFolder = GetDropboxFolder() + "\\MS.Katusha";

        private static string GetDropboxFolder()
        {
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Dropbox\\host.db");
            var dbBase64Text = Convert.FromBase64String(File.ReadAllLines(dbPath)[1]);
            var folderPath = Encoding.ASCII.GetString(dbBase64Text);
            return folderPath;
        }
        /// <summary>
        /// Crawls a TravelGirls Page
        /// eg. http://www.travelgirls.com/members/girls/page/1
        /// </summary>
        /// <param name="values">first argument is gender (Male/Female) second argument page number</param>
        /// <returns></returns>
        public IDictionary<string, string> CrawlPage(params string[] values)
        {
            int pageNo;
            var gender = ParseCrawlPageParameters(values, out pageNo);
            return GetPage(gender, pageNo);
        }

        public async void CrawlPageAsync(CrawlPageReadyEvent onCrawlPageReady, params string[] values)
        {
            int pageNo;
            var gender = ParseCrawlPageParameters(values, out pageNo);
            var future = GetPageAsync(gender, pageNo);
            if (onCrawlPageReady != null) {
                var result = await future;
                onCrawlPageReady(this, new CrawlPageResult { Items = result });
            }
        }

        private static Sex ParseCrawlPageParameters(string[] values, out int pageNo)
        {
            Sex gender;
            pageNo = 0;
            if (values.Length != 2) throw new ArgumentException("Need two values: First Gender second is page number");
            switch (values[0].ToLowerInvariant()) {
                case "female":
                    gender = Sex.Female;
                    break;
                case "male":
                    gender = Sex.Male;
                    break;
                default:
                    throw new ArgumentException("Gender unknown, must be male or female, case doesn't matter.");
            }
            if (!int.TryParse(values[1], out pageNo))
                throw new ArgumentException("Second parameter must be integer.");
            if (pageNo < 1 || pageNo > 100)
                throw new ArgumentException("page number must be between 1 and 100.");
            return gender;
        }

        private  async Task<IDictionary<string, string>> GetPageAsync(Sex gender, int page)
        {
            var sex = (gender == Sex.Male) ? "men" : "girls";
            var client = new HttpClient();
            //client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.4 (KHTML, like Gecko) Chrome/22.0.1229.79 Safari/537.4");
            //client.DefaultRequestHeaders.Host = "www.travelgirls.com";
            //client.DefaultRequestHeaders.Connection.Add("keep-alive");
            //client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("text/html"));
            //client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/xhtml+xml"));
            //client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/xml"));
            //client.DefaultRequestHeaders.AcceptLanguage.Add(StringWithQualityHeaderValue.Parse("en-US"));
            //client.DefaultRequestHeaders.AcceptCharset.Add(StringWithQualityHeaderValue.Parse("ISO-8859-1"));
            //client.DefaultRequestHeaders.AcceptCharset.Add(StringWithQualityHeaderValue.Parse("utf-8"));

            var future = client.GetStringAsync(String.Format("{0}/members/{1}/page/{2}", BaseUrl, sex, page));
            var content = await future;
            var doc = new HtmlDocument();
            doc.LoadHtml(content);
            var nodes = doc.DocumentNode.SelectNodes("//div[@class=\"memberPhoto\"]");
            if (nodes == null || nodes.Count <= 0) return new Dictionary<string, string>();
            var dict = new Dictionary<string, string>(nodes.Count);
            foreach (var memberNode in nodes) {
                var linkNode = memberNode.SelectSingleNode("a");
                var hrefAttr = linkNode.Attributes["href"];
                if (hrefAttr != null && !dict.ContainsKey(hrefAttr.Value.Trim())) dict.Add(hrefAttr.Value.Trim(), hrefAttr.Value.Trim());
            }
            return dict;
        }

        private IDictionary<string, string> GetPage(Sex gender, int page)
        {
            var sex = (gender == Sex.Male) ? "men" : "girls";
            using (var stream = new WebClient().OpenRead(String.Format("{0}/members/{1}/page/{2}", BaseUrl, sex, page))) {
                var doc = new HtmlDocument();
                doc.Load(stream);
                var nodes = doc.DocumentNode.SelectNodes("//div[@class=\"memberPhoto\"]");
                if (nodes.Count <= 0) return new Dictionary<string, string>();
                var dict = new Dictionary<string, string>(nodes.Count);
                foreach (HtmlNode memberNode in nodes) {
                    var linkNode = memberNode.SelectSingleNode("a");
                    var hrefAttr = linkNode.Attributes["href"];
                    if (hrefAttr != null && !dict.ContainsKey(hrefAttr.Value.Trim())) dict.Add(hrefAttr.Value.Trim(), hrefAttr.Value.Trim());
                }
                return dict;
            }
        }

        public CrawlItemResult CrawlItem(params string[] parameters)
        {
            Uri uri;
            string country;
            string uniqueId;
            var gender = ParseCrawlItemParameters(out uniqueId, parameters, out uri, out country);
            return ProcessProfilePage(uniqueId, country, gender, uri);
        }

        public async void CrawlItemAsync(CrawlItemReadyEvent onCrawlItemReady, params string[] parameters)
        {
            Uri uri;
            string country;
            string uniqueId;
            var gender = ParseCrawlItemParameters(out uniqueId, parameters, out uri, out country);
            var future = ProcessProfilePageAsync(uniqueId, country, gender, uri);
            if (onCrawlItemReady != null) {
                var result = await future;
                onCrawlItemReady(this, result);
            }
        }

        private CrawlItemResult ProcessProfilePage(string uniqueId, string country, Sex gender, Uri uri)
        {
            return new CrawlItemResult { UniqueId = uniqueId, Output = ProcessProfilePageDetail(uniqueId, country, gender, (new WebClient()).DownloadString(uri)) };
        }

        private async Task<CrawlItemResult> ProcessProfilePageAsync(string uniqueId, string country, Sex gender, Uri uri)
        {
            var client = new HttpClient();
            var future = client.GetStringAsync(uri);
            var content = await future;
            var outputFuture = ProcessProfilePageDetailAsync(uniqueId, country, gender, content);
            var output = await outputFuture;
            return new CrawlItemResult { UniqueId = uniqueId, Uri = uri,  Output = output};
        }

        private static string ProcessProfilePageDetail(string uniqueId, string country, Sex gender, string content)
        {
            try {
                var doc = new HtmlDocument();
                doc.LoadHtml(content);
                var node = doc.DocumentNode.SelectSingleNode("//div[@class=\"main_content\"]");
                if (node != null) {
                    var infos = node.SelectNodes("//div[@class=\"member_information_block\"]/table");
                    var images = node.SelectNodes("//a[@class=\"ajaxing\"]/img");
                    if (infos.Count > 0 && images.Count > 0) {
                        var name = node.SelectSingleNode("h1/span").InnerText;
                        int birthYear;
                        try {
                            birthYear = DateTime.Now.Year - int.Parse(node.SelectSingleNode("h1/span[2]").InnerText.Split(' ')[0]);
                        } catch {
                            birthYear = 1980;
                        }
                        string aboutMe;
                        try {
                            aboutMe = node.SelectSingleNode("//div[@class=\"about_me\"]").InnerHtml;
                        } catch {
                            aboutMe = "No description";
                        }
                        var guid = Guid.NewGuid();
                        var user = new ApiAdminUser { Email = "mskatusha.user@gmail.com", Guid = guid, UserName = uniqueId, EmailValidated = true, MembershipType = MembershipType.Normal, UserRole = UserRole.Normal, Password = "690514", Expires = DateTime.Now.AddYears(2) };
                        var profile = new ApiProfile { Gender = gender, BirthYear = birthYear, Guid = guid, Name = name, Location = new ApiLocation { CountryName = country }, Photos = new List<ApiPhoto>(), Description = aboutMe };
                        var adminProfile = new AdminExtendedProfile(new ApiExtendedProfile()) { User = user, Profile = profile, PhotoBackups = new List<ApiPhotoBackup>() };
                        return ProcessProfile(adminProfile, infos, images);
                    }
                }
            } catch { }
            return "";
        }

        private static async Task<string> ProcessProfilePageDetailAsync(string uniqueId, string country, Sex gender, string content)
        {
            try {
                var doc = new HtmlDocument();
                doc.LoadHtml(content);
                var node = doc.DocumentNode.SelectSingleNode("//div[@class=\"main_content\"]");
                if (node != null) {
                    var infos = node.SelectNodes("//div[@class=\"member_information_block\"]/table");
                    var images = node.SelectNodes("//a[@class=\"ajaxing\"]/img");
                    if (infos.Count > 0 && images.Count > 0) {
                        var name = node.SelectSingleNode("h1/span").InnerText;
                        int birthYear;
                        try {
                            birthYear = DateTime.Now.Year - int.Parse(node.SelectSingleNode("h1/span[2]").InnerText.Split(' ')[0]);
                        } catch {
                            birthYear = 1980;
                        }
                        string aboutMe;
                        try {
                            aboutMe = node.SelectSingleNode("//div[@class=\"about_me\"]").InnerHtml;
                        } catch {
                            aboutMe = "No description";
                        }
                        var guid = Guid.NewGuid();
                        var user = new ApiAdminUser { Email = "mskatusha.user@gmail.com", Guid = guid, UserName = uniqueId, EmailValidated = true, MembershipType = MembershipType.Normal, UserRole = UserRole.Normal, Password = "690514", Expires = DateTime.Now.AddYears(2) };
                        var profile = new ApiProfile { Gender = gender, BirthYear = birthYear, Guid = guid, Name = name, Location = new ApiLocation { CountryName = country }, Photos = new List<ApiPhoto>(), Description = aboutMe };
                        var adminProfile = new AdminExtendedProfile(new ApiExtendedProfile()) { User = user, Profile = profile, PhotoBackups = new List<ApiPhotoBackup>() };
                        var future  = ProcessProfileAsync(adminProfile, infos, images);
                        var retval = await future;
                        return retval;
                    }
                }
            } catch { }
            return "";
        }

        private static Sex ParseCrawlItemParameters(out string uniqueId, string[] parameters, out Uri uri, out string country)
        {
            Sex gender;
            if (parameters.Length != 2) throw new ArgumentException("Need two values: First Gender second href number");
            switch (parameters[0].ToLowerInvariant()) {
                case "female":
                    gender = Sex.Female;
                    break;
                case "male":
                    gender = Sex.Male;
                    break;
                default:
                    throw new ArgumentException("Gender unknown, must be male or female, case doesn't matter.");
            }

            var href = parameters[1];
            if (!Uri.TryCreate(BaseUrl + href, UriKind.Absolute, out uri))
                throw new ArgumentException("Cannot parse HRef parameter. The 2nd one.");
            var values = href.Substring(8).Split('-');
            var id = values[0];
            country = values[2];
            uniqueId = "tg_" + id;
            return gender;
        }

        private static string ProcessProfile(AdminExtendedProfile profile, IEnumerable<HtmlNode> infos, IEnumerable<HtmlNode> images)
        {
            SetProfile(profile, infos);
            var imageOrder = 0;
            foreach (var src in images.Select(imageNode => imageNode.Attributes["src"].Value.Trim()).Where(src => src.StartsWith("/uploads/") && src.IndexOf("/mini", StringComparison.Ordinal) > 0)) {
                imageOrder++;
                var guid = Guid.NewGuid();
                var url = src.Replace("/mini", "/");
                var data = (new WebClient()).DownloadData(BaseUrl + url);
                profile.Profile.Photos.Add(new ApiPhoto {Guid = guid, ContentType = "image/jpg", FileName = url, Description = "", Status = (byte) PhotoStatus.Ready});
                profile.PhotoBackups.Add(new ApiPhotoBackup {Data = data, Guid = guid});
                if (imageOrder == 1) profile.Profile.ProfilePhotoGuid = guid;
            }
            var str =  JsonConvert.SerializeObject(profile);
            return str;
        }

        private static async Task<DownloadPhotoResult> DownloadPhotoAsync(Uri uri, int imageOrder)
        {
            var client = new HttpClient();
            var data = client.GetByteArrayAsync(uri);
            var future = await data;
            var downloadPhotoResult = new DownloadPhotoResult {Data = future, Uri = uri, ImageOrder = imageOrder};
            return downloadPhotoResult;
        }

        private static async Task<string> ProcessProfileAsync(AdminExtendedProfile profile, IEnumerable<HtmlNode> infos, IEnumerable<HtmlNode> images)
        {
            var imageOrder = 0;
            var tasks = new List<Task<DownloadPhotoResult>>();
            foreach (var src in images.Select(imageNode => imageNode.Attributes["src"].Value.Trim()).Where(src => src.StartsWith("/uploads/") && src.IndexOf("/mini", StringComparison.Ordinal) > 0)) {
                imageOrder++;
                var url = src.Replace("/mini", "/");
                Uri uri;
                if (!Uri.TryCreate(BaseUrl + url, UriKind.Absolute, out uri)) continue;
                var dataTask = DownloadPhotoAsync(uri, imageOrder);
                tasks.Add(dataTask);
            }
            var downloadPhotoResults = await Task.WhenAll(tasks);
            SetProfile(profile, infos);
            foreach (var downloadPhotoResult in downloadPhotoResults) {
                var guid = Guid.NewGuid();
                profile.Profile.Photos.Add(new ApiPhoto { Guid = guid, ContentType = "image/jpg", FileName = downloadPhotoResult.Uri.ToString(), Description = "", Status = (byte)PhotoStatus.Ready });
                profile.PhotoBackups.Add(new ApiPhotoBackup { Data = downloadPhotoResult.Data, Guid = guid });
                if (downloadPhotoResult.ImageOrder == 1) profile.Profile.ProfilePhotoGuid = guid;
            }
            var str = JsonConvert.SerializeObject(profile);
            return str;
        }

        private static void SetProfile(AdminExtendedProfile profile, IEnumerable<HtmlNode> infos)
        {
            var p = profile.Profile;
            foreach (var info in infos) {
                var val = info.ChildNodes[0].ChildNodes[1].InnerText.Trim();
                var key = info.ChildNodes[0].ChildNodes[0].InnerText.Trim();
                CheckLookup(key, val);
                switch (key) {
                    case "City, Country:":
                        ToLocation(p, val);
                        break;
                    case "Language:":
                        profile.LanguagesSpoken = ToArray(val);
                        break;
                    case "Height:":
                        int height;
                        try {
                            height = int.Parse(val.Split(' ')[0]);
                        } catch {
                            height = 170;
                        }
                        p.Height = height;
                        break;
                    case "Body type:":
                        BodyBuild bodyBuild;
                        if (Enum.TryParse(val, true, out bodyBuild)) p.BodyBuild = bodyBuild;
                        break;
                    case "Eyes:":
                        EyeColor eyeColor;
                        if (Enum.TryParse(val, true, out eyeColor)) p.EyeColor = eyeColor;
                        break;
                    case "Hair:":
                        HairColor hairColor;
                        if (Enum.TryParse(val, true, out hairColor)) p.HairColor = hairColor;
                        break;
                    case "Looking for:":
                        profile.Searches = ToArray(val);
                        break;
                    case "Dreams to visit:":
                        profile.CountriesToVisit = ToArray(val);
                        break;
                }
            }
        }

        private static void ToLocation(ApiProfile apiProfile, string val)
        {
            if (String.IsNullOrWhiteSpace(val)) return;
            if (String.IsNullOrEmpty(apiProfile.Location.CountryName)) {
                apiProfile.Location.CountryName = val.IndexOf(',') >= 0 ? val.Split(',')[1].Trim() : val;
            }
            if (val.IndexOf(',') >= 0 && !String.IsNullOrEmpty(apiProfile.Location.CountryName)) {
                apiProfile.Location.CityName = val.Split(',')[0].Trim();
            }
        }

        private static string[] ToArray(string val)
        {
            var list = new List<string>();
            if (val == null) return list.ToArray();
            list.AddRange(val.Split(',').Select(str => str.Trim()));
            return list.ToArray();
        }

        private static void CheckLookup(string key, string val)
        {
            var filename = key.Replace(" ", "").Replace(":", "").Replace(",", "");
            using (var file = new StreamWriter(String.Format(KatushaFolder + @"\Lookups\{0}.txt", filename), true)) {
                if (val.IndexOf(',') > 0) {
                    foreach (var item in val.Split(',')) {
                        file.WriteLine(item.Trim());
                    }
                } else
                    file.WriteLine(val);
            }
        }
    }
}
