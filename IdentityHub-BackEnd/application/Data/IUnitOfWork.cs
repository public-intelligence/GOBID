using Microsoft.EntityFrameworkCore;
using IdentityHub.Domain.Response;

namespace IdentityHub.Application.Data;

public interface IUnitOfWork<out TContext>
    where TContext : DbContext
{
    public TContext Context { get; }

    public Task<Result> BeginTransactionAsync();
    public Task<Result> RollbackAsync();
    public Task<Result> CommitAsync();
}
