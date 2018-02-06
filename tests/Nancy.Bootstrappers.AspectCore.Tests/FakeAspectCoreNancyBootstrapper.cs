using System;
using System.Collections.Generic;
using AspectCore.Injector;
using Nancy.Bootstrapper;
using Nancy.Bootstrappers.AspectCore;
using Nancy.Bootstrappers.AspectCore.Tests.Services;

namespace Nancy.Bootstrappers.AspectCore.Tests {
    public class FakeAspectCoreNancyBootstrapper : AspectCoreNancyBoostrapperBase {
        private readonly Func<ITypeCatalog, NancyInternalConfiguration> configuration;

        public FakeAspectCoreNancyBootstrapper()
        {
            this.configuration = NancyInternalConfiguration.Default;
        }

        public FakeAspectCoreNancyBootstrapper(Func<ITypeCatalog, NancyInternalConfiguration> configuration) {
            this.configuration = configuration;
        }

        protected override Func<ITypeCatalog, NancyInternalConfiguration> InternalConfiguration => configuration ?? base.InternalConfiguration;

        protected override void RegisterRequestContainerModules(IServiceResolver resolver, IEnumerable<ModuleRegistration> moduleRegistrationTypes) { }

        protected override void ConfigureApplicationContainer(IServiceContainer existingContainer) {
            base.ConfigureApplicationContainer(existingContainer);
            existingContainer.AddType<IService1, Service1>();
        }
    }
}