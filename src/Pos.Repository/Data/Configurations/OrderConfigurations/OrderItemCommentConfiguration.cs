namespace POS.Repository.Data.Configurations.OrderConfigurations
{
    public class OrderItemCommentConfiguration : IEntityTypeConfiguration<OrderItemComment>
    {
        public void Configure(EntityTypeBuilder<OrderItemComment> builder)
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Comment).HasMaxLength(500);
            builder.Property(c => c.AddedBy).HasMaxLength(100);
        }
    }
}
