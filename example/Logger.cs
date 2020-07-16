using System;
using Microsoft.Extensions.Logging;

namespace EFCoreMigratorExample{
    class Logger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            throw new NotImplementedException();
        }
        public void LogError(string message){
             Console.WriteLine($"Error: {message}");
        }
        public void LogInformation(string message){
            Console.WriteLine($"Info: {message}");
        }
        public void LogTrace(string message){
            Console.WriteLine($"Trace: {message}");
        }
        public void LogWarning(string message){
            Console.WriteLine($"Warning: {message}");
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            return;
        }
    }
}