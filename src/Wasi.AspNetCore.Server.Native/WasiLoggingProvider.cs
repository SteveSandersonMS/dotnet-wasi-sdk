// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Wasi.AspNetCore.Server.Native;

internal class WasiLoggingProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new MinimalLogger(categoryName);
    }

    public void Dispose()
    {
    }

    private class MinimalLogger : ILogger
    {
        private string categoryName;

        public MinimalLogger(string categoryName)
        {
            this.categoryName = categoryName;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => new MyScope();

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel > LogLevel.Debug;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Console.WriteLine($"{ShortName(logLevel)}: {categoryName}");
            Console.Write("      ");
            Console.WriteLine(formatter(state, exception));

            if (exception is not null)
            {
                Console.Error.WriteLine(exception);
                Console.Error.WriteLine();
            }
        }

        private static string ShortName(LogLevel logLevel) => logLevel switch
        {
            LogLevel.Trace => "trce",
            LogLevel.Debug => "dbug",
            LogLevel.Information => "info",
            LogLevel.Warning => "warn",
            LogLevel.Error => "erro",
            LogLevel.Critical => "crit",
            LogLevel l => l.ToString(),
        };

        private class MyScope : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}