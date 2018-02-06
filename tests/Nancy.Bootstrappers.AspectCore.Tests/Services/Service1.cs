using System;

namespace Nancy.Bootstrappers.AspectCore.Tests.Services {
   
    public interface IService1 {
        [LoggerInterceptor1]
        void Display();
    }

    public class Service1 : IService1 {
        public void Display() {
            Console.WriteLine("Services1");
        }
    }
}