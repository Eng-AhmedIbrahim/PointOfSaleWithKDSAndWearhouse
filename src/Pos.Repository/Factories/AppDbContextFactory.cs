using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pos.Repository.Data;

namespace Pos.Repository.Factories;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        
        // Plain connection string for migrations only
        optionsBuilder.UseSqlServer("Server=.;Database=PointOfSale;Trusted_Connection=True;TrustServerCertificate=true");

        return new AppDbContext(optionsBuilder.Options);
    }
}
