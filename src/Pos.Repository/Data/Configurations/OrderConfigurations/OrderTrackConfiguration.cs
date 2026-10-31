namespace POS.Repository.Data.Configurations.OrderConfigurations;

public class OrderTrackConfiguration : IEntityTypeConfiguration<OrderTrack>
{
    public void Configure(EntityTypeBuilder<OrderTrack> builder)
    {
        builder.HasKey(ot => ot.Id);

        builder.Property(ot => ot.Id)
            .ValueGeneratedOnAdd();

        builder.Property(ot => ot.OrderId)
            .IsRequired();

        builder.Property(ot => ot.OrderType)
            .HasMaxLength(50);

        builder.Property(ot => ot.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(ot => ot.UserName)
            .HasMaxLength(150);

        builder.Property(ot => ot.UserId)
            .HasMaxLength(200);

        builder.Property(ot => ot.MachineName)
            .HasMaxLength(200);

        builder.Property(ot => ot.TableName)
            .HasMaxLength(50);

        builder.Property(ot => ot.Details)
            .HasMaxLength(2000);

        builder.Property(ot => ot.ActionDateTime)
            .IsRequired();

        builder.HasIndex(ot => ot.OrderId);
        builder.HasIndex(ot => ot.ActionDateTime);
    }
}
