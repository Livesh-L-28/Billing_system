using System;
using System.IO;
using Serilog;

namespace BillingSoftware.Infrastructure.Logging;

public static class LoggerConfig
{
    public static void Initialize()
    {
        var appFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BillingSoftware"
        );
        
        var logFolder = Path.Combine(appFolder, "Logs");
        if (!Directory.Exists(logFolder))
        {
            Directory.CreateDirectory(logFolder);
        }

        var logFilePath = Path.Combine(logFolder, "log-.txt");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(
                path: logFilePath,
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
            )
            .CreateLogger();

        Log.Information("Serilog logging initialized successfully.");
    }
}
