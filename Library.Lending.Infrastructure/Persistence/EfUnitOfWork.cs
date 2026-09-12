using Library.Lending.Application.Abstractions.Persistence;
using Library.Lending.Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Library.Lending.Infrastructure.Persistence;

internal sealed class EfUnitOfWork(LendingDbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException(
                "Another request changed the same book or loan at the same time. Retry the operation.",
                exception);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres)
        {
            throw new ConcurrencyConflictException(
                $"A conflicting record was created at the same time (constraint '{postgres.ConstraintName}'). Retry the operation.",
                exception);
        }
    }
}
