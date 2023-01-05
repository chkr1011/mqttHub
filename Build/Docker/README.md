# Build

To build MQTTnet.Server run the following command in the directory where the `Dockerfile` resides:

    docker build .

Then use the last image ID (`Successfully built <ID>`) to reference the image, possibly tag it.

    docker tag <ID> mqttserver:<version>

# Start Up

Start MQTTnet.Server with the necessary ports. Export some or all of them:

  - 80 (HTTP)
  - 443 (HTTPS)
  - 1883 (TCP Endpoint)
  - 8883 (Encrypted TCP Endpoint)

To persist configuration, keys, and certificates use Docker Volumes and mount.

  1) Create a new directory, for instance `mkdir -p /var/docker/mqttserver/{config,keys,certificate}`
  1) Copy the `appsettings.json` from the Git repository into it, e.g. `/var/docker/mqttserver/config/`
  1) Change owner and group to 990:990 for the file `chown 990:990 /var/docker/mqttserver/config/appsettings.json`
  1) Change mode to 440 `chmod 440 /var/docker/mqttserver/config/appsettings.json`
  1) Give the MQTTnet.Server process access to the keys directory `chown 990:990 /var/docker/mqttserver/keys`
  1) Change owner and group to 990:990 for certificates (if used) `chown 990:990 /var/docker/mqttserver/certificate/`
  1) Change mode to 550 for certificates (if used) `chmod 550 /var/docker/mqttserver/certificate/`
  1) Once you place a certificate (PFX) under `/var/docker/mqttserver/certificate/mqttserver.pfx` make sure that it is readable by user 990.
  
Example with persistent config and keys:

    docker run -d -p "80:80" -p "443:443" -p "1883:1883" -p "8883:8883" --mount "type=bind,source=/var/docker/mqttserver/config/appsettings.json,target=/MQTTnet.Server/appsettings.json" -v "/va
r/docker/mqttserver/keys:/MQTTnet.Server/.aspnet/DataProtection-Keys" mqttserver:<version>

To use a PFX certificate, update `appsettings.json` and modify the following section to point to `/MQTTnet.Server/certificate/mqttserver.pfx`. Also add the password.

    "Certificate": {
      "Path": "/MQTTnet.Server/certificate/mqttserver.pfx",
      "Password": ""
    }

Then add another volume when starting the image: `-v /var/docker/mqttserver/certificate/:/MQTTnet.Server/certificate/`.

