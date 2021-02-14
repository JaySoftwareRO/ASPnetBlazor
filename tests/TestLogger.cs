using Microsoft.Extensions.Logging;

namespace tests
{
    class TestLogger
    {
        public static ILogger NewLogger(string name)
        {
            return LoggerFactory.Create(builder => {
                builder.AddFilter("Microsoft", LogLevel.Warning)
                       .AddFilter("System", LogLevel.Warning)
                       .AddFilter("lib", LogLevel.Debug)
                       .AddConsole();
            }).CreateLogger(name);
        }
    }
}
