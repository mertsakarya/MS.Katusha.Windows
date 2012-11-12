using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MS.Katusha.Domain.Entities;
using MS.Katusha.Domain.Service;
using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Client.Embedded;
using Raven.Client.Indexes;
using Raven.Database;

namespace MS.Katusha.SDK
{
    public class LastUpdateObject
    {
        public string Id { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    public class RavenStore
    {
        private EmbeddableDocumentStore _docStore;

        public RavenStore(string folderName) {
            _docStore = new EmbeddableDocumentStore {
                DataDirectory = folderName,
                UseEmbeddedHttpServer = true
            };
            _docStore.Initialize();
        }

        public DateTime LastUpdate(string name, DateTime? value = null)
        {
            if(value == null) {
                using(var session = _docStore.OpenSession()) {
                    var lastUpdateObject = session.Load<LastUpdateObject>(name);
                    if(lastUpdateObject == null) return new DateTime(1900, 1, 1);
                    return lastUpdateObject.LastUpdate;
                }
            } else {
                using (var session = _docStore.OpenSession()) {
                    var luo = new LastUpdateObject() {Id = name, LastUpdate = (DateTime) value};
                    session.Store(luo);
                    session.SaveChanges();
                    return luo.LastUpdate;
                }
            }
        }

        public void AddProfile(Profile profile)
        {
            using (var session = _docStore.OpenSession()) {
                session.Store(profile);
                session.SaveChanges();
            }
        }

        public IList<Profile> GetProfiles()
        {

            using (var session = _docStore.OpenSession()) {
                return session.Query<Profile>().Take(1000).ToList();
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
            //using (var session = _docStore.OpenSession()) {
            //    return session.Query<Profile>().Where(
            //        p=>
            //            p.User.UserName.IndexOf(str) > 0 ||
            //            p.User.Email.IndexOf(str) > 0 ||
            //            p.Name.IndexOf(str) > 0
            //        ).Take(1000).ToList();
            //}
        }

        private static bool TextSearch(Profile profile, string str, string criteria)
        {
            if (criteria == "Text")
                return (profile.User.UserName.ToLowerInvariant().IndexOf(str, System.StringComparison.Ordinal) >= 0) ||
                       (profile.User.Email.ToLowerInvariant().IndexOf(str, System.StringComparison.Ordinal) >= 0) ||
                       (profile.Name.ToLowerInvariant().IndexOf(str, System.StringComparison.Ordinal) >= 0) ||
                       (profile.Guid.ToString().ToLowerInvariant().IndexOf(str, System.StringComparison.Ordinal) >= 0);
            else if(criteria == "Id")
                return profile.Id == int.Parse(str);
            return false;
        }

        public static Dictionary<string, RavenStore> RavenStores = new Dictionary<string, RavenStore>();
        public static RavenStore GetInstance(string key)
        {
            if (RavenStores.ContainsKey(key))
                return RavenStores[key];
            var store = new RavenStore(key);
            RavenStores.Add(key, store);
            return store;
        }
        public void Clear<T>()
        {
            while (CanDelete25<T>()) ;
        }

        private bool CanDelete25<T>()
        {
            List<T> list = new List<T>();
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
    }
}
