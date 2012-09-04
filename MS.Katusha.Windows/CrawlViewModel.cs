using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MS.Katusha.Crawler;
using MS.Katusha.Windows.Utilities;

namespace MS.Katusha.Windows
{
    public class CrawlViewModel : INotifyPropertyChanged
    {


        private readonly ObservableCollection<string> _crawlItems = new ObservableCollection<string>();
        private string _currentItem;
        private readonly ICrawler _crawler;

        public string CurrentItem
        {
            get { return _currentItem; }
            set { SetProperty(ref _currentItem, value); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (object.Equals(storage, value)) return false;

            storage = value;
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        public ICommand GetCrawlItems { get; private set; }

        public ObservableCollection<string> CrawlItems { get { return _crawlItems; } }

        public CrawlViewModel()
        {
            _crawler = new TravelGirlsCrawler();
            GetCrawlItems = new DelegateCommand(async () => await _crawler.CrawlPageAsync(), () => true);
        }
    }
}
