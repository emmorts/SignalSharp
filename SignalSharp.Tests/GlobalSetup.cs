using Extensions.Logging.NUnit;
using Microsoft.Extensions.Logging;
using SignalSharp.Logging;

namespace SignalSharp.Tests;

[SetUpFixture]
public class GlobalSetup
{
    [OneTimeSetUp]
    public void GlobalTestSetup() {
        GlobalLogger.Initialize();
    }
}

internal static class GlobalLogger {
    static GlobalLogger() {
        var factory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddFilter("SignalSharp", LogLevel.Trace)
                .AddProvider(new NUnitLoggerProvider());
        });
        LoggerProvider.Configure(factory);
    }

    public static void Initialize() {}
}