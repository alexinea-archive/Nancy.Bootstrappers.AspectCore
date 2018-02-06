using System;
using System.Collections.Generic;
using System.Linq;
using Nancy.Bootstrapper;
using Nancy.Configuration;
using Nancy.Conventions;

namespace Nancy.Bootstrappers.AspectCore.Core {
    public abstract partial class TwoStepBootstrapperBase<TService, TResolver> : INancyBootstrapper, INancyModuleCatalog, IDisposable
        where TService : class where TResolver : class {
        private bool _initialised;
        private bool _disposing;

        private IAssemblyCatalog _assemblyCatalog;
        private ITypeCatalog _typeCatalog;
        private IRootPathProvider _rootPathProvider;
        private NancyConventions _conventions;
        private NancyInternalConfiguration _internalConfiguration;
        private Func<ITypeCatalog, NancyInternalConfiguration> _internalConfigurationFactory;

        protected TService ApplicationServices { get; private set; }

        protected TwoStepBootstrapperBase() {
            ApplicationPipelines = new Pipelines();
        }

        /// <summary>
        /// Initialise the bootstrapper. Must be called prior to GetEngine.
        /// </summary>
        public void Initialise() {
            var configuration = GetInitializedInternalConfiguration() ?? throw new InvalidOperationException("Configuration cannot be null");
            if (!configuration.IsValid) throw new InvalidOperationException("Configuration is invalid");
            ApplicationServices = CreateServiceCollection();

            RegisterBootstrapperTypes(ApplicationServices);
            ConfigureApplicationContainer(ApplicationServices);

            ConfigureConventions(Conventions);
            var validationRet = Conventions.Validate();
            if (!validationRet.Item1) throw new InvalidOperationException(string.Format("Conventions are invalid:\n\n{0}", validationRet.Item2));

            RegisterTypes(ApplicationServices, configuration.GetTypeRegistrations());
            RegisterCollectionTypes(ApplicationServices, configuration.GetCollectionTypeRegistrations().Concat(CollectionTypeRegistrations()));
            RegisterInstances(ApplicationServices, Conventions.GetInstanceRegistrations().Concat(InstancesTypeRegistrations()));
            RegisterRegistrationTasks(ApplicationServices);
            RegisterNancyEnvironment(ApplicationServices);
            RegisterModules(ApplicationServices, Modules);

            ApplicationResolver = BuildServices(ApplicationServices);

            foreach (var appStartupTask in GetApplicationStartupTasks(ApplicationResolver).ToList()) {
                appStartupTask.Initialize(ApplicationPipelines);
            }

            ApplicationStartup(ApplicationResolver, ApplicationPipelines);

            RequestStartupTaskTypeCache = RequestStartupTasks.ToArray();

            FavIconInitialize(ApplicationPipelines);

           // GetDiagnostics(ApplicationResolver).Initialize(ApplicationPipelines);

            _initialised = true;
        }

        protected virtual IAssemblyCatalog AssemblyCatalog {
#if !NETSTANDARD2_0
            get => _assemblyCatalog ?? (_assemblyCatalog = new AppDomainAssemblyCatalog());
#else
            get => _assemblyCatalog ?? (_assemblyCatalog = new DependencyContextAssemblyCatalog());
#endif
        }

        protected virtual ITypeCatalog TypeCatalog => _typeCatalog ?? (_typeCatalog = new DefaultTypeCatalog(AssemblyCatalog));

        protected virtual Func<ITypeCatalog, NancyInternalConfiguration> InternalConfiguration =>
            _internalConfigurationFactory ?? (_internalConfigurationFactory = NancyInternalConfiguration.Default);

        private NancyInternalConfiguration GetInitializedInternalConfiguration() =>
            _internalConfiguration ?? (_internalConfiguration = InternalConfiguration.Invoke(TypeCatalog));

        protected abstract TService CreateServiceCollection();

        protected abstract void RegisterBootstrapperTypes(TService services);

        protected virtual void ConfigureApplicationContainer(TService services) { }

        protected virtual NancyConventions Conventions => _conventions ?? (_conventions = new NancyConventions(TypeCatalog));

        protected abstract void RegisterTypes(TService services, IEnumerable<TypeRegistration> typeRegistrations);

        protected abstract void RegisterCollectionTypes(TService services, IEnumerable<CollectionTypeRegistration> collectionTypeRegistrations);

        protected abstract void RegisterInstances(TService servoces, IEnumerable<InstanceRegistration> instanceRegistrations);

        protected virtual void ConfigureConventions(NancyConventions nancyConventions) { }

        protected abstract void RegisterRegistrationTasks(TService services);

        public virtual void Configure(INancyEnvironment environment) { }

        protected abstract void RegisterNancyEnvironment(TService services);

        protected abstract void RegisterModules(TService services, IEnumerable<ModuleRegistration> moduleRegistrationTypes);

        public void Dispose() {
            // Prevent StackOverflowException if ApplicationContainer.Dispose re-triggers this Dispose
            if (_disposing) {
                return;
            }

            // Only dispose if we're initialised, prevents possible issue with recursive disposing.
            if (!_initialised) {
                return;
            }

            _disposing = true;

            var container = ApplicationResolver as IDisposable;

            if (container != null) {
                try {
                    container.Dispose();
                }
                catch (ObjectDisposedException) { }
            }


            Dispose(true);
        }

        protected virtual void Dispose(bool disposing) { }

        /// <summary>
        /// Hides ToString from the overrides list
        /// </summary>
        /// <returns>String representation</returns>
        public sealed override string ToString() => base.ToString();

        /// <summary>
        /// Hides Equals from the overrides list
        /// </summary>
        /// <param name="obj">Object to compare</param>
        /// <returns>Boolean indicating equality</returns>
        public sealed override bool Equals(object obj) {
            return base.Equals(obj);
        }

        /// <summary>
        /// Hides GetHashCode from the overrides list
        /// </summary>
        /// <returns>Hash code integer</returns>
        public sealed override int GetHashCode() {
            return base.GetHashCode();
        }
    }
}