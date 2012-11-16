using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using MS.Katusha.SDK.Raven;

namespace MS.Katusha.Windows.DependencyManager
{
    public class DependencyHelper
    {
        public static readonly string[] Servers = new[] { "http://www.mskatusha.com/", "https://mskatusha.apphb.com/", "https://mskatushaeu.apphb.com/", "http://localhost:10595/", "http://localhost/" };
        public static readonly string[] Buckets = new[] { "s.mskatusha.com", "MS.Katusha", "MS.Katusha.EU", "MS.Katusha.Test" };

        public static void Create()
        {
            var builder = new ContainerBuilder();
            builder.RegisterGeneric(typeof(RavenRepository<>)).As(typeof(RavenRepository<>)).SingleInstance();

        }
    }
}
