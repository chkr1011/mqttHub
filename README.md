# MQTTnetServer

_MQTTnetServer_ is a standalone cross platform MQTT server basing on the library _MQTTnet_. It has the following features.

* Running portable (no installation required)
* Runs on Windows, Linux, macOS, Raspberry Pi
* Python scripting support for manipulating messages, validation of clients, building business logic etc.
* Supports WebSocket and TCP (with and without TLS) connections
* Provides a HTTP based API (including Swagger endpoint)
* Extensive configuration parameters and customization supported

<p align="center">
<img src="https://github.com/chkr1011/MQTTnet.Server/blob/main/Images/Screenshot1.png?raw=true">
</p>

## Starting
The server is fully portable and includes .NET runtime. The server can be started using the contained executable files.

Windows:
> MQTTnetServer.exe

Linux & macOS:
> ./MQTTnetServer (must be set to _executable_ first via chmod +x ./MQTTnetServer)

## Swagger API

The URI for the Swagger API frontend is: _/api/index.html_
