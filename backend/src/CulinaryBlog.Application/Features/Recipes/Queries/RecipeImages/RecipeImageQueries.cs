using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Application.Features.Recipes.Queries.RecipeImages;

// Query: Lấy danh sách tất cả hình ảnh của một công thức
public record GetRecipeImagesQuery(Guid RecipeId) : IRequest<IReadOnlyList<RecipeImageDto>>;

// Query: Lấy thông tin chi tiết 1 hình ảnh
public record GetRecipeImageByIdQuery(Guid Id, Guid RecipeId) : IRequest<RecipeImageDto>;

public class RecipeImageQueryHandler :
    IRequestHandler<GetRecipeImagesQuery, IReadOnlyList<RecipeImageDto>>,
    IRequestHandler<GetRecipeImageByIdQuery, RecipeImageDto>
{
    private readonly IApplicationDbContext _context;

    public RecipeImageQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    // Lấy danh sách hình ảnh của Recipe, sắp xếp IsPrimary lên đầu, rồi SortOrder tăng dần
    public async Task<IReadOnlyList<RecipeImageDto>> Handle(GetRecipeImagesQuery request, CancellationToken cancellationToken)
    {
        var images = await _context.RecipeImages
            .AsNoTracking()
            .Where(img => img.RecipeId == request.RecipeId && !img.IsDeleted)
            .OrderByDescending(img => img.IsPrimary)
            .ThenBy(img => img.SortOrder)
            .Select(img => new RecipeImageDto(
                img.Id,
                img.OriginalUrl,
                img.AltText,
                img.IsPrimary,
                img.SortOrder))
            .ToListAsync(cancellationToken);

        return images;
    }

    // Lấy chi tiết 1 hình ảnh
    public async Task<RecipeImageDto> Handle(GetRecipeImageByIdQuery request, CancellationToken cancellationToken)
    {
        var image = await _context.RecipeImages
            .AsNoTracking()
            .Where(img => img.Id == request.Id && img.RecipeId == request.RecipeId && !img.IsDeleted)
            .Select(img => new RecipeImageDto(
                img.Id,
                img.OriginalUrl,
                img.AltText,
                img.IsPrimary,
                img.SortOrder))
            .FirstOrDefaultAsync(cancellationToken);

        if (image is null)
        {
            throw new NotFoundException($"Recipe image with ID '{request.Id}' was not found.");
        }

        return image;
    }
}