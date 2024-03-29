﻿using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace mqttHub.Scripting.DataSharing;

public class DataSharingService
{
    readonly ILogger<DataSharingService> _logger;
    readonly PythonScriptHostService _pythonScriptHostService;
    readonly Dictionary<string, object> _storage = new();

    public DataSharingService(PythonScriptHostService pythonScriptHostService, ILogger<DataSharingService> logger)
    {
        _pythonScriptHostService = pythonScriptHostService ?? throw new ArgumentNullException(nameof(pythonScriptHostService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Configure()
    {
        _pythonScriptHostService.RegisterProxyObject("write_shared_data", new Action<string, object>(Write));
        _pythonScriptHostService.RegisterProxyObject("read_shared_data", new Func<string, object, object>(Read));
    }

    public void Write(string key, object value)
    {
        lock (_storage)
        {
            _storage[key] = value;
            _logger.LogInformation("Shared data with key '{Key}' updated", key);
        }
    }

    public object Read(string key, object defaultValue)
    {
        lock (_storage)
        {
            if (!_storage.TryGetValue(key, out var value))
            {
                return defaultValue;
            }

            return value;
        }
    }
}