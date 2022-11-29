// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace MQTTnetServer.Configuration;

public sealed class RetainedApplicationMessagesModel
{
    public bool Persist { get; set; } = false;

    public int WriteInterval { get; set; } = 10;

    public string? Path { get; set; }
}