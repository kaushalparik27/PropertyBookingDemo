using BookingApi.Dtos;
using BookingApi.Entities;
using BookingApi.Exceptions;
using BookingApi.Managers;
using BookingApi.Repositories;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace BookingApi.Tests;

public class BookingManagerTests
{
    private static readonly DateTimeOffset Now = new(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IBookingRepository> _bookings = new();
    private readonly FakeTimeProvider _clock = new(Now);
    private readonly BookingManager _manager;

    public BookingManagerTests()
    {
        _manager = new BookingManager(_bookings.Object, _clock);
    }

    private static Slot SlotStartingIn(TimeSpan offset) => new()
    {
        Id = Guid.NewGuid(),
        StartsAt = Now + offset,
        Capacity = 5
    };

    private void GivenSlot(Slot slot) =>
        _bookings.Setup(r => r.GetSlotByIdAsync(slot.Id)).ReturnsAsync(slot);

    private Booking GivenBooking(BookingStatus status, TimeSpan slotStartsIn)
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            Slot = SlotStartingIn(slotStartsIn),
            PartySize = 2,
            Status = status
        };
        _bookings.Setup(r => r.GetByIdAsync(booking.Id)).ReturnsAsync(booking);
        return booking;
    }

    private static readonly CreateBookingRequest BookRequest = new() { RenterEmail = "Alice@Example.com", PartySize = 2 };

    // ---- Booking ----

    [Fact]
    public async Task Book_UnknownSlot_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _manager.BookAsync(Guid.NewGuid(), BookRequest));
    }

    [Fact]
    public async Task Book_SlotAlreadyStarted_ThrowsConflict()
    {
        var slot = SlotStartingIn(TimeSpan.FromMinutes(-1));
        GivenSlot(slot);

        await Assert.ThrowsAsync<ConflictException>(() => _manager.BookAsync(slot.Id, BookRequest));
    }

    [Fact]
    public async Task Book_RenterAlreadyHasActiveBooking_ThrowsConflict()
    {
        var slot = SlotStartingIn(TimeSpan.FromDays(1));
        GivenSlot(slot);
        _bookings.Setup(r => r.HasActiveBookingAsync(slot.Id, "alice@example.com")).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _manager.BookAsync(slot.Id, BookRequest));
    }

    [Fact]
    public async Task Book_NotEnoughSeats_ThrowsConflict()
    {
        var slot = SlotStartingIn(TimeSpan.FromDays(1));
        GivenSlot(slot);
        _bookings.Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<BookingStateChange>())).ReturnsAsync(false);

        await Assert.ThrowsAsync<ConflictException>(() => _manager.BookAsync(slot.Id, BookRequest));
    }

    [Fact]
    public async Task Book_Valid_ReturnsConfirmedBookingWithNormalisedEmail()
    {
        var slot = SlotStartingIn(TimeSpan.FromDays(1));
        GivenSlot(slot);
        _bookings.Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<BookingStateChange>())).ReturnsAsync(true);

        var result = await _manager.BookAsync(slot.Id, BookRequest);

        Assert.Equal(BookingStatus.Confirmed, result.Status);
        Assert.Equal("alice@example.com", result.RenterEmail);
        Assert.Equal(2, result.PartySize);
    }

    // ---- Transitions that are allowed ----

    [Theory]
    [InlineData(Actor.Renter)]
    [InlineData(Actor.Owner)]
    public async Task Cancel_BeforeSlotStarts_Succeeds_AndReleasesSeats(Actor actor)
    {
        var booking = GivenBooking(BookingStatus.Confirmed, TimeSpan.FromHours(2));

        var result = await _manager.TransitionAsync(booking.Id, Transition(BookingStatus.Cancelled, actor));

        Assert.Equal(BookingStatus.Cancelled, result.Status);
        _bookings.Verify(r => r.SaveTransitionAsync(booking, It.IsAny<BookingStateChange>(), true), Times.Once);
    }

    [Theory]
    [InlineData(BookingStatus.Attended)]
    [InlineData(BookingStatus.NoShow)]
    public async Task AttendedOrNoShow_ByOwnerAfterSlotStarts_Succeeds_WithoutReleasingSeats(BookingStatus to)
    {
        var booking = GivenBooking(BookingStatus.Confirmed, TimeSpan.FromHours(-1));

        var result = await _manager.TransitionAsync(booking.Id, Transition(to, Actor.Owner));

        Assert.Equal(to, result.Status);
        _bookings.Verify(r => r.SaveTransitionAsync(booking, It.IsAny<BookingStateChange>(), false), Times.Once);
    }

    // ---- Transitions that are rejected ----

    [Theory]
    [InlineData(BookingStatus.Cancelled, Actor.Renter)]
    [InlineData(BookingStatus.Cancelled, Actor.Owner)]
    public async Task Cancel_AfterSlotStarts_ThrowsConflict(BookingStatus to, Actor actor)
    {
        var booking = GivenBooking(BookingStatus.Confirmed, TimeSpan.FromMinutes(-1));

        await Assert.ThrowsAsync<ConflictException>(() => _manager.TransitionAsync(booking.Id, Transition(to, actor)));
    }

    [Theory]
    [InlineData(BookingStatus.Attended)]
    [InlineData(BookingStatus.NoShow)]
    public async Task AttendedOrNoShow_ByRenter_ThrowsConflict(BookingStatus to)
    {
        var booking = GivenBooking(BookingStatus.Confirmed, TimeSpan.FromHours(-1));

        await Assert.ThrowsAsync<ConflictException>(() => _manager.TransitionAsync(booking.Id, Transition(to, Actor.Renter)));
    }

    [Theory]
    [InlineData(BookingStatus.Attended)]
    [InlineData(BookingStatus.NoShow)]
    public async Task AttendedOrNoShow_BeforeSlotStarts_ThrowsConflict(BookingStatus to)
    {
        var booking = GivenBooking(BookingStatus.Confirmed, TimeSpan.FromHours(1));

        await Assert.ThrowsAsync<ConflictException>(() => _manager.TransitionAsync(booking.Id, Transition(to, Actor.Owner)));
    }

    [Theory]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Attended)]
    [InlineData(BookingStatus.NoShow)]
    public async Task FinalStates_CannotChange(BookingStatus current)
    {
        var booking = GivenBooking(current, TimeSpan.FromHours(-1));

        await Assert.ThrowsAsync<ConflictException>(() =>
            _manager.TransitionAsync(booking.Id, Transition(BookingStatus.Attended, Actor.Owner)));
    }

    [Fact]
    public async Task Transition_BackToConfirmed_ThrowsConflict()
    {
        var booking = GivenBooking(BookingStatus.Confirmed, TimeSpan.FromHours(1));

        await Assert.ThrowsAsync<ConflictException>(() =>
            _manager.TransitionAsync(booking.Id, Transition(BookingStatus.Confirmed, Actor.Owner)));
    }

    [Fact]
    public async Task Transition_UnknownBooking_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _manager.TransitionAsync(Guid.NewGuid(), Transition(BookingStatus.Cancelled, Actor.Owner)));
    }

    private static TransitionRequest Transition(BookingStatus to, Actor actor) =>
        new() { To = to, Actor = actor, Reason = "test" };
}
