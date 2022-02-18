# MQTTnet.Server

_MQTTnet Server_ is a standalone cross platform MQTT server (like mosquitto) basing on the library _MQTTnet_. It has the following features.

* Running portable (no installation required)
* Runs under Windows, Linux, macOS, Raspberry Pi
* Python scripting support for manipulating messages, validation of clients, building business logic etc.
* Supports WebSocket and TCP (with and without TLS) connections
* Provides a HTTP based API (including Swagger endpoint)
* Extensive configuration parameters and customization supported

<p align="center">
<img src="https://github.com/chkr1011/MQTTnet.Server/blob/main/Images/Screenshot1.png?raw=true">
</p>

## Starting portable version
The portable version requires a local installation of the .net core runtime. With that runtime installed the server can be started via the following comand.

> dotnet .\MQTTnet.Server.dll

## Starting self contained versions
The self contained versions are fully portable versions including the .net core runtime. The server can be started using the contained executable files.

Windows:    
> MQTTnet.Server.exe
> 
Linux:		
> ./MQTTnet.Server (must be set to _executable_ first via chmod +c ./MQTTnet.Server)
