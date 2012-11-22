using System;
using System.Collections.Generic;
using System.Linq;
using MS.Katusha.Domain.Entities;
using MS.Katusha.Domain.Entities.BaseEntities;
using Raven.Client;
using Raven.Client.Linq;

namespace MS.Katusha.SDK.Raven
{
    public class RavenStoreListManager<T>  where T : BaseGuidModel
    {
        private readonly string _typeName;
        private readonly IDocumentStore _docStore;

        public RavenStoreListManager(IDocumentStore docStore)
        {
            _typeName = typeof(T).Name;
            _docStore = docStore;
        }

        public DateTime GetLastUpdate()
        {
            using (var session = _docStore.OpenSession())
            {
                var o =
                    session.Query<T>()
                           .Customize(x => x.WaitForNonStaleResults(TimeSpan.FromSeconds(30)))
                           .OrderByDescending(p => p.ModifiedDate)
                           .FirstOrDefault();

                var maxDate = (o == null) ? DateTime.MinValue : o.ModifiedDate;
                return maxDate;
            }
        }

        public void SetLastUpdate(DateTime dateTime)
        {
            using (var session = _docStore.OpenSession())
            {
                var lastUpdateObject = new LastUpdateObject {Id = _typeName, LastUpdate = dateTime};
                session.Store(lastUpdateObject);
                session.SaveChanges();
            }
        }

        public List<T> GetItems(int start, int end)
        {
            using (var session = _docStore.OpenSession())
            {
                RavenQueryStatistics stats;
                var list = session.Query<T>().Customize(x => x.WaitForNonStaleResults(TimeSpan.FromSeconds(30))).Skip(start).Take(end - start).OrderByDescending(p => p.ModifiedDate).ToList();
                return list;
            }
        }

        public List<T> GetItems(int page, int pageSize, out int total)
        {
            using (var session = _docStore.OpenSession())
            {
                RavenQueryStatistics stats;
                var list = session.Query<T>().Statistics(out stats).Customize(x => x.WaitForNonStaleResults(TimeSpan.FromSeconds(30))).Skip((page-1)*pageSize).Take(pageSize).OrderByDescending(p => p.ModifiedDate).ToList();
                total = stats.TotalResults;
                return list;
            }
        }
        
        public int GetItemCount()
        {
            using (var session = _docStore.OpenSession())
            {
                RavenQueryStatistics stats;
                var a = session.Query<T>().Customize(x=> x.WaitForNonStaleResults(new TimeSpan(0,2,0))).Statistics(out stats).Take(0).ToArray();
                return stats.TotalResults;
            }
        }

        public DateTime AddItems(IList<T> items)
        {
            var maxDate = new DateTime(1900, 1, 1);
            using (var session = _docStore.OpenSession())
            {
                foreach (var item in items)
                {
                    session.Store(item);
                    if (item.ModifiedDate > maxDate) maxDate = item.ModifiedDate;
                }
                var luo = new LastUpdateObject() { Id = _typeName, LastUpdate = (DateTime)maxDate.AddSeconds(1) };
                session.Store(luo);
                session.SaveChanges();
            }
            return maxDate;
        }

        public void Delete(long id)
        {
            using (var session = _docStore.OpenSession())
            {
                var item = session.Query<T>().SingleOrDefault(p => p.Id == id);
                if (item != null)
                {
                    session.Delete(item);
                    session.SaveChanges();
                }
            }
        }

        public void Update(Photo data)
        {
            using (var session = _docStore.OpenSession())
            {
                session.Store(data);
                session.SaveChanges();
            }
        }
    }
}