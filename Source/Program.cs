using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using mqttHub.Web;

namespace mqttHub;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            PrintLogo();

            Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder => { webBuilder.ConfigureKestrel(_ => { }).UseWebRoot(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Web", "wwwroot")).UseStartup<Startup>(); }).Build().Run();

            return 0;
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            return -1;
        }
    }

    static void PrintLogo()
    {
        var fileVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
        
        const string logo = @"
███    ███  ██████  ████████ ████████ ██   ██ ██    ██ ██████  
████  ████ ██    ██    ██       ██    ██   ██ ██    ██ ██   ██ 
██ ████ ██ ██    ██    ██       ██    ███████ ██    ██ ██████  
██  ██  ██ ██ ▄▄ ██    ██       ██    ██   ██ ██    ██ ██   ██ 
██      ██  ██████     ██       ██    ██   ██  ██████  ██████  
               ▀▀                                              
";
        
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write(logo);
        Console.ResetColor();
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Copyright (c) 2017-2023 The mqttHub team (MIT license)");
        Console.WriteLine();
        Console.WriteLine("Homepage:      https://github.com/chkr1011/mqttHub");
        Console.WriteLine($"Version:       {fileVersion.ProductVersion}");
        Console.WriteLine();
    }
}