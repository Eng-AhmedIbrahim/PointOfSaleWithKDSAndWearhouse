namespace POS.Repository.Data.Configurations.ItemConfigurations;

public class AttributeConfigurations : IEntityTypeConfiguration<Attributes>
{
    private readonly int MaxLength = 255;
    private readonly int MinLength = 70;
    public void Configure(EntityTypeBuilder<Attributes> builder)
    {

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(c => c.ArabicName)
           .HasColumnType("nvarchar")
           .HasMaxLength(MinLength)
           .IsRequired();

        builder.Property(c => c.EnglishName)
            .HasColumnType("nvarchar")
            .HasMaxLength(MinLength)
            .IsRequired();

        builder.HasMany(a => a.AttributeItems)
             .WithOne(a => a.Attribute)
             .HasForeignKey(a => a.AttributeId)
             .OnDelete(DeleteBehavior.Cascade);
    }
}
