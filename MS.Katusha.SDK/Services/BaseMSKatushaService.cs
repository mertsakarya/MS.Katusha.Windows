using System;
using System.IO;
using MS.Katusha.SDK.Raven;
using RestSharp;

namespace MS.Katusha.SDK.Services
{
    public abstract class BaseMSKatushaService
    {
        protected S3FS S3Fs { get; private set; }
        protected string Username { get; private set; }
        protected string Password { get; private set; }
        protected string DataFolder { get; private set; }
        protected string BaseUrl { get; private set; }
        protected HttpBasicAuthenticator Authenticator { get; private set; }
        public string Result = "";

        protected BaseMSKatushaService(MSKatushaServiceSettings serviceSettings)
        {
            S3Fs = serviceSettings.S3Fs;
            Username = serviceSettings.Username;
            Password = serviceSettings.Password;
            BaseUrl = (!String.IsNullOrWhiteSpace(serviceSettings.BaseUrl)) ? serviceSettings.BaseUrl : "http://www.mskatusha.com/";

            var s3Folder = "\\" + new Uri(BaseUrl).Host;
            DataFolder = serviceSettings.DataFolder + s3Folder;
            if (!Directory.Exists(DataFolder)) Directory.CreateDirectory(DataFolder);
            if (!Directory.Exists(DataFolder + "\\Images")) Directory.CreateDirectory(DataFolder + "\\Images");
            Authenticator = new HttpBasicAuthenticator(Username, Password);
        }

    }
}