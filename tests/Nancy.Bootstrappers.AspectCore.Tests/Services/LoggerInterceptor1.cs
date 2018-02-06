using System;
using System.Diagnostics;
using System.Threading.Tasks;
using AspectCore.DynamicProxy;

namespace Nancy.Bootstrappers.AspectCore.Tests.Services {
    public class LoggerInterceptor1 : AbstractInterceptorAttribute {

        public override async Task Invoke(AspectContext context, AspectDelegate next) {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            await context.Invoke(next);
            sw.Stop();
            Console.WriteLine($"Used {sw.ElapsedMilliseconds} ms.");
        }
    }
}