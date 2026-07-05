using Xunit;
using Moq;
using CatalogService.Services;
using CatalogService.Interfaces;
using CatalogService.Services.Cache;
using SharedModels.Models;
using SharedModels.Dtos;

namespace CatalogService.Tests;

public class GiftsServiceTests
{
    private readonly Mock<IGiftsRepository> _repositoryMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly Mock<ILogger<GiftsService>> _loggerMock;
    private readonly GiftsService _service;

    public GiftsServiceTests()
    {
        _repositoryMock = new Mock<IGiftsRepository>();
        _cacheMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<GiftsService>>();
        _service = new GiftsService(_repositoryMock.Object, _cacheMock.Object, _loggerMock.Object);
    }

    // ── GetAllGiftsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetAllGiftsAsync_CacheHit_ReturnsCachedList()
    {
        var cached = new List<DtoGifts> { new DtoGifts { GiftId = 1, Name = "Laptop" } };
        _cacheMock.Setup(c => c.GetAsync<List<DtoGifts>>("gifts:all")).ReturnsAsync(cached);

        var result = (await _service.GetAllGiftsAsync()).ToList();

        Assert.Single(result);
        _repositoryMock.Verify(r => r.GetAllGiftsAsync(), Times.Never);
    }

    [Fact]
    public async Task GetAllGiftsAsync_CacheMiss_FetchesFromRepository()
    {
        _cacheMock.Setup(c => c.GetAsync<List<DtoGifts>>("gifts:all")).ReturnsAsync((List<DtoGifts>?)null);
        _repositoryMock.Setup(r => r.GetAllGiftsAsync())
            .ReturnsAsync(new List<Gift>
            {
                new Gift { GiftId = 1, Name = "Laptop", Price = 999m, Quantity = 5 }
            });

        var result = (await _service.GetAllGiftsAsync()).ToList();

        Assert.Single(result);
        _cacheMock.Verify(c => c.SetAsync("gifts:all", It.IsAny<List<DtoGifts>>(), null), Times.Once);
    }

    // ── GetGiftByIdAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetGiftByIdAsync_CacheHit_ReturnsCachedGift()
    {
        var cached = new DtoGifts { GiftId = 1, Name = "Phone" };
        _cacheMock.Setup(c => c.GetAsync<DtoGifts>("gifts:1")).ReturnsAsync(cached);

        var result = await _service.GetGiftByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Phone", result!.Name);
        _repositoryMock.Verify(r => r.GetGiftByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetGiftByIdAsync_CacheMiss_FetchesFromRepository()
    {
        _cacheMock.Setup(c => c.GetAsync<DtoGifts>("gifts:1")).ReturnsAsync((DtoGifts?)null);
        _repositoryMock.Setup(r => r.GetGiftByIdAsync(1))
            .ReturnsAsync(new Gift { GiftId = 1, Name = "Phone", Price = 499m, Quantity = 3 });

        var result = await _service.GetGiftByIdAsync(1);

        Assert.NotNull(result);
        _cacheMock.Verify(c => c.SetAsync("gifts:1", It.IsAny<DtoGifts>(), null), Times.Once);
    }

    [Fact]
    public async Task GetGiftByIdAsync_InvalidId_ReturnsNull()
    {
        var result = await _service.GetGiftByIdAsync(0);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetGiftByIdAsync_NotFound_ReturnsNull()
    {
        _cacheMock.Setup(c => c.GetAsync<DtoGifts>("gifts:99")).ReturnsAsync((DtoGifts?)null);
        _repositoryMock.Setup(r => r.GetGiftByIdAsync(99)).ReturnsAsync((Gift?)null);

        var result = await _service.GetGiftByIdAsync(99);

        Assert.Null(result);
    }

    // ── CreateGiftAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateGiftAsync_ValidRequest_ReturnsNewId()
    {
        _repositoryMock.Setup(r => r.CreateGiftAsync(It.IsAny<Gift>())).ReturnsAsync(7);

        var request = new DtoCreateGiftRequest { Name = "Tablet", Price = 299m, Quantity = 10, DonorId = 1, CategoryId = 1 };
        var id = await _service.CreateGiftAsync(request);

        Assert.Equal(7, id);
        _cacheMock.Verify(c => c.RemoveAsync("gifts:all"), Times.Once);
    }

    [Fact]
    public async Task CreateGiftAsync_EmptyName_ThrowsArgumentException()
    {
        var request = new DtoCreateGiftRequest { Name = "", Price = 100m, Quantity = 1 };
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateGiftAsync(request));
    }

    [Fact]
    public async Task CreateGiftAsync_ZeroPrice_ThrowsArgumentException()
    {
        var request = new DtoCreateGiftRequest { Name = "Gift", Price = 0m, Quantity = 1 };
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateGiftAsync(request));
    }

    [Fact]
    public async Task CreateGiftAsync_NegativeQuantity_ThrowsArgumentException()
    {
        var request = new DtoCreateGiftRequest { Name = "Gift", Price = 10m, Quantity = -1 };
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateGiftAsync(request));
    }

    // ── UpdateGiftAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task UpdateGiftAsync_ExistingGift_InvalidatesBothCacheKeys()
    {
        _repositoryMock.Setup(r => r.GetGiftByIdAsync(1))
            .ReturnsAsync(new Gift { GiftId = 1, Name = "Old", Price = 50m, Quantity = 5 });
        _repositoryMock.Setup(r => r.UpdateGiftAsync(It.IsAny<Gift>())).ReturnsAsync(true);

        var result = await _service.UpdateGiftAsync(1, new DtoUpdateGiftRequest { Price = 75m });

        Assert.True(result);
        _cacheMock.Verify(c => c.RemoveAsync("gifts:1"), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync("gifts:all"), Times.Once);
    }

    [Fact]
    public async Task UpdateGiftAsync_NotFound_ReturnsFalse()
    {
        _repositoryMock.Setup(r => r.GetGiftByIdAsync(99)).ReturnsAsync((Gift?)null);

        var result = await _service.UpdateGiftAsync(99, new DtoUpdateGiftRequest { Price = 10m });

        Assert.False(result);
    }

    // ── UpdateGiftQuantityAsync ───────────────────────────────────────────

    [Fact]
    public async Task UpdateGiftQuantityAsync_ValidChange_InvalidatesCache()
    {
        _repositoryMock.Setup(r => r.GetGiftByIdAsync(1))
            .ReturnsAsync(new Gift { GiftId = 1, Quantity = 10 });
        _repositoryMock.Setup(r => r.UpdateGiftQuantityAsync(1, -2)).ReturnsAsync(true);

        var result = await _service.UpdateGiftQuantityAsync(1, -2);

        Assert.True(result);
        _cacheMock.Verify(c => c.RemoveAsync("gifts:1"), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync("gifts:all"), Times.Once);
    }

    // ── GetGiftsByPriceRangeAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetGiftsByPriceRangeAsync_ValidRange_ReturnsGifts()
    {
        _repositoryMock.Setup(r => r.GetGiftsByPriceRangeAsync(10m, 100m))
            .ReturnsAsync(new List<Gift>
            {
                new Gift { GiftId = 1, Name = "Book", Price = 50m, Quantity = 5 }
            });

        var result = (await _service.GetGiftsByPriceRangeAsync(10m, 100m)).ToList();

        Assert.Single(result);
    }

    [Fact]
    public async Task GetGiftsByPriceRangeAsync_InvalidRange_ReturnsEmpty()
    {
        var result = await _service.GetGiftsByPriceRangeAsync(500m, 10m);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetGiftsByPriceRangeAsync_NegativeMin_ReturnsEmpty()
    {
        var result = await _service.GetGiftsByPriceRangeAsync(-1m, 100m);
        Assert.Empty(result);
    }

    // ── GetFilteredGiftsAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetFilteredGiftsAsync_WithSearchTerm_FiltersResults()
    {
        _repositoryMock.Setup(r => r.GetFilteredGiftsAsync(null, null, null, null))
            .ReturnsAsync(new List<Gift>
            {
                new Gift { GiftId = 1, Name = "Gaming Laptop", Description = "Fast", Quantity = 1, Price = 999m },
                new Gift { GiftId = 2, Name = "Phone",         Description = "Mobile", Quantity = 1, Price = 499m }
            });

        var filter = new DtoGiftFilter { SearchTerm = "gaming" };
        var result = (await _service.GetFilteredGiftsAsync(filter)).ToList();

        Assert.Single(result);
        Assert.Equal("Gaming Laptop", result[0].Name);
    }

    [Fact]
    public async Task GetFilteredGiftsAsync_NoSearchTerm_ReturnsAll()
    {
        _repositoryMock.Setup(r => r.GetFilteredGiftsAsync(null, null, null, null))
            .ReturnsAsync(new List<Gift>
            {
                new Gift { GiftId = 1, Name = "A", Description = "", Quantity = 1, Price = 10m },
                new Gift { GiftId = 2, Name = "B", Description = "", Quantity = 1, Price = 20m }
            });

        var result = (await _service.GetFilteredGiftsAsync(new DtoGiftFilter())).ToList();

        Assert.Equal(2, result.Count);
    }
}
