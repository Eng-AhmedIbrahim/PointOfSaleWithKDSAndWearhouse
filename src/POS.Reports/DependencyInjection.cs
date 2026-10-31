using Microsoft.Extensions.DependencyInjection;

namespace POS.Reports;

public static class DependencyInjection
{
    public static IServiceCollection AddReportingInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IFastReportService, FastReportService>();
        services.AddScoped<IReportsManager, ReportsManager>();
        return services;
    }
}
