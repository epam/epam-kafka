
using Microsoft.Extensions.Hosting;

namespace Epam.Kafka.Sample.Net462
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Host.CreateDefaultBuilder().ConfigureServices(services =>
            {

            }).Build().Run();
        }
    }
}
