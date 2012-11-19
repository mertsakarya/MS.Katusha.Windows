using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MS.Katusha.Domain.Entities;
using MS.Katusha.Domain.Entities.BaseEntities;
using Raven.Abstractions.Indexing;
using Raven.Client;
using Raven.Client.Embedded;
using Raven.Client.Indexes;

namespace MS.Katusha.SDK.Raven
{
    public class MaxDateIndex<T> : AbstractIndexCreationTask<T, DateTime> where T : BaseGuidModel
    {
        public MaxDateIndex()
        {
            Map = docs => from doc in docs
                          select doc.ModifiedDate;
            Stores.Add(x => x, FieldStorage.Yes);
        }
    }

    public class MaxDateTimeResult
    {
        public DateTime MaximumDate { get; set; }
    }

    public static class DocumentStoreManager
    {
        private static readonly Dictionary<string, IDocumentStore> RavenStores = new Dictionary<string, IDocumentStore>();
        public static string InstanceName;
        private const string DataFolderName = "\\Data";
        public static IDocumentStore GetInstance(string key)
        {
            InstanceName = key;
            var folder = key + DataFolderName;
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            if (RavenStores.ContainsKey(folder))
                return RavenStores[folder];
            var store = new EmbeddableDocumentStore
            {
                DataDirectory = folder,
                UseEmbeddedHttpServer = true
            };
            store.Initialize();
            RavenStores.Add(folder, store);
            return store;
        }
   
    }
}
