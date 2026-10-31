namespace ERPFront.Extensions;

public static class SerilogServiceExtension
{
    public static WebApplicationBuilder AddSerilogService(this WebApplicationBuilder builder)
    {
        var loggerConfiguration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("serilog.json", optional: false, reloadOnChange: true)
        .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(loggerConfiguration)
            .CreateLogger();

        builder.Host.UseSerilog();
        return builder;
    }
}