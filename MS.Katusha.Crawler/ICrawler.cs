using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MS.Katusha.Crawler
{
    public class CrawlItemResult
    {
        public string UniqueId { get; set; }
        public string Output { get; set; }
    }

    public interface ICrawler
    {
        IDictionary<string, string> CrawlPage(params string[] values);
        CrawlItemResult CrawlItem(params string[] values);
        Task<IDictionary<string, string>> CrawlPageAsync(params string[] values);
        Task<CrawlItemResult> CrawlItemAsync(params string[] values);
    }
}
