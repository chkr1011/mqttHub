# Build MQTTnet.Server
FROM mcr.microsoft.com/dotnet/sdk as buildinstance

RUN mkdir /build/

WORKDIR /build/
RUN git clone https://github.com/chkr1011/MQTTnet.Server.git

WORKDIR /build/MQTTnet.Server/Source
RUN dotnet publish -f net5.0 -r linux-x64 -c Release --sc #-p:PublishSingleFile=true


# Run MQTTnet.Server
FROM debian
RUN apt-get update && apt-get install -y libicu-dev

RUN mkdir /MQTTnet.Server

COPY --from=buildinstance /build/MQTTnet.Server/Source/bin/Release/net5.0/linux-x64/publish/ /MQTTnet.Server/

RUN groupadd -r -g 990 mqttserver && \
        useradd -u 990 -d /MQTTnet.Server -g mqttserver -r mqttserver && \
        mkdir -p /MQTTnet.Server/.aspnet/DataProtection-Keys/ && \
        mkdir -p /MQTTnet.Server/certificate/ && \
        chown -R mqttserver /MQTTnet.Server

USER mqttserver
WORKDIR /MQTTnet.Server/

EXPOSE 80/tcp
EXPOSE 443/tcp
EXPOSE 1883/tcp
EXPOSE 8883/tcp

VOLUME /MQTTnet.Server/.aspnet/DataProtection-Keys/
VOLUME /MQTTnet.Server/certificate/

CMD ["/MQTTnet.Server/MQTTnet.Server"]

