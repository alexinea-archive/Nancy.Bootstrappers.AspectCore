using Nancy.Bootstrappers.AspectCore.Tests.Services;

namespace Nancy.Bootstrappers.AspectCore.Tests {
    using Nancy;

    public class HomeModule : NancyModule {
        public HomeModule(IService1 service1) {
            Get("/", args => get(service1));
        }

        private dynamic get(IService1 service1) {
            service1.Display();
            return "Hello from Nancy running on CoreCLR";
        }
    }
}