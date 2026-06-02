using NewbieCoder.Core.Interfaces.Repositories;
using NewbieCoder.Infrastructure.Data;

namespace NewbieCoder.Infrastructure.UnitOfWork;

public class EfUnitOfWork(AppDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    public ValueTask DisposeAsync() => context.DisposeAsync();
}
