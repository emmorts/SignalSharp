using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SignalSharp.Logging;

/// <summary>
/// Provides centralized logging capabilities for the SignalSharp library.
/// </summary>
public static class LoggerProvider
{
    private static ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;

    /// <summary>
    /// Configures the library to use the specified logger factory.
    /// </summary>
    /// <param name="loggerFactory">The logger factory to use. If null, a null logger will be used.</param>
    public static void Configure(ILoggerFactory? loggerFactory)
    {
        _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
    }

    /// <summary>
    /// Creates a logger for the specified type.
    /// </summary>
    /// <typeparam name="T">The type to create a logger for.</typeparam>
    /// <returns>An instance of <see cref="ILogger"/> for the specified type.</returns>
    public static ILogger CreateLogger<T>()
    {
        return _loggerFactory.CreateLogger<T>();
    }

    /// <summary>
    /// Creates a logger with the specified category name.
    /// </summary>
    /// <param name="categoryName">The category name for the logger.</param>
    /// <returns>An instance of <see cref="ILogger"/> with the specified category name.</returns>
    public static ILogger CreateLogger(string categoryName)
    {
        return _loggerFactory.CreateLogger(categoryName);
    }
}
