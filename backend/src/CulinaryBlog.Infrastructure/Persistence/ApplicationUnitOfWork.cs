using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CulinaryBlog.Infrastructure.Persistence;

public class ApplicationUnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public ApplicationUnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            var entry = exception.Entries.FirstOrDefault();
            var keyProperty = entry?.Metadata.FindPrimaryKey()?.Properties.FirstOrDefault();
            var entityId = keyProperty is null
                ? "unknown"
                : entry!.Property(keyProperty.Name).CurrentValue ?? "unknown";

            throw new ConcurrencyConflictException(
                entry?.Metadata.ClrType.Name ?? "Record",
                entityId,
                exception);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException postgresException &&
            postgresException.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new ConflictException("A unique Recipe value already exists.");
        }
    }
}
