using System.Collections.Generic;
using System.IO;
using Raven.Client;
using Raven.Client.Embedded;

namespace MS.Katusha.SDK.Raven
{
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
