using BookingApi.Entities;

namespace BookingApi.Dtos;

public class BookingResponse
{
    public Guid Id { get; set; }
    public Guid SlotId { get; set; }
    public string RenterEmail { get; set; } = "";
    public int PartySize { get; set; }
    public BookingStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
