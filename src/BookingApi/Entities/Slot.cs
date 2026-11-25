namespace BookingApi.Entities;

public class Slot
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public int DurationMinutes { get; set; }
    public int Capacity { get; set; }
    public int BookedSeats { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
