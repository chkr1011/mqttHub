using System;
using System.Net;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace MQTTnetServer.Configuration;

public sealed class TcpEndPointModel
{
    public CertificateSettingsModel Certificate { get; set; }

    public bool Enabled { get; set; } = true;

    public string IPv4 { get; set; }

    public string IPv6 { get; set; }


    public int Port { get; set; } = 1883;

    public bool TryReadIPv4(out IPAddress address)
    {
        if (IPv4 == "*")
        {
            address = IPAddress.Any;
            return true;
        }

        if (IPv4 == "localhost")
        {
            address = IPAddress.Loopback;
            return true;
        }

        if (IPv4 == "disable")
        {
            address = IPAddress.None;
            return true;
        }

        if (IPAddress.TryParse(IPv4, out var ip))
        {
            address = ip;
            return true;
        }

        throw new Exception($"Could not parse IPv4 address: {IPv4}");
    }

    public bool TryReadIPv6(out IPAddress address)
    {
        if (IPv6 == "*")
        {
            address = IPAddress.IPv6Any;
            return true;
        }

        if (IPv6 == "localhost")
        {
            address = IPAddress.IPv6Loopback;
            return true;
        }

        if (IPv6 == "disable")
        {
            address = IPAddress.None;
            return true;
        }

        if (IPAddress.TryParse(IPv6, out var ip))
        {
            address = ip;
            return true;
        }

        throw new Exception($"Could not parse IPv6 address: {IPv6}");
    }
}