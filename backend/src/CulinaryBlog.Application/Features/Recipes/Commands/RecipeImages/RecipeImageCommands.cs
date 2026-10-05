using ICacheService = CulinaryBlog.Application.Common.Interfaces.ICacheService;
using CulinaryBlog.Application.Common.Utilities;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Recipes.Dtos;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.Application.Features.Recipes.Commands.RecipeImages;

// 1. Command Thêm ảnh mới cho Recipe (trực tiếp từ URL đã có)
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

// 2. Command Tải lên tệp ảnh (Multipart File Upload qua IStorageService)
public record UploadRecipeImageCommand(
    Guid RecipeId,
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileLength,
    string? AltText = null,
    bool? IsPrimary = null,
    Guid? CurrentUserId = null,
    bool IsAdmin = false
) : IRequest<RecipeImageDto>;

// 3. Command Cập nhật ảnh Recipe (PATCH: AltText, OrderIndex, IsPrimary)
public record UpdateRecipeImageCommand(
    Guid RecipeId,
    Guid ImageId,
    string? AltText = null,
    int? OrderIndex = null,
    bool? IsPrimary = null,
    Guid? CurrentUserId = null,
    bool IsAdmin = false
) : IRequest<RecipeImageDto>;

// 4. Command Đổi ảnh đại diện (Set Primary)
public record SetPrimaryRecipeImageCommand(
    Guid RecipeId,
    Guid ImageId,
    Guid? CurrentUserId = null,
    bool IsAdmin = false
) : IRequest<RecipeImageDto>;

// 5. Command Xóa mềm ảnh Recipe (Soft Delete + Tự động chuyển Primary cho ảnh có OrderIndex nhỏ nhất)
public record DeleteRecipeImageCommand(
    Guid RecipeId,
    Guid ImageId,
    Guid? CurrentUserId = null,
    bool IsAdmin = false
) : IRequest<bool>;

