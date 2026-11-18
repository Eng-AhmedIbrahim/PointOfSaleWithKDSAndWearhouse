using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Pos.Repository.Identity;

namespace Pos.Repository.Factories;

public class AppIdentityDbContextFactory : IDesignTimeDbContextFactory<AppIdentityDbContext>
{
    public AppIdentityDbContext CreateDbContext(string[] args)
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

        var optionsBuilder = new DbContextOptionsBuilder<AppIdentityDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new AppIdentityDbContext(optionsBuilder.Options);
    }
}
