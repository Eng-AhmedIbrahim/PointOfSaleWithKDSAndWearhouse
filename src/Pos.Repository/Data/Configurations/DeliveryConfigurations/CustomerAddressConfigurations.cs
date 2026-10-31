public class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.BranchName)
            .HasMaxLength(100);

        builder.Property(c => c.ZoneName)
            .HasMaxLength(100);

        builder.Property(c => c.HomeNumber)
            .HasMaxLength(20);

        builder.Property(c => c.FloorNumber)
            .HasMaxLength(10);

        builder.Property(c => c.FlatNumber)
            .HasMaxLength(10);

        builder.Property(c => c.ClientAddress)
            .HasMaxLength(500);

        builder.Property(c => c.AddressNote)
            .HasMaxLength(500);

        builder.HasOne(c => c.Branch)
      .WithMany(b => b.CustomerAddresses)  // Explicit relationship
      .HasForeignKey(c => c.BranchId)
      .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.DeliveryZone)
            .WithMany(c=>c.CustomerAddresses)
            .HasForeignKey(c => c.DeliveryZoneId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.DeliveryCustomer)
            .WithMany(d => d.CustomerAddresses) // Ensure DeliveryCustomer has a list of CustomerAddresses
            .HasForeignKey(c => c.DeliveryCustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
