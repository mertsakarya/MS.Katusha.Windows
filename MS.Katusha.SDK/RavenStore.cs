using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MS.Katusha.Domain.Entities;
using MS.Katusha.Domain.Service;
using Raven.Client;
using Raven.Client.Embedded;

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

        public IList<Profile> GetProfiles(string text)
        {
            var profiles = GetProfiles();
            if (String.IsNullOrWhiteSpace(text)) return profiles;
            var result = new List<Profile>();
            var str = text; //.ToLowerInvariant();
            foreach (var profile in profiles) {
                if(
                    (profile.User.UserName.ToLowerInvariant().IndexOf(str, System.StringComparison.Ordinal) > 0) ||
                    (profile.User.Email.ToLowerInvariant().IndexOf(str, System.StringComparison.Ordinal) > 0) ||
                    (profile.Name.ToLowerInvariant().IndexOf(str, System.StringComparison.Ordinal) > 0) ||
                    (profile.Id.ToString(CultureInfo.InvariantCulture).ToLowerInvariant().IndexOf(str, System.StringComparison.Ordinal) > 0) ||
                    (profile.Guid.ToString().ToLowerInvariant().IndexOf(str, System.StringComparison.Ordinal) > 0)
                    )    
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
    }
}
