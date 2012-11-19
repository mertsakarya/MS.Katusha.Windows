using System;
using System.Collections.Generic;
using System.Linq;
using MS.Katusha.Domain.Entities;
using MS.Katusha.Domain.Entities.BaseEntities;
using Raven.Client;
using Raven.Client.Linq;

namespace MS.Katusha.SDK.Raven
{
    public class LastUpdateObject
    {
        public string Id { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    public class RavenStore 
    {
        private readonly IDocumentStore _docStore;

        public RavenStore(IDocumentStore docStore) {
            _docStore = docStore;
        }

        public DateTime LastUpdate(string name, DateTime? value = null)
        {
            if(value == null) {
                using(var session = _docStore.OpenSession()) {
                    var lastUpdateObject = session.Load<LastUpdateObject>(name);
                    if(lastUpdateObject == null) return new DateTime(1900, 1, 1);
                    return lastUpdateObject.LastUpdate;
                }
            }
            using (var session = _docStore.OpenSession()) {
                var luo = new LastUpdateObject {Id = name, LastUpdate = (DateTime) value};
                session.Store(luo);
                session.SaveChanges();
                return luo.LastUpdate;
            }
        }

        public void AddProfile(Profile profile)
        {
            using (var session = _docStore.OpenSession()) {
                session.Store(profile);
                session.SaveChanges();
            }
        }

        public Profile GetProfile(long id)
        {
            using (var session = _docStore.OpenSession()) {
                return session.Load<Profile>(id);
            }
        }

        public IList<Profile> GetProfiles()
        {
            using (var session = _docStore.OpenSession()) {
                return session.Query<Profile>().Take(1000).ToList();
            }
        }

        public IList<Profile> GetProfiles(int start, int end)
        {
            using (var session = _docStore.OpenSession()) {
                RavenQueryStatistics stats;
                var list = session.Query<Profile>().Statistics(out stats).Customize(x => x.WaitForNonStaleResults(TimeSpan.FromSeconds(30))).Skip(start).Take(end - start).OrderByDescending(p => p.ModifiedDate).ToList();
                return list;
            }
        }

        public IList<Profile> GetProfiles(string text, string criteria)
        {
            var profiles = GetProfiles();
            if (String.IsNullOrWhiteSpace(text)) return profiles;
            var result = new List<Profile>();
            var str = text.ToLowerInvariant();
            foreach (var profile in profiles) {
                if(TextSearch(profile, str, criteria))    
                    result.Add(profile);
            }
            return result;
        }

        private static bool TextSearch(Profile profile, string str, string criteria)
        {
            if (criteria == "Text")
                return (profile.User.UserName.ToLowerInvariant().IndexOf(str, StringComparison.Ordinal) >= 0) ||
                       (profile.User.Email.ToLowerInvariant().IndexOf(str, StringComparison.Ordinal) >= 0) ||
                       (profile.Name.ToLowerInvariant().IndexOf(str, StringComparison.Ordinal) >= 0) ||
                       (profile.Guid.ToString().ToLowerInvariant().IndexOf(str, StringComparison.Ordinal) >= 0);
            if(criteria == "Id")
                return profile.Id == int.Parse(str);
            return false;
        }

        public void Clear<T>()
        {
            while (CanDelete25<T>())
            {
            }
        }

        private bool CanDelete25<T>()
        {
            List<T> list;
            using (var session = _docStore.OpenSession()) {
                list = session.Query<T>().Take(25).ToList();
                foreach (var i in list) {
                    session.Delete(i);
                }
                session.SaveChanges();
            }
            return list.Count > 0;
        }

        public void DeleteAll()
        {
            Clear<Profile>();
            Clear<LastUpdateObject>();
        }

        public void Delete<T>(Guid guid) where T:BaseGuidModel
        {
            using (var session = _docStore.OpenSession())
            {
                var item = session.Query<T>().Where(p=> p.Guid == guid).FirstOrDefault();
                if(item != null)
                {
                    session.Delete(item);
                }
                session.SaveChanges();
            }
        }
    }
}
