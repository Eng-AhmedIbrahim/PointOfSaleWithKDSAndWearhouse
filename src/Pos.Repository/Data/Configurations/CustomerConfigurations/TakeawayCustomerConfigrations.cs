namespace POS.Repository.Data.Configurations.CustomerConfigurations;

public class TakeawayCustomerConfiguration : IEntityTypeConfiguration<TakeawayCustomer>
{
    public void Configure(EntityTypeBuilder<TakeawayCustomer> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.CustomerName)
               .HasMaxLength(100)
               .IsRequired(false);

        builder.Property(t => t.CustomerPhone)
               .HasMaxLength(20)
               .IsRequired(false);

        builder.HasMany(o=>o.Orders)
            .WithOne(o=>o.TakeawayCustomer)
            .HasForeignKey(o=>o.TakeawayCustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}