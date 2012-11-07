using System;
using System.Collections.Generic;
using MS.Katusha.Domain.Service;

namespace MS.Katusha.Crawler
{
    public class CrawlItemResult
    {
        public string UniqueId { get; set; }
        public string Output { get; set; }
        public Uri Uri { get; set; }
    }

    public class CrawlPageResult
    {
        public IDictionary<string, string> Items { get; set; }
    }

    public class DownloadPhotoResult
    {
        public Uri Uri { get; set; }
        public int ImageOrder { get; set; }
        public byte[] Data { get; set; }
    }

    public delegate void CrawlPageReadyEvent(ICrawler crawler, CrawlPageResult crawlPageResult);
    public delegate void CrawlItemReadyEvent(ICrawler crawler, CrawlItemResult crawlItemResult);

    public interface ICrawler
    {
        IDictionary<string, string> CrawlPage(params string[] values);
        CrawlItemResult CrawlItem(params string[] values);
        void CrawlPageAsync(CrawlPageReadyEvent onCrawlPageReady, params string[] values);
        void CrawlItemAsync(CrawlItemReadyEvent onCrawlItemReady, params string[] values);
    }
}
