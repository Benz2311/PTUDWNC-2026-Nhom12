using System.ComponentModel.DataAnnotations.Schema;
using CulinaryBlog.Domain.Common;

namespace CulinaryBlog.Domain.Entities;

public class ShoppingListItem : BaseEntity
{
    public Guid? ShoppingListId { get; set; }
    public ShoppingList? ShoppingList { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Guid? RecipeId { get; set; }
    public Recipe? Recipe { get; set; }

    [NotMapped]
    public string IngredientName
    {
        get => Name;
        set => Name = value;
    }

    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public decimal? Quantity { get; set; }
    public string? Unit { get; set; }
    public bool IsChecked { get; set; }
    public int SortOrder { get; set; }
}
