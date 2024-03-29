﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IronPython.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using mqttHub.Configuration;

namespace mqttHub.Scripting;

public class PythonScriptHostService
{
    readonly ILogger<PythonScriptHostService> _logger;
    readonly IDictionary<string, object> _proxyObjects = new ExpandoObject()!;
    readonly ScriptEngine _scriptEngine;
    readonly ScriptingSettingsModel _scriptingSettings;
    readonly List<PythonScriptInstance> _scriptInstances = new();
    readonly string _scriptsPath;

    public PythonScriptHostService(ScriptingSettingsModel scriptingSettings, PythonIOStream pythonIoStream, ILogger<PythonScriptHostService> logger)
    {
        _scriptingSettings = scriptingSettings ?? throw new ArgumentNullException(nameof(scriptingSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _scriptEngine = Python.CreateEngine();
        _scriptEngine.Runtime.IO.SetOutput(pythonIoStream, Encoding.UTF8);

        _scriptsPath = PathHelper.ExpandPath(scriptingSettings.ScriptsPath);
    }

    public void Configure()
    {
        AddSearchPaths(_scriptEngine);

        TryInitializeScriptsAsync().GetAwaiter().GetResult();
    }

    public void RegisterProxyObject(string name, object @object)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (@object == null)
        {
            throw new ArgumentNullException(nameof(@object));
        }

        _proxyObjects.Add(name, @object);
    }

    public void InvokeOptionalFunction(string name, object parameters)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        lock (_scriptInstances)
        {
            foreach (var pythonScriptInstance in _scriptInstances)
            {
                try
                {
                    pythonScriptInstance.InvokeOptionalFunction(name, parameters);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Error while invoking function '{Name}' at script '{PythonScriptInstanceUid}'", name, pythonScriptInstance.Uid);
                }
            }
        }
    }

    public List<string> GetScriptUids()
    {
        lock (_scriptInstances)
        {
            return _scriptInstances.Select(si => si.Uid).ToList();
        }
    }

    public Task<string> ReadScriptAsync(string uid, CancellationToken cancellationToken)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        string? path;

        lock (_scriptInstances)
        {
            path = _scriptInstances.FirstOrDefault(si => si.Uid == uid)?.Path;
        }

        if (path == null || !File.Exists(path))
        {
            return Task.FromResult(string.Empty);
        }

        return File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken);
    }

    public async Task WriteScriptAsync(string uid, string code, CancellationToken cancellationToken)
    {
        var path = Path.Combine(_scriptsPath, uid + ".py");

        await File.WriteAllTextAsync(path, code, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        await TryInitializeScriptsAsync().ConfigureAwait(false);
    }

    public async Task DeleteScriptAsync(string uid)
    {
        var path = Path.Combine(_scriptsPath, uid + ".py");

        if (File.Exists(path))
        {
            File.Delete(path);
            await TryInitializeScriptsAsync().ConfigureAwait(false);
        }
    }

    public async Task TryInitializeScriptsAsync()
    {
        lock (_scriptInstances)
        {
            foreach (var scriptInstance in _scriptInstances)
            {
                try
                {
                    scriptInstance.InvokeOptionalFunction("destroy");
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Error while unloading script \'{ScriptInstanceUid}\'", scriptInstance.Uid);
                }
            }

            _scriptInstances.Clear();
        }

        foreach (var path in Directory.GetFiles(_scriptsPath, "*.py", SearchOption.AllDirectories).OrderBy(file => file))
        {
            await TryInitializeScriptAsync(path).ConfigureAwait(false);
        }
    }

    async Task TryInitializeScriptAsync(string path)
    {
        var uid = new FileInfo(path).Name.Replace(".py", string.Empty, StringComparison.OrdinalIgnoreCase);

        try
        {
            _logger.LogTrace("Initializing Python script \'{Uid}\'...", uid);
            var code = await File.ReadAllTextAsync(path).ConfigureAwait(false);

            var scriptInstance = CreateScriptInstance(uid, path, code);
            scriptInstance.InvokeOptionalFunction("initialize");

            lock (_scriptInstances)
            {
                _scriptInstances.Add(scriptInstance);
            }

            _logger.LogInformation("Initialized script \'{Uid}\'", uid);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error while initializing script \'{Uid}\'", uid);
        }
    }

    PythonScriptInstance CreateScriptInstance(string uid, string path, string code)
    {
        var scriptScope = _scriptEngine.CreateScope();

        var source = scriptScope.Engine.CreateScriptSourceFromString(code, SourceCodeKind.File);
        var compiledCode = source.Compile();

        scriptScope.SetVariable("mqtt_net_server", _proxyObjects);
        compiledCode.Execute(scriptScope);

        return new PythonScriptInstance(uid, path, scriptScope);
    }

    void AddSearchPaths(ScriptEngine scriptEngine)
    {
        if (_scriptingSettings.IncludePaths?.Any() != true)
        {
            return;
        }

        var searchPaths = scriptEngine.GetSearchPaths();

        foreach (var path in _scriptingSettings.IncludePaths)
        {
            var effectivePath = PathHelper.ExpandPath(path);

            if (Directory.Exists(effectivePath))
            {
                searchPaths.Add(effectivePath);
                _logger.LogInformation("Added Python lib path: {EffectivePath}", effectivePath);
            }
        }

        scriptEngine.SetSearchPaths(searchPaths);
    }
}