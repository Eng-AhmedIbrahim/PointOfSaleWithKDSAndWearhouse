namespace Pos.Repository.Identity;

public class AppIdentityDbContext : IdentityDbContext<AppUser, ApplicationRole, string>
{
    public AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options) : base(options) { }

    public DbSet<Permission> Permissions { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUser>().Property(u => u.ArabicName)
            .HasColumnType("nvarchar").HasMaxLength(100);
        builder.Entity<AppUser>().Property(u => u.DisplayName)
            .HasColumnType("nvarchar").HasMaxLength(100);
        builder.Entity<AppUser>().Property(u => u.ImageUrl)
            .HasColumnType("nvarchar").HasMaxLength(100);
    }
}