public class RecipeImageCommandHandler :
    IRequestHandler<AddRecipeImageCommand, RecipeImageDto>,
    IRequestHandler<UploadRecipeImageCommand, RecipeImageDto>,
    IRequestHandler<UpdateRecipeImageCommand, RecipeImageDto>,
    IRequestHandler<SetPrimaryRecipeImageCommand, RecipeImageDto>,
    IRequestHandler<DeleteRecipeImageCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cacheService;
    private readonly IServiceProvider? _serviceProvider;
    private IStorageService? _storageService;

    public RecipeImageCommandHandler(
        IApplicationDbContext context,
        ICacheService cacheService,
        IServiceProvider? serviceProvider = null)
    {
        _context = context;
        _cacheService = cacheService;
        _serviceProvider = serviceProvider;
    }

    public void SetStorageServiceForTesting(IStorageService storageService)
    {
        _storageService = storageService;
    }

    private static void VerifyRecipeOwnership(Recipe recipe, Guid? currentUserId, bool isAdmin)
    {
        if (isAdmin)
        {
            return;
        }

        if (!currentUserId.HasValue || currentUserId.Value != recipe.AuthorId)
        {
            throw new ForbiddenException("You do not have permission to modify images for this recipe.");
        }
    }

    private async Task InvalidateRecipeDetailCacheAsync(string recipeSlug, CancellationToken cancellationToken)
    {
        var cacheKey = $"recipe:{recipeSlug.Trim().ToLowerInvariant()}";
        await _cacheService.RemoveAsync(cacheKey, cancellationToken);
    }

    // Xử lý Thêm ảnh mới theo URL sẵn có (dành cho Seeder hoặc Integration test)
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

        var errors = new Dictionary<string, string[]>();
        if (request.AltText != null && request.AltText.Length > 200)
        {
            errors["AltText"] = ["AltText cannot exceed 200 characters."];
        }
        if (request.OrderIndex.HasValue && request.OrderIndex.Value < 0)
        {
            errors["OrderIndex"] = ["OrderIndex must be greater than or equal to 0."];
        }
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

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

            // Invalidate Recipe Detail cache sau khi transaction đã commit thành công
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

    // Xử lý Tải lên tệp ảnh (Multipart Form: validate MIME, magic bytes, size 5MB, upload MinIO)
    public async Task<RecipeImageDto> Handle(UploadRecipeImageCommand request, CancellationToken cancellationToken)
    {
        var recipe = await _context.Recipes
            .Include(r => r.Images)
            .FirstOrDefaultAsync(r => r.Id == request.RecipeId && !r.IsDeleted, cancellationToken);

        if (recipe == null)
        {
            throw new NotFoundException($"Recipe with ID '{request.RecipeId}' was not found.");
        }

        VerifyRecipeOwnership(recipe, request.CurrentUserId, request.IsAdmin);

        var errors = new Dictionary<string, string[]>();
        if (request.AltText != null && request.AltText.Length > 200)
        {
            errors["AltText"] = ["AltText cannot exceed 200 characters."];
        }
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        // Validate tệp ảnh nghiêm ngặt: dung lượng <= 5MB, MIME type, Magic bytes / File signature
        var validationResult = ImageValidator.Validate(
            request.FileStream,
            request.ContentType,
            request.FileName,
            request.FileLength);

        if (!validationResult.IsValid)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["File"] = [validationResult.ErrorMessage ?? "Tệp hình ảnh không hợp lệ."]
            });
        }

        // Upload file lên Object Storage (MinIO)
        var fileId = Guid.NewGuid();
        var objectKey = $"recipes/{recipe.Id}/images/{fileId}{validationResult.Extension}";
        var config = _serviceProvider?.GetService<IConfiguration>();
        var bucketName = config?.GetSection("Storage")["DefaultBucket"] ?? "culinaryblog";

        IStorageService? storage = _storageService;
        if (storage == null && _serviceProvider != null)
        {
            try
            {
                storage = _serviceProvider.GetService<IStorageService>();
            }
            catch
            {
                // Storage service chưa được cấu hình đầy đủ (ví dụ môi trường test không có cấu hình S3)
                storage = null;
            }
        }

        string publicUrl;
        if (storage != null)
        {
            try
            {
                publicUrl = await storage.UploadAsync(
                    bucketName,
                    objectKey,
                    request.FileStream,
                    validationResult.DetectedMimeType,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Dịch vụ lưu trữ tệp (File Storage) hiện không khả dụng. Vui lòng thử lại sau.", ex);
            }
        }
        else
        {
            // Fallback URL nếu service storage chưa được cung cấp
            publicUrl = $"/storage/{bucketName}/{objectKey}";
        }

        var activeImages = recipe.Images.Where(img => !img.IsDeleted).ToList();

        // SRS FR-RCP-008: Ảnh đầu tiên của Recipe mặc định là Primary
        bool shouldBePrimary;
        if (activeImages.Count == 0)
        {
            shouldBePrimary = true;
        }
        else
        {
            shouldBePrimary = request.IsPrimary ?? false;
        }

        // OrderIndex tự động tăng dựa trên các ảnh đang hoạt động
        int assignedOrderIndex = activeImages.Count > 0 ? activeImages.Max(x => x.OrderIndex) + 1 : 0;

        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        try
        {
            // Khi đặt primary, tất cả ảnh khác được bỏ primary trong cùng transaction
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
                Id = fileId,
                RecipeId = request.RecipeId,
                OriginalUrl = publicUrl,
                AltText = request.AltText,
                IsPrimary = shouldBePrimary,
                OrderIndex = assignedOrderIndex,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.RecipeImages.Add(newImage);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

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

    // Xử lý Cập nhật ảnh theo PATCH (AltText, OrderIndex, IsPrimary)
    public async Task<RecipeImageDto> Handle(UpdateRecipeImageCommand request, CancellationToken cancellationToken)
    {
        var recipe = await _context.Recipes
            .Include(r => r.Images)
            .FirstOrDefaultAsync(r => r.Id == request.RecipeId && !r.IsDeleted, cancellationToken);

        if (recipe == null)
        {
            throw new NotFoundException($"Recipe with ID '{request.RecipeId}' was not found.");
        }

        VerifyRecipeOwnership(recipe, request.CurrentUserId, request.IsAdmin);

        var errors = new Dictionary<string, string[]>();
        if (request.AltText != null && request.AltText.Length > 200)
        {
            errors["AltText"] = ["AltText cannot exceed 200 characters."];
        }
        if (request.OrderIndex.HasValue && request.OrderIndex.Value < 0)
        {
            errors["OrderIndex"] = ["OrderIndex must be greater than or equal to 0."];
        }
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        var activeImages = recipe.Images.Where(img => !img.IsDeleted).ToList();
        var targetImage = activeImages.FirstOrDefault(img => img.Id == request.ImageId);

        if (targetImage == null)
        {
            throw new NotFoundException($"Image with ID '{request.ImageId}' was not found in Recipe.");
        }

        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        try
        {
            bool modified = false;

            if (request.AltText != null)
            {
                targetImage.AltText = request.AltText;
                modified = true;
            }

            if (request.OrderIndex.HasValue)
            {
                targetImage.OrderIndex = request.OrderIndex.Value;
                modified = true;
            }

            if (request.IsPrimary.HasValue)
            {
                if (request.IsPrimary.Value)
                {
                    if (!targetImage.IsPrimary)
                    {
                        // 1. Hạ cờ Primary của tất cả các ảnh khác trước để tránh vi phạm Unique Index
                        foreach (var img in activeImages.Where(img => img.Id != request.ImageId && img.IsPrimary))
                        {
                            img.IsPrimary = false;
                            img.UpdatedAt = DateTime.UtcNow;
                        }
                        await _context.SaveChangesAsync(cancellationToken);

                        // 2. Nâng cờ Primary của ảnh được chọn
                        targetImage.IsPrimary = true;
                        modified = true;
                    }
                }
                else
                {
                    // Nếu bỏ cờ Primary trên ảnh đang là Primary -> Tự động chuyển cho ảnh có OrderIndex nhỏ nhất còn lại
                    if (targetImage.IsPrimary)
                    {
                        var remaining = activeImages
                            .Where(img => img.Id != request.ImageId)
                            .OrderBy(img => img.OrderIndex)
                            .FirstOrDefault();

                        if (remaining != null)
                        {
                            targetImage.IsPrimary = false;
                            await _context.SaveChangesAsync(cancellationToken);

                            remaining.IsPrimary = true;
                            remaining.UpdatedAt = DateTime.UtcNow;
                            await _context.SaveChangesAsync(cancellationToken);
                            modified = true;
                        }
                    }
                }
            }

            if (modified)
            {
                targetImage.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
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

    // Xử lý Đổi ảnh đại diện theo SRS FR-RCP-008
    public async Task<RecipeImageDto> Handle(SetPrimaryRecipeImageCommand request, CancellationToken cancellationToken)
    {
        return await Handle(
            new UpdateRecipeImageCommand(request.RecipeId, request.ImageId, IsPrimary: true, CurrentUserId: request.CurrentUserId, IsAdmin: request.IsAdmin),
            cancellationToken);
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

            // Invalidate Recipe Detail cache sau khi transaction đã commit thành công
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
