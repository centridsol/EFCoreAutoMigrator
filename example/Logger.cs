using System;
using Microsoft.Extensions.Logging;

namespace EFCoreAutoMigratorExample{
    class Logger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if ( (int)logLevel > 1)
            {
                Console.WriteLine($" {formatter(state, exception)}");
            }
        }
    }
}