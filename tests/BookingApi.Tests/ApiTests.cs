using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BookingApi.Tests;

// These tests call the real API and the SQL Server container (docker compose up -d).
public class ApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private record SlotDto(Guid Id, Guid PropertyId, int Capacity, int SeatsRemaining);
    private record BookingDto(Guid Id, string Status);
    private record HistoryDto(string? FromStatus, string ToStatus, string Actor, string? Reason);

    private async Task<SlotDto> CreateSlot(Guid propertyId, int capacity = 5)
    {
        var response = await _client.PostAsJsonAsync("/slots", new
        {
            propertyId,
            startsAt = DateTimeOffset.UtcNow.AddDays(1),
            durationMinutes = 30,
            capacity
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SlotDto>(Json))!;
    }

    private Task<HttpResponseMessage> Book(Guid slotId, string email, int partySize = 1) =>
        _client.PostAsJsonAsync($"/slots/{slotId}/bookings", new { renterEmail = email, partySize });

    private async Task<SlotDto> GetSlot(Guid propertyId) =>
        (await _client.GetFromJsonAsync<List<SlotDto>>($"/slots?propertyId={propertyId}", Json))!.Single();

    [Fact]
    public async Task CreateSlot_InvalidCapacity_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/slots", new
        {
            propertyId = Guid.NewGuid(),
            startsAt = DateTimeOffset.UtcNow.AddDays(1),
            durationMinutes = 30,
            capacity = 21
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateSlot_StartInThePast_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/slots", new
        {
            propertyId = Guid.NewGuid(),
            startsAt = DateTimeOffset.UtcNow.AddDays(-1),
            durationMinutes = 30,
            capacity = 5
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetSlots_PageSizeOver100_Returns400()
    {
        var response = await _client.GetAsync("/slots?pageSize=101");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Book_UnknownSlot_Returns404()
    {
        var response = await Book(Guid.NewGuid(), "a@example.com");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Book_MoreThanRemainingSeats_Returns409()
    {
        var slot = await CreateSlot(Guid.NewGuid(), capacity: 3);

        Assert.Equal(HttpStatusCode.Created, (await Book(slot.Id, "a@example.com", 2)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Book(slot.Id, "b@example.com", 2)).StatusCode);
    }

    [Fact]
    public async Task Book_SameRenterTwice_Returns409()
    {
        var slot = await CreateSlot(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Created, (await Book(slot.Id, "a@example.com")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Book(slot.Id, "A@example.com")).StatusCode);
    }

    [Fact]
    public async Task CancelBooking_FreesSeats_AndRecordsHistory()
    {
        var propertyId = Guid.NewGuid();
        var slot = await CreateSlot(propertyId, capacity: 4);
        var booking = (await (await Book(slot.Id, "a@example.com", 3)).Content.ReadFromJsonAsync<BookingDto>(Json))!;
        Assert.Equal(1, (await GetSlot(propertyId)).SeatsRemaining);

        var cancel = await _client.PostAsJsonAsync($"/bookings/{booking.Id}/transitions",
            new { to = "Cancelled", actor = "renter", reason = "Change of plans" });

        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        Assert.Equal(4, (await GetSlot(propertyId)).SeatsRemaining);

        var history = (await _client.GetFromJsonAsync<List<HistoryDto>>($"/bookings/{booking.Id}/history", Json))!;
        Assert.Equal(2, history.Count);
        Assert.Equal("Cancelled", history[1].ToStatus);
        Assert.Equal("renter", history[1].Actor.ToLowerInvariant());
        Assert.Equal("Change of plans", history[1].Reason);
    }

    [Fact]
    public async Task Transition_CancelledBookingAgain_Returns409()
    {
        var slot = await CreateSlot(Guid.NewGuid());
        var booking = (await (await Book(slot.Id, "a@example.com")).Content.ReadFromJsonAsync<BookingDto>(Json))!;
        var body = new { to = "Cancelled", actor = "owner", reason = "x" };

        await _client.PostAsJsonAsync($"/bookings/{booking.Id}/transitions", body);
        var second = await _client.PostAsJsonAsync($"/bookings/{booking.Id}/transitions", body);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Transition_InvalidActor_Returns400()
    {
        var response = await _client.PostAsJsonAsync($"/bookings/{Guid.NewGuid()}/transitions",
            new { to = "Cancelled", actor = "admin" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task History_UnknownBooking_Returns404()
    {
        var response = await _client.GetAsync($"/bookings/{Guid.NewGuid()}/history");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ConcurrentBookings_NeverOverbookTheSlot()
    {
        var propertyId = Guid.NewGuid();
        var slot = await CreateSlot(propertyId, capacity: 5);

        var responses = await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(i => Book(slot.Id, $"renter{i}@example.com")));

        Assert.Equal(5, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(15, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        Assert.Equal(0, (await GetSlot(propertyId)).SeatsRemaining);
    }
}
