using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Comments.Dtos;
using CulinaryBlog.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Application.Features.Comments.Queries.GetRecipeComments;

public record GetRecipeCommentsQuery(
    Guid RecipeId,
    int Page = 1,
    int PageSize = 10,
    bool IsAdmin = false
) : IRequest<CommentPagedResultDto>;

public class GetRecipeCommentsQueryHandler : IRequestHandler<GetRecipeCommentsQuery, CommentPagedResultDto>
{
    private readonly IApplicationDbContext _context;

    public GetRecipeCommentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CommentPagedResultDto> Handle(GetRecipeCommentsQuery request, CancellationToken cancellationToken)
    {
        var recipeExists = await _context.Recipes
            .AnyAsync(r => r.Id == request.RecipeId && !r.IsDeleted, cancellationToken);

        if (!recipeExists)
        {
            throw new NotFoundException($"Recipe with ID '{request.RecipeId}' was not found.");
        }

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        // Root comments query (ParentCommentId == null and not deleted)
        var rootCommentsQuery = _context.RecipeComments
            .AsNoTracking()
            .Where(c => c.RecipeId == request.RecipeId && c.ParentCommentId == null && !c.IsDeleted);

        var totalCount = await rootCommentsQuery.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        var rootComments = await rootCommentsQuery
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.RecipeId,
                c.Content,
                c.CreatedAt,
                c.UpdatedAt,
                c.ParentCommentId,
                c.IsHidden,
                Author = new CommentAuthorDto
                {
                    Id = c.User.Id,
                    DisplayName = c.User.DisplayName,
                    AvatarUrl = c.User.AvatarUrl
                }
            })
            .ToListAsync(cancellationToken);

        var rootCommentIds = rootComments.Select(c => c.Id).ToList();

        // Query replies for these root comments
        var replies = await _context.RecipeComments
            .AsNoTracking()
            .Where(c => c.RecipeId == request.RecipeId && c.ParentCommentId.HasValue && rootCommentIds.Contains(c.ParentCommentId.Value) && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.RecipeId,
                c.Content,
                c.CreatedAt,
                c.UpdatedAt,
                c.ParentCommentId,
                c.IsHidden,
                Author = new CommentAuthorDto
                {
                    Id = c.User.Id,
                    DisplayName = c.User.DisplayName,
                    AvatarUrl = c.User.AvatarUrl
                }
            })
            .ToListAsync(cancellationToken);

        var repliesLookup = replies
            .GroupBy(r => r.ParentCommentId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(r => new CommentDto
            {
                Id = r.Id,
                RecipeId = r.RecipeId,
                Content = r.IsHidden && !request.IsAdmin
                    ? "[Bình luận đã bị ẩn do vi phạm tiêu chuẩn cộng đồng]"
                    : r.Content,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                ParentCommentId = r.ParentCommentId,
                IsHidden = r.IsHidden,
                Author = r.Author,
                Replies = new List<CommentDto>()
            }).ToList());

        var items = rootComments.Select(c => new CommentDto
        {
            Id = c.Id,
            RecipeId = c.RecipeId,
            Content = c.IsHidden && !request.IsAdmin
                ? "[Bình luận đã bị ẩn do vi phạm tiêu chuẩn cộng đồng]"
                : c.Content,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            ParentCommentId = c.ParentCommentId,
            IsHidden = c.IsHidden,
            Author = c.Author,
            Replies = repliesLookup.TryGetValue(c.Id, out var repList) ? repList : new List<CommentDto>()
        }).ToList();

        return new CommentPagedResultDto
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1 && page <= totalPages
        };
    }
}
