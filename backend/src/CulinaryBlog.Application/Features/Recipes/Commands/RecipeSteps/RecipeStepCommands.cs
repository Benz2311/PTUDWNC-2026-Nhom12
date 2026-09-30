using ICacheService = CulinaryBlog.Application.Common.Interfaces.ICacheService;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes.Dtos;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Application.Features.Recipes.Commands.RecipeSteps;

// 1. Command Thêm bước mới cho Recipe (Server tự cấp StepNumber)
public record CreateRecipeStepCommand(
    Guid RecipeId,
    string? Title,
    string Description,
    int? TimerMinutes = null,
    string? ImageUrl = null,
    Guid? CurrentUserId = null,
    bool IsAdmin = false
) : IRequest<RecipeStepDto>;

// 2. Command Cập nhật bước thực hiện (Không đổi StepNumber)
public record UpdateRecipeStepCommand(
    Guid RecipeId,
    Guid StepId,
    string? Title,
    string Description,
    int? TimerMinutes = null,
    string? ImageUrl = null,
    Guid? CurrentUserId = null,
    bool IsAdmin = false
) : IRequest<RecipeStepDto>;

// 3. Command Xóa mềm bước thực hiện + Đánh số lại liên tục trong cùng Transaction
public record DeleteRecipeStepCommand(
    Guid RecipeId,
    Guid StepId,
    Guid? CurrentUserId = null,
    bool IsAdmin = false
) : IRequest<bool>;

// 4. Command Sắp xếp lại toàn bộ Active Steps trong Recipe
public record ReorderRecipeStepsCommand(
    Guid RecipeId,
    List<Guid> StepIds,
    Guid? CurrentUserId = null,
    bool IsAdmin = false
) : IRequest<List<RecipeStepDto>>;

