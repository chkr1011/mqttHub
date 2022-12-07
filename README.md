# mqttHub

_mqttHub_ is a standalone cross platform MQTT broker based on the .NET library _MQTTnet_. It has the following
features.

* Portable (no installation or .NET framework required)
* Runs on Windows, Linux, macOS, Raspberry Pi
* Python scripting support for manipulating messages, validation of clients, building business logic etc.
* Supports WebSocket and TCP (with and without TLS) connections
* Provides a HTTP based API (including Swagger endpoint)

![](https://github.com/chkr1011/mqttHub/blob/main/Images/Screenshot1.png?raw=true)

## Starting
The server is fully portable and is shipped including the .NET runtime etc. The broker can be started using the contained executable files.

### Windows
> mqttHub.exe

### Linux & macOS

The executable must be set to executable first via "chmod +x ./mqttHub".
> ./mqttHub 

## Swagger API

The URI for the Swagger API frontend is:
> /api/index.html
