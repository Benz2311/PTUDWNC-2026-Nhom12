using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Comments.Dtos;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Application.Features.Comments.Commands.CreateComment;

public record CreateCommentCommand(
    Guid RecipeId,
    Guid UserId,
    string Content,
    Guid? ParentCommentId = null
) : IRequest<CommentDto>;

public class CreateCommentCommandHandler : IRequestHandler<CreateCommentCommand, CommentDto>
{
    private readonly IApplicationDbContext _context;

    public CreateCommentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CommentDto> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
    {
        var trimmedContent = request.Content?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedContent))
        {
            throw new ValidationException("Comment content cannot be empty.");
        }

        if (trimmedContent.Length > 1000)
        {
            throw new ValidationException("Comment content cannot exceed 1000 characters.");
        }

        // 1. Kiểm tra công thức phải tồn tại và ở trạng thái Published
        var recipe = await _context.Recipes
            .AsNoTracking()
            .Where(r => r.Id == request.RecipeId && !r.IsDeleted)
            .Select(r => new { r.Id, r.Status })
            .FirstOrDefaultAsync(cancellationToken);

        if (recipe == null)
        {
            throw new NotFoundException($"Recipe with ID '{request.RecipeId}' was not found.");
        }

        if (recipe.Status != RecipeStatus.Published)
        {
            throw new ValidationException("Comments are only permitted on Published recipes.");
        }

        // 2. Kiểm tra người dùng
        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == request.UserId && u.IsActive)
            .Select(u => new { u.Id, u.DisplayName, u.AvatarUrl })
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            throw new ForbiddenException("User not found or inactive.");
        }

        // 3. Nếu là reply, kiểm tra cấu trúc 1 cấp
        Guid? finalParentId = null;
        if (request.ParentCommentId.HasValue && request.ParentCommentId.Value != Guid.Empty)
        {
            var parent = await _context.RecipeComments
                .AsNoTracking()
                .Where(c => c.Id == request.ParentCommentId.Value && !c.IsDeleted)
                .Select(c => new { c.Id, c.RecipeId, c.ParentCommentId, c.IsHidden })
                .FirstOrDefaultAsync(cancellationToken);

            if (parent == null)
            {
                throw new NotFoundException($"Parent comment '{request.ParentCommentId.Value}' was not found.");
            }

            if (parent.RecipeId != request.RecipeId)
            {
                throw new ValidationException("Parent comment does not belong to the specified recipe.");
            }

            if (parent.IsHidden)
            {
                throw new ValidationException("Cannot reply to a hidden comment.");
            }

            // Quy định: Cấu trúc 1 cấp -> nếu reply vào 1 reply, trỏ về comment gốc cấp 1 (parent.ParentCommentId)
            finalParentId = parent.ParentCommentId ?? parent.Id;
        }

        var comment = new RecipeComment(request.RecipeId, request.UserId, trimmedContent, finalParentId);
        await _context.RecipeComments.AddAsync(comment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new CommentDto
        {
            Id = comment.Id,
            RecipeId = comment.RecipeId,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt,
            ParentCommentId = comment.ParentCommentId,
            IsHidden = comment.IsHidden,
            Author = new CommentAuthorDto
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl
            },
            Replies = new List<CommentDto>()
        };
    }
}
