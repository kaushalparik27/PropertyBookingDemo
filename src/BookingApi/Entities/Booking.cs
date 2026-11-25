namespace BookingApi.Entities;

public class Booking
{
    public Guid Id { get; set; }
    public Guid SlotId { get; set; }
    public Slot Slot { get; set; } = null!;
    public string RenterEmail { get; set; } = "";
    public int PartySize { get; set; }
    public BookingStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
