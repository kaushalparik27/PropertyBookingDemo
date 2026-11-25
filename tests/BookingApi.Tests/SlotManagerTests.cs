using BookingApi.Dtos;
using BookingApi.Entities;
using BookingApi.Exceptions;
using BookingApi.Managers;
using BookingApi.Repositories;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace BookingApi.Tests;

public class SlotManagerTests
{
    private static readonly DateTimeOffset Now = new(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<ISlotRepository> _slots = new();
    private readonly SlotManager _manager;

    public SlotManagerTests()
    {
        _manager = new SlotManager(_slots.Object, new FakeTimeProvider(Now));
    }

    [Fact]
    public async Task Create_StartInThePast_ThrowsBadRequest()
    {
        var request = new CreateSlotRequest
        {
            PropertyId = Guid.NewGuid(),
            StartsAt = Now.AddMinutes(-1),
            DurationMinutes = 30,
            Capacity = 5
        };

        await Assert.ThrowsAsync<BadRequestException>(() => _manager.CreateAsync(request));
    }

    [Fact]
    public async Task Create_Valid_ReturnsSlotWithAllSeatsFree()
    {
        var request = new CreateSlotRequest
        {
            PropertyId = Guid.NewGuid(),
            StartsAt = Now.AddDays(1),
            DurationMinutes = 30,
            Capacity = 5
        };

        var result = await _manager.CreateAsync(request);

        Assert.Equal(5, result.SeatsRemaining);
        _slots.Verify(r => r.AddAsync(It.IsAny<Slot>()), Times.Once);
    }

    [Fact]
    public async Task Get_ReportsSeatsRemaining()
    {
        var slot = new Slot { Id = Guid.NewGuid(), Capacity = 10, BookedSeats = 7 };
        _slots.Setup(r => r.ListAsync(It.IsAny<SlotFilter>())).ReturnsAsync([slot]);

        var result = await _manager.GetAsync(new SlotFilter());

        Assert.Equal(3, Assert.Single(result).SeatsRemaining);
    }
}
