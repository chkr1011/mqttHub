﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Microsoft.Scripting.Utils;
using mqttHub.Configuration;
using mqttHub.Mqtt;
using mqttHub.Scripting;
using mqttHub.Scripting.DataSharing;
using MQTTnet.AspNetCore;
using Newtonsoft.Json.Converters;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace mqttHub.Web;

public sealed class Startup
{
    public Startup(IConfiguration configuration)
    {
        var builder = new ConfigurationBuilder().AddJsonFile("appsettings.json").AddEnvironmentVariables();

        Configuration = builder.Build();
    }

    public IConfigurationRoot Configuration { get; }

    public void Configure(IApplicationBuilder application, MqttServerService mqttServerService, PythonScriptHostService pythonScriptHostService, DataSharingService dataSharingService, MqttSettingsModel mqttSettings)
    {
        application.UseDefaultFiles();
        application.UseStaticFiles();

        application.UseHsts();
        application.UseRouting();
        application.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

        application.UseAuthentication();
        application.UseAuthorization();

        application.UseEndpoints(endpoints => { endpoints.MapControllers(); });

        ConfigureWebSocketEndpoint(application, mqttServerService, mqttSettings);

        dataSharingService.Configure();
        pythonScriptHostService.Configure();

        mqttServerService.Configure();

        application.UseSwagger(o => o.RouteTemplate = "/api/{documentName}/swagger.json");

        application.UseSwaggerUI(o =>
        {
            o.RoutePrefix = "api";
            o.DocumentTitle = "MQTTnetServer API";
            o.SwaggerEndpoint("/api/v1/swagger.json", "MQTTnetServer API v1");
            o.DisplayRequestDuration();
            o.DocExpansion(DocExpansion.List);
            o.DefaultModelRendering(ModelRendering.Model);
        });
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddCors();

        services.AddControllers();

        services.AddMvc().AddNewtonsoftJson(o => { o.SerializerSettings.Converters.Add(new StringEnumConverter()); });

        ReadMqttSettings(services);

        services.AddSingleton<PythonIOStream>();
        services.AddSingleton<PythonScriptHostService>();
        services.AddSingleton<DataSharingService>();

        services.AddSingleton<MqttNetLoggerWrapper>();
        services.AddSingleton<CustomMqttFactory>();
        services.AddSingleton<MqttServerService>();
        services.AddSingleton<MqttServerStorage>();

        services.AddSwaggerGen(c =>
        {
            var securityScheme = new OpenApiSecurityScheme
            {
                Scheme = "basic",
                Name = HeaderNames.Authorization,
                Type = SecuritySchemeType.Http,
                In = ParameterLocation.Header,
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Swagger"
                }
            };

            c.AddSecurityDefinition("Swagger", securityScheme);

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [securityScheme] = new List<string>()
            });

            c.SwaggerDoc("v1",
                new OpenApiInfo
                {
                    Title = "mqttHub API",
                    Version = "v1",
                    Description = "The public API of mqttHub.",
                    License = new OpenApiLicense
                    {
                        Name = "MIT",
                        Url = new Uri("https://github.com/chkr1011/mqttHub/blob/main/README.md")
                    },
                    Contact = new OpenApiContact
                    {
                        Name = "mqttHub",
                        Email = string.Empty,
                        Url = new Uri("https://github.com/chkr1011/mqttHub")
                    }
                });
        });

        services.AddAuthentication("Basic").AddScheme<AuthenticationSchemeOptions, AuthenticationHandler>("Basic", null).AddCookie();
    }

    void ReadMqttSettings(IServiceCollection services)
    {
        var mqttSettings = new MqttSettingsModel();
        Configuration.Bind("MQTT", mqttSettings);
        services.AddSingleton(mqttSettings);

        var scriptingSettings = new ScriptingSettingsModel();
        Configuration.Bind("Scripting", scriptingSettings);
        services.AddSingleton(scriptingSettings);
    }

    static void ConfigureWebSocketEndpoint(IApplicationBuilder application, MqttServerService mqttServerService, MqttSettingsModel mqttSettings)
    {
        if (mqttSettings?.WebSocketEndPoint?.Enabled != true)
        {
            return;
        }

        if (string.IsNullOrEmpty(mqttSettings.WebSocketEndPoint.Path))
        {
            return;
        }

        var webSocketOptions = new WebSocketOptions
        {
            KeepAliveInterval = TimeSpan.FromSeconds(mqttSettings.WebSocketEndPoint.KeepAliveInterval)
        };

        if (mqttSettings.WebSocketEndPoint.AllowedOrigins?.Any() == true)
        {
            webSocketOptions.AllowedOrigins.AddRange(mqttSettings.WebSocketEndPoint.AllowedOrigins);
        }

        application.UseWebSockets(webSocketOptions);

        application.Use(async (context, next) =>
        {
            if (context.Request.Path == mqttSettings.WebSocketEndPoint.Path)
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    string? subProtocol = null;
                    if (context.Request.Headers.TryGetValue("Sec-WebSocket-Protocol", out var requestedSubProtocolValues))
                    {
                        subProtocol = MqttSubProtocolSelector.SelectSubProtocol(requestedSubProtocolValues);
                    }

                    using (var webSocket = await context.WebSockets.AcceptWebSocketAsync(subProtocol).ConfigureAwait(false))
                    {
                        await mqttServerService.RunWebSocketConnectionAsync(webSocket, context).ConfigureAwait(false);
                    }
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                }
            }
            else
            {
                await next().ConfigureAwait(false);
            }
        });
    }
}