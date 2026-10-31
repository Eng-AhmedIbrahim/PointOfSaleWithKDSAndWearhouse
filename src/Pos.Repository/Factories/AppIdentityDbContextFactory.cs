using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pos.Repository.Identity;

namespace Pos.Repository.Factories;

public class AppIdentityDbContextFactory : IDesignTimeDbContextFactory<AppIdentityDbContext>
{
    public AppIdentityDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppIdentityDbContext>();
        
        // Plain connection string for migrations only
        optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS;Database=PointOfSaleDB;User Id=sa;Password=NewTechn0;TrustServerCertificate=true");

        return new AppIdentityDbContext(optionsBuilder.Options);
    }
}
