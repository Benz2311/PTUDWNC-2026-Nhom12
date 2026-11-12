using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Features.Comments.Dtos;
using CulinaryBlog.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Application.Features.Comments.Commands.UpdateComment;

public record UpdateCommentCommand(
    Guid CommentId,
    Guid UserId,
    string Content
) : IRequest<CommentDto>;

public class UpdateCommentCommandHandler : IRequestHandler<UpdateCommentCommand, CommentDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateCommentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CommentDto> Handle(UpdateCommentCommand request, CancellationToken cancellationToken)
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

        var comment = await _context.RecipeComments
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == request.CommentId && !c.IsDeleted, cancellationToken);

        if (comment == null)
        {
            throw new NotFoundException($"Comment with ID '{request.CommentId}' was not found.");
        }

        if (comment.IsHidden)
        {
            throw new ValidationException("Cannot edit a hidden comment.");
        }

        // Quy tắc: Người dùng thông thường không thể chỉnh sửa bình luận của tài khoản khác
        if (comment.UserId != request.UserId)
        {
            throw new ForbiddenException("You are not allowed to edit another user's comment.");
        }

        comment.Content = trimmedContent;
        comment.UpdatedAt = DateTime.UtcNow;

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
                Id = comment.User.Id,
                DisplayName = comment.User.DisplayName,
                AvatarUrl = comment.User.AvatarUrl
            },
            Replies = new List<CommentDto>()
        };
    }
}
