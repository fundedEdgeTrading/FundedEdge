using FundedEdge.Infrastructure.Services;

namespace FundedEdge.Application.Tests;

public class TradeSetupServiceTests
{
    private const string UserId = "user-1";

    private static TradeSetupService BuildService(InMemoryDbContextFactory factory) =>
        new(factory, new FakeCurrentUserAccessor(UserId));

    [Fact]
    public async Task CreateAsync_ThenGetAllAsync_ReturnsSetupOrderedByName()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var sut = BuildService(factory);

        await sut.CreateAsync("Breakout apertura");
        await sut.CreateAsync("Pullback EMA20");

        var setups = await sut.GetAllAsync();

        Assert.Equal(2, setups.Count);
        Assert.Equal("Breakout apertura", setups[0].Name);
        Assert.Equal("Pullback EMA20", setups[1].Name);
    }

    [Fact]
    public async Task CreateAsync_DuplicateNameForSameUser_Throws()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var sut = BuildService(factory);

        await sut.CreateAsync("Breakout apertura");

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateAsync("Breakout apertura"));
    }

    [Fact]
    public async Task DeleteAsync_RemovesSetup()
    {
        var factory = new InMemoryDbContextFactory(Guid.NewGuid().ToString());
        var sut = BuildService(factory);

        var id = await sut.CreateAsync("Breakout apertura");
        await sut.DeleteAsync(id);

        Assert.Empty(await sut.GetAllAsync());
    }
}
