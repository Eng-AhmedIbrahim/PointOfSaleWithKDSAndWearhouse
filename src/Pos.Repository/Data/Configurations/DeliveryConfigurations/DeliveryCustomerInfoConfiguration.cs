namespace Pos.Repository.Data.Configurations.DeliveryConfigurations;

public class DeliveryCustomerInfoConfiguration : IEntityTypeConfiguration<DeliveryCustomerInfo>
{
    public void Configure(EntityTypeBuilder<DeliveryCustomerInfo> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.FirstPhoneNumber)
            .HasMaxLength(15);

        builder.Property(d => d.SecondPhoneNumber)
            .HasMaxLength(15);

        builder.Property(d => d.ClientTitle)
            .HasMaxLength(50);

        builder.Property(d => d.CustomerName)
            .HasMaxLength(100);

        builder.HasMany(d => d.CustomerAddresses)
            .WithOne(d=>d.DeliveryCustomer)
            .HasForeignKey(d=>d.DeliveryCustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
