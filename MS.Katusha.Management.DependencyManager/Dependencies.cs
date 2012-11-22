using Autofac;
using MS.Katusha.Repositories.RavenDB.Base;
using MS.Katusha.SDK.II.Raven;
using MS.Katusha.SDK.Raven;
using Raven.Client;

namespace MS.Katusha.Management.DependencyManager
{
    public static class Dependencies
    {
        private static IContainer _container;

        private static string _name;

        private static void BuildContainer()
        {
            var builder = new ContainerBuilder();
            Build(builder);
            _container = builder.Build();
        }

        public static IContainer Container {
            get { 
                if (_container == null) BuildContainer();
                return _container;
            }
        }

        public static string Name {
            get { return _name; }
            set { 
                _name = value;
                _container = null;
            }
        }

        private static void Build(ContainerBuilder builder)
        {
            builder.RegisterType<MSKatushaRavenStore>().As<IDocumentStore>().WithParameter(new NamedParameter("name", _name)).SingleInstance();
            builder.RegisterGeneric(typeof (BaseGuidRepositoryRavenDB<>)).SingleInstance();
            builder.RegisterGeneric(typeof (RavenStoreListManager<>)).SingleInstance();
            
            //builder.RegisterType<UserService>().As<IUserService>().InstancePerHttpRequest();
            //builder.RegisterType<CountriesToVisitRepositoryDB>()
            //       .As<ICountriesToVisitRepositoryDB>()
            //       .InstancePerHttpRequest();
            //builder.RegisterGeneric(typeof (GridService<>)).As(typeof (IGridService<>)).InstancePerHttpRequest();
            //builder.RegisterGeneric(typeof (RepositoryDB<>)).As(typeof (IRepository<>)).InstancePerHttpRequest();

        }

    }

}
