namespace Pos.Repository.Data.Configurations.AppDateConfigurations;

public class AppDateConfiguration : IEntityTypeConfiguration<AppDate>
{
    public void Configure(EntityTypeBuilder<AppDate> builder)
    {
        builder.ToTable("AppDates");

        builder.HasKey(a => a.Id);
        
        builder.Property(a => a.BranchId)
               .IsRequired(); 

        builder.Property(a => a.PosDate)
               .HasColumnType("DATETIME")
               .IsRequired();

        builder.Property(a => a.StoreDate)
               .HasColumnType("DATETIME")
               .IsRequired();

        builder.Property(a => a.CurrentOrderNumber)
               .IsRequired();
    }
}