namespace Pos.Repository.Data.Configurations.KitchenConfigurations;

public class PrintingSettingsConfiguration : IEntityTypeConfiguration<PrintingSettings>
{
    public void Configure(EntityTypeBuilder<PrintingSettings> builder)
    {
        builder.ToTable("PrintingSettings");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.BranchID).IsRequired();

        builder.Property(p => p.OrderType)
            .HasMaxLength(50);

        builder.Property(p => p.ComputerName)
            .HasMaxLength(100);


        builder.Property(p => p.OutLetType)
            .HasMaxLength(100);

        builder.HasOne(p => p.Kitchen)
            .WithMany()
            .HasForeignKey(p => p.KitchenId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.BranchID);
        builder.HasIndex(p => p.KitchenId);
    }
}