public class RecipeStepCommandHandler :
    IRequestHandler<CreateRecipeStepCommand, RecipeStepDto>,
    IRequestHandler<UpdateRecipeStepCommand, RecipeStepDto>,
    IRequestHandler<DeleteRecipeStepCommand, bool>,
    IRequestHandler<ReorderRecipeStepsCommand, List<RecipeStepDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cacheService;

    public RecipeStepCommandHandler(IApplicationDbContext context, ICacheService cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }

    private static void VerifyRecipeOwnership(Recipe recipe, Guid? currentUserId, bool isAdmin)
    {
        if (isAdmin)
        {
            return;
        }

        if (!currentUserId.HasValue || currentUserId.Value != recipe.AuthorId)
        {
            throw new ForbiddenException("You do not have permission to modify steps for this recipe.");
        }
    }

    private async Task InvalidateRecipeDetailCacheAsync(string recipeSlug, CancellationToken cancellationToken)
    {
        var cacheKey = $"recipe:{recipeSlug.Trim().ToLowerInvariant()}";
        await _cacheService.RemoveAsync(cacheKey, cancellationToken);
    }

    private static void ValidateStepFields(string? title, string description, int? timerMinutes, string? imageUrl)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(description))
        {
            errors["Description"] = ["Description is required and cannot be empty."];
        }

        if (title != null && title.Length > 200)
        {
            errors["Title"] = ["Title cannot exceed 200 characters."];
        }

        if (timerMinutes.HasValue && timerMinutes.Value < 0)
        {
            errors["TimerMinutes"] = ["TimerMinutes must be greater than or equal to 0."];
        }

        if (imageUrl != null && imageUrl.Length > 500)
        {
            errors["ImageUrl"] = ["ImageUrl cannot exceed 500 characters."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }

    private static RecipeStepDto MapToDto(RecipeStep step)
    {
        return new RecipeStepDto
        {
            Id = step.Id,
            StepNumber = step.StepNumber,
            Title = step.Title,
            Description = step.Description,
            TimerMinutes = step.TimerMinutes,
            ImageUrl = step.ImageUrl
        };
    }

    // 1. Xử lý Thêm bước mới (Server tự cấp StepNumber)
    public async Task<RecipeStepDto> Handle(CreateRecipeStepCommand request, CancellationToken cancellationToken)
    {
        ValidateStepFields(request.Title, request.Description, request.TimerMinutes, request.ImageUrl);

        var recipe = await _context.Recipes
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == request.RecipeId && !r.IsDeleted, cancellationToken);

        if (recipe == null)
        {
            throw new NotFoundException($"Recipe with ID '{request.RecipeId}' was not found.");
        }

        VerifyRecipeOwnership(recipe, request.CurrentUserId, request.IsAdmin);

        var activeSteps = recipe.Steps.Where(s => !s.IsDeleted).ToList();
        int assignedStepNumber = activeSteps.Count > 0 ? activeSteps.Max(s => s.StepNumber) + 1 : 1;

        var newStep = new RecipeStep
        {
            Id = Guid.NewGuid(),
            RecipeId = request.RecipeId,
            StepNumber = assignedStepNumber,
            Title = request.Title?.Trim(),
            Description = request.Description.Trim(),
            TimerMinutes = request.TimerMinutes,
            ImageUrl = request.ImageUrl?.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        _context.RecipeSteps.Add(newStep);
        await _context.SaveChangesAsync(cancellationToken);

        await InvalidateRecipeDetailCacheAsync(recipe.Slug, cancellationToken);

        return MapToDto(newStep);
    }

    // 2. Xử lý Cập nhật bước (Không đổi StepNumber, bảo vệ cross-recipe)
    public async Task<RecipeStepDto> Handle(UpdateRecipeStepCommand request, CancellationToken cancellationToken)
    {
        ValidateStepFields(request.Title, request.Description, request.TimerMinutes, request.ImageUrl);

        var recipe = await _context.Recipes
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == request.RecipeId && !r.IsDeleted, cancellationToken);

        if (recipe == null)
        {
            throw new NotFoundException($"Recipe with ID '{request.RecipeId}' was not found.");
        }

        VerifyRecipeOwnership(recipe, request.CurrentUserId, request.IsAdmin);

        var targetStep = recipe.Steps.FirstOrDefault(s => s.Id == request.StepId && !s.IsDeleted);
        if (targetStep == null)
        {
            // Kiểm tra xem step có thuộc recipe khác hoặc bị xóa không
            var existingStep = await _context.RecipeSteps
                .FirstOrDefaultAsync(s => s.Id == request.StepId, cancellationToken);

            if (existingStep != null && existingStep.RecipeId != request.RecipeId)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["StepId"] = [$"Step with ID '{request.StepId}' does not belong to Recipe '{request.RecipeId}'."]
                });
            }

            throw new NotFoundException($"Step with ID '{request.StepId}' was not found in Recipe '{request.RecipeId}'.");
        }

        targetStep.Title = request.Title?.Trim();
        targetStep.Description = request.Description.Trim();
        targetStep.TimerMinutes = request.TimerMinutes;
        targetStep.ImageUrl = request.ImageUrl?.Trim();
        targetStep.UpdatedAt = DateTime.UtcNow;
        // StepNumber giữ nguyên tuyệt đối

        await _context.SaveChangesAsync(cancellationToken);

        await InvalidateRecipeDetailCacheAsync(recipe.Slug, cancellationToken);

        return MapToDto(targetStep);
    }

    // 3. Xử lý Xóa mềm bước và Đánh số lại liên tục (Atomic transaction)
    public async Task<bool> Handle(DeleteRecipeStepCommand request, CancellationToken cancellationToken)
    {
        var recipe = await _context.Recipes
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == request.RecipeId && !r.IsDeleted, cancellationToken);

        if (recipe == null)
        {
            throw new NotFoundException($"Recipe with ID '{request.RecipeId}' was not found.");
        }

        VerifyRecipeOwnership(recipe, request.CurrentUserId, request.IsAdmin);

        var activeSteps = recipe.Steps.Where(s => !s.IsDeleted).ToList();
        var targetStep = activeSteps.FirstOrDefault(s => s.Id == request.StepId);

        if (targetStep == null)
        {
            var existingStep = await _context.RecipeSteps
                .FirstOrDefaultAsync(s => s.Id == request.StepId, cancellationToken);

            if (existingStep != null && existingStep.RecipeId != request.RecipeId)
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["StepId"] = [$"Step with ID '{request.StepId}' does not belong to Recipe '{request.RecipeId}'."]
                });
            }

            throw new NotFoundException($"Step with ID '{request.StepId}' was not found in Recipe '{request.RecipeId}'.");
        }

        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        try
        {
            // 1. Soft-delete target step
            targetStep.IsDeleted = true;
            targetStep.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            // 2. Renumber các active step còn lại theo thứ tự hiện tại
            var remainingSteps = activeSteps
                .Where(s => s.Id != request.StepId)
                .OrderBy(s => s.StepNumber)
                .ToList();

            if (remainingSteps.Count > 0)
            {
                // Phase 1: Gán temporary offset để tránh trùng lặp unique index trên PostgreSQL
                for (int i = 0; i < remainingSteps.Count; i++)
                {
                    remainingSteps[i].StepNumber = 10000 + i;
                    remainingSteps[i].UpdatedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync(cancellationToken);

                // Phase 2: Đánh số tuần tự liên tục 1..N
                for (int i = 0; i < remainingSteps.Count; i++)
                {
                    remainingSteps[i].StepNumber = i + 1;
                    remainingSteps[i].UpdatedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            await InvalidateRecipeDetailCacheAsync(recipe.Slug, cancellationToken);

            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    // 4. Xử lý Sắp xếp lại thứ tự các bước (Atomic transaction + Full active set validation)
    public async Task<List<RecipeStepDto>> Handle(ReorderRecipeStepsCommand request, CancellationToken cancellationToken)
    {
        var recipe = await _context.Recipes
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == request.RecipeId && !r.IsDeleted, cancellationToken);

        if (recipe == null)
        {
            throw new NotFoundException($"Recipe with ID '{request.RecipeId}' was not found.");
        }

        VerifyRecipeOwnership(recipe, request.CurrentUserId, request.IsAdmin);

        var activeSteps = recipe.Steps.Where(s => !s.IsDeleted).ToList();

        // 1. Kiểm tra đủ số lượng active steps
        if (request.StepIds == null || request.StepIds.Count != activeSteps.Count)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["StepIds"] = [$"Reorder request must contain exactly all {activeSteps.Count} active steps of the recipe."]
            });
        }

        // 2. Kiểm tra không được duplicate
        if (request.StepIds.Distinct().Count() != request.StepIds.Count)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["StepIds"] = ["Reorder request contains duplicate step IDs."]
            });
        }

        // 3. Kiểm tra tất cả StepId thuộc về Recipe này và đang active
        var activeStepMap = activeSteps.ToDictionary(s => s.Id);
        foreach (var stepId in request.StepIds)
        {
            if (!activeStepMap.ContainsKey(stepId))
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["StepIds"] = [$"Step with ID '{stepId}' does not belong to the active steps of Recipe '{request.RecipeId}'."]
                });
            }
        }

        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        try
        {
            // Phase 1: Gán temporary offset để tránh trùng lặp unique index trên PostgreSQL
            for (int i = 0; i < request.StepIds.Count; i++)
            {
                var step = activeStepMap[request.StepIds[i]];
                step.StepNumber = 10000 + i;
                step.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync(cancellationToken);

            // Phase 2: Gán StepNumber tuần tự 1..N theo đúng thứ tự request
            for (int i = 0; i < request.StepIds.Count; i++)
            {
                var step = activeStepMap[request.StepIds[i]];
                step.StepNumber = i + 1;
                step.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            await InvalidateRecipeDetailCacheAsync(recipe.Slug, cancellationToken);

            return request.StepIds.Select(id => MapToDto(activeStepMap[id])).ToList();
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
