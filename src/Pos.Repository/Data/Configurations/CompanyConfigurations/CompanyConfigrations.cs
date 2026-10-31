namespace POS.Repository.Data.Configurations.CompanyConfigurations;

public class CompanyConfigurations : IEntityTypeConfiguration<Company>
{
    private readonly int  MaxLength = 255; 
    private readonly int  MinLength = 70; 
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.Property(c => c.EnglishName)
            .HasColumnType("nvarchar")
            .HasMaxLength(MinLength)
            .IsRequired();

        builder.Property(c => c.NormalizedEnglishName)
            .HasColumnType("nvarchar")
            .HasMaxLength(MinLength)
            .IsRequired();

        builder.Property(c => c.ArabicName)
            .HasColumnType("nvarchar")
            .HasMaxLength(MinLength)
            .IsRequired();

        builder.Property(c => c.Email)
           .HasColumnType("nvarchar")
           .HasMaxLength(MaxLength);

        builder.Property(c => c.NormalizedEmail)
           .HasColumnType("nvarchar")
           .HasMaxLength(MaxLength);

        builder.Property(c => c.Website)
          .HasColumnType("nvarchar")
          .HasMaxLength(MaxLength);

        builder.Property(c => c.MobileNumber)
        .HasColumnType("nvarchar")
        .HasMaxLength(15);

        builder.Property(c => c.PhoneNumber)
         .HasColumnType("nvarchar")
         .HasMaxLength(15);

        builder.Property(c => c.Address)
        .HasColumnType("nvarchar")
        .HasMaxLength(MaxLength);

        builder.Property(c => c.CreationDate)
            .HasColumnType("datetime");

    }
}
