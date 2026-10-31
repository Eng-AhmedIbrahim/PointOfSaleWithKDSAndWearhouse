
namespace POS.Core.Specifications.RecipeSpecs
{
    public class RecipeWithIngredientsSpecification : BaseSpecifications<Recipe>
    {
        public RecipeWithIngredientsSpecification() : base()
        {
            Includes.Add(r => r.MenuSalesItem!);
            Includes.Add(r => r.Ingredients);
            IncludeStrings.Add("Ingredients.MenuSalesIngredient");
            IncludeStrings.Add("Ingredients.Unit");
        }

        public RecipeWithIngredientsSpecification(int itemId) : base(r => r.MenuSalesItemId == itemId && r.IsActive)
        {
            Includes.Add(r => r.MenuSalesItem!);
            Includes.Add(r => r.Ingredients);
            IncludeStrings.Add("Ingredients.MenuSalesIngredient");
            IncludeStrings.Add("Ingredients.Unit");
        }
    }

}
