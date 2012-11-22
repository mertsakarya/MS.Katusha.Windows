using System.IO;
using Raven.Client;
using Raven.Client.Embedded;

namespace MS.Katusha.SDK.II.Raven
{
    public class MSKatushaRavenStore : EmbeddableDocumentStore, IDocumentStore
    {
        public string InstanceName { get; set; }

        private const string DataFolderName = "\\Data";
        public MSKatushaRavenStore(string name)
        {
            InstanceName = name;
            var folder = name + DataFolderName;
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            var store = new EmbeddableDocumentStore
            {
                DataDirectory = folder,
                UseEmbeddedHttpServer = true
            };
            store.Initialize();
        }
    }
}
