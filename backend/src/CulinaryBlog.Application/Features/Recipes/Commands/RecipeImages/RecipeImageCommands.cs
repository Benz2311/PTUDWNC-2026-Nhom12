using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes.Dtos;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Application.Features.Recipes.Commands.RecipeImages;

// 1. Command Thêm ảnh mới cho Recipe
public record AddRecipeImageCommand(
    Guid RecipeId,
    string OriginalUrl,
    string? MediumUrl = null,
    string? ThumbnailUrl = null,
    string? AltText = null,
    bool? IsPrimary = null,
    int? OrderIndex = null,
    Guid? CurrentUserId = null,
    bool IsAdmin = false
) : IRequest<RecipeImageDto>;

// 2. Command Đổi ảnh đại diện (Set Primary)
public record SetPrimaryRecipeImageCommand(
    Guid RecipeId,
    Guid ImageId,
    Guid? CurrentUserId = null,
    bool IsAdmin = false
) : IRequest<RecipeImageDto>;

// 3. Command Xóa mềm ảnh Recipe (Soft Delete + Tự động chuyển Primary cho ảnh có OrderIndex nhỏ nhất)
public record DeleteRecipeImageCommand(
    Guid RecipeId,
    Guid ImageId,
    Guid? CurrentUserId = null,
    bool IsAdmin = false
) : IRequest<bool>;

