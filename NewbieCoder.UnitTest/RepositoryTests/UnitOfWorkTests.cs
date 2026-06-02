using Microsoft.EntityFrameworkCore;
using NewbieCoder.Infrastructure.Data;
using NewbieCoder.Infrastructure.UnitOfWork;

namespace NewbieCoder.UnitTest.RepositoryTests;

public class UnitOfWorkTests
{
    [Fact]
    public async Task SaveChangesAsync_ReturnsZero_WhenNoChanges()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);
        await using var unitOfWork = new EfUnitOfWork(context);

        var result = await unitOfWork.SaveChangesAsync();

        Assert.Equal(0, result);
    }
}
