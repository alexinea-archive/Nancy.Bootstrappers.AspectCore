namespace Nancy.Bootstrappers.AspectCore.Tests {
    using Microsoft.AspNetCore.Builder;
    using Nancy.Owin;

    public class Startup {
        public void Configure(IApplicationBuilder app) {
            app.UseOwin(x => x.UseNancy(pipeline=> pipeline.Bootstrapper = new FakeAspectCoreNancyBootstrapper()));
        }
    }
}