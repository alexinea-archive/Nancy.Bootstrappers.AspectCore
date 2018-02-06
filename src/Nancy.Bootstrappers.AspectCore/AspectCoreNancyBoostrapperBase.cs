using System;
using System.Collections.Generic;
using AspectCore.Injector;
using Nancy.Bootstrapper;
using Nancy.Bootstrappers.AspectCore.Core;
using Nancy.Configuration;
using Nancy.Diagnostics;
using NancyLifetime = Nancy.Bootstrapper.Lifetime;
using AspectCoreLifetime = AspectCore.Injector.Lifetime;

namespace Nancy.Bootstrappers.AspectCore {
    public abstract class AspectCoreNancyBoostrapperBase : TwoStepBootstrapperBase<IServiceContainer, IServiceResolver> {

        private readonly string _contextKey = $"{typeof(IServiceContainer).FullName}BootstrapperChildContainer";

        protected virtual string ContextKey => _contextKey;

        private IEnumerable<ModuleRegistration> _moduleRegistrationTypeCache;

        protected override IServiceContainer CreateServiceCollection() {
            return new ServiceContainer();
        }

        protected override void RegisterBootstrapperTypes(IServiceContainer services) {
            services.Add(new InstanceServiceDefinition(typeof(INancyModuleCatalog), this));
        }

        protected override void RegisterTypes(IServiceContainer services, IEnumerable<TypeRegistration> typeRegistrations) {
            foreach (var typeRegistration in typeRegistrations) {
                switch (typeRegistration.Lifetime) {
                    case NancyLifetime.Transient: {
                        RegisterAsTransient(services, typeRegistration.RegistrationType, typeRegistration.ImplementationType);
                        break;
                    }

                    case NancyLifetime.Singleton: {
                        RegisterAsSingleton(services, typeRegistration.RegistrationType, typeRegistration.ImplementationType);
                        break;
                    }

                    case NancyLifetime.PerRequest: {
                        throw new InvalidOperationException("Unable to directly register a per request lifetime.");
                    }

                    default: {
                        throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        protected override void RegisterCollectionTypes(IServiceContainer services, IEnumerable<CollectionTypeRegistration> collectionTypeRegistrations) {
            foreach (var typeRegistration in collectionTypeRegistrations) {
                foreach (var implementationType in typeRegistration.ImplementationTypes) {
                    switch (typeRegistration.Lifetime) {
                        case NancyLifetime.Transient: {
                            RegisterAsTransient(services, typeRegistration.RegistrationType, implementationType);
                            break;
                        }

                        case NancyLifetime.Singleton: {
                            RegisterAsSingleton(services, typeRegistration.RegistrationType, implementationType);
                            return;
                        }

                        case NancyLifetime.PerRequest: {
                            throw new InvalidOperationException("Unable to directly register a per request lifetime.");
                        }

                        default: {
                            throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }
        }

        protected override void RegisterInstances(IServiceContainer servoces, IEnumerable<InstanceRegistration> instanceRegistrations) {
            foreach (var instanceRegistration in instanceRegistrations) {
                servoces.AddInstance(instanceRegistration.RegistrationType, instanceRegistration.Implementation);
            }
        }

        protected override void RegisterRegistrationTasks(IServiceContainer services) {
            services.AddDelegate<IRegistrationsHack>(provider => {
                var tasks = provider.ResolveMany<IRegistrations>();
                foreach (var task in tasks) {
                    var typeRegs = task.TypeRegistrations;
                    if (typeRegs != null) RegisterTypes(services, typeRegs);
                    var collRegs = task.CollectionTypeRegistrations;
                    if (collRegs != null) RegisterCollectionTypes(ApplicationServices, collRegs);
                    var instRegs = task.InstanceRegistrations;
                    if (instRegs != null) RegisterInstances(ApplicationServices, instRegs);
                }

                return default(IRegistrationsHack);
            }, AspectCoreLifetime.Singleton);
        }

        protected override void RegisterNancyEnvironment(IServiceContainer services) {
            services.AddDelegate<INancyEnvironment>(provider => provider.Resolve<INancyEnvironmentConfigurator>().ConfigureEnvironment(Configure));
        }

        private bool _modulesRegistered;

        protected override void RegisterModules(IServiceContainer services, IEnumerable<ModuleRegistration> moduleRegistrationTypes) {
            if (_modulesRegistered) return;
            _moduleRegistrationTypeCache = moduleRegistrationTypes;
            foreach (var moduleRegistrationType in moduleRegistrationTypes) {
                RegisterAsScoped(services, typeof(INancyModule), moduleRegistrationType.ModuleType);
            }

            _modulesRegistered = true;
        }

        protected override IServiceResolver BuildServices(IServiceContainer services) {
            ConfigureService(services);
            return services.Build();
        }

        protected override IEnumerable<IApplicationStartup> GetApplicationStartupTasks(IServiceResolver resolver) {
            return resolver.ResolveMany<IApplicationStartup>();
        }

        protected override IDiagnostics GetDiagnostics(IServiceResolver resolver) {
            return resolver.Resolve<IDiagnostics>();
        }

        protected abstract void RegisterRequestContainerModules(IServiceResolver resolver, IEnumerable<ModuleRegistration> moduleRegistrationTypes);

        public override IEnumerable<INancyModule> GetAllModules(NancyContext context) {
            var scopedResolver = GetConfiguredRequestContainer(context);

            RegisterRequestContainerModules(scopedResolver, _moduleRegistrationTypeCache);

            return scopedResolver.ResolveMany<INancyModule>();
        }

        public override INancyModule GetModule(Type moduleType, NancyContext context) {
            var scopedResolver = GetConfiguredRequestContainer(context);
            return (INancyModule) scopedResolver.Resolve(moduleType);
        }

        protected override INancyEngine GetEngineInternal() {
            return ApplicationResolver.Resolve<INancyEngine>();
        }

        public override INancyEnvironment GetEnvironment() {
            return ApplicationResolver.Resolve<INancyEnvironment>();
        }

        protected override IEnumerable<IRequestStartup> RegisterAndGetRequestStartupTasks(IServiceResolver resolver, Type[] requestStartupTypes) {
            throw new NotImplementedException();
        }

        protected IServiceResolver GetConfiguredRequestContainer(NancyContext context) {
            context.Items.TryGetValue(ContextKey, out var contextObject);
            var requestContainer = contextObject as IServiceResolver;

            if (requestContainer == null) {
                requestContainer = CreateRequestContainer(context);
                context.Items[ContextKey] = requestContainer;
            }

            return requestContainer;
        }

        private IServiceResolver CreateRequestContainer(NancyContext context) {
            return ApplicationResolver.CreateScope();
        }

        protected virtual void ConfigureService(IServiceContainer services) { }

        private static void RegisterAsTransient(IServiceContainer container, Type serviceType, Type implType) {
            container.Add(new TypeServiceDefinition(serviceType, implType, AspectCoreLifetime.Transient));
        }

        private static void RegisterAsSingleton(IServiceContainer container, Type serviceType, Type implType) {
            container.Add(new TypeServiceDefinition(serviceType, implType, AspectCoreLifetime.Singleton));
        }

        private static void RegisterAsScoped(IServiceContainer container, Type serviceType, Type implType) {
            container.Add(new TypeServiceDefinition(serviceType, implType, AspectCoreLifetime.Scoped));
        }
    }
}