using CulinaryBlog.Application.Features.Recipes;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;

namespace CulinaryBlog.UnitTests;

public sealed class RecipeWriteServiceTests
{
    [Fact]
    public async Task CreateAsync_AddsRecipe_AndSavesOnce()
    {
        var (service, repository, unitOfWork) = CreateService();
        var recipe = new Recipe();

        await service.CreateAsync(recipe);

        Assert.Equal(["AddRecipe"], repository.Calls);
        Assert.Same(recipe, repository.LastRecipe);
        Assert.Equal(1, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesRecipe_AndSavesOnce()
    {
        var (service, repository, unitOfWork) = CreateService();
        var recipe = new Recipe();

        await service.UpdateAsync(recipe);

        Assert.Equal(["UpdateRecipe"], repository.Calls);
        Assert.Same(recipe, repository.LastRecipe);
        Assert.Equal(1, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task ReplaceContentsAsync_ReplacesAllChildren_AndSavesOnce()
    {
        var (service, repository, unitOfWork) = CreateService();
        var recipe = new Recipe();
        ICollection<RecipeIngredient> ingredients = [new()];
        ICollection<RecipeStep> steps = [new()];
        ICollection<RecipeImage> images = [new()];

        await service.ReplaceContentsAsync(recipe, ingredients, steps, images);

        Assert.Equal(["ReplaceChildren", "UpdateRecipe"], repository.Calls);
        Assert.Same(recipe, repository.LastRecipe);
        Assert.Same(ingredients, repository.LastIngredients);
        Assert.Same(steps, repository.LastSteps);
        Assert.Same(images, repository.LastImages);
        Assert.Equal(1, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task DeleteAsync_DeletesRecipe_AndSavesOnce()
    {
        var (service, repository, unitOfWork) = CreateService();
        var recipe = new Recipe();

        await service.DeleteAsync(recipe);

        Assert.Equal(["DeleteRecipe"], repository.Calls);
        Assert.Same(recipe, repository.LastRecipe);
        Assert.Equal(1, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Ingredient_write_methods_delegateAndSaveEachChange()
    {
        var (service, repository, unitOfWork) = CreateService();
        var ingredient = new RecipeIngredient();

        await service.AddIngredientAsync(ingredient);
        await service.UpdateIngredientAsync(ingredient);
        await service.DeleteIngredientAsync(ingredient);

        Assert.Equal(["AddIngredient", "UpdateIngredient", "DeleteIngredient"], repository.Calls);
        Assert.Equal(3, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Step_write_methods_delegateAndSaveEachChange()
    {
        var (service, repository, unitOfWork) = CreateService();
        var step = new RecipeStep();

        await service.AddStepAsync(step);
        await service.UpdateStepAsync(step);

        Assert.Equal(["AddStep", "UpdateStep"], repository.Calls);
        Assert.Equal(2, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task DeleteStepAndReorderAsync_SavesOnlyWhenStepExists()
    {
        var (service, repository, unitOfWork) = CreateService();
        var recipeId = Guid.NewGuid();
        var stepId = Guid.NewGuid();

        repository.DeleteStepResult = false;
        Assert.False(await service.DeleteStepAndReorderAsync(recipeId, stepId));
        Assert.Equal(0, unitOfWork.SaveCalls);

        repository.DeleteStepResult = true;
        Assert.True(await service.DeleteStepAndReorderAsync(recipeId, stepId));
        Assert.Equal((recipeId, stepId), repository.LastDeletedStep);
        Assert.Equal(1, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task SetPrimaryImageAsync_SavesOnlyWhenImageExists()
    {
        var (service, repository, unitOfWork) = CreateService();
        var recipeId = Guid.NewGuid();
        var imageId = Guid.NewGuid();

        repository.SetPrimaryImageResult = false;
        Assert.False(await service.SetPrimaryImageAsync(recipeId, imageId));
        Assert.Equal(0, unitOfWork.SaveCalls);

        repository.SetPrimaryImageResult = true;
        Assert.True(await service.SetPrimaryImageAsync(recipeId, imageId));
        Assert.Equal((recipeId, imageId), repository.LastPrimaryImage);
        Assert.Equal(1, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task DeleteImageAsync_SavesOnlyWhenImageExists()
    {
        var (service, repository, unitOfWork) = CreateService();
        var recipeId = Guid.NewGuid();
        var imageId = Guid.NewGuid();

        repository.DeleteImageResult = false;
        Assert.False(await service.DeleteImageAsync(recipeId, imageId));
        Assert.Equal(0, unitOfWork.SaveCalls);

        repository.DeleteImageResult = true;
        Assert.True(await service.DeleteImageAsync(recipeId, imageId));
        Assert.Equal((recipeId, imageId), repository.LastDeletedImage);
        Assert.Equal(1, unitOfWork.SaveCalls);
    }

    private static (RecipeWriteService Service, RecordingRecipeRepository Repository, RecordingUnitOfWork UnitOfWork) CreateService()
    {
        var repository = new RecordingRecipeRepository();
        var unitOfWork = new RecordingUnitOfWork();
        return (new RecipeWriteService(repository, unitOfWork), repository, unitOfWork);
    }

    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public int SaveCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(1);
        }
    }

    private sealed class RecordingRecipeRepository : IRecipeRepository
    {
        public List<string> Calls { get; } = [];
        public Recipe? LastRecipe { get; private set; }
        public ICollection<RecipeIngredient>? LastIngredients { get; private set; }
        public ICollection<RecipeStep>? LastSteps { get; private set; }
        public ICollection<RecipeImage>? LastImages { get; private set; }
        public bool DeleteStepResult { get; set; }
        public bool SetPrimaryImageResult { get; set; }
        public bool DeleteImageResult { get; set; }
        public (Guid RecipeId, Guid StepId) LastDeletedStep { get; private set; }
        public (Guid RecipeId, Guid ImageId) LastPrimaryImage { get; private set; }
        public (Guid RecipeId, Guid ImageId) LastDeletedImage { get; private set; }

        public Task<Recipe?> GetByIdAsync(Guid id, bool includeDetails = false, CancellationToken cancellationToken = default) => Task.FromResult<Recipe?>(null);
        public Task<Recipe?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Recipe?>(null);
        public Task<Recipe?> GetBySlugAsync(string slug, bool includeDeleted = false, CancellationToken cancellationToken = default) => Task.FromResult<Recipe?>(null);
        public Task<bool> ExistsBySlugAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task AddAsync(Recipe recipe, CancellationToken cancellationToken = default)
        {
            Calls.Add("AddRecipe");
            LastRecipe = recipe;
            return Task.CompletedTask;
        }

        public void ReplaceChildren(Recipe recipe, ICollection<RecipeIngredient> ingredients, ICollection<RecipeStep> steps, ICollection<RecipeImage> images)
        {
            Calls.Add("ReplaceChildren");
            LastRecipe = recipe;
            LastIngredients = ingredients;
            LastSteps = steps;
            LastImages = images;
        }

        public Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default)
        {
            Calls.Add("UpdateRecipe");
            LastRecipe = recipe;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken = default)
        {
            Calls.Add("DeleteRecipe");
            LastRecipe = recipe;
            return Task.CompletedTask;
        }

        public Task AddIngredientAsync(RecipeIngredient ingredient, CancellationToken cancellationToken = default)
        {
            Calls.Add("AddIngredient");
            return Task.CompletedTask;
        }

        public Task UpdateIngredientAsync(RecipeIngredient ingredient, CancellationToken cancellationToken = default)
        {
            Calls.Add("UpdateIngredient");
            return Task.CompletedTask;
        }

        public Task DeleteIngredientAsync(RecipeIngredient ingredient, CancellationToken cancellationToken = default)
        {
            Calls.Add("DeleteIngredient");
            return Task.CompletedTask;
        }

        public Task AddStepAsync(RecipeStep step, CancellationToken cancellationToken = default)
        {
            Calls.Add("AddStep");
            return Task.CompletedTask;
        }

        public Task UpdateStepAsync(RecipeStep step, CancellationToken cancellationToken = default)
        {
            Calls.Add("UpdateStep");
            return Task.CompletedTask;
        }

        public Task<bool> DeleteStepAndReorderAsync(Guid recipeId, Guid stepId, CancellationToken cancellationToken = default)
        {
            Calls.Add("DeleteStepAndReorder");
            LastDeletedStep = (recipeId, stepId);
            return Task.FromResult(DeleteStepResult);
        }

        public Task<bool> SetPrimaryImageAsync(Guid recipeId, Guid imageId, CancellationToken cancellationToken = default)
        {
            Calls.Add("SetPrimaryImage");
            LastPrimaryImage = (recipeId, imageId);
            return Task.FromResult(SetPrimaryImageResult);
        }

        public Task<bool> DeleteImageAsync(Guid recipeId, Guid imageId, CancellationToken cancellationToken = default)
        {
            Calls.Add("DeleteImage");
            LastDeletedImage = (recipeId, imageId);
            return Task.FromResult(DeleteImageResult);
        }
    }
}
