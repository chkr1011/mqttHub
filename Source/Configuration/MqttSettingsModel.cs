// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace mqttHub.Configuration;

public sealed class MqttSettingsModel
{
    public int CommunicationTimeout { get; set; } = 15;

    public int ConnectionBacklog { get; set; }

    public bool EnablePersistentSessions { get; set; } = false;

    public TcpEndPointModel? TcpEndPoint { get; set; } = new();

    public TcpEndPointModel? EncryptedTcpEndPoint { get; set; } = new();

    public WebSocketEndPointModel? WebSocketEndPoint { get; set; } = new();

    public int MaxPendingMessagesPerClient { get; set; } = 250;

    public RetainedApplicationMessagesModel? RetainedApplicationMessages { get; set; } = new();

    public bool EnableDebugLogging { get; set; } = false;
}