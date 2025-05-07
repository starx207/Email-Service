using Microsoft.Extensions.Logging;

namespace EmailService.Api.Tests.Harness;

public record TestLogEntry(LogLevel LogLevel, string Message, Exception? Exception = null);

public sealed class TestEmailLogger : ILogger {
    private readonly List<TestLogEntry> _logEntries = [];

    public IEnumerable<TestLogEntry> LogEntries => _logEntries;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Error || logLevel == LogLevel.Critical; // Test logger only logs errors.
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
        if (!IsEnabled(logLevel)) {
            return;
        }

        _logEntries.Add(new(logLevel, formatter(state, exception), exception));
    }
}

public sealed class TestEmailLoggerProvider : ILoggerProvider {
    private readonly TestEmailLogger _logger;
    public TestEmailLoggerProvider(TestEmailLogger logger) => _logger = logger;
    public ILogger CreateLogger(string categoryName) => _logger;
    public void Dispose() { }
}
