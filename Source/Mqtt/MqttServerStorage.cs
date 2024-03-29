﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using mqttHub.Configuration;
using MQTTnet;
using Newtonsoft.Json;

namespace mqttHub.Mqtt;

public sealed class MqttServerStorage
{
    readonly ILogger<MqttServerStorage> _logger;
    readonly List<MqttApplicationMessage> _messages = new();

    readonly MqttSettingsModel _mqttSettings;
    bool _messagesHaveChanged;

    string _path = string.Empty;

    public MqttServerStorage(MqttSettingsModel mqttSettings, ILogger<MqttServerStorage> logger)
    {
        _mqttSettings = mqttSettings ?? throw new ArgumentNullException(nameof(mqttSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Configure()
    {
        if (_mqttSettings.RetainedApplicationMessages?.Persist != true || string.IsNullOrEmpty(_mqttSettings.RetainedApplicationMessages.Path))
        {
            _logger.LogInformation("Persisting of retained application messages is disabled");
            return;
        }

        _path = PathHelper.ExpandPath(_mqttSettings.RetainedApplicationMessages.Path);

        // The retained application messages are stored in a separate thread.
        // This is mandatory because writing them to a slow storage (like RaspberryPi SD card) 
        // will slow down the whole message processing speed.
        Task.Run(SaveRetainedMessagesInternalAsync, CancellationToken.None);
    }

    public Task SaveRetainedMessagesAsync(IList<MqttApplicationMessage> messages)
    {
        lock (_messages)
        {
            _messages.Clear();
            _messages.AddRange(messages);

            _messagesHaveChanged = true;
        }

        return Task.CompletedTask;
    }

    async Task SaveRetainedMessagesInternalAsync()
    {
        while (true)
        {
            try
            {
                var interval = _mqttSettings.RetainedApplicationMessages?.WriteInterval ?? 30;

                await Task.Delay(TimeSpan.FromSeconds(interval)).ConfigureAwait(false);

                List<MqttApplicationMessage> messages;
                lock (_messages)
                {
                    if (!_messagesHaveChanged)
                    {
                        continue;
                    }

                    messages = new List<MqttApplicationMessage>(_messages);
                    _messagesHaveChanged = false;
                }

                var json = JsonConvert.SerializeObject(messages);
                await File.WriteAllTextAsync(_path, json, Encoding.UTF8).ConfigureAwait(false);

                _logger.LogInformation("{MessagesCount} retained MQTT messages written", messages.Count);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while writing retained MQTT messages");
            }
        }
    }

    public async Task<List<MqttApplicationMessage>> LoadRetainedMessagesAsync()
    {
        if (_mqttSettings.RetainedApplicationMessages?.Persist != true)
        {
            return new List<MqttApplicationMessage>();
        }

        if (!File.Exists(_path))
        {
            return new List<MqttApplicationMessage>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_path).ConfigureAwait(false);
            var applicationMessages = JsonConvert.DeserializeObject<List<MqttApplicationMessage>>(json) ?? new List<MqttApplicationMessage>();

            _logger.LogInformation("{ApplicationMessagesCount} retained MQTT messages loaded", applicationMessages.Count);

            return applicationMessages;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Error while loading persisted retained application messages");
            return new List<MqttApplicationMessage>();
        }
    }
}