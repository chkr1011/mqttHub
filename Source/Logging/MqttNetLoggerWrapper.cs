using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using MQTTnet.Diagnostics.Logger;

namespace MQTTnet.Server.Logging;

public sealed class MqttNetLoggerWrapper : IMqttNetLogger
{
    private readonly ILogger<MqttServer> _logger;

    public MqttNetLoggerWrapper(ILogger<MqttServer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Publish(MqttNetLogLevel level, string source, string message, object[] parameters, Exception exception)
    {
        var convertedLogLevel = ConvertLogLevel(level);
        _logger.Log(convertedLogLevel, exception, message, parameters);

        var logMessagePublishedEvent = LogMessagePublished;
        if (logMessagePublishedEvent != null)
        {
            var logMessage = new MqttNetLogMessage
            {
                Timestamp = DateTime.UtcNow,
                ThreadId = Thread.CurrentThread.ManagedThreadId,
                Source = source,
                Level = level,
                Message = message,
                Exception = exception
            };

            logMessagePublishedEvent.Invoke(this, new MqttNetLogMessagePublishedEventArgs(logMessage));
        }
    }

    public bool IsEnabled => true;

    public event EventHandler<MqttNetLogMessagePublishedEventArgs> LogMessagePublished;

    public void Publish(MqttNetLogLevel logLevel, string message, object[] parameters, Exception exception)
    {
        Publish(logLevel, null, message, parameters, exception);
    }

    private static LogLevel ConvertLogLevel(MqttNetLogLevel logLevel)
    {
        switch (logLevel)
        {
            case MqttNetLogLevel.Error: return LogLevel.Error;
            case MqttNetLogLevel.Warning: return LogLevel.Warning;
            case MqttNetLogLevel.Info: return LogLevel.Information;
            case MqttNetLogLevel.Verbose: return LogLevel.Trace;
        }

        return LogLevel.Debug;
    }
}