using Microsoft.Extensions.Configuration;

namespace tests
{
    class Config
    {
        public static IConfiguration InitConfiguration()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.tests.json")
                .Build();
            return config;
        }
    }
}