public class RecipeImageCommandHandler :
    IRequestHandler<AddRecipeImageCommand, RecipeImageDto>,
    IRequestHandler<SetPrimaryRecipeImageCommand, RecipeImageDto>,
    IRequestHandler<DeleteRecipeImageCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cacheService;

    public RecipeImageCommandHandler(IApplicationDbContext context, ICacheService cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }

    /// <summary>
    /// Kiểm tra quyền tác giả hoặc Admin khi thực hiện mutation hình ảnh (Phase 13).
    /// Nguyên tắc bảo vệ tầng Business/Application:
    /// - Nếu IsAdmin == true => Allow.
    /// - Nếu CurrentUserId == null => throw ForbiddenException (chặn Guest).
    /// - Nếu CurrentUserId != Recipe.AuthorId => throw ForbiddenException (chặn Non-owner).
    /// - Nếu CurrentUserId == Recipe.AuthorId => Allow (Owner).
    /// Tuyệt đối không tin tưởng hoặc nhận AuthorId từ request body.
    /// </summary>
    private static void VerifyRecipeOwnership(Recipe recipe, Guid? currentUserId, bool isAdmin)
    {
        // 1. Admin được phép toàn quyền can thiệp
        if (isAdmin)
        {
            return;
        }

        // 2. Chặn Guest (CurrentUserId == null): Bắt buộc phải có định danh người dùng
        if (!currentUserId.HasValue)
        {
            throw new ForbiddenException("Authentication is required to modify recipe images.");
        }

        // 3. Chặn Non-owner: Người dùng không phải là tác giả của công thức
        if (recipe.AuthorId != currentUserId.Value)
        {
            throw new ForbiddenException("You do not have permission to modify images for this recipe.");
        }

        // 4. CurrentUserId == Recipe.AuthorId => Cho phép
    }

    /// <summary>
    /// Invalidate Recipe Detail cache sau khi mutation hình ảnh thành công (Major-02).
    /// Bọc try-catch fail-safe để lỗi Redis không làm hỏng mutation DB.
    /// </summary>
    private async Task InvalidateRecipeDetailCacheAsync(string? slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return;
        }

        try
        {
            var cacheKey = $"recipe:{slug.Trim().ToLowerInvariant()}";
            await _cacheService.RemoveAsync(cacheKey, cancellationToken);
        }
        catch
        {
            // Fail-safe: lỗi cache không làm gián đoạn hay thất bại thao tác mutation database
        }
    }

    // Xử lý Thêm ảnh mới theo SRS FR-RCP-008
    public async Task<RecipeImageDto> Handle(AddRecipeImageCommand request, CancellationToken cancellationToken)
    {
        var recipe = await _context.Recipes
            .Include(r => r.Images)
            .FirstOrDefaultAsync(r => r.Id == request.RecipeId && !r.IsDeleted, cancellationToken);

        if (recipe == null)
        {
            throw new NotFoundException($"Recipe with ID '{request.RecipeId}' was not found.");
        }

        // Kiểm tra quyền tác giả sở hữu hoặc Admin
        VerifyRecipeOwnership(recipe, request.CurrentUserId, request.IsAdmin);

        var activeImages = recipe.Images.Where(img => !img.IsDeleted).ToList();

        // SRS FR-RCP-008: Ảnh đầu tiên mặc định primary
        bool shouldBePrimary;
        if (activeImages.Count == 0)
        {
            shouldBePrimary = true;
        }
        else
        {
            shouldBePrimary = request.IsPrimary ?? false;
        }

        // Xác định OrderIndex
        int assignedOrderIndex = request.OrderIndex ?? (activeImages.Count > 0 ? activeImages.Max(x => x.OrderIndex) + 1 : 0);

        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        try
        {
            // SRS FR-RCP-008: Khi đặt primary, tất cả ảnh khác được bỏ primary trong cùng transaction
            if (shouldBePrimary && activeImages.Count > 0)
            {
                foreach (var img in activeImages.Where(img => img.IsPrimary))
                {
                    img.IsPrimary = false;
                    img.UpdatedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync(cancellationToken);
            }

            var newImage = new RecipeImage
            {
                RecipeId = request.RecipeId,
                OriginalUrl = request.OriginalUrl,
                MediumUrl = request.MediumUrl,
                ThumbnailUrl = request.ThumbnailUrl,
                AltText = request.AltText,
                IsPrimary = shouldBePrimary,
                OrderIndex = assignedOrderIndex,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.RecipeImages.Add(newImage);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // Major-02: Invalidate Recipe Detail cache sau khi transaction đã commit thành công
            await InvalidateRecipeDetailCacheAsync(recipe.Slug, cancellationToken);

            return new RecipeImageDto
            {
                Id = newImage.Id,
                OriginalUrl = newImage.OriginalUrl,
                MediumUrl = newImage.MediumUrl,
                ThumbnailUrl = newImage.ThumbnailUrl,
                AltText = newImage.AltText,
                IsPrimary = newImage.IsPrimary,
                OrderIndex = newImage.OrderIndex
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    // Xử lý Đổi ảnh đại diện theo SRS FR-RCP-008
    public async Task<RecipeImageDto> Handle(SetPrimaryRecipeImageCommand request, CancellationToken cancellationToken)
    {
        var recipe = await _context.Recipes
            .Include(r => r.Images)
            .FirstOrDefaultAsync(r => r.Id == request.RecipeId && !r.IsDeleted, cancellationToken);

        if (recipe == null)
        {
            throw new NotFoundException($"Recipe with ID '{request.RecipeId}' was not found.");
        }

        // Kiểm tra quyền tác giả sở hữu hoặc Admin
        VerifyRecipeOwnership(recipe, request.CurrentUserId, request.IsAdmin);

        var activeImages = recipe.Images.Where(img => !img.IsDeleted).ToList();
        var targetImage = activeImages.FirstOrDefault(img => img.Id == request.ImageId);

        if (targetImage == null)
        {
            throw new NotFoundException($"Image with ID '{request.ImageId}' was not found in Recipe.");
        }

        if (targetImage.IsPrimary)
        {
            return new RecipeImageDto
            {
                Id = targetImage.Id,
                OriginalUrl = targetImage.OriginalUrl,
                MediumUrl = targetImage.MediumUrl,
                ThumbnailUrl = targetImage.ThumbnailUrl,
                AltText = targetImage.AltText,
                IsPrimary = targetImage.IsPrimary,
                OrderIndex = targetImage.OrderIndex
            };
        }

        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        try
        {
            // 1. Hạ cờ Primary của tất cả các ảnh khác trước
            foreach (var img in activeImages.Where(img => img.Id != request.ImageId && img.IsPrimary))
            {
                img.IsPrimary = false;
                img.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync(cancellationToken);

            // 2. Nâng cờ Primary của ảnh được chọn
            targetImage.IsPrimary = true;
            targetImage.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            // Major-02: Invalidate Recipe Detail cache sau khi transaction đã commit thành công
            await InvalidateRecipeDetailCacheAsync(recipe.Slug, cancellationToken);

            return new RecipeImageDto
            {
                Id = targetImage.Id,
                OriginalUrl = targetImage.OriginalUrl,
                MediumUrl = targetImage.MediumUrl,
                ThumbnailUrl = targetImage.ThumbnailUrl,
                AltText = targetImage.AltText,
                IsPrimary = targetImage.IsPrimary,
                OrderIndex = targetImage.OrderIndex
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    // Xử lý Xóa mềm ảnh và fallback primary theo SRS FR-RCP-008
    public async Task<bool> Handle(DeleteRecipeImageCommand request, CancellationToken cancellationToken)
    {
        var recipe = await _context.Recipes
            .Include(r => r.Images)
            .FirstOrDefaultAsync(r => r.Id == request.RecipeId && !r.IsDeleted, cancellationToken);

        if (recipe == null)
        {
            throw new NotFoundException($"Recipe with ID '{request.RecipeId}' was not found.");
        }

        // Kiểm tra quyền tác giả sở hữu hoặc Admin
        VerifyRecipeOwnership(recipe, request.CurrentUserId, request.IsAdmin);

        var activeImages = recipe.Images.Where(img => !img.IsDeleted).ToList();
        var targetImage = activeImages.FirstOrDefault(img => img.Id == request.ImageId);

        if (targetImage == null)
        {
            throw new NotFoundException($"Image with ID '{request.ImageId}' was not found in Recipe.");
        }

        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        try
        {
            var wasPrimary = targetImage.IsPrimary;

            // 1. Xóa mềm và hạ cờ primary của targetImage trước
            targetImage.IsDeleted = true;
            targetImage.IsPrimary = false;
            targetImage.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            // 2. SRS FR-RCP-008: Khi xóa ảnh primary, ảnh có orderIndex nhỏ nhất còn lại trở thành primary
            if (wasPrimary)
            {
                var remainingImages = activeImages
                    .Where(img => img.Id != request.ImageId)
                    .OrderBy(img => img.OrderIndex)
                    .ToList();

                if (remainingImages.Count > 0)
                {
                    remainingImages[0].IsPrimary = true;
                    remainingImages[0].UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }

            await transaction.CommitAsync(cancellationToken);

            // Major-02: Invalidate Recipe Detail cache sau khi transaction đã commit thành công
            await InvalidateRecipeDetailCacheAsync(recipe.Slug, cancellationToken);

            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
