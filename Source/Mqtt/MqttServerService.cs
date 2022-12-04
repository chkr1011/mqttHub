using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using IronPython.Runtime;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Adapter;
using MQTTnet.AspNetCore;
using MQTTnet.Implementations;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnetServer.Configuration;
using MQTTnetServer.Scripting;

namespace MQTTnetServer.Mqtt;

public sealed class MqttServerService
{
    const string WrappedSessionItemsKey = "WRAPPED_ITEMS";

    readonly ILogger<MqttServerService> _logger;
    readonly CustomMqttFactory _mqttFactory;
    readonly MqttServerStorage _mqttServerStorage;
    readonly PythonScriptHostService _pythonScriptHostService;

    readonly MqttSettingsModel _settings;
    readonly MqttWebSocketServerAdapter _webSocketServerAdapter;

    MqttServer? _mqttServer;

    public MqttServerService(MqttSettingsModel mqttSettings, CustomMqttFactory mqttFactory, MqttServerStorage mqttServerStorage, PythonScriptHostService pythonScriptHostService, ILogger<MqttServerService> logger)
    {
        _settings = mqttSettings ?? throw new ArgumentNullException(nameof(mqttSettings));
        _mqttFactory = mqttFactory ?? throw new ArgumentNullException(nameof(mqttFactory));
        _mqttServerStorage = mqttServerStorage ?? throw new ArgumentNullException(nameof(mqttServerStorage));
        _pythonScriptHostService = pythonScriptHostService ?? throw new ArgumentNullException(nameof(pythonScriptHostService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _webSocketServerAdapter = new MqttWebSocketServerAdapter();
    }

    public void Configure()
    {
        _pythonScriptHostService.RegisterProxyObject("publish", new Action<PythonDictionary>(Publish));

        _mqttServerStorage.Configure();

        var options = CreateMqttServerOptions();

        var adapters = new List<IMqttServerAdapter>
        {
            new MqttTcpServerAdapter
            {
                TreatSocketOpeningErrorAsWarning = true // Opening other ports than for HTTP is not allows in Azure App Services.
            },
            _webSocketServerAdapter
        };

        _mqttServer = _mqttFactory.CreateMqttServer(options, adapters);

        _mqttServer.ClientConnectedAsync += OnClientConnected;
        _mqttServer.ClientDisconnectedAsync += OnClientDisconnected;

        _mqttServer.ClientSubscribedTopicAsync += OnClientSubscribed;
        _mqttServer.ClientUnsubscribedTopicAsync += OnClientUnsubscribed;

        _mqttServer.InterceptingSubscriptionAsync += InterceptSubscription;
        _mqttServer.InterceptingUnsubscriptionAsync += InterceptUnsubscription;

        _mqttServer.InterceptingPublishAsync += InterceptPublish;
        _mqttServer.ValidatingConnectionAsync += ValidateConnection;

        _mqttServer.LoadingRetainedMessageAsync += LoadRetainedMessages;
        _mqttServer.RetainedMessagesClearedAsync += ClearRetainedMessages;

        _mqttServer.StartAsync().GetAwaiter().GetResult();

        _logger.LogInformation("MQTT server started");
    }

    Task ClearRetainedMessages(EventArgs eventArgs)
    {
        _mqttServerStorage.SaveRetainedMessagesAsync(new List<MqttApplicationMessage>());
        return Task.CompletedTask;
    }

    async Task LoadRetainedMessages(LoadingRetainedMessagesEventArgs eventArgs)
    {
        var retainedMessages = await _mqttServerStorage.LoadRetainedMessagesAsync();
        if (retainedMessages.Any())
        {
            eventArgs.LoadedRetainedMessages = retainedMessages;
        }
    }

    Task ValidateConnection(ValidatingConnectionEventArgs eventArgs)
    {
        try
        {
            var sessionItems = new PythonDictionary();

            var pythonContext = new PythonDictionary
            {
                {
                    "endpoint", eventArgs.Endpoint
                },
                {
                    "is_secure_connection", eventArgs.IsSecureConnection
                },
                {
                    "client_id", eventArgs.ClientId
                },
                {
                    "username", eventArgs.UserName
                },
                {
                    "password", eventArgs.Password
                },
                {
                    "raw_password", new Bytes(eventArgs.RawPassword ?? Array.Empty<byte>())
                },
                {
                    "clean_session", eventArgs.CleanSession
                },
                {
                    "authentication_method", eventArgs.AuthenticationMethod
                },
                {
                    "authentication_data", new Bytes(eventArgs.AuthenticationData ?? Array.Empty<byte>())
                },
                {
                    "session_items", sessionItems
                },
                {
                    "result", PythonConvert.Pythonfy(eventArgs.ReasonCode)
                }
            };

            _pythonScriptHostService.InvokeOptionalFunction("on_validate_client_connection", pythonContext);

            eventArgs.ReasonCode = PythonConvert.ParseEnum<MqttConnectReasonCode>((string)pythonContext["result"]);

            eventArgs.SessionItems[WrappedSessionItemsKey] = sessionItems;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error while validating client connection");

            eventArgs.ReasonCode = MqttConnectReasonCode.UnspecifiedError;
        }

        return Task.CompletedTask;
    }

    Task OnClientUnsubscribed(ClientUnsubscribedTopicEventArgs eventArgs)
    {
        try
        {
            var pythonEventArgs = new PythonDictionary
            {
                {
                    "client_id", eventArgs.ClientId
                },
                {
                    "topic", eventArgs.TopicFilter
                }
            };

            _pythonScriptHostService.InvokeOptionalFunction("on_client_unsubscribed_topic", pythonEventArgs);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error while handling client unsubscribed topic event");
        }

        return Task.CompletedTask;
    }

    Task OnClientSubscribed(ClientSubscribedTopicEventArgs eventArgs)
    {
        try
        {
            var pythonEventArgs = new PythonDictionary
            {
                {
                    "client_id", eventArgs.ClientId
                },
                {
                    "topic", eventArgs.TopicFilter.Topic
                },
                {
                    "qos", (int)eventArgs.TopicFilter.QualityOfServiceLevel
                }
            };

            _pythonScriptHostService.InvokeOptionalFunction("on_client_subscribed_topic", pythonEventArgs);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error while handling client subscribed topic event");
        }

        return Task.CompletedTask;
    }

    Task InterceptUnsubscription(InterceptingUnsubscriptionEventArgs eventArgs)
    {
        try
        {
            var pythonEventArgs = new PythonDictionary
            {
                {
                    "client_id", eventArgs.ClientId
                },
                {
                    "topic", eventArgs.Topic
                }
            };

            _pythonScriptHostService.InvokeOptionalFunction("on_client_unsubscribed_topic", pythonEventArgs);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error while handling client unsubscribed topic event");
        }

        return Task.CompletedTask;
    }

    Task InterceptSubscription(InterceptingSubscriptionEventArgs eventArgs)
    {
        try
        {
            var sessionItems = eventArgs.SessionItems[WrappedSessionItemsKey] as PythonDictionary;

            var pythonContext = new PythonDictionary
            {
                {
                    "client_id", eventArgs.ClientId
                },
                {
                    "session_items", sessionItems
                },
                {
                    "topic", eventArgs.TopicFilter.Topic
                },
                {
                    "qos", (int)eventArgs.TopicFilter.QualityOfServiceLevel
                },
                {
                    "process_subscription", eventArgs.ProcessSubscription
                },
                {
                    "close_connection", eventArgs.CloseConnection
                }
            };

            _pythonScriptHostService.InvokeOptionalFunction("on_intercept_subscription", pythonContext);

            eventArgs.ProcessSubscription = (bool)pythonContext["process_subscription"];
            eventArgs.CloseConnection = (bool)pythonContext["close_connection"];
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error while intercepting subscription");
        }

        return Task.CompletedTask;
    }

    Task InterceptPublish(InterceptingPublishEventArgs eventArgs)
    {
        try
        {
            // This might be not set when a message was published by the server instead of a client.

            object? sessionItems = null;
            if (eventArgs.SessionItems.Contains(WrappedSessionItemsKey))
            {
                sessionItems = eventArgs.SessionItems[WrappedSessionItemsKey];
            }

            var pythonContext = new PythonDictionary
            {
                {
                    "client_id", eventArgs.ClientId
                },
                {
                    "session_items", sessionItems
                },
                {
                    "retain", eventArgs.ApplicationMessage.Retain
                },
                {
                    "process_publish", eventArgs.ProcessPublish
                },
                {
                    "close_connection", eventArgs.CloseConnection
                },
                {
                    "topic", eventArgs.ApplicationMessage.Topic
                },
                {
                    "qos", (int)eventArgs.ApplicationMessage.QualityOfServiceLevel
                }
            };

            _pythonScriptHostService.InvokeOptionalFunction("on_intercept_application_message", pythonContext);

            eventArgs.ProcessPublish = (bool)pythonContext.get("process_publish", eventArgs.ProcessPublish);
            eventArgs.CloseConnection = (bool)pythonContext.get("close_connection", eventArgs.CloseConnection);
            eventArgs.ApplicationMessage.Topic = (string)pythonContext.get("topic", eventArgs.ApplicationMessage.Topic);
            eventArgs.ApplicationMessage.QualityOfServiceLevel = (MqttQualityOfServiceLevel)(int)pythonContext.get("qos", (int)eventArgs.ApplicationMessage.QualityOfServiceLevel);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error while intercepting application message");
        }

        return Task.CompletedTask;
    }

    Task OnClientDisconnected(ClientDisconnectedEventArgs eventArgs)
    {
        try
        {
            var pythonEventArgs = new PythonDictionary
            {
                {
                    "client_id", eventArgs.ClientId
                },
                {
                    "type", PythonConvert.Pythonfy(eventArgs.DisconnectType)
                }
            };

            _pythonScriptHostService.InvokeOptionalFunction("on_client_disconnected", pythonEventArgs);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error while handling client disconnected event");
        }

        return Task.CompletedTask;
    }

    Task OnClientConnected(ClientConnectedEventArgs eventArgs)
    {
        try
        {
            var pythonEventArgs = new PythonDictionary
            {
                {
                    "client_id", eventArgs.ClientId
                }
            };

            _pythonScriptHostService.InvokeOptionalFunction("on_client_connected", pythonEventArgs);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error while handling client connected event");
        }

        return Task.CompletedTask;
    }

    public Task RunWebSocketConnectionAsync(WebSocket webSocket, HttpContext httpContext)
    {
        return _webSocketServerAdapter.RunWebSocketConnectionAsync(webSocket, httpContext);
    }

    public Task<IList<MqttClientStatus>> GetClientStatusAsync()
    {
        return _mqttServer!.GetClientsAsync();
    }

    public Task<IList<MqttSessionStatus>> GetSessionStatusAsync()
    {
        return _mqttServer!.GetSessionsAsync();
    }

    public Task ClearRetainedApplicationMessagesAsync()
    {
        return _mqttServer!.DeleteRetainedMessagesAsync();
    }

    public Task<IList<MqttApplicationMessage>> GetRetainedApplicationMessagesAsync()
    {
        return _mqttServer!.GetRetainedMessagesAsync();
    }

    public void Publish(MqttApplicationMessage applicationMessage)
    {
        if (applicationMessage == null)
        {
            throw new ArgumentNullException(nameof(applicationMessage));
        }

        _mqttServer?.InjectApplicationMessage(new InjectedMqttApplicationMessage(applicationMessage)
        {
            SenderClientId = "MQTTnet.Server"
        });
    }

    void Publish(PythonDictionary parameters)
    {
        try
        {
            var applicationMessageBuilder = new MqttApplicationMessageBuilder().WithTopic((string)parameters.get("topic", null)).WithRetainFlag((bool)parameters.get("retain", false)).WithQualityOfServiceLevel((MqttQualityOfServiceLevel)(int)parameters.get("qos", 0));

            var payload = parameters.get("payload", null);
            byte[] binaryPayload;

            if (payload == null)
            {
                binaryPayload = Array.Empty<byte>();
            }
            else if (payload is string stringPayload)
            {
                binaryPayload = Encoding.UTF8.GetBytes(stringPayload);
            }
            else if (payload is ByteArray byteArray)
            {
                binaryPayload = byteArray.ToArray();
            }
            else if (payload is IEnumerable<int> intArray)
            {
                binaryPayload = intArray.Select(Convert.ToByte).ToArray();
            }
            else
            {
                throw new NotSupportedException("Payload type not supported.");
            }

            applicationMessageBuilder = applicationMessageBuilder.WithPayload(binaryPayload);

            var applicationMessage = applicationMessageBuilder.Build();

            Publish(applicationMessage);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error while publishing application message from server");
        }
    }

    MqttServerOptions CreateMqttServerOptions()
    {
        var options = new MqttServerOptionsBuilder().WithMaxPendingMessagesPerClient(_settings.MaxPendingMessagesPerClient).WithDefaultCommunicationTimeout(TimeSpan.FromSeconds(_settings.CommunicationTimeout));

        // Configure unencrypted connections
        if (_settings.TcpEndPoint?.Enabled == true)
        {
            options.WithDefaultEndpoint();

            if (_settings.TcpEndPoint.TryReadIPv4(out var address4))
            {
                options.WithDefaultEndpointBoundIPAddress(address4);
            }

            if (_settings.TcpEndPoint.TryReadIPv6(out var address6))
            {
                options.WithDefaultEndpointBoundIPV6Address(address6);
            }

            if (_settings.TcpEndPoint.Port > 0)
            {
                options.WithDefaultEndpointPort(_settings.TcpEndPoint.Port);
            }
        }
        else
        {
            options.WithoutDefaultEndpoint();
        }

        // Configure encrypted connections
        if (_settings.EncryptedTcpEndPoint?.Enabled == true)
        {
#if NETCOREAPP3_1_OR_GREATER
                options
                    .WithEncryptedEndpoint()
                    .WithEncryptionSslProtocol(SslProtocols.Tls13);
#else
            options.WithEncryptedEndpoint().WithEncryptionSslProtocol(SslProtocols.Tls12);
#endif

            var certificate = _settings.EncryptedTcpEndPoint?.Certificate?.ReadCertificate();
            if (certificate != null)
            {
                if (!string.IsNullOrEmpty(_settings.EncryptedTcpEndPoint?.Certificate?.Path))
                {
                    IMqttServerCertificateCredentials? certificateCredentials = null;

                    if (!string.IsNullOrEmpty(_settings.EncryptedTcpEndPoint?.Certificate?.Password))
                    {
                        certificateCredentials = new MqttServerCertificateCredentials
                        {
                            Password = _settings.EncryptedTcpEndPoint.Certificate.Password
                        };
                    }
                
                    options.WithEncryptionCertificate(certificate, certificateCredentials);
                } 
            }

            if (_settings.EncryptedTcpEndPoint?.TryReadIPv4(out var address4) == true)
            {
                options.WithEncryptedEndpointBoundIPAddress(address4);
            }

            if (_settings.EncryptedTcpEndPoint?.TryReadIPv6(out var address6) == true)
            {
                options.WithEncryptedEndpointBoundIPV6Address(address6);
            }

            if (_settings.EncryptedTcpEndPoint?.Port > 0)
            {
                options.WithEncryptedEndpointPort(_settings.EncryptedTcpEndPoint.Port);
            }
        }
        else
        {
            options.WithoutEncryptedEndpoint();
        }

        if (_settings.ConnectionBacklog > 0)
        {
            options.WithConnectionBacklog(_settings.ConnectionBacklog);
        }

        if (_settings.EnablePersistentSessions)
        {
            options.WithPersistentSessions();
        }

        return options.Build();
    }
}