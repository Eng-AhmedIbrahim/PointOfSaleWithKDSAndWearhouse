using POS.Core.Entities.Shift;

namespace Pos.Repository.Data.Configurations.ShiftConfigurations;

public class ShiftHandoverConfiguration : IEntityTypeConfiguration<ShiftHandover>
{
    public void Configure(EntityTypeBuilder<ShiftHandover> builder)
    {
        // Table Name
        builder.ToTable("ShiftHandovers");

        // Primary Key
        builder.HasKey(sh => sh.Id);

        builder.Property(sh => sh.BranchID)
               .IsRequired()
               .HasDefaultValue(1);

        builder.Property(sh => sh.WorkingDate)
               .IsRequired(false);

        builder.Property(sh => sh.StartShiftTime)
               .IsRequired(false);

        builder.Property(sh => sh.EndShiftTime)
               .IsRequired(false);

        builder.Property(sh => sh.ShiftNumber)
               .IsRequired(false);

        builder.Property(sh => sh.FromCashierID)
               .IsRequired(false);

        builder.Property(sh => sh.ToCashierID)
               .IsRequired(false);

        builder.Property(sh => sh.FromCashierName)
               .HasMaxLength(100)
               .IsRequired(false);

        builder.Property(sh => sh.ToCashierName)
               .HasMaxLength(100)
               .IsRequired(false);

        builder.Property(sh => sh.Amount)
               .HasColumnType("decimal(18,2)")
               .IsRequired(false);

        builder.HasMany(sh => sh.Orders)
               .WithOne(o => o.Shift)
               .HasForeignKey(o => o.ShiftID)
               .OnDelete(DeleteBehavior.NoAction);
    }
}
