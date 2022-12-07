using System.Collections.Generic;

// ReSharper disable CollectionNeverUpdated.Global

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace mqttHub.Configuration;

public sealed class ScriptingSettingsModel
{
    public string? ScriptsPath { get; set; }

    public List<string>? IncludePaths { get; set; }
}