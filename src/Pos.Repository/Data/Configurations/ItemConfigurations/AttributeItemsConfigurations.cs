namespace POS.Repository.Data.Configurations.ItemConfigurations;

public class AttributeItemsConfigurations : IEntityTypeConfiguration<AttributeItem>
{
    private readonly int MaxLength = 255;
    private readonly int MinLength = 70;
    public void Configure(EntityTypeBuilder<AttributeItem> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(c => c.AppearanceIndex)
           .HasColumnType("int");

        builder.HasOne(c => c.Attribute)
            .WithMany(c => c.AttributeItems)
            .HasForeignKey(c => c.AttributeId)
            .OnDelete(DeleteBehavior.Cascade);


        builder.HasOne(c => c.RelatedMenuItem)
        .WithMany()
        .HasForeignKey(c => c.RelatedMenuItemId)
        .OnDelete(DeleteBehavior.Restrict);
    }
}