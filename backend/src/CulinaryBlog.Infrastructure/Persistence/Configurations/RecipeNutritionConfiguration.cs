using CulinaryBlog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

public class RecipeNutritionConfiguration
    : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.OwnsOne(
            recipe => recipe.Nutrition,
            nutrition =>
            {
                nutrition.Property(x => x.Calories)
                    .HasPrecision(8, 2)
                    .HasColumnName("Nutrition_Calories");

                nutrition.Property(x => x.Protein)
                    .HasPrecision(8, 2)
                    .HasColumnName("Nutrition_Protein");

                nutrition.Property(x => x.Carbohydrates)
                    .HasPrecision(8, 2)
                    .HasColumnName("Nutrition_Carbohydrates");

                nutrition.Property(x => x.Fat)
                    .HasPrecision(8, 2)
                    .HasColumnName("Nutrition_Fat");

                nutrition.Property(x => x.Fiber)
                    .HasPrecision(8, 2)
                    .HasColumnName("Nutrition_Fiber");

                nutrition.Property(x => x.Sodium)
                    .HasPrecision(8, 2)
                    .HasColumnName("Nutrition_Sodium");
            });
    }
}