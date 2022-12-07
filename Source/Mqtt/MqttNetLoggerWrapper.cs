using System;
using Microsoft.Extensions.Logging;
using MQTTnet.Diagnostics;
using MQTTnet.Server;

namespace mqttHub.Mqtt;

public sealed class MqttNetLoggerWrapper : IMqttNetLogger
{
    readonly ILogger _logger;

    // ReSharper disable once ContextualLoggerProblem
    public MqttNetLoggerWrapper(ILogger<MqttServer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Publish(MqttNetLogLevel level, string source, string message, object[] parameters, Exception exception)
    {
        var convertedLogLevel = ConvertLogLevel(level);
        _logger.Log(convertedLogLevel, exception, message, parameters);
    }

    public bool IsEnabled => true;

    static LogLevel ConvertLogLevel(MqttNetLogLevel logLevel)
    {
        switch (logLevel)
        {
            case MqttNetLogLevel.Error:
                return LogLevel.Error;
            case MqttNetLogLevel.Warning:
                return LogLevel.Warning;
            case MqttNetLogLevel.Info:
                return LogLevel.Information;
            case MqttNetLogLevel.Verbose:
                return LogLevel.Trace;
        }

        return LogLevel.Debug;
    }
}