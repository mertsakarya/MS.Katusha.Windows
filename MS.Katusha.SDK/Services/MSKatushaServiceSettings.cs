namespace MS.Katusha.SDK.Services
{
    public class MSKatushaServiceSettings
    {
        public S3FS S3Fs { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string DataFolder { get; set; }
        public string BaseUrl { get; set; }
    }
}