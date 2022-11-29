using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using MQTTnetServer.Web;

namespace MQTTnetServer;

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
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Red;
        const string LogoText = @"

███╗   ███╗ ██████╗ ████████╗████████╗███╗   ██╗███████╗████████╗    ███████╗███████╗██████╗ ██╗   ██╗███████╗██████╗ 
████╗ ████║██╔═══██╗╚══██╔══╝╚══██╔══╝████╗  ██║██╔════╝╚══██╔══╝    ██╔════╝██╔════╝██╔══██╗██║   ██║██╔════╝██╔══██╗
██╔████╔██║██║   ██║   ██║      ██║   ██╔██╗ ██║█████╗     ██║       ███████╗█████╗  ██████╔╝██║   ██║█████╗  ██████╔╝
██║╚██╔╝██║██║▄▄ ██║   ██║      ██║   ██║╚██╗██║██╔══╝     ██║       ╚════██║██╔══╝  ██╔══██╗╚██╗ ██╔╝██╔══╝  ██╔══██╗
██║ ╚═╝ ██║╚██████╔╝   ██║      ██║   ██║ ╚████║███████╗   ██║       ███████║███████╗██║  ██║ ╚████╔╝ ███████╗██║  ██║
╚═╝     ╚═╝ ╚══▀▀═╝    ╚═╝      ╚═╝   ╚═╝  ╚═══╝╚══════╝   ╚═╝       ╚══════╝╚══════╝╚═╝  ╚═╝  ╚═══╝  ╚══════╝╚═╝  ╚═╝
                                                                                                                      
";

        Console.WriteLine(LogoText);
        Console.ResetColor();
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("The official MQTT server implementation of MQTTnet");
        Console.WriteLine("Copyright (c) 2017-2022 The MQTTnet.Server Team");
        Console.WriteLine(@"https://github.com/chkr1011/MQTTnet.Server");

        Console.ForegroundColor = ConsoleColor.White;

        var fileVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

        Console.WriteLine($@"
Version:    {fileVersion.ProductVersion}
License:    MIT
Support:    https://github.com/chkr1011/MQTTnet.Server/issues
Docs:       https://github.com/chkr1011/MQTTnet.Server/wiki
");

        Console.WriteLine();
    }
}