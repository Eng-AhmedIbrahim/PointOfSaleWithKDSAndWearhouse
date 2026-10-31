namespace Pos.Repository.Data.Configurations.DeliveryConfigurations;

public class DeliveryZoneConfiguration : IEntityTypeConfiguration<DeliveryZone>
{
    public void Configure(EntityTypeBuilder<DeliveryZone> builder)
    {
        builder.ToTable("DeliveryZones");

        builder.HasKey(dz => dz.Id);

        builder.Property(dz => dz.ZoneName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(dz => dz.DeliveryFee)
            .HasColumnType("decimal(18,2)");

        builder.HasOne(z => z.Branch)
     .WithMany(b => b.DeliveryZones)  // Explicit relationship
     .HasForeignKey(z => z.BranchId)
     .OnDelete(DeleteBehavior.Cascade);


        builder.HasMany(z => z.CustomerAddresses)
              .WithOne(c => c.DeliveryZone)
              .HasForeignKey(c => c.DeliveryZoneId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}
