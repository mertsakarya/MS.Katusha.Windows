using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MS.Katusha.Configuration;
using MS.Katusha.FileSystems;

namespace MS.Katusha.Windows
{
    public class S3FS
    {
        private readonly S3FileSystem _fs;

        public S3FS(string name) { Name = name;
            _fs = new S3FileSystem(name);
        }
        public string Name { get; private set; }
        public S3FileSystem FileSystem { get { return _fs; } }
        public override string ToString() { return Name; }
    }
}
