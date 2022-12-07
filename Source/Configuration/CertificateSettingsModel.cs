using System.IO;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace mqttHub.Configuration;

public sealed class CertificateSettingsModel
{
    public string? Path { get; set; }

    public string? Password { get; set; }

    public byte[] ReadCertificate()
    {
        if (string.IsNullOrEmpty(Path) || string.IsNullOrWhiteSpace(Path))
        {
            throw new FileNotFoundException("No path set");
        }

        if (!File.Exists(Path))
        {
            throw new FileNotFoundException($"Could not find Certificate in path: {Path}");
        }

        return File.ReadAllBytes(Path);
    }
}