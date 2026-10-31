namespace Pos.Repository.Data.Configurations.DeliveryConfigurations;

public class CustomerTitleConfiguration : IEntityTypeConfiguration<DeliveryCustomerTitle>
{
    public void Configure(EntityTypeBuilder<DeliveryCustomerTitle> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TitleName)
            .HasMaxLength(50)
            .IsRequired();
    }
}
