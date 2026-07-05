using Xunit;
using Moq;
using LotteryService.Services;
using LotteryService.Interfaces;
using LotteryService.HttpClients;
using SharedModels.Models;
using SharedModels.Dtos;

namespace LotteryService.Tests;

public class LotteryDrawServiceTests
{
    private readonly Mock<ILotteryRepository> _repositoryMock;
    private readonly Mock<AuthServiceClient> _authClientMock;
    private readonly Mock<CatalogServiceClient> _catalogClientMock;
    private readonly Mock<ILogger<LotteryDrawService>> _loggerMock;
    private readonly LotteryDrawService _service;

    public LotteryDrawServiceTests()
    {
        _repositoryMock = new Mock<ILotteryRepository>();
        _authClientMock = new Mock<AuthServiceClient>();
        _catalogClientMock = new Mock<CatalogServiceClient>();
        _loggerMock = new Mock<ILogger<LotteryDrawService>>();

        _service = new LotteryDrawService(
            _repositoryMock.Object,
            _authClientMock.Object,
            _catalogClientMock.Object,
            _loggerMock.Object);
    }

    // ── CreateLotteryTicketAsync ──────────────────────────────────────────

    [Fact]
    public async Task CreateLotteryTicketAsync_ValidUserAndGift_ReturnsSuccess()
    {
        _authClientMock.Setup(c => c.UserExistsAsync(1)).ReturnsAsync(true);
        _catalogClientMock.Setup(c => c.GetGiftAsync(1))
            .ReturnsAsync((true, 100m, 5, "Gift A"));
        _repositoryMock.Setup(r => r.CreateLotteryAsync(It.IsAny<Lottery>())).ReturnsAsync(10);

        var (success, ticketId, _) = await _service.CreateLotteryTicketAsync(1, 1);

        Assert.True(success);
        Assert.Equal(10, ticketId);
    }

    [Fact]
    public async Task CreateLotteryTicketAsync_UserNotFound_ReturnsFail()
    {
        _authClientMock.Setup(c => c.UserExistsAsync(99)).ReturnsAsync(false);

        var (success, _, message) = await _service.CreateLotteryTicketAsync(99, 1);

        Assert.False(success);
        Assert.NotEmpty(message);
    }

    [Fact]
    public async Task CreateLotteryTicketAsync_GiftNotFound_ReturnsFail()
    {
        _authClientMock.Setup(c => c.UserExistsAsync(1)).ReturnsAsync(true);
        _catalogClientMock.Setup(c => c.GetGiftAsync(99))
            .ReturnsAsync((false, 0m, 0, string.Empty));

        var (success, _, message) = await _service.CreateLotteryTicketAsync(1, 99);

        Assert.False(success);
        Assert.NotEmpty(message);
    }

    [Fact]
    public async Task CreateLotteryTicketAsync_SetsStatusToActive()
    {
        Lottery? captured = null;
        _authClientMock.Setup(c => c.UserExistsAsync(1)).ReturnsAsync(true);
        _catalogClientMock.Setup(c => c.GetGiftAsync(1))
            .ReturnsAsync((true, 50m, 3, "Gift"));
        _repositoryMock.Setup(r => r.CreateLotteryAsync(It.IsAny<Lottery>()))
            .Callback<Lottery>(l => captured = l)
            .ReturnsAsync(1);

        await _service.CreateLotteryTicketAsync(1, 1);

        Assert.NotNull(captured);
        Assert.Equal("active", captured!.Status);
    }

    // ── RunLotteryAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task RunLotteryAsync_NoActiveTickets_ReturnsFail()
    {
        _repositoryMock.Setup(r => r.GetLotteryByStatusAsync("active"))
            .ReturnsAsync(new List<Lottery>());

        var (success, _, _) = await _service.RunLotteryAsync();

        Assert.False(success);
    }

