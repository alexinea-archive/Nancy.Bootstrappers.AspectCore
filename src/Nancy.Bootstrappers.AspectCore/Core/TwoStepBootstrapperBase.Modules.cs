using System.Collections.Generic;
using System.Linq;
using Nancy.Bootstrapper;
using Nancy.Diagnostics;
using Nancy.Extensions;

namespace Nancy.Bootstrappers.AspectCore.Core {
    public abstract partial class TwoStepBootstrapperBase<TService, TResolver> {
        private ModuleRegistration[] _modules;

        protected virtual IEnumerable<ModuleRegistration> Modules =>
            _modules ?? (_modules = TypeCatalog
                .GetTypesAssignableTo<INancyModule>(TypeResolveStrategies.ExcludeNancy)
                .NotOfType<DiagnosticModule>()
                .Select(t => new ModuleRegistration(t))
                .ToArray());
    }
}