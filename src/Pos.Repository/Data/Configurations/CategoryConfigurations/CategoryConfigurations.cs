namespace POS.Repository.Data.Configurations.CategoryConfigurations;

public class CategoryConfigurations : IEntityTypeConfiguration<Category>
{
    private readonly int MinLength = 70;
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.Property(c => c.Id)
            .ValueGeneratedNever();

        builder.Property(c => c.EnglishName)
            .HasColumnType("nvarchar")
            .HasMaxLength(MinLength)
            .IsRequired();

        builder.Property(c => c.ArabicName)
            .HasColumnType("nvarchar")
            .HasMaxLength(MinLength)
            .IsRequired();

        builder.Property(c => c.NormalizedEnglishName)
        .HasColumnType("nvarchar")
        .HasMaxLength(MinLength)
        .IsRequired();

        builder.Property(c => c.Invisible)
        .HasColumnType("bit")
        .HasDefaultValue(false);

        builder.Property(c => c.ItemsFont)
            .HasColumnType("nvarchar")
            .HasMaxLength(MinLength);

        builder.Property(c => c.UpdateDate)
            .HasColumnType("datetime");


        builder.Property(c => c.PrintInBackupReceipt)
            .HasColumnType("bit")
             .HasDefaultValue(true);


        builder.Property(c => c.CreationDate)
            .HasColumnType("datetime");


        builder.Property(c => c.BranchId)
           .HasColumnType("int")
            .HasDefaultValue(1);


        builder.Property(c => c.KitchenTypeId)
          .HasColumnType("int")
          .IsRequired(false);
           

        builder.HasMany(c => c.MenuSalesItems)
            .WithOne(c => c.Category)
            .HasForeignKey(c => c.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.KitchenType)
            .WithMany(c => c.Categories)
            .HasForeignKey(c => c.KitchenTypeId);
    }
}
