public class TakeawayCustomerConfiguration : IEntityTypeConfiguration<TakeawayCustomer>
{
    public void Configure(EntityTypeBuilder<TakeawayCustomer> builder)
    {
        builder.ToTable("TakeawayCustomers");

        builder.HasKey(tc => tc.Id);

        builder.Property(tc => tc.CustomerName)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(tc => tc.CustomerPhone)
               .IsRequired()
               .HasMaxLength(20);

        builder.HasIndex(tc => tc.CustomerPhone)
               .IsUnique();

        builder.HasMany(tc => tc.Orders)
               .WithOne(o => o.TakeawayCustomer)
               .HasForeignKey(o => o.TakeawayCustomerId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}