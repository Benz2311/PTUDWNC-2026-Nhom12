using CulinaryBlog.Application.Exceptions;
using CulinaryBlog.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Application.Features.Comments.Commands.DeleteComment;

public record DeleteCommentCommand(
    Guid CommentId,
    Guid UserId,
    bool IsAdmin = false
) : IRequest<bool>;

public class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteCommentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await _context.RecipeComments
            .Include(c => c.Replies)
            .FirstOrDefaultAsync(c => c.Id == request.CommentId && !c.IsDeleted, cancellationToken);

        if (comment == null)
        {
            throw new NotFoundException($"Comment with ID '{request.CommentId}' was not found.");
        }

        if (comment.UserId != request.UserId && !request.IsAdmin)
        {
            throw new ForbiddenException("You are not allowed to delete another user's comment.");
        }

        // Soft delete comment and any replies
        comment.IsDeleted = true;
        comment.UpdatedAt = DateTime.UtcNow;

        foreach (var reply in comment.Replies)
        {
            reply.IsDeleted = true;
            reply.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
