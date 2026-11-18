using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Pos.Repository.Data;

namespace Pos.Repository.Factories;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var apiPath = Path.Combine(basePath, "..", "POS.API");
        if (Directory.Exists(apiPath))
            basePath = apiPath;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        if (!string.IsNullOrEmpty(connectionString) && !connectionString.StartsWith("Server=", StringComparison.OrdinalIgnoreCase))
        {
            connectionString = POS.Core.Helpers.EncryptionHelper.DecryptString(connectionString);
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
