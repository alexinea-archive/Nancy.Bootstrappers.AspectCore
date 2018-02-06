using System;
using System.Collections.Generic;
using System.Linq;
using Nancy.Bootstrapper;
using Nancy.Configuration;
using Nancy.Diagnostics;

namespace Nancy.Bootstrappers.AspectCore.Core {
    public abstract partial class TwoStepBootstrapperBase<TService, TResolver> {
        protected Type[] RequestStartupTaskTypeCache { get; private set; }
        protected IPipelines ApplicationPipelines { get; private set; }
        protected TResolver ApplicationResolver { get; private set; }

        protected abstract TResolver BuildServices(TService services);

        protected abstract IEnumerable<IApplicationStartup> GetApplicationStartupTasks(TResolver resolver);

        protected virtual void ApplicationStartup(TResolver resolver, IPipelines pipelines) { }

        protected virtual byte[] FavIcon => FavIconApplicationStartup.FavIcon;

        protected virtual void FavIconInitialize(IPipelines pipelines) {
            if (FavIcon != null) {
                pipelines.BeforeRequest.AddItemToStartOfPipeline(ctx => {
                    if (ctx.Request == null || string.IsNullOrEmpty(ctx.Request.Path)) {
                        return null;
                    }

                    if (string.Equals(ctx.Request.Path, "/favicon.ico", StringComparison.OrdinalIgnoreCase)) {
                        var response = new Response {
                            ContentType = "image/vnd.microsoft.icon",
                            StatusCode = HttpStatusCode.OK,
                            Contents = s => s.Write(FavIcon, 0, FavIcon.Length),
                            Headers = {["Cache-Control"] = "public, max-age=604800, must-revalidate"}
                        };

                        return response;
                    }

                    return null;
                });
            }
        }

        protected abstract IDiagnostics GetDiagnostics(TResolver resolver);

        public abstract IEnumerable<INancyModule> GetAllModules(NancyContext context);

        public abstract INancyModule GetModule(Type moduleType, NancyContext context);

        public INancyEngine GetEngine() {
            if (!_initialised) throw new InvalidOperationException("Bootstrapper is not initialised. Call Initialise before GetEngine");
            var engine = SafeGetNancyEngineInstance();
            engine.RequestPipelinesFactory = InitializeRequestPipelines;
            return engine;
        }

        private INancyEngine SafeGetNancyEngineInstance() {
            try {
                return GetEngineInternal();
            }
            catch (Exception ex) {
                throw new InvalidOperationException(
                    "Something went wrong when trying to satisfy one of the dependencies during composition, make sure that you've registered all new dependencies in the container and inspect the innerexception for more details.",
                    ex);
            }
        }

        protected abstract INancyEngine GetEngineInternal();

        protected virtual IPipelines InitializeRequestPipelines(NancyContext context) {
            var requestPipelines = new Pipelines(ApplicationPipelines);

            if (RequestStartupTaskTypeCache.Any()) {
                var startupTasks = RegisterAndGetRequestStartupTasks(ApplicationResolver, RequestStartupTaskTypeCache);

                foreach (var requestStartup in startupTasks) {
                    requestStartup.Initialize(requestPipelines, context);
                }
            }

            RequestStartup(ApplicationResolver, requestPipelines, context);

            return requestPipelines;
        }

        public abstract INancyEnvironment GetEnvironment();

        protected abstract IEnumerable<IRequestStartup> RegisterAndGetRequestStartupTasks(TResolver resolver, Type[] requestStartupTypes);

        protected virtual void RequestStartup(TResolver resolver, IPipelines pipelines, NancyContext context) { }
    }
}