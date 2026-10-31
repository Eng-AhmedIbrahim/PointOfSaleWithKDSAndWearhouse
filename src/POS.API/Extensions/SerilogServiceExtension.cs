namespace POS.API.Extensions;

using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Serilog;

public static class SerilogServiceExtension
{
    public static WebApplicationBuilder AddSerilogService(this WebApplicationBuilder builder)
    {
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var desktopLogFolder = Path.Combine(desktopPath, "POS-Logs");
        Directory.CreateDirectory(desktopLogFolder);

        var loggerConfiguration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("serilog.json", optional: false, reloadOnChange: true)
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(loggerConfiguration)
            .WriteTo.File(
                path: Path.Combine(desktopLogFolder, "POS-API-.txt"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        builder.Host.UseSerilog();
        return builder;
    }
}

