using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using POS.Core.Entities.Item;

namespace POS.Repository.Data.Configurations.ItemConfigurations;

public class RecipeIngredientConfiguration : IEntityTypeConfiguration<RecipeIngredient>
{
    public void Configure(EntityTypeBuilder<RecipeIngredient> builder)
    {
        builder.HasOne(ri => ri.Unit)
            .WithMany()
            .HasForeignKey(ri => ri.UnitId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(ri => ri.Recipe)
            .WithMany(r => r.Ingredients)
            .HasForeignKey(ri => ri.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ri => ri.MenuSalesIngredient)
            .WithMany()
            .HasForeignKey(ri => ri.MenuSalesIngredientId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.Property(ri => ri.Quantity)
            .HasColumnType("decimal(18,4)");
    }
}
