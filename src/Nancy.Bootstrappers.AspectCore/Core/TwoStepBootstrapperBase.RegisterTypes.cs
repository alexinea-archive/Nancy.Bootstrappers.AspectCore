using System;
using System.Collections.Generic;
using System.Linq;
using Nancy.Bootstrapper;
using Nancy.Cryptography;
using Nancy.ModelBinding;
using Nancy.Validation;
using Nancy.ViewEngines;

namespace Nancy.Bootstrappers.AspectCore.Core {
    public abstract partial class TwoStepBootstrapperBase<TService, TResolver> {
        private IEnumerable<CollectionTypeRegistration> CollectionTypeRegistrations() {
            return new[] {
                new CollectionTypeRegistration(typeof(IViewEngine), ViewEngines),
                new CollectionTypeRegistration(typeof(IModelBinder), ModelBinders),
                new CollectionTypeRegistration(typeof(ITypeConverter), TypeConverters),
                new CollectionTypeRegistration(typeof(IBodyDeserializer), BodyDeserializers),
                new CollectionTypeRegistration(typeof(IApplicationStartup), ApplicationStartupTasks),
                new CollectionTypeRegistration(typeof(IRegistrations), RegistrationTasks),
                new CollectionTypeRegistration(typeof(IModelValidatorFactory), ModelValidatorFactories)
            };
        }

        protected virtual IEnumerable<Type> ViewEngines => TypeCatalog.GetTypesAssignableTo<IViewEngine>();

        /// <summary>
        /// Gets the available custom model binders
        /// </summary>
        protected virtual IEnumerable<Type> ModelBinders => TypeCatalog.GetTypesAssignableTo<IModelBinder>();

        /// <summary>
        /// Gets the available custom type converters
        /// </summary>
        protected virtual IEnumerable<Type> TypeConverters => TypeCatalog.GetTypesAssignableTo<ITypeConverter>(TypeResolveStrategies.ExcludeNancy);

        /// <summary>
        /// Gets the available custom body deserializers
        /// </summary>
        protected virtual IEnumerable<Type> BodyDeserializers => TypeCatalog.GetTypesAssignableTo<IBodyDeserializer>(TypeResolveStrategies.ExcludeNancy);

        /// <summary>
        /// Gets all application startup tasks
        /// </summary>
        protected virtual IEnumerable<Type> ApplicationStartupTasks => TypeCatalog.GetTypesAssignableTo<IApplicationStartup>();

        /// <summary>
        /// Gets all request startup tasks
        /// </summary>
        protected virtual IEnumerable<Type> RequestStartupTasks => TypeCatalog.GetTypesAssignableTo<IRequestStartup>();

        /// <summary>
        /// Gets all registration tasks
        /// </summary>
        protected virtual IEnumerable<Type> RegistrationTasks => TypeCatalog.GetTypesAssignableTo<IRegistrations>();

        /// <summary>
        /// Gets the root path provider
        /// </summary>
        protected virtual IRootPathProvider RootPathProvider => _rootPathProvider ?? (_rootPathProvider = GetRootPathProvider());

        /// <summary>
        /// Gets the validator factories.
        /// </summary>
        protected virtual IEnumerable<Type> ModelValidatorFactories => TypeCatalog.GetTypesAssignableTo<IModelValidatorFactory>();

        private IRootPathProvider GetRootPathProvider() {
            var providerTypes = TypeCatalog.GetTypesAssignableTo<IRootPathProvider>(TypeResolveStrategies.ExcludeNancy).ToArray();
            if (providerTypes.Length > 1) throw new MultipleRootPathProvidersLocatedException(providerTypes);
            var providerType = providerTypes.SingleOrDefault() ?? typeof(DefaultRootPathProvider);
            return Activator.CreateInstance(providerType) as IRootPathProvider;
        }

        private IEnumerable<InstanceRegistration> InstancesTypeRegistrations() {
            return new[] {
                new InstanceRegistration(typeof(CryptographyConfiguration), CryptographyConfiguration),
                new InstanceRegistration(typeof(NancyInternalConfiguration), GetInitializedInternalConfiguration()),
                new InstanceRegistration(typeof(IRootPathProvider), RootPathProvider),
                new InstanceRegistration(typeof(IAssemblyCatalog), AssemblyCatalog),
                new InstanceRegistration(typeof(ITypeCatalog), TypeCatalog)
            };
        }

        protected virtual CryptographyConfiguration CryptographyConfiguration => CryptographyConfiguration.Default;
    }
}