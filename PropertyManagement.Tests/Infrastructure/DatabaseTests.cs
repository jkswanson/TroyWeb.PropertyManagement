using PropertyManagement.Tests.Infrastructure;

namespace PropertyManagement.Tests;

[TestClass]
public class DatabaseTests
{
    [TestMethod]
    public async Task CreateAsync_CreatesDatabaseSuccessfully()
    {
        await using var database = await TestDatabase.CreateAsync();
        Assert.IsTrue(await database.Context.Database.CanConnectAsync());
    }
}