    [Fact]
    public async Task RunLotteryAsync_WithActiveTickets_ReturnsSuccess()
    {
        _repositoryMock.Setup(r => r.GetLotteryByStatusAsync("active"))
            .ReturnsAsync(new List<Lottery>
            {
                new Lottery { LotteryId = 1, UserId = 1, GiftId = 1, Status = "active" },
                new Lottery { LotteryId = 2, UserId = 2, GiftId = 1, Status = "active" }
            });
        _repositoryMock.Setup(r => r.UpdateLotteryStatusAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.MarkAsWonAsync(It.IsAny<int>())).ReturnsAsync(true);

        var (success, winnerCount, _) = await _service.RunLotteryAsync();

        Assert.True(success);
        Assert.Equal(1, winnerCount); // one winner per gift
    }

    [Fact]
    public async Task RunLotteryAsync_SelectsExactlyOneWinnerPerGift()
    {
        _repositoryMock.Setup(r => r.GetLotteryByStatusAsync("active"))
            .ReturnsAsync(new List<Lottery>
            {
                new Lottery { LotteryId = 1, UserId = 1, GiftId = 1, Status = "active" },
                new Lottery { LotteryId = 2, UserId = 2, GiftId = 1, Status = "active" },
                new Lottery { LotteryId = 3, UserId = 3, GiftId = 2, Status = "active" }
            });
        _repositoryMock.Setup(r => r.UpdateLotteryStatusAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _repositoryMock.Setup(r => r.MarkAsWonAsync(It.IsAny<int>())).ReturnsAsync(true);

        var (success, winnerCount, _) = await _service.RunLotteryAsync();

        Assert.True(success);
        Assert.Equal(2, winnerCount); // one winner per gift (gift 1 and gift 2)
    }

    // ── GetWinnersAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetWinnersAsync_ReturnsEnrichedWinners()
    {
        _repositoryMock.Setup(r => r.GetWinnersAsync())
            .ReturnsAsync(new List<Lottery>
            {
                new Lottery { LotteryId = 1, UserId = 1, GiftId = 1, WonAt = DateTime.UtcNow }
            });
        _authClientMock.Setup(c => c.GetUserAsync(1))
            .ReturnsAsync((true, "winner@example.com", "John", "Doe"));
        _catalogClientMock.Setup(c => c.GetGiftAsync(1))
            .ReturnsAsync((true, 100m, 1, "Prize Gift"));

        var result = (await _service.GetWinnersAsync()).ToList();

        Assert.Single(result);
        Assert.Equal("winner@example.com", result[0].UserEmail);
        Assert.Equal("Prize Gift", result[0].GiftName);
    }

    // ── GetUserLotteryTicketsAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetUserLotteryTicketsAsync_ReturnsUserTickets()
    {
        _repositoryMock.Setup(r => r.GetLotteryByUserIdAsync(1))
            .ReturnsAsync(new List<Lottery>
            {
                new Lottery { LotteryId = 1, UserId = 1, GiftId = 1, Status = "active" },
                new Lottery { LotteryId = 2, UserId = 1, GiftId = 2, Status = "won" }
            });

        var result = (await _service.GetUserLotteryTicketsAsync(1)).ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetUserLotteryTicketsAsync_NoTickets_ReturnsEmpty()
    {
        _repositoryMock.Setup(r => r.GetLotteryByUserIdAsync(99))
            .ReturnsAsync(new List<Lottery>());

        var result = await _service.GetUserLotteryTicketsAsync(99);

        Assert.Empty(result);
    }

    // ── GetLotteryStatisticsAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetLotteryStatisticsAsync_ReturnsCorrectCounts()
    {
        _repositoryMock.Setup(r => r.GetTotalTicketsAsync()).ReturnsAsync(10);
        _repositoryMock.Setup(r => r.GetWinningTicketsCountAsync()).ReturnsAsync(3);
        _repositoryMock.Setup(r => r.GetWinnersAsync()).ReturnsAsync(new List<Lottery>());

        var stats = await _service.GetLotteryStatisticsAsync();

        Assert.Equal(10, stats.TotalTickets);
        Assert.Equal(3, stats.WinningTickets);
        Assert.Equal(7, stats.LosingTickets);
    }
}
