using System;
using System.IO;

namespace MQTTnetServer.Configuration;

public static class PathHelper
{
    public static string ExpandPath(string path)
    {
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        var uri = new Uri(path, UriKind.RelativeOrAbsolute);
        if (!uri.IsAbsoluteUri)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
        }

        return path;
    }